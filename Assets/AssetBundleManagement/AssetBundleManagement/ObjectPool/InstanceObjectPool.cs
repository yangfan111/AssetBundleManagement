using Common;
using Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using Utils.AssetManager;

namespace AssetBundleManagement.ObjectPool
{
    public class InstanceObjectPool
    {
        private InstanceObjectPoolState m_PoolState = InstanceObjectPoolState.WaitAssetLoading;

        public InstanceObjectPoolState PoolState
        {
            get { return m_PoolState; }
        }
        public void SetState(InstanceObjectPoolState target)
        {
            if (m_PoolState == InstanceObjectPoolState.Disposed)
            {
                s_logger.Warn("InstanceObjectPool has been disposed,should not use again");
                return;
            }

            if (m_PoolState == InstanceObjectPoolState.WaitAssetLoading && (target != m_PoolState))
            {
                attachPoolContainer.RemovePoolLoadingList(this);
            }
            m_PoolState = target;
        }
        public AssetInfo AssetInfo;

        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.InstanceObjectPool");

        private InstanceObjectPoolContainer attachPoolContainer;
        public float WaitDestroyStartTimeS;

        private int m_RetainTime=0;

        private bool m_ForbidUnload = false;
        //   private bool IsSetWaitDestroyStartTimeS
        public void SetForbidUnload(bool forbidUnload)
        {
            if (m_ForbidUnload != forbidUnload)
            {
                m_ForbidUnload = forbidUnload;
                InspectDestroyState();
            }
        }

        public void SetRetainTime(int retainTime)
        {
            m_RetainTime = retainTime;
            InspectDestroyState();
        }

        public bool IsDisposed
        {
            get { return m_PoolState == InstanceObjectPoolState.Disposed; }
        }
        public bool IsUsableState
        {
            get
            {
                return m_PoolState == InstanceObjectPoolState.Loaded || m_PoolState == InstanceObjectPoolState.WaitDestroy;
            }
        }
        public int RefCount
        {
            get { return m_HandleList.Count; }
        }
        public int BackupCount
        {
            get { return m_InstanceObjects.Count; }
        }
        private int waitExecuteTaskCount;
        public void IncrWaitExecuteTask()
        {
            ++waitExecuteTaskCount;
            InspectDestroyState();

        }
        public void DecrWaitExecuteTask()
        {
            --waitExecuteTaskCount;
            InspectDestroyState();
        }

        bool ForbidUnload
        {
            get{ return !ResourceConfigure.SupportUnload || m_ForbidUnload;}
        }
     
        void InspectDestroyState()
        {
            if (IsDisposed)
            {
                attachPoolContainer.RemoveObjectPoolFromWaitDestroyList(this);
                return;

            }
            if (ForbidUnload)
            {
                if (m_PoolState == InstanceObjectPoolState.WaitDestroy)
                {
                    attachPoolContainer.RemoveObjectPoolFromWaitDestroyList(this);
                }
                return;
            }

            if (m_PoolState == InstanceObjectPoolState.WaitDestroy)
            {
                if (waitExecuteTaskCount > 0 || RefCount > 0)
                {
                    attachPoolContainer.RemoveObjectPoolFromWaitDestroyList(this);
                }
            }
            else if (m_PoolState == InstanceObjectPoolState.Loaded)
            {
                if (waitExecuteTaskCount == 0 && RefCount == 0)
                {
                    attachPoolContainer.AddObjectPoolToWaitDestroyList(this);
                }
            }
        }

       // private static int instanceHandlerId;
        private Queue<UnityEngine.Object> m_InstanceObjects = new Queue<UnityEngine.Object>();
        private HashSet<int> m_HandleList = new HashSet<int>();
        private IResourceHandler m_AssetTemplate;
        private readonly GameObject objectPoolContainerRoot;  // 对象池容器的根节点
        private GameObject objectPoolRoot;
        private bool isGameObjectType;   // 对象池持有的类型是否是 GameObject类型，对象池也支持放一些Shader Material实例化对象


        // Asset还没异步加载好，暂存task，等待加载好后CallBack加入队列
        public Queue<InstanceInitializeTask> m_WaitPendingInitializeTasks = new Queue<InstanceInitializeTask>();
        
        public InstanceObjectPool(AssetInfo assetInfo, LoadPriority loadPriority, ResourceLoadManager resourceLoadManager, GameObject objectPoolContainerRoot, InstanceObjectPoolContainer poolContainer, ISortableJob job,bool isSync)
        {
            this.objectPoolContainerRoot = objectPoolContainerRoot;
            attachPoolContainer = poolContainer;
        //    SetState();
            AssetInfo = assetInfo;

            CreateObjectPoolRootGameObject();
            LoadAssetTemple(loadPriority, resourceLoadManager, job,isSync);
            poolContainer.AddPoolLoadingList(this);
        }

