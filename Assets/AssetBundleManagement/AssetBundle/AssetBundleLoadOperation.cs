

using UnityEditor.VersionControl;
using UnityEngine;

namespace AssetBundleManagement
{

    public class AssetBundleLoadOperation : ILoadOperation
    {
        //  private AssetBundle m_BunleObject;
        private AssetBundleLoader m_AssetBundleLoader;
        private AssetBundleRequestOptions m_LoadRequestOptions;
        private int m_DependenciesRemainCount;
        //private bool m_NeedLoadDependcy;

        private IAssetBundleLoadMananger m_AbMananger;

        public System.Action<AssetBundle> OnSelfLoadComplete;
        public System.Action OnDependenciesLoadComplete;
        

        public string GetLoadKey()
        {
            return m_LoadRequestOptions.RealName;
        }

        public bool HasNoDepend()
        {
            return m_DependenciesRemainCount == 0;
        }
        public AssetBundleLoadOperation(AssetBundleRequestOptions requestOptions, IAssetBundleLoadMananger mgr,int DependenciesRemainCount)
        {
        //    m_NeedLoadDependcy         = loadDependcy;
            m_LoadRequestOptions   = requestOptions;
            m_DependenciesRemainCount = DependenciesRemainCount;
            m_AbMananger           = mgr;
            m_AssetBundleLoader    = AssetBundleLoadFactory.CreateAssetBundleLoader();

        }

        public void ReduceDependenciesRemain()
        {
            Debug.Assert(m_DependenciesRemainCount > 0, "OnOneDependenciesLoadComplete state dont correct");
            --m_DependenciesRemainCount;
            if (m_DependenciesRemainCount == 0)
            {
                if (OnDependenciesLoadComplete != null)
                    OnDependenciesLoadComplete();
            }
        }

        public void BeginOperation()
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
            AssetBundle assetBundle = m_AssetBundleLoader.GetResult();
            if (assetBundle != null)
            {
                m_AbMananger.RemoveLoadingOperation(this);
                if (OnSelfLoadComplete != null)
                    OnSelfLoadComplete(assetBundle);
                m_AssetBundleLoader = null;
            }
        }
    }
}
