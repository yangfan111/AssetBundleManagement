using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetBundleManagement.ObjectPool
{
    public class InstanceObjectPool
    {
        public InstanceObjectPoolState PoolState;
        public AssetInfo AssetInfo;
        public Action<InstanceObjectPool> OnAssetLoadComplete;
        public Action<InstanceObjectPool> OnObjectPoolReferenceChangedNotify;
        public Action<InstanceObjectPool> OnObjectPoolDestroyNotify;
        // 对象池的销毁策略
        private AbstractInstanceObjectPoolDestroyStrategy DestroyStrategy { get; set; }
        public bool InWaitDestroyList
        {
            get { return DestroyStrategy.InWaitDestroyList; }
            set { DestroyStrategy.InWaitDestroyList = value; }
        }
        
        public bool IsDisposed { get; private set; }
        
        public int RefCount
        {
            get { return m_OutPoolHandlers.Count; }
        }

        private int instanceHandlerId;
        private Queue<InstanceObjectHandler> m_InstanceObjects = new Queue<InstanceObjectHandler>();
        private HashSet<InstanceObjectHandler> m_OutPoolHandlers = new HashSet<InstanceObjectHandler>(); // 从对象池出去的，并且应用层持有的Handler
        private AssetObjectHandler assetTemplate;
        private GameObject objectPoolContainerRoot;  // 对象池容器的根节点
        private GameObject objectPoolRoot;
        private bool isGameObjectType;   // 对象池持有的类型是否是 GameObject类型，对象池也支持放一些Shader Material实例化对象

        
        // Asset还没异步加载好，暂存task，等待加载好后CallBack加入队列
        public Queue<InstanceInitializeTask> WaitPendingInitializeTasks = new Queue<InstanceInitializeTask>();
        public InstanceObjectPool(AssetInfo assetInfo, LoadPriority loadPriority, ResourceLoadManager resourceLoadManager, 
            AbstractInstanceObjectPoolDestroyStrategy strategy, GameObject objectPoolContainerRoot, Action<InstanceObjectPool> OnAssetLoadComplete,
            Action<InstanceObjectPool> OnObjectPoolReferenceChangedNotify,Action<InstanceObjectPool> OnObjectPoolDestroyNotify)
        {
            this.objectPoolContainerRoot = objectPoolContainerRoot;
            this.OnAssetLoadComplete += OnAssetLoadComplete;
            this.OnObjectPoolReferenceChangedNotify += OnObjectPoolReferenceChangedNotify;
            this.OnObjectPoolDestroyNotify += OnObjectPoolDestroyNotify;
            PoolState = InstanceObjectPoolState.WaitAssetLoading;
            AssetInfo = assetInfo;

            CreateObjectPoolRootGameObject();
            LoadAssetTemple(loadPriority, resourceLoadManager);
            SwitchDestroyStrategy(strategy);
        }

        public void CollectProfileInfo(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("        PoolHold InstanceObjectHandler Count:{0}\n", m_InstanceObjects.Count);
            // foreach (var handler in m_InstanceObjects)
            // {
            //     handler.CollectProfileInfo(ref stringBuilder);
            // }
            stringBuilder.AppendFormat("        PoolOut InstanceObjectHandler Count:{0}\n", m_OutPoolHandlers.Count);
            foreach (var handler in m_OutPoolHandlers)
            {
                handler.CollectProfileInfo(ref stringBuilder);
            }
        }

        public InstanceObjectHandler GetInstanceObjectHandler(Transform parent, LoadResourceOption resourceOption)
        {
            if (PoolState == InstanceObjectPoolState.Loaded)
            {
                if (m_InstanceObjects.Count > 0)
                {
                    var handler = m_InstanceObjects.Dequeue();
                    ReInitInstanceObjectHandler(parent, resourceOption, handler);
                    return handler;
                }
                return InitializeNewInstanceObjectHandler(parent, resourceOption);
            }

            return null;
        }

        public void InstantDestroy()
        {
            assetTemplate.Release();
            GameObject.Destroy(objectPoolRoot);
            foreach (var handler in m_InstanceObjects)
            {
                ResourceObjectHolder<InstanceObjectHandler>.Free(handler);
            }

            if (m_OutPoolHandlers.Count > 0) // 不应该出现这种情况
            {
                AssetLogger.LogToFile("Error, InstanceObjectPool Destroy, m_OutPoolHandlers is not null", AssetInfo.ToString());
            }
        }
        
        
        public bool Release(InstanceObjectHandler instanceObjectHandler)
        {
            if (m_OutPoolHandlers.Contains(instanceObjectHandler))
            {
                m_OutPoolHandlers.Remove(instanceObjectHandler);
                if (instanceObjectHandler.ResourceObject == null)  // 如果在应用层被销毁了 handler就不要回实例化对象池
                {
                    OnObjectPoolReferenceChangedNotify(this);
                    AssetLogger.LogToFile("{0} Release InstanceHandler, ResourceObject is Null", AssetInfo.ToString());
                    ResourceObjectHolder<InstanceObjectHandler>.Free(instanceObjectHandler);
                    return true;
                }
                
                
                if (isGameObjectType)  // GameObject回到对象池根节点下
                {
                    var gameObject = instanceObjectHandler.AsGameObject;
                    gameObject.transform.SetParent(objectPoolRoot.transform);
                    TestCode(gameObject);
                }
                
                m_InstanceObjects.Enqueue(instanceObjectHandler);
                OnObjectPoolReferenceChangedNotify(this);
                
                return true;
            }

            return false;
        }

        // TODO:测试代码，待删除
        private void TestCode(GameObject gameObject)
        {
            gameObject.transform.position = new Vector3((m_InstanceObjects.Count - 1) * 0.3f, 0, 0);
        }
        


        public void SwitchDestroyStrategy(AbstractInstanceObjectPoolDestroyStrategy strategy)
        {
            DestroyStrategy = strategy;
            DestroyStrategy.InstanceObjectPool = this;
        }

        public void Destroy()
        {
            DestroyStrategy.Destroy();
        }
        
        public bool CheckCanDestroy()
        {
            return DestroyStrategy.CheckCanDestroy();
        }
        
        public bool CheckCanInWaitDestroyList()
        {
            return DestroyStrategy.CheckCanInWaitDestroyList();
        }
        
        
        /// <summary>
        /// 出池的时候重新初始化一下
        /// </summary>
        private void ReInitInstanceObjectHandler(Transform parent, LoadResourceOption resourceOption, InstanceObjectHandler handler)
        {
            handler.RefreshHandlerId(AcquireHandleId());
            SetObjectParent(handler, parent);
            m_OutPoolHandlers.Add(handler);
            handler.ResourceOption = resourceOption;
        }
        
        
        private void LoadAssetTemple(LoadPriority loadPriority, ResourceLoadManager resourceLoadManager)
        {
            LoadResourceOption assetOption = new LoadResourceOption(OnAssetLoadSuccess, OnAssetLoadFailed,
                ResourceHandlerType.Asset, loadPriority, this);
            resourceLoadManager.LoadAsset(this.AssetInfo, ref assetOption);
        }
        
        private void CreateObjectPoolRootGameObject()
        {
            Transform[] children = objectPoolContainerRoot.GetComponentsInChildren<Transform>(true);
            
            bool hasChild = false;
            foreach (Transform child in children)
            {
                if (child.name == AssetInfo.ToString())
                {
                    hasChild = true;
                    break;
                }
            }

            if (!hasChild)
            {
                objectPoolRoot = new GameObject(AssetInfo.ToString());
                objectPoolRoot.transform.SetParent(objectPoolContainerRoot.transform);
            }
        }
        
        
        private void SetObjectParent(InstanceObjectHandler instanceObjectHandler, Transform parent)
        {
            var gameObject = instanceObjectHandler.AsGameObject;
            if (gameObject!=null)
            {
                gameObject.transform.SetParent(parent);
            }
        }

        private InstanceObjectHandler InitializeNewInstanceObjectHandler(Transform parent, LoadResourceOption resourceOption)
        {
            var obj = UnityEngine.Object.Instantiate(assetTemplate.ResourceObject);
            var newInstanceObjectHandler = InstanceObjectHandler.Alloc(obj, this);
            ReInitInstanceObjectHandler(parent, resourceOption, newInstanceObjectHandler);
            return newInstanceObjectHandler;
        }
        
        private int GenerateInstanceHandlerId()
        {
            return ++instanceHandlerId;
        }

        private int AcquireHandleId()
        {
            var handlerId = GenerateInstanceHandlerId();
            OnObjectPoolReferenceChangedNotify(this);
            return handlerId;
        }
        
        private void OnAssetLoadSuccess(ResourceHandler assetAccessor)
        {
            PoolState = InstanceObjectPoolState.Loaded;
            assetTemplate = assetAccessor as AssetObjectHandler;
            isGameObjectType = assetAccessor.ResourceObject is GameObject;
            OnAssetLoadComplete(this);

        }
        private void OnAssetLoadFailed(string errorMessage, object context)
        {
            AssetLogger.LogToFile("{0} LoadAssetFailed", AssetInfo.ToString());
            PoolState = InstanceObjectPoolState.Failed;
            WaitPendingInitializeTasks.Clear();
            OnAssetLoadComplete(this);
        }
    }
}