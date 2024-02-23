using System;
using System.Collections.Generic;
using UnityEngine;


namespace AssetBundleManagement
{
    public class AssetBundleLoadMananger : IAssetBundleLoadMananger
    {
        private HashSet<ILoadOperation> m_BackgroundLoadingOperation = new HashSet<ILoadOperation>();

        //  private List<ILoadOperation> m_PendingAddLoadingOperation = new List<ILoadOperation>();
        private List<ILoadOperation> m_PendingRemoveLoadingOperation = new List<ILoadOperation>();

        private Dictionary<BundleKey, AssetBundleResource> m_AssetBundleReosurceMap =
                        new Dictionary<BundleKey, AssetBundleResource>(2048);
        //private List<AssetBundleResource> m_ToBeenDestroyedBundleList = new List<AssetBundleResource>(128);


        //加载asset对应的ab，并添加引用计数
        private AssetBundleResource LoadAssetBundleAsyncWithReference(string bundleName)
        {
            BundleKey bundleKey = AssetUtility.ConvertToBundleKey(bundleName);
        
            AssetBundleResource topResource;
            if (m_AssetBundleReosurceMap.TryGetValue(bundleKey, out topResource))
            {
                if(topResource.IsSelfLoadDone())
                   topResource.AddReference(true);
            }
            
            topResource = LoadAssetBundleDirectly(bundleKey);

            if (topResource.IsFullLoaded())
            {
                topResource.AddReference(true);
                return topResource;
            }
            //todo:
            return null;
        }

        //加载asset对应的ab，并加载它依赖的ab
        private AssetBundleResource LoadAssetBundleDirectly(BundleKey bundleKey)
        {
            AssetBundleRequestOptions loadRequest = AssetUtility.ConvertToAssetBundleRequest(bundleKey);
            AssetBundleResource topResource = new AssetBundleResource(
                new AssetBundleLoadOperation(loadRequest, this, loadRequest.GetDependenciesCount()), bundleKey,true);
            topResource.BundleWillDestroyAction += OnBundleWillDestroy;
            m_AssetBundleReosurceMap.Add(bundleKey, topResource);
            //Recursively building AssetBundle Dependencies data.
            for (int i = 0; i < loadRequest.Dependencies.Length; i++)
            {
                AssetBundleResource depBundleRes;
                BundleKey           dependKey = AssetUtility.ConvertToBundleKey(loadRequest.Dependencies[i]);
                if (!m_AssetBundleReosurceMap.TryGetValue(dependKey, out depBundleRes))
                {
                    depBundleRes = LoadAssetBundleIndirectly(dependKey);
                    m_AssetBundleReosurceMap.Add(dependKey, depBundleRes);
                }

                topResource.ChainBundleDependencies(depBundleRes);
            }

            topResource.Start();
            return topResource;
        }

        //加载bundle依赖的ab,不递归加载它的依赖
        private AssetBundleResource LoadAssetBundleIndirectly(BundleKey bundleKey)
        {
            AssetBundleRequestOptions loadRequest = AssetUtility.ConvertToAssetBundleRequest(bundleKey);
            AssetBundleResource depAssetBundle = new AssetBundleResource(
                new AssetBundleLoadOperation(loadRequest, this, 0), bundleKey,false);
            depAssetBundle.Start();
            return depAssetBundle;
        }

        private void OnBundleWillDestroy(AssetBundleResource resource)
        {
            m_AssetBundleReosurceMap.Remove(resource.BundleKey);
            //   m_ToBeenDestroyedBundleList.Add(resource);
        }


        private bool m_PollUpdateLoadingState;

        public void AddLoadingOperation(ILoadOperation loadOperation)
        {
            //      if (m_PollUpdateLoadingState)
            //      {
            //         m_PendingAddLoadingOperation.Add(loadOperation);
            //      }
            //     else
            {
                m_BackgroundLoadingOperation.Add(loadOperation);
            }
        }

        public void RemoveLoadingOperation(ILoadOperation loadOperation)
        {
            if (m_PollUpdateLoadingState)
            {
                m_PendingRemoveLoadingOperation.Add(loadOperation);
            }
            else
            {
                m_BackgroundLoadingOperation.Remove(loadOperation);
            }
        }

        public void PollLoadingOperationResult()
        {
            m_PollUpdateLoadingState = true;
            foreach (var loadOperation in m_BackgroundLoadingOperation)
            {
                loadOperation.PollOperationResult();
            }

            foreach (var loadOperation in m_PendingRemoveLoadingOperation)
            {
                m_BackgroundLoadingOperation.Remove(loadOperation);
            }

            m_PendingRemoveLoadingOperation.Clear();

            m_PollUpdateLoadingState = false;
        }
    }
}