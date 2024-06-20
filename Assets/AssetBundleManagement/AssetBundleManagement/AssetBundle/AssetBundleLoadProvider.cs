using System;
using System.Collections.Generic;
using System.Text;
using AssetBundleManagement.Manifest;
using AssetBundleManager.Manifest;
using AssetBundleManager.Warehouse;
using Core.Utils;
using JetBrains.Annotations;
using UnityEngine;
using Utils.AssetManager;


namespace AssetBundleManagement
{
    internal class AssetBundleLoadProvider 
    {
     
        private Dictionary<BundleKey, AssetBundleResourceHandle> m_AssetBundleReosurceMap =
                        new Dictionary<BundleKey, AssetBundleResourceHandle>(2048);

        internal Dictionary<BundleKey, AssetBundleResourceHandle> GetAssetBundleReosurceMap()
        {
            return m_AssetBundleReosurceMap;
        }
        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.AssetBundleLoadProvider");


        private IResourceUpdater m_ResourceUpdateMananger;
        private ResourceEventMonitor m_EventMananger;
        private ManiFestInitializer m_Manifest;
        private System.Action<AssetBundleResourceHandle> m_BundleSelfLoadCompleteCache;
        //private System.Action<AssetBundleResourceHandle> m_BundleFullLoadCompletexCache;
        private System.Action<AssetBundleResourceHandle> m_BundleReferenceChangedCache;
        private System.Action<AssetBundleResourceHandle> m_BundleWillDestroyCache;
     
        internal AssetBundleLoadProvider(IResourceUpdater updateMananger, ManiFestInitializer maniFestInitializer, ResourceEventMonitor eventMananger)
        {
            m_ResourceUpdateMananger       = updateMananger;
            m_Manifest                     = maniFestInitializer;
            m_EventMananger                = eventMananger;
            m_BundleSelfLoadCompleteCache  = OnBundleSelfCompleteCallback;
      //      m_BundleFullLoadCompletexCache = OnBundleFullCompleteCallback;
            m_BundleReferenceChangedCache  = OnBundleReferenceChangedCallback;
            m_BundleWillDestroyCache       = OnBundleWillDestroyCallback;
        }
        //加载asset对应的ab，并添加引用计数
        internal AssetBundleResourceHandle LoadAssetBundleAsync(AssetInfo path)
        {
            BundleKey bundleKey = AssetUtility.ConvertToBundleKey(path);
        
            AssetBundleResourceHandle topResource;
            if (m_AssetBundleReosurceMap.TryGetValue(bundleKey, out topResource))
            {
                if (topResource.IsFaild())
                {
                    return null;
                }
                if(topResource.IsIndirectBundle())
                {
                    AssetBundleInfoItem loadRequest = m_Manifest.GetAsssetBundleInfo(bundleKey.BundleName);
                    if (loadRequest == null)
                    {
                        return null;
                    }
                    LoadAssetBundleDependencies(topResource, loadRequest);
                }
            }
            else
            {
                topResource = LoadAssetBundleDirectly(bundleKey);
            }
         //   topResource.AddReference(true);
            return topResource;
        }
        private void FireBundleEvent(ResourceEventType bundleEventType, AssetBundleResourceHandle attachedBundle)
        {
            BundleRelatedEventArgs bundleRelatedEventArgs = ResourceObjectHolder<BundleRelatedEventArgs>.Allocate();
            bundleRelatedEventArgs.EventType = bundleEventType;
            bundleRelatedEventArgs.Bundle = attachedBundle;
            m_EventMananger.FireEvent(bundleRelatedEventArgs);
        }
        void OnBundleSelfCompleteCallback(AssetBundleResourceHandle attachedBundle)
        {
            ResourceEventType resourceEventType = attachedBundle.IsFaild() ? ResourceEventType.LoadBundleFailed : ResourceEventType.LoadBundleSelfSucess;
            FireBundleEvent(resourceEventType, attachedBundle);
        }
        // void OnBundleFullCompleteCallback(AssetBundleResourceHandle attachedBundle)
        // {
        //     FireBundleEvent(ResourceEventType.LoadBundleFullSucess, attachedBundle);
        // }
        private AssetBundleResourceHandle CreateBundleHandle(AssetBundleWarehouse bundleWarehouse, BundleKey bundleKey)
        {
            var loader           = bundleWarehouse.CreateNewBundleLoader(bundleKey.BundleName);
        //    var bundleInfo       = bundleWarehouse.GetAssetBundleInfo(bundleKey.BundleName);
          //  int dependenciesCunt = bundleInfo != null ? bundleInfo.GetDependenciesCount() : 0;
            
            BundleLoadCallbacks bundleLoad = new BundleLoadCallbacks(m_BundleSelfLoadCompleteCache, m_BundleReferenceChangedCache, m_BundleWillDestroyCache);
            AssetBundleResourceHandle topResource = new AssetBundleResourceHandle(
                new AssetBundleLoadOperation(bundleKey.BundleName, m_ResourceUpdateMananger,loader),
                    bundleKey, bundleLoad);
           // m_AssetBundleReosurceMap.Add(bundleKey, topResource);
            return topResource;

        }

        //加载asset对应的ab，并加载它依赖的ab
        private AssetBundleResourceHandle LoadAssetBundleDirectly(BundleKey bundleKey)
        {
            AssetBundleWarehouse bundleWarehouse = m_Manifest.FindWarehouse(bundleKey.BundleName);
            var                  bundleInfo        = bundleWarehouse.GetAssetBundleInfo(bundleKey.BundleName);
            if (bundleInfo == null)
            {
                s_logger.ErrorFormat("Manifest dont has bundle {0}",bundleKey.BundleName);
                return null;
            }
            AssetBundleResourceHandle topResource = CreateBundleHandle(bundleWarehouse,bundleKey);
            m_AssetBundleReosurceMap.Add(bundleKey, topResource);
            FireBundleEvent(ResourceEventType.LoadBundleDirectly, topResource);
            //ResourceProfiler.LoadBundleDirectly(topResource);
            LoadAssetBundleDependencies(topResource, bundleInfo);
            topResource.BeginOperation();

            return topResource;
        }
        private void LoadAssetBundleDependencies(AssetBundleResourceHandle topResource,AssetBundleInfoItem loadRequest)
        {
            //构建AssetBundle依赖数据.loadRequest一定是有值的
            topResource.StartLoadingDependencie();
            for (int i = 0; i < loadRequest.GetDependencies().Length; i++)
            {
                AssetBundleResourceHandle depBundleRes;
                BundleKey dependKey = AssetUtility.ConvertToBundleKey(loadRequest.GetDependencies()[i]);
                if (!m_AssetBundleReosurceMap.TryGetValue(dependKey, out depBundleRes))
                {
                    depBundleRes = LoadAssetBundleIndirectly(topResource,dependKey);
                    m_AssetBundleReosurceMap.Add(dependKey, depBundleRes);
                }

                topResource.ChainBundleDependencies(depBundleRes);
            }
            topResource.EndLoadingDependencie();
        }

        //加载bundle依赖的ab,不加载它的依赖
        private AssetBundleResourceHandle LoadAssetBundleIndirectly(AssetBundleResourceHandle topResource, BundleKey bundleKey)
        {
            var warehouse      = m_Manifest.FindWarehouse(bundleKey.BundleName);
            AssetBundleResourceHandle depAssetBundle = CreateBundleHandle(warehouse, bundleKey);
       
            FireBundleEvent(ResourceEventType.LoadBundleIndirectly, depAssetBundle);
            // ResourceProfiler.LoadBundleInDirectly(depAssetBundle, topResource);

            depAssetBundle.BeginOperation();
            return depAssetBundle;
        }
        private void OnBundleReferenceChangedCallback(AssetBundleResourceHandle assetBundleResource)
        {
            m_ResourceUpdateMananger.InspectResourceDisposeState(assetBundleResource);
        }

        private void OnBundleWillDestroyCallback(AssetBundleResourceHandle assetBundleResource)
        {
            m_AssetBundleReosurceMap.Remove(assetBundleResource.BundleKey);
            FireBundleEvent(ResourceEventType.WillDestroyBundle, assetBundleResource);
        }

        #region//profiler
        public void InitSetBundleData(ProfileDataAdapter profileDataAdapter)
        {
            profileDataAdapter.InitSetBundleData(m_AssetBundleReosurceMap);
        }

        internal void GetProviderAllInfo(StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("====>AssetBundle LoadData,Size:{0}\n", m_AssetBundleReosurceMap.Count);
            foreach (var bundle in m_AssetBundleReosurceMap)
            {
                stringBuilder.AppendFormat("    [BundleItem]{0}\n", bundle.Value.ToStringDetail());
            }
        }
        internal void GetInspectorData(List<BundleInspectorData> datas)
        {
            datas.Clear();
            foreach (var item in m_AssetBundleReosurceMap.Values)
            {
                datas.Add(new BundleInspectorData()
                {
                    LoadState = item.GetOutputState(),
                    WaitDestroyStart = item.WaitDestroyStartTime,
                    Name = item.BundleKey.BundleName,
                    Reference = item.GetReferenceCount(),
                    BundleKey = item.BundleKey,
                    AllDependenceBundleKeys = item.GetAllDependenceBundleKeys(),
                });
            }
        }

        internal void GetResourceStatus(ref ResourceManangerStatus resourceManangerStatus)
        {
            int sum = 0;

            foreach (var bundle in m_AssetBundleReosurceMap)
            {
                sum += bundle.Value.GetReferenceCount();
                if (bundle.Value.IsFaild())
                {
                    resourceManangerStatus.AllBundleFailedCont++;
                }
                else
                {
                    resourceManangerStatus.AllBundleSucessCont++;
                }
            }

            resourceManangerStatus.AllBundleHoldReference = sum;
        }
        #endregion

        public AssetBundleOutputState QueryBundleLoadState(string bundleName)
        {
            AssetBundleResourceHandle resourceHandle;
            if (m_AssetBundleReosurceMap.TryGetValue(new BundleKey(bundleName, 0), out resourceHandle))
            {
                return resourceHandle.GetOutputState();
            }

            return AssetBundleOutputState.Unkown;
        }

        internal bool CheckTableExistAsset(string assetName)
        {
            AssetBundleResourceHandle resourceHandle;
            if (m_AssetBundleReosurceMap.TryGetValue(new BundleKey("tables", 0), out resourceHandle))
            {
                var ab = resourceHandle.GetAssetBundle();
                if (ab != null)
                    return ab.Contains(assetName);
            }
            return false;

        }
    }
}