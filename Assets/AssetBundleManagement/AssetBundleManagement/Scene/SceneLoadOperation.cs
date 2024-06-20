using AssetBundleManagement;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetBundleManagement
{
    internal class SceneLoadOperation : ILoadOperation
    {
        private SceneLoader m_SceneLoader;
        private NewSceneInfo m_SceneInfo;
        internal System.Action<bool> OnSceneLoadComplete;
        private IResourceUpdater m_UpdateMgr;

        private AssetBundleLoadingPattern m_Pattern;
        internal SceneLoadOperation(IResourceUpdater mgr, NewSceneInfo sceneInfo, AssetBundleLoadingPattern pattern)
        {
            //m_SceneLoader = AssetBundleFactoryFacade.CreateSceneLoader(pattern);
            m_UpdateMgr = mgr;
            m_SceneInfo = sceneInfo;
            m_Pattern = pattern;
        }

        internal void SetIsSync()
        {
            if (m_SceneLoader != null)
                m_SceneLoader.SetIsSync();
        }

        internal AsyncOperation GetSceneOperation()
        {
            if (m_SceneLoader != null)
                return m_SceneLoader.AsyncOperation;
            return null;
        }
        internal void Restart()
        {
            if(m_SceneLoader != null)
            {
                m_UpdateMgr.RemoveLoadingOperation(this);
                m_SceneLoader = null;
            }
        }
        internal void BeginOperation(AssetBundle assetBundle)
        {
            
            if (assetBundle == null) //load failed
            {
                m_UpdateMgr.RemoveLoadingOperation(this);
                if (OnSceneLoadComplete != null)
                    OnSceneLoadComplete(false);
                m_SceneLoader = null;
            }
            else
            {
                m_SceneLoader = CreateSceneLoader();
                m_SceneLoader.Begin(m_SceneInfo);
                m_UpdateMgr.AddLoadingOperation(this);
            }

        }

        SceneLoader CreateSceneLoader()
        {
            return new SceneLoader(m_Pattern == AssetBundleLoadingPattern.Simulation);
        }

        public void PollOperationResult()
        {
            if (m_SceneLoader == null)
            {
                m_UpdateMgr.RemoveLoadingOperation(this);
                return;
            }
            if (m_SceneLoader.IsDone())
            {
                if (OnSceneLoadComplete != null)
                    OnSceneLoadComplete(!m_SceneLoader.IsFailed());
                m_UpdateMgr.RemoveLoadingOperation(this);
                m_SceneLoader = null;
            }
        }

        public LoadOpType LoadOp
        {
            get { return LoadOpType.Scene; }
        }

        public override string ToString()
        {
            return string.Format("LoadScene:{0}", m_SceneInfo);
        }
    }
}
