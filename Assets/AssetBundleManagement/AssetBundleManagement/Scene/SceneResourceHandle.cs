using AssetBundleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetBundleManagement

{
    public class SceneResourceHandle : IBundleObserver
    {
        private AssetLoadState m_LoadState;
        internal AssetLoadState LoadState { get { return m_LoadState; } }
        internal NewSceneInfo LoadSceneInfo;
        private AssetBundleResourceHandle m_BundleResource;
        private SceneLoadOperation m_SceneLoadOperation;
        private IAssetLoadNotification m_AssetCallbacks;
        public BundleKey BundleKey  
        {
            get { return m_BundleResource.BundleKey;}
        }

        internal System.Action<SceneResourceHandle> SceneLoadCompleteNotify;

        public string AssetInfoString { get;private set; }

        internal SceneResourceHandle(NewSceneInfo loadSceneInfo,SceneLoadOperation sceneLoadOperation, IAssetLoadNotification assetNotifycation)
        {
            m_SceneLoadOperation = sceneLoadOperation;
            LoadSceneInfo = loadSceneInfo;
            m_LoadState = AssetLoadState.NotBegin;
            AssetInfoString = loadSceneInfo.Asset.ToString();
            m_SceneLoadOperation.OnSceneLoadComplete = OnSceneLoadCompleteCallback;
            m_AssetCallbacks = assetNotifycation;
        }

        internal void SetIsSync()
        {
            if(m_SceneLoadOperation != null)
                m_SceneLoadOperation.SetIsSync();
        }

        public AsyncOperation GetAsyncOperation()
        {
            if (LoadState == AssetLoadState.LoadingAssetBundle || LoadState == AssetLoadState.LoadingSelf)
            {
                return m_SceneLoadOperation.GetSceneOperation();
            }

            return null;
        }
        public override string ToString()
        {
            return LoadSceneInfo.ToString();
        }
        internal void BeginOperation(AssetBundle ab)
        {
        //    UnityEngine.Debug.Assert(m_LoadState < AssetLoadState.LoadingSelf, "m_AssetLoadState error");
            if (m_LoadState < AssetLoadState.LoadingSelf)
            {
                m_LoadState = AssetLoadState.LoadingSelf;
                m_SceneLoadOperation.BeginOperation(ab);
                m_AssetCallbacks.OnSceneLoadStart(this);
            }
           
        }
        //注意：场景可能会重复加载，要支持状态覆盖，控制好引用计数,
        internal void InitalizeChainAttachedBundleObject(AssetBundleResourceHandle bundleResource)
        {
            if(m_BundleResource != bundleResource)
            {
                if (m_BundleResource != null)
                    m_BundleResource.RemoveObserver(this);
                if(bundleResource != null)
                    bundleResource.AddObserver(this);
                m_BundleResource = bundleResource;
            }
            m_SceneLoadOperation.Restart();

            if (m_BundleResource == null)
            {
                m_LoadState = AssetLoadState.Failed;
                m_SceneLoadOperation.BeginOperation(null);
                return;
            }
            if (bundleResource.IsFullLoaded())
            {
                BeginOperation(bundleResource.GetAssetBundle());
            }
            else if (bundleResource.IsFaild())
            {
                BeginOperation(null);
            }
            else
            {
                m_LoadState = AssetLoadState.LoadingAssetBundle;
                //  bundleResource.BundleFullLoadCompleteAction += BeginOperation;
            }
           
        }

        public void OnBundleLoaded(AssetBundle bundle)
        {
            BeginOperation(bundle);
        }
        void OnSceneLoadCompleteCallback(bool isSucc)
        {
            if(isSucc)
            {
                m_LoadState = AssetLoadState.Loaded;
                m_AssetCallbacks.OnSceneLoadSucessCallback(this);
            }
            else
            {
                m_LoadState = AssetLoadState.Failed;
                if (m_BundleResource != null)
                {
                    m_BundleResource.RemoveObserver(this);
                    m_BundleResource = null;
                }
                m_AssetCallbacks.OnSceneLoadFailedCallback(this);
            }
            if (SceneLoadCompleteNotify != null)
            {
                SceneLoadCompleteNotify(this);
            }
        }

        internal void Destroy()
        {
            if (m_BundleResource != null)
            {
                m_BundleResource.RemoveObserver(this);
                m_BundleResource = null;

            }
        }
            
          
        public List<BundleKey> GetAllDependenceBundleKeys()
        {
            if(m_BundleResource != null)
              return  m_BundleResource.GetAllDependenceBundleKeys();
            return null;
        }
    }
}
