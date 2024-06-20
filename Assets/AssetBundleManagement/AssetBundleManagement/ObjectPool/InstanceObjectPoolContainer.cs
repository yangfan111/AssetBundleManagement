using System;
using System.Collections.Generic;
using System.Text;
using Core.Utils;
using Utils.AssetManager;
using UnityEngine;

namespace AssetBundleManagement.ObjectPool
{
    public class InstanceObjectPoolContainer:ILoadOwner
    {

         static HashSet<InstanceObjectPoolContainer> s_PoolContainerSet = new HashSet<InstanceObjectPoolContainer>();

        public static HashSet<InstanceObjectPoolContainer> S_PoolContainerSet { get { return s_PoolContainerSet; } }
        private Dictionary<AssetInfo, InstanceObjectPool> m_InstancePools =
            new Dictionary<AssetInfo, InstanceObjectPool>(1024, AssetInfo.AssetInfoComparer.Instance);

        private HashSet<InstanceObjectPool> m_PoolLoadingList = new HashSet<InstanceObjectPool>();
        // 待卸载的对象池队列
        private HashSet<InstanceObjectPool> m_WaitDestroyObjectPoolList = new HashSet<InstanceObjectPool>();
   
        private ResourceLoadManager m_ResourceMgr;
        private Queue<InstanceInitializeTask> m_ExecuteInitializeTasks = new Queue<InstanceInitializeTask>();
        private GameObject m_ObjectPoolContainerRoot;// 对象池容器的根节点
        private string m_PoolName;
        private ILoadOwner m_RootOwner;
        internal ResourceRecorder Recorder;

        private LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.InstanceObjectPoolContainer");
        public ILoadOwner GetRootOwner()
        {
            return m_RootOwner;
        }
        GameObject GetObjectPoolRoot()
        {
            if(m_ObjectPoolContainerRoot == null)
            {
                m_ObjectPoolContainerRoot = new GameObject("UnityObjectPool_" + m_PoolName);
                m_ObjectPoolContainerRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                GameObject.DontDestroyOnLoad(m_ObjectPoolContainerRoot); //先写成不销毁，不然切场景的时候会导致InstanceObjectPool访问root空异常
                s_PoolContainerSet.Add(this);
            }
            return m_ObjectPoolContainerRoot;
        }
        public InstanceObjectPoolContainer(ILoadOwner rootOwner,ResourceLoadManager resourceLoadManager,string poolName)
        {
            m_RootOwner       = rootOwner;
            m_PoolName    = poolName;
            m_ResourceMgr = resourceLoadManager;
            m_ResourceMgr.Event.Register(ResourceEventType.LoadAssetOptionComplete,HandleLoadAssetOptionComplete);
            Recorder = m_ResourceMgr.Recorder;

            GetObjectPoolRoot();
        }

        private void HandleLoadAssetOptionComplete(ResourceEventArgs eventArgs)
        {
            LoadAssetOptionCompleteEventArgs loadAssetOptionCompleteEvent = eventArgs as LoadAssetOptionCompleteEventArgs;
            if (loadAssetOptionCompleteEvent.LoadOption.Owner == this)
            {
                loadAssetOptionCompleteEvent.Handled = true;
                ResourceProvider.DoHandleLoadAssetComplete(m_ResourceMgr.AssetCompletePoster,loadAssetOptionCompleteEvent, null);
            }
        }

        public void Dispose()
        {
            m_ResourceMgr.Event.UnRegister(ResourceEventType.LoadAssetOptionComplete,HandleLoadAssetOptionComplete);

        }
        
        public void Update()
        {
            try
            {
                ExecuteInitializeTask();
                UpdateWaitDestroyObjectPool();
            }
            catch (Exception e)
            {
                s_logger.ErrorFormat("err:{0},stack:{1}", e.Message, e.StackTrace);
            }
        
        }
        private List<InstanceObjectPool> m_RemoveWaitDestroyObjectPoolList = new List<InstanceObjectPool>();//temp
        private List<InstanceObjectPool> m_ExecuteDestroyObjectPoolList = new List<InstanceObjectPool>();//temp
        private void UpdateWaitDestroyObjectPool()
        {
            m_RemoveWaitDestroyObjectPoolList.Clear();
            m_ExecuteDestroyObjectPoolList.Clear();
            foreach (var instanceObjectPool in m_WaitDestroyObjectPoolList)
            {
                if (!instanceObjectPool.CheckCanInWaitDestroyList())
                {
                    m_RemoveWaitDestroyObjectPoolList.Add(instanceObjectPool);
                }
                else if (instanceObjectPool.CheckCanDestroy())
                {
                    m_ExecuteDestroyObjectPoolList.Add(instanceObjectPool);
                }
            }
            foreach (var instanceObjectPool in m_RemoveWaitDestroyObjectPoolList)
            {
                RemoveObjectPoolFromWaitDestroyList(instanceObjectPool);
            }

            foreach (var instanceObjectPool in m_ExecuteDestroyObjectPoolList)
            {
                instanceObjectPool.BattleDestroy();
                m_WaitDestroyObjectPoolList.Remove(instanceObjectPool);
            }
            
        }