        public void CollectProfileInfo(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("        PoolHold InstanceObjectHandler Count:{0}\n", m_InstanceObjects.Count);
            // foreach (var handler in m_InstanceObjects)
            // {
            //     handler.CollectProfileInfo(ref stringBuilder);
            // }
            stringBuilder.AppendFormat("        PoolOut InstanceObjectHandler Count:{0}\n", m_HandleList.Count);
            // foreach (var handler in m_OutPoolHandlers)
            // {
            //     handler.CollectProfileInfo(ref stringBuilder);
            // }
        }

        public IResourceHandler GetInstanceObjectHandler(Transform parent, LoadResourceOption resourceOption, bool forceCreate)
        {
         
            UnityEngine.Object target = null;
            while (m_InstanceObjects.Count > 0 && target == null)
            {

                target = m_InstanceObjects.Dequeue();
                if (target == null)
                    s_logger.ErrorFormat("instance Pool {0} has Null instance object", AssetInfo.ToString());
            }

            bool newCreate = false;
            if (target == null)
            {
                if (!forceCreate)
                    return null;
                newCreate = true;
                attachPoolContainer.Recorder.StartInstantiate();
                var obj = m_AssetTemplate.ResourceObject;
                if (obj != null)
                {
                    if (isGameObjectType)
                    {
                        target = UnityEngine.Object.Instantiate(obj, AssetUtility.ParentOrDefault(parent), false);
                        if(isGameObjectType)
                            AssetUtility.InitializeGameObjectAssetTemplate(target as GameObject, m_AssetTemplate.AssetKey);
                    }
                    else
                    {
                        target = UnityEngine.Object.Instantiate(obj);
                    }
                }
                else
                {
                    AssetObjectHandler assetObjectHandler = (AssetObjectHandler) m_AssetTemplate;
                    
                    s_logger.Error("instance Pool template asset dont exist,AssetInfo:"+assetObjectHandler.GetAssetResourceInfo());
                }
               
            }
            var newHandler = new InstanceObjectHandler(target, this,  AcquireHandleId());
            ResourceProvider.Profiler.AddInstanceObjectProfileDataNotify(ref newHandler, ref resourceOption);
            if(isGameObjectType && !newCreate)
                newHandler.AsGameObject.SetParentOrDefaultWithoutWorldPosStay(parent);
            if(newCreate)
                attachPoolContainer.Recorder.StopInstantiate();
            //attachPoolContainer.OnObjectPoolReferenceChangedCallback(this);
            return newHandler;

        }

        public void LoadCancel(object context)
        {
            foreach (var initializeTask in m_WaitPendingInitializeTasks)
            {
                initializeTask.LoadCancel(context);
            }
        }

        public void InstantDestroy()
        {
            try
            {
                SetState(InstanceObjectPoolState.Disposed);

                m_HandleList.Clear();
                while (m_WaitPendingInitializeTasks.Count > 0)
                {
                    var val = m_WaitPendingInitializeTasks.Dequeue();
                    ResourceObjectHolder<InstanceInitializeTask>.Free(val);
                }
                if(m_AssetTemplate != null)
                {
                    m_AssetTemplate.Release();
                    m_AssetTemplate = null;

                }
                if(objectPoolRoot)
                {
                    GameObject.Destroy(objectPoolRoot);
                    objectPoolRoot = null;
                }

                if (!isGameObjectType)
                {
                    while (m_InstanceObjects.Count > 0 )
                    {
                        var target = m_InstanceObjects.Dequeue();
                        if (target)
                        {
                            Object.Destroy(target);
                        }
                    }
                }
                m_InstanceObjects.Clear();

                if (m_HandleList.Count > 0) // 不应该出现这种情况
                {
                    s_logger.Error("Error, InstanceObjectPool Destroy, m_OutPoolHandlers is not null");
                }
            }
            catch (Exception e)
            {
                s_logger.ErrorFormat("err:{0},{1}", e.Message,e.StackTrace);
            }
         
        }


        public bool Release(InstanceObjectHandler instanceObjectHandler)
        {
            if (ReleasHandleId(instanceObjectHandler.GetHandleId()))
            {
                if (instanceObjectHandler.ResourceObject == null )  // 如果在应用层被销毁了 handler就不要回实例化对象池
                {
                    s_logger.ErrorFormat("{0} Release InstanceHandler, ResourceObject is Null", AssetInfo.ToString());
                    
                    //      attachPoolContainer.OnObjectPoolReferenceChangedCallback(this);
                    //  ResourceObjectHolder<InstanceObjectHandler>.Free(instanceObjectHandler);
                    return false;
                }

                if (objectPoolRoot == null)
                {
                    s_logger.ErrorFormat("{0} Release InstanceHandler, objectPoolRoot is Null", AssetInfo.ToString());
                    return false;
                }
            
                if (isGameObjectType)  // GameObject回到对象池根节点下
                {
                    var gameObject = instanceObjectHandler.AsGameObject;
                    gameObject.SetParentOrDefaultWithoutWorldPosStay(objectPoolRoot.transform);
                 //   TestCode(gameObject);
                }

                m_InstanceObjects.Enqueue(instanceObjectHandler.ResourceObject);
                //     attachPoolContainer.OnObjectPoolReferenceChangedCallback(this);

                return true;
            }

            return false;
        }

