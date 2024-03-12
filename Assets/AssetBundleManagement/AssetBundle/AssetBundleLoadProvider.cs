using System;
using System.Collections.Generic;
using System.Text;
using AssetBundleManagement.Manifest;
using UnityEngine;


namespace AssetBundleManagement
{
    internal class AssetBundleLoadProvider 
    {
     
        private Dictionary<BundleKey, AssetBundleResourceHandle> m_AssetBundleReosurceMap =
                        new Dictionary<BundleKey, AssetBundleResourceHandle>(2048);

        internal void GetProviderAllInfo(StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("====>AssetBundle LoadData,Size:{0}\n", m_AssetBundleReosurceMap.Count);
            foreach (var bundle in m_AssetBundleReosurceMap)
            {
                stringBuilder.AppendFormat("    [BundleItem]{0}\n", bundle.Value.ToStringDetail());
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
        private IResourceUpdater m_ResourceUpdateMananger;
        private ManiFestProvider m_maniFestProvider;
        internal AssetBundleLoadProvider(IResourceUpdater updateMananger, ManiFestProvider maniFestProvider)
        {
            m_ResourceUpdateMananger = updateMananger;
            m_maniFestProvider = maniFestProvider;
        }
        //加载asset对应的ab，并添加引用计数
        internal AssetBundleResourceHandle LoadAssetBundleAsync(AssetInfo path)
        {
            BundleKey bundleKey = AssetUtility.ConvertToBundleKey(path);
        
            AssetBundleResourceHandle topResource;
            if (m_AssetBundleReosurceMap.TryGetValue(bundleKey, out topResource))
            {
                if(topResource.IsIndirectBundle())
                {
                    AssetBundleRequestOptions loadRequest = m_maniFestProvider.GetABRequsetOptions(bundleKey);
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

        //加载asset对应的ab，并加载它依赖的ab
        private AssetBundleResourceHandle LoadAssetBundleDirectly(BundleKey bundleKey)
        {
            AssetBundleRequestOptions loadRequest = m_maniFestProvider.GetABRequsetOptions(bundleKey);
            if (loadRequest == null)
                return null;
            AssetBundleResourceHandle topResource = new AssetBundleResourceHandle(
                new AssetBundleLoadOperation(loadRequest, m_ResourceUpdateMananger, loadRequest.GetDependenciesCount()), bundleKey);
            topResource.BundleReferenceChangedNotify += OnBundleReferenceChangedCallback;
            topResource.BundleWillDestroyNotify          += OnBundleWillDestroyCallback;
            m_AssetBundleReosurceMap.Add(bundleKey, topResource);
            ResourceProfiler.LoadBundleDirectly(topResource);
            LoadAssetBundleDependencies(topResource, loadRequest);
            topResource.BeginOperation();

            return topResource;
        }
        private void LoadAssetBundleDependencies(AssetBundleResourceHandle topResource,AssetBundleRequestOptions loadRequest)
        {
            //构建AssetBundle依赖数据.
            topResource.SetLoadingDependencie();
            for (int i = 0; i < loadRequest.Dependencies.Length; i++)
            {
                AssetBundleResourceHandle depBundleRes;
                BundleKey dependKey = AssetUtility.ConvertToBundleKey(loadRequest.Dependencies[i]);
                if (!m_AssetBundleReosurceMap.TryGetValue(dependKey, out depBundleRes))
                {
                    depBundleRes = LoadAssetBundleIndirectly(topResource,dependKey);
                    m_AssetBundleReosurceMap.Add(dependKey, depBundleRes);
                }

                topResource.ChainBundleDependencies(depBundleRes);
            }
        }

        //加载bundle依赖的ab,不加载它的依赖
        private AssetBundleResourceHandle LoadAssetBundleIndirectly(AssetBundleResourceHandle topResource, BundleKey bundleKey)
        {

            AssetBundleRequestOptions loadRequest = m_maniFestProvider.GetABRequsetOptions(bundleKey);
            AssetBundleResourceHandle depAssetBundle = new AssetBundleResourceHandle(
                new AssetBundleLoadOperation(loadRequest, m_ResourceUpdateMananger, loadRequest.GetDependenciesCount()), bundleKey);
            depAssetBundle.BundleReferenceChangedNotify += OnBundleReferenceChangedCallback;
            depAssetBundle.BundleWillDestroyNotify          += OnBundleWillDestroyCallback;
            ResourceProfiler.LoadBundleInDirectly(depAssetBundle, topResource);

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
        }
       


   
    }
}