        private void ExecuteInitializeTask()
        {
            while (m_ExecuteInitializeTasks.Count > 0)
            {
                if (m_ResourceMgr.Recorder.CanAssetInstantiate())
                {
                    var task = m_ExecuteInitializeTasks.Dequeue();
                    task.ExecuteAndFree(m_ResourceMgr);
                }
                else
                {
                    break;
                }

            }
        }
        public void AddObjectPoolToWaitDestroyList(InstanceObjectPool instanceObjectPool)
        {
            m_WaitDestroyObjectPoolList.Add(instanceObjectPool);
            instanceObjectPool.SetState(InstanceObjectPoolState.WaitDestroy);
            instanceObjectPool.WaitDestroyStartTimeS = Time.realtimeSinceStartup;
        }
        public void RemoveObjectPoolFromWaitDestroyList(InstanceObjectPool instanceObjectPool)
        {
            m_WaitDestroyObjectPoolList.Remove(instanceObjectPool);
            instanceObjectPool.SetState(InstanceObjectPoolState.Loaded);
       
        }

        public void ForceDestroyInstancePoolWithBundleName(string bundleName)
        {
            s_logger.InfoFormat("ForceDestroyInstancePoolWithBundleName {0} Begin",bundleName);
            List<InstanceObjectPool> list = new List<InstanceObjectPool>();
            foreach (var instancePool in m_InstancePools)
            {
                if (instancePool.Value.AssetInfo.BundleName.Equals(bundleName))
                {
                    list.Add(instancePool.Value);
                }
            }

            foreach (var item in list)
            {
                s_logger.InfoFormat("ForceDestroyInstancePoolItem {0} ",item.AssetInfo);
                item.BattleDestroy();
            }
            s_logger.InfoFormat("ForceDestroyInstancePoolWithBundleName {0} End",bundleName);
            
        }
        public void OnDestroyInstancPool(InstanceObjectPool instanceObjectPool)
        {
            m_InstancePools.Remove(instanceObjectPool.AssetInfo);
        }

        public void SetInstancePoolRetainTime(AssetInfo assetInfo,int retain)
        {
            InstanceObjectPool instanceObjectPool = null;
            if (m_InstancePools.TryGetValue(assetInfo, out instanceObjectPool))
            {
                instanceObjectPool.SetRetainTime(retain);   
            }
            else
            {
                s_logger.WarnFormat("{0} dont has instancePool yet",assetInfo);
            }
        }
        public void SetInstancePoolForbidenUnload(AssetInfo assetInfo,bool forbidUnload)
        {
            InstanceObjectPool instanceObjectPool = null;
            if (m_InstancePools.TryGetValue(assetInfo, out instanceObjectPool))
            {
                instanceObjectPool.SetForbidUnload(forbidUnload);   
            }
            else
            {
                s_logger.WarnFormat("{0} dont has instancePool yet",assetInfo);
            }
        }
        public void Instantiate(AssetInfo assetInfo,ref LoadResourceOption loadOption, Transform parent = null, ISortableJob job = null,bool isSync = false)
        {
            if(loadOption.Owner == null)
                AssetUtility.ThrowException("Owner should not be null");
            if (assetInfo.IsValid())
            {
                InstanceObjectPool instanceObjectPool = null;
                if (!m_InstancePools.TryGetValue(assetInfo, out instanceObjectPool))
                {
                    instanceObjectPool = new InstanceObjectPool(assetInfo, 
                        loadOption.Priority, 
                        m_ResourceMgr,
                        GetObjectPoolRoot(),
                        this, job,isSync);
                    m_InstancePools.Add(assetInfo, instanceObjectPool);
                }
                
                
                switch (instanceObjectPool.PoolState)
                {
                    case InstanceObjectPoolState.WaitAssetLoading:
                        var waitPendingTask = ResourceObjectHolder<InstanceInitializeTask>.Allocate();
                        waitPendingTask.Init(instanceObjectPool, parent, ref loadOption);
                        instanceObjectPool.m_WaitPendingInitializeTasks.Enqueue(waitPendingTask);
                        break;
                    case InstanceObjectPoolState.Failed:
                        m_ResourceMgr.OnLoadAssetComplete(new FailedObjectHandler(assetInfo), loadOption);
                        //loadOption.CallOnFailed(assetInfo);
                        break;
                    case InstanceObjectPoolState.Loaded:
                    case InstanceObjectPoolState.WaitDestroy:  // 等待销毁阶段进行资源加载，移出销毁队列
                        
                        RemoveObjectPoolFromWaitDestroyList(instanceObjectPool);
                        var handler = instanceObjectPool.GetInstanceObjectHandler(parent, loadOption,false);
                        if (handler != null) // 池里有handler 直接返回结果 不走资源加载
                        {
                            m_ResourceMgr.OnLoadAssetComplete(handler, loadOption);
                            //  loadOption.CallOnSuccess(handler, assetInfo);
                            return;
                        }
                        var directTask = ResourceObjectHolder<InstanceInitializeTask>.Allocate();
                        directTask.Init(instanceObjectPool, parent, ref loadOption);
                        m_ExecuteInitializeTasks.Enqueue(directTask);
                        break;
                    default:
                        AssetUtility.ThrowException(string.Format("{0} Should not exist",instanceObjectPool.PoolState));
                        break;

                }
            }
            else
            {
                m_ResourceMgr.OnLoadAssetComplete(new FailedObjectHandler(assetInfo), loadOption);
            }
        }




