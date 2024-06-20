

using System;
using AssetBundleManagement;
using AssetBundleManager.Manifest;
using Core.Utils;
using UnityEngine;

namespace AssetBundleManagement
{

    internal class AssetBundleLoadOperation : ILoadOperation
    {
        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.AssetBundleLoadOperation");
        //  private AssetBundle m_BunleObject;
        private AssetBundleLoader m_AssetBundleLoader;
        private string m_BundleName;
        private int m_DependenciesRemainCount;
        //private bool m_NeedLoadDependcy;

        private IResourceUpdater m_AbMananger;

        internal System.Action<AssetBundle,bool> OnSelfLoadComplete;
        internal System.Action OnDependenciesLoadComplete;


        internal string GetLoadKey()
        {
            return m_BundleName;
        }

        internal bool HasNoDepend()
        {
            return m_DependenciesRemainCount == 0;
        }
        internal AssetBundleLoadOperation(string bundleName, IResourceUpdater mgr,AssetBundleLoader assetBundleLoader)
        {
            //    m_NeedLoadDependcy         = loadDependcy;
            m_BundleName              = bundleName;
            m_DependenciesRemainCount = int.MaxValue;
            m_AbMananger              = mgr;
            m_AssetBundleLoader       = assetBundleLoader;

        }

        internal void IncreaseDependenciesRemain()
        {
            ++m_DependenciesRemainCount;
        }

        internal void InitDependenciesRemain()
        {
            m_DependenciesRemainCount = 0;
        }
        internal void ReduceDependenciesRemain()
        {
            AssertUtility.Assert(m_DependenciesRemainCount > 0, "OnOneDependenciesLoadComplete state dont correct");
            --m_DependenciesRemainCount;
      //      ResourceProfiler.BundleOneDepndencyDone(GetLoadKey(),m_DependenciesRemainCount);
        //    Debug.Log("Reduce dependices");
            CheckDependenciesRemain();
        }

        internal void CheckDependenciesRemain()
        {
            if (m_DependenciesRemainCount == 0)
            {
                if (OnDependenciesLoadComplete != null)
                    OnDependenciesLoadComplete();
            }
        }

        internal void BeginOperation()
        {
            m_AssetBundleLoader.Begin();
            m_AbMananger.AddLoadingOperation(this);
            
        }
        

        public void PollOperationResult()
        {
            if (m_AssetBundleLoader == null)
            {
                m_AbMananger.RemoveLoadingOperation(this);
                return;
            }
            AssetBundle assetBundle;
            if (m_AssetBundleLoader.GetResult(out assetBundle))
            {
                m_AbMananger.RemoveLoadingOperation(this);
                if (OnSelfLoadComplete != null)
                {
                    OnSelfLoadComplete(assetBundle,m_AssetBundleLoader.IsSimulation);
                    OnSelfLoadComplete = null;
                }
                m_AssetBundleLoader = null;

               
            }
        }

        public LoadOpType LoadOp
        {
            get { return LoadOpType.Bundle; }
        }

        public override string ToString()
        {
            return string.Format("LoadBundle:{0}", GetLoadKey());
        }
    }
}