        private void TestCode(GameObject gameObject)
        {
//            gameObject.transform.position = new Vector3((m_InstanceObjects.Count - 1) * 0.3f, 0, 0);
        }


        private void LoadAssetTemple(LoadPriority loadPriority, ResourceLoadManager resourceLoadManager, ISortableJob job,bool isSync)
        {
            LoadResourceOption assetOption = new LoadResourceOption(OnAssetLoadComplete, loadPriority, this);
            assetOption.Owner = attachPoolContainer;
            if(job == null)
                resourceLoadManager.LoadAsset(this.AssetInfo,isSync, ref assetOption);
            else
                resourceLoadManager.LoadSortableAsset(this.AssetInfo, job, ref assetOption);
        }

        private void CreateObjectPoolRootGameObject()
        {

            objectPoolRoot = new GameObject("AO|" + AssetInfo.ToString());
            objectPoolRoot.SetParentOrDefaultWithoutWorldPosStay(objectPoolContainerRoot.transform);
            objectPoolRoot.SetActive(false);
            objectPoolRoot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }


  

        static int NextHandleId()
        {
            return AssetUtility.GenerateNewHandle();
        }

        private int AcquireHandleId()
        {
            var handlerId = NextHandleId();
            m_HandleList.Add(handlerId);
            InspectDestroyState();
            return handlerId;
        }
        private bool ReleasHandleId(int handleId)
        {
            if (m_HandleList.Remove(handleId))
            {
                InspectDestroyState();
                return true;
            }
            return false;
        }

        private void OnAssetLoadComplete(System.Object context, UnityObject assetAccessor)
        {
            if(IsDisposed) //已经被销毁：回收
            {
                assetAccessor.Release();
                return;
            }
            if (assetAccessor.IsValid())
            {
                SetState(InstanceObjectPoolState.Loaded);
                m_AssetTemplate  = assetAccessor.GetNewHandler();
                isGameObjectType = m_AssetTemplate.ResourceObject is GameObject;

                if(!isGameObjectType)
                {
                    s_logger.WarnFormat("{0} Asset {1} Load from InstaneceObjectPool,Instantiate may cause error",
           m_AssetTemplate.ResourceObject.GetType(), AssetInfo);
                }
                LoadAssetComplete();
            }
            else
            {
                SetState(InstanceObjectPoolState.Failed);
                m_AssetTemplate = null;
                s_logger.ErrorFormat("{0} LoadAssetFailed", AssetInfo.ToString());
             
            
                LoadAssetComplete();
            }
          

        }
        public void LoadAssetComplete()
        {
            while (m_WaitPendingInitializeTasks.Count > 0)
            {
                InstanceInitializeTask task = m_WaitPendingInitializeTasks.Dequeue();
                attachPoolContainer.EnqueueExecuteTask(task);
            }
        }

        #region 对象池销毁策略

        /// <summary>
        /// 直接销毁对象池， 正在使用的不卸载
        /// </summary>
        public bool HallDestroy(bool withContainerRemove = true)
        {
            if (RefCount > 0)
            {
                return false;
            }
           // PoolState = InstanceObjectPoolState.WaitDestroy;

            //    WaitPendingInitializeTasks.Clear();
            InstantDestroy();
            if (withContainerRemove)
                attachPoolContainer.OnDestroyInstancPool(this);
            return true;
        }

        /// <summary>
        /// 直接销毁对象池，不管在池外的InstanceHandler了
        /// </summary>
        public void BattleDestroy(bool withCallback=true)
        {
        //    PoolState = InstanceObjectPoolState.WaitDestroy;
           
          
        //    WaitPendingInitializeTasks.Clear();
            InstantDestroy();
            if(withCallback)
                attachPoolContainer.OnDestroyInstancPool(this);
        }
        
        public bool CheckCanDestroy()
        {
            if (ForbidUnload)
                return false;
            int retainTime = m_RetainTime > 0 ? m_RetainTime : ResourceConfigure.UnusedInstanceObjectPoolWaitTime;
            if (Time.realtimeSinceStartup - WaitDestroyStartTimeS > retainTime)
            {
                return true;
            }
            return false;
        }

        public bool CheckCanInWaitDestroyList()
        {
            if (ForbidUnload || IsDisposed)
                return false;
            return (m_PoolState == InstanceObjectPoolState.WaitDestroy) && waitExecuteTaskCount == 0 && RefCount == 0;

        }


        #endregion


    }
}