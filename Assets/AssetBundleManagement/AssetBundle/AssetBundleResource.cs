using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleManagement
{
    public class AssetBundleResource
    {
        public bool IsSelfLoadDone()
        {
            return m_SelfLoadState == AssetBundleLoadState.Loaded;
        }

        public bool IsLoading()
        {
            return m_SelfLoadState == AssetBundleLoadState.Loading || m_DependenciesLoadState == AssetBundleLoadState.Loading;
        }
        public bool IsFullLoaded()
        {
            return m_SelfLoadState == AssetBundleLoadState.Loaded &&
                            m_DependenciesLoadState == AssetBundleLoadState.Loaded;
        }
        AssetBundleLoadOperation m_LoadOperation;
        private AssetBundleLoadState m_SelfLoadState;
        private AssetBundleLoadState m_DependenciesLoadState;

        //Recursively collect all dependencies for the current asset bundle, not just the direct dependencies.
        List<AssetBundleResource> m_AllDependencies = new List<AssetBundleResource>(16);
        AssetBundle m_AssetBundle;
        private int m_ReferenceCount;
        public readonly BundleKey BundleKey;
        private bool m_IsDisposed;

        //callback
        public System.Action BundleSelfLoadCompleteAction;
        public System.Action BundleFullLoadCompleteAction;
        public System.Action<AssetBundleResource> BundleWillDestroyAction;


        public AssetBundleResource(AssetBundleLoadOperation loadOperation, BundleKey bundleKey, bool loadWithDepend)
        {
            m_LoadOperation = loadOperation;
            BundleKey = bundleKey;
            m_SelfLoadState = AssetBundleLoadState.Loading;
            m_DependenciesLoadState = loadWithDepend ? AssetBundleLoadState.Loading : AssetBundleLoadState.NotBegin;
            loadOperation.OnSelfLoadComplete += OnBundleLoadDone;
            if (loadWithDepend)
                loadOperation.OnDependenciesLoadComplete += OntAllDependenciesLoadDone;


            // m_AllDependenciess = allDependenciess;
            //  if (autoAddAssetReference)
            //        AddReference(true);
        }

        public void Start()
        {
            m_LoadOperation.BeginOperation();
        }

        void OnBundleLoadDone(AssetBundle assetBundle)
        {
            m_AssetBundle   = assetBundle;
            m_SelfLoadState = AssetBundleLoadState.Loaded;
            if (BundleSelfLoadCompleteAction != null)
            {
                BundleSelfLoadCompleteAction();
                BundleSelfLoadCompleteAction = null;
            }

            if (m_DependenciesLoadState == AssetBundleLoadState.Loaded)
            {
                if (BundleFullLoadCompleteAction != null)
                {
                    BundleFullLoadCompleteAction();
                    BundleFullLoadCompleteAction = null;
                }
            }
        }

        void OntAllDependenciesLoadDone()
        {
            m_DependenciesLoadState = AssetBundleLoadState.Loaded;
            if (m_SelfLoadState == AssetBundleLoadState.Loaded)
            {
                if (BundleFullLoadCompleteAction != null)
                {
                    BundleFullLoadCompleteAction();
                    BundleFullLoadCompleteAction = null;
                }
            }
        }

        public void ChainBundleDependencies(AssetBundleResource depBundle)
        {
            m_AllDependencies.Add(depBundle);
            if (depBundle.IsSelfLoadDone())
            {
                m_LoadOperation.ReduceDependenciesRemain();
            }
            else
            {
                depBundle.BundleSelfLoadCompleteAction += ReduceDependenciesRemain;
            }
        }

        void ReduceDependenciesRemain()
        {
            m_LoadOperation.ReduceDependenciesRemain();
        }

        void Destroy()
        {
            m_IsDisposed = true;
            if (BundleWillDestroyAction != null)
            {
                BundleWillDestroyAction(this);
                BundleWillDestroyAction = null;
            }

            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
            }

            m_LoadOperation = null;
        }

        public void AddReference(bool includeDependencies)
        {
            ++m_ReferenceCount;
            if (includeDependencies)
            {
                foreach (var dependItem in m_AllDependencies)
                {
                    dependItem.AddReference(false);
                }
            }
        }

        public void ReleaseReference(bool includeDependencies)
        {
            //todo:
            /*
            if (!HasResourceState(AssetBundleResourceState.SelfLoadDone | AssetBundleResourceState.DependLoadDone))
                throw new System.Exception("When ReleaseReference operation,assetbundle must has been fully loaded !!");
            --m_ReferenceCount;
            if (includeDependencies)
            {
                foreach (var dependItem in m_AllDependenciess)
                {
                    dependItem.ReleaseReference(false);
                }
            }

            if (m_ReferenceCount == 0)
                Destroy();
                
                */
        }

        public string GetDebugName()
        {
            return m_AssetBundle != null ? m_AssetBundle.name : "LoadingAb";
        }
    }
}