        public void EnqueueExecuteTask(InstanceInitializeTask task)
        {
            m_ExecuteInitializeTasks.Enqueue(task);
        }

        public void AddPoolLoadingList(InstanceObjectPool pool)
        {
            m_PoolLoadingList.Add(pool);
        }

        public void RemovePoolLoadingList(InstanceObjectPool pool)
        {
            m_PoolLoadingList.Remove(pool);
        }

        
        public void OnlyDestroyInstance()
        {
            if (m_ObjectPoolContainerRoot != null)
            {
                GameObject.Destroy(m_ObjectPoolContainerRoot);
                m_ObjectPoolContainerRoot = null;
            }
        }

        public void HallDestroyAll()
        {
            while (m_ExecuteInitializeTasks.Count > 0)
            {
                var task = m_ExecuteInitializeTasks.Dequeue();
                task.ExecuteAndFree(null);//此处需要释放task引用计数，不然对象池永远是持有状态
                //ResourceObjectHolder<InstanceInitializeTask>.Free(task);
            }
         //   m_WaitDestroyObjectPoolList.Clear();

            List<InstanceObjectPool> removeList = new List<InstanceObjectPool>();
            foreach (var instanceObjectPoolPair in m_InstancePools)
            {
                if (instanceObjectPoolPair.Value.HallDestroy(false))
                {
                    removeList.Add(instanceObjectPoolPair.Value);
                }
            }
            foreach (var re in removeList)
            {
                m_InstancePools.Remove(re.AssetInfo);
                m_WaitDestroyObjectPoolList.Remove(re);
            }

            // if (m_ObjectPoolContainerRoot != null)
            // {
            //     GameObject.Destroy(m_ObjectPoolContainerRoot);
            //     m_ObjectPoolContainerRoot = null;
            // }
            // S_PoolContainerSet.Remove(this);
        }

        public void DestroyAll()
        {
            while (m_ExecuteInitializeTasks.Count > 0)
            {
                var task = m_ExecuteInitializeTasks.Dequeue();
                ResourceObjectHolder<InstanceInitializeTask>.Free(task);
            }
            m_WaitDestroyObjectPoolList.Clear();
            foreach (var instanceObjectPoolPair in m_InstancePools)
            {
                instanceObjectPoolPair.Value.BattleDestroy(false);
            }
            m_InstancePools.Clear();
            if (m_ObjectPoolContainerRoot != null)
            {
                GameObject.Destroy(m_ObjectPoolContainerRoot);
                m_ObjectPoolContainerRoot = null;
            }
            S_PoolContainerSet.Remove(this);

        }


        #region Profile
        public void CollectProfileInfo(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("====>InstanceObjectPoolContainer pool count:{0}\n", m_InstancePools.Count);
            foreach (var poolPair in m_InstancePools)
            {
                stringBuilder.AppendFormat("    [InstanceObjectPool]{0}\n", poolPair.Value.AssetInfo.ToString());
                poolPair.Value.CollectProfileInfo(ref stringBuilder);
            }
        }

        public void CollectProfileInfo(List<ObjectPoolInspectorData> inspectorDatas)
        {
            inspectorDatas.Clear();
            foreach (var poolPair in m_InstancePools)
            {
                inspectorDatas.Add(new ObjectPoolInspectorData() { Name = string.Format("{0}_{1}", m_PoolName, poolPair.Key.ToString()), 
                    LoadState = poolPair.Value.PoolState, UsedCount = poolPair.Value.RefCount, 
                    PoolCount = poolPair.Value.BackupCount, WaitDestroyStart = poolPair.Value.WaitDestroyStartTimeS,
                    AssetInfo = poolPair.Value.AssetInfo });
            }
        }
        
   
        

        #endregion


        public void LoadCancel(object source)
        {
            foreach (var loadingPool in m_PoolLoadingList)
            {
                loadingPool.LoadCancel(source);
            }
            foreach (var task in m_ExecuteInitializeTasks)
            {
                task.LoadCancel(source);
            }
        }
    }
}
