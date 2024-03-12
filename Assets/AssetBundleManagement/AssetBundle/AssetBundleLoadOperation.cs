

using Assets.AssetBundleManagement.Loader;
using UnityEngine;

namespace AssetBundleManagement
{

    internal class AssetBundleLoadOperation : ILoadOperation
    {
        //  private AssetBundle m_BunleObject;
        private AssetBundleLoader m_AssetBundleLoader;
        private AssetBundleRequestOptions m_LoadRequestOptions;
        private int m_DependenciesRemainCount;
        //private bool m_NeedLoadDependcy;

        private IResourceUpdater m_AbMananger;

        internal System.Action<AssetBundle> OnSelfLoadComplete;
        internal System.Action OnDependenciesLoadComplete;


        internal string GetLoadKey()
        {
            return m_LoadRequestOptions.RealName;
        }

        internal bool HasNoDepend()
        {
            return m_DependenciesRemainCount == 0;
        }
        internal AssetBundleLoadOperation(AssetBundleRequestOptions requestOptions, IResourceUpdater mgr, int DependenciesRemainCount)
        {
            //    m_NeedLoadDependcy         = loadDependcy;
            m_LoadRequestOptions = requestOptions;
            m_DependenciesRemainCount = DependenciesRemainCount;
            m_AbMananger = mgr;
            m_AssetBundleLoader = AssetBundleFactoryFacade.CreateAssetBundleLoader(requestOptions.Pattern);

        }

        internal void ReduceDependenciesRemain()
        {
            Debug.Assert(m_DependenciesRemainCount > 0, "OnOneDependenciesLoadComplete state dont correct");
            --m_DependenciesRemainCount;
            ResourceProfiler.BundleOneDepndencyDone(GetLoadKey(),m_DependenciesRemainCount);
        //    Debug.Log("Reduce dependices");
            if (m_DependenciesRemainCount == 0)
            {
                if (OnDependenciesLoadComplete != null)
                    OnDependenciesLoadComplete();
            }
        }

        internal void BeginOperation()
        {
            m_AssetBundleLoader.Begin(m_LoadRequestOptions);
            m_AbMananger.AddLoadingOperation(this);
            if (m_DependenciesRemainCount == 0)
            {
                if (OnDependenciesLoadComplete != null)
                    OnDependenciesLoadComplete();
            }
        }

        public void PollOperationResult()
        {
            AssetBundle assetBundle;
            if (m_AssetBundleLoader.GetResult(out assetBundle))
            {
                m_AbMananger.RemoveLoadingOperation(this);
                if (OnSelfLoadComplete != null)
                    OnSelfLoadComplete(assetBundle);
                m_AssetBundleLoader = null;
            }
        }
        public override string ToString()
        {
            return string.Format("LoadBundle:{0}", GetLoadKey());
        }
    }
}
