//using Assets.ABSystem.Scripts.AssetBundleManagement;
//using NUnit.Framework;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Resources;
//using System.Text;
//using UnityEditor.VersionControl;
//using UnityEngine;

//namespace AssetBundleManagement
//{
//    internal enum InstanceObjectPoolState
//    {
//        WaitAssetLoading,
//        Loaded,
//        Failed,
//    }
//    internal class InstanceObjectPool
//    {
//        private AsyncAssetOperationHandler m_Template;
//        private int m_WaitLoadingRequest;
//        internal void IncreaseLoadingRequest() { ++m_WaitLoadingRequest; }
//        internal void DecreaseLoadingRequest() { --m_WaitLoadingRequest; }

//        internal InstanceObjectPoolState PoolState { get; private set; }
//        internal readonly AssetInfo Asset;
//        private Queue<GaneObjectPoolContainer> m_Objects = new Queue<GaneObjectPoolContainer>();
//        internal List<LoadAssetOption> LoadCompleteCallbacks = new List<LoadAssetOption>();
//        internal System.Action<InstanceObjectPool> OnAssetLoadComplete;
//        internal InstanceObjectPool(AssetInfo path, LoadPriority loadPriority, ResourceLoadManager resourceLoadManager)
//        {
//            PoolState = InstanceObjectPoolState.WaitAssetLoading;
//            Asset = path;
//            LoadAssetOption assetOption = new LoadAssetOption(OnAssetLoadSucess, OnAssetLoadFailed, loadPriority, this);
//            resourceLoadManager.LoadAsset(path, assetOption);
//        }

//        internal GaneObjectPoolContainer GetFromCache()
//        {
//            if (PoolState == InstanceObjectPoolState.Loaded)
//            {
//                return m_Objects.Count > 0 ? m_Objects.Dequeue() : null;
//            }
//            return null;
//        }
//        void OnAssetLoadSucess(AsyncAssetOperationHandler assetAccessor)
//        {
//            PoolState = InstanceObjectPoolState.Loaded;
//            m_Template = assetAccessor;
//            OnAssetLoadComplete(this);

//        }
//        void OnAssetLoadFailed(string errorMessage, object context)
//        {
//            PoolState = InstanceObjectPoolState.Failed;
//            OnAssetLoadComplete(this);

//        }



//    }
//    internal class InstanceInstantiaetTask
//    {
//        internal InstanceObjectPool AttachPool;
//        internal AssetLoadOperation AssetLoadOperation;
//    }
//    public class UnityInstanceObjectPoolContainer
//    {
//        private Dictionary<AssetInfo, InstanceObjectPool> m_InstancePools = new Dictionary<AssetInfo, InstanceObjectPool>(1024, AssetInfo.AssetInfoComparer.Instance);
//        private ResourceLoadManager m_ResourceMgr;
//        private int m_InstantiaetExecuteLimit;
//        internal UnityInstanceObjectPoolContainer(ResourceLoadManager resourceLoadManager, int instantiaetExecuteLimit)
//        {
//            m_ResourceMgr = resourceLoadManager;
//            m_InstantiaetExecuteLimit = instantiaetExecuteLimit;
//        }
//        private Queue<InstanceInstantiaetTask> m_InstanceTasks = new Queue<InstanceInstantiaetTask>();
//        private void OnAssetLoadComplete(InstanceObjectPool objectPool)
//        {
//            foreach (InstanceInstantiaetTask task in m_InstanceTasks)
//            {
//                m_InstanceTasks.Enqueue(new InstanceInstantiaetTask());
//            }
//        }
//        public void InstantiateAsync(AssetInfo path, LoadAssetOption loadOption, Transform parent = null)
//        {
//            if (path.IsValid())
//            {
//                InstanceObjectPool instanceObjectPool = null;
//                if (!m_InstancePools.TryGetValue(path, out instanceObjectPool))
//                {
//                    instanceObjectPool = new InstanceObjectPool(path, loadOption.Priority, m_ResourceMgr);
//                    instanceObjectPool.OnAssetLoadComplete += OnAssetLoadComplete;
//                    m_InstancePools.Add(path, instanceObjectPool);
//                }
//                switch (instanceObjectPool.PoolState)
//                {
//                    case InstanceObjectPoolState.WaitAssetLoading:
//                        instanceObjectPool.LoadCompleteCallbacks.Add(loadOption);
//                        instanceObjectPool.IncreaseLoadingRequest();
//                        break;
//                    case InstanceObjectPoolState.Loaded:
//                        m_InstanceTasks.Enqueue(new InstanceInstantiaetTask());
//                        instanceObjectPool.IncreaseLoadingRequest();
//                        break;

//                    case InstanceObjectPoolState.Failed:
//                        if (loadOption.OnFailure != null)
//                        {
//                            loadOption.OnFailure("AssetInfo is not valid", loadOption.Context);
//                        }
//                        break;

//                }
//            }
//            else
//            {
//                if (loadOption.OnFailure != null)
//                {
//                    loadOption.OnFailure("AssetInfo is not valid", loadOption.Context);
//                }
//            }
//        }

//    }
//}
