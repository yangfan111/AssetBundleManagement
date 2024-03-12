using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AssetBundleManagement.ObjectPool
{
    public class InstanceObjectPoolContainer
    {
        private Dictionary<AssetInfo, InstanceObjectPool> m_InstancePools =
            new Dictionary<AssetInfo, InstanceObjectPool>(1024, AssetInfo.AssetInfoComparer.Instance);

        // 待卸载的对象池队列
        private HashSet<InstanceObjectPool> m_WaitDestroyObjectPoolList = new HashSet<InstanceObjectPool>();
        private List<InstanceObjectPool> m_RemoveWaitDestroyObjectPoolList = new List<InstanceObjectPool>();
        private List<InstanceObjectPool> m_ExecuteDestroyObjectPoolList = new List<InstanceObjectPool>();
        private ResourceLoadManager m_ResourceMgr;
        private Queue<InstanceInitializeTask> m_ExecuteInitializeTasks = new Queue<InstanceInitializeTask>();
        private Queue<InstanceInitializeTask> m_PendingInitializeTasks = new Queue<InstanceInitializeTask>();
        private bool IsExecutting;  // 是否正在执行InstanceInitializeTask
        private GameObject ObjectPoolContainerRoot { get; set; }  // 对象池容器的根节点
        
        public InstanceObjectPoolContainer(ResourceLoadManager resourceLoadManager)
        {
            m_ResourceMgr = resourceLoadManager;
            ObjectPoolContainerRoot = GameObject.Find("UnityObjectPool");  // todo： 这里临时这么写，最好改成构造函数传递过来
        }

        public void CollectProfileInfo(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("====>InstanceObjectPoolContainer pool count:{0}\n", m_InstancePools.Count);
            foreach (var poolPair in m_InstancePools)
            {
                stringBuilder.AppendFormat("    [InstanceObjectPool]{0}\n", poolPair.Value.AssetInfo.ToString());
                poolPair.Value.CollectProfileInfo(ref stringBuilder);
            }
        }


        public void Update()
        {
            ExecuteInitializeTask();
            UpdateWaitDestroyObjectPool();
        }

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
                instanceObjectPool.InWaitDestroyList = false;
                m_WaitDestroyObjectPoolList.Remove(instanceObjectPool);
            }

            foreach (var instanceObjectPool in m_ExecuteDestroyObjectPoolList)
            {
                instanceObjectPool.Destroy();
                m_WaitDestroyObjectPoolList.Remove(instanceObjectPool);
            }
            
        }

        private void ExecuteInitializeTask()
        {
            IsExecutting = true;
            
            while (m_ExecuteInitializeTasks.Count > 0)
            {
                var task = m_ExecuteInitializeTasks.Dequeue();
                task.Execute();
                ResourceObjectHolder<InstanceInitializeTask>.Free(task);
            }
            
            IsExecutting = false;

            while (m_PendingInitializeTasks.Count>0)
            {
                var task = m_PendingInitializeTasks.Dequeue();
                m_ExecuteInitializeTasks.Enqueue(task);
            }
        }

        private void OnObjectPoolReferenceChangedCallback(InstanceObjectPool instanceObjectPool)
        {
            if (instanceObjectPool.CheckCanInWaitDestroyList())
            {
                if (!instanceObjectPool.InWaitDestroyList)
                {
                    m_WaitDestroyObjectPoolList.Add(instanceObjectPool);
                    instanceObjectPool.InWaitDestroyList = true;
                }
            }
            else
            {
                if (instanceObjectPool.InWaitDestroyList)
                {
                    instanceObjectPool.InWaitDestroyList = false;
                    m_WaitDestroyObjectPoolList.Remove(instanceObjectPool);
                }
            }
        }
        
        private void OnObjectPoolDestroyCallback(InstanceObjectPool instanceObjectPool)
        {
            m_InstancePools.Remove(instanceObjectPool.AssetInfo);
        }
        
        
        public void InstantiateAsync(AssetInfo assetInfo,ref LoadResourceOption loadOption, Transform parent = null)
        {
            if (assetInfo.IsValid())
            {
                InstanceObjectPool instanceObjectPool = null;
                if (!m_InstancePools.TryGetValue(assetInfo, out instanceObjectPool))
                {
                    AbstractInstanceObjectPoolDestroyStrategy strategy;
                    if (ResourceConfigure.SupportUnload)
                        strategy = new TimeDelayObjectPoolDestroyStrategy(3); //todo：时间改成配置
                    else
                        strategy = new PermanentObjectPoolDestroyStrategy();

                    
                    instanceObjectPool = new InstanceObjectPool(assetInfo, 
                        loadOption.Priority, 
                        m_ResourceMgr, 
                        strategy, 
                        ObjectPoolContainerRoot,
                        OnAssetLoadCompleteCallBack,
                        OnObjectPoolReferenceChangedCallback,
                        OnObjectPoolDestroyCallback);
                    m_InstancePools.Add(assetInfo, instanceObjectPool);
                }
                
                
                switch (instanceObjectPool.PoolState)
                {
                    case InstanceObjectPoolState.WaitAssetLoading:
                        var waitPendingTask = ResourceObjectHolder<InstanceInitializeTask>.Allocate();
                        waitPendingTask.Init(instanceObjectPool, parent, ref loadOption);
                        instanceObjectPool.WaitPendingInitializeTasks.Enqueue(waitPendingTask);
                        break;
                    case InstanceObjectPoolState.Loaded:
                        var handler = instanceObjectPool.GetInstanceObjectHandler(parent, loadOption);
                        if (handler != null)  // 池里有handler 直接返回结果 不走资源加载
                        {
                            loadOption.CallOnSuccess(handler, assetInfo);
                            break;
                        }
                            
                        var directTask = ResourceObjectHolder<InstanceInitializeTask>.Allocate();
                        directTask.Init(instanceObjectPool, parent, ref loadOption);
                        AddInitializeTaskInQueue(directTask);
                        break;

                    case InstanceObjectPoolState.Failed:
                        loadOption.CallOnFailed(assetInfo);
                        break;

                }
            }
            else
            {
                loadOption.CallOnFailed(assetInfo);
            }
        }
        

        private void AddInitializeTaskInQueue(InstanceInitializeTask task)
        {
            if (IsExecutting)
                m_PendingInitializeTasks.Enqueue(task);
            else
                m_ExecuteInitializeTasks.Enqueue(task);
        }

        private void OnAssetLoadCompleteCallBack(InstanceObjectPool objectPool)
        {
            if (objectPool.PoolState == InstanceObjectPoolState.Loaded)
            {
                while (objectPool.WaitPendingInitializeTasks.Count > 0)
                {
                    var task = objectPool.WaitPendingInitializeTasks.Dequeue();
                    AddInitializeTaskInQueue(task);
                }
            }
        }
    }
}
