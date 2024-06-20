using AssetBundleManagement;
using AssetBundleManagement.Manifest;
using Common;
using Core.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using Utils.AssetManager;
using UnityEngine.SceneManagement;

namespace AssetBundleManagement
{
    internal class SceneLoadProvider
    {
        private readonly Dictionary<string, SceneResourceHandle> m_SceneResourceMap = new Dictionary<string, SceneResourceHandle>();

        private ResourceUpdater m_UpdateMgr;
        private ManiFestInitializer m_ManiFestProcider;
        private HashSet<AssetInfo> m_LoadingSceneList = new HashSet<AssetInfo>();
        private HashSet<AssetInfo> m_LoadFailedSceneList = new HashSet<AssetInfo>();


        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.SceneLoadProvider");

        public SceneLoadProvider(ResourceUpdater updateMgr)
        {
         //   SceneManager.sceneLoaded += LoadSceneHandler;
       //     SceneManager.sceneUnloaded += UnloadSceneHandler;
        }
        void LoadSceneHandler(Scene scene, LoadSceneMode loadedScene)
        {
           // m_LoadedSceneList.Add(scene);
        }
        void UnloadSceneHandler(Scene scene)
        {
           // m_LoadedSceneList.Remove(scene);
        }
        public SceneLoadProvider(ResourceUpdater updateMgr, ManiFestInitializer maniFestProcider)
        {
            m_ManiFestProcider = maniFestProcider;
            m_UpdateMgr = updateMgr;
        }
        void SceneLoadCompleteCallback(SceneResourceHandle sceneHandle)
        {
            m_LoadingSceneList.Remove(sceneHandle.LoadSceneInfo.Asset.NewToAsset());
            if(sceneHandle.LoadState == AssetLoadState.Failed)
            {
                m_LoadFailedSceneList.Add(sceneHandle.LoadSceneInfo.Asset.NewToAsset());
            }
        }
        internal void UnloadSceneResourceData(string sceneName)
        {
            SceneResourceHandle sceneResource;
            sceneName = AssetUtility.StandardSceneProviderKey(sceneName);
            if (m_SceneResourceMap.TryGetValue(sceneName,out sceneResource))
            {
                sceneResource.Destroy();
                m_SceneResourceMap.Remove(sceneName);
            }
            else
            {
                s_logger.ErrorFormat("Unload scene execption, {0} dont in data", sceneName);
            }
       
        }
        internal SceneResourceHandle CreateOrGetSceneResource(IAssetLoadNotification assetNotifycation, AssetInfo path, bool isAdditive, bool isAsync, LoadPriority priority = LoadPriority.P_MIDDLE)
        {
            SceneResourceHandle resourceHandle;

            var pathInfo = path.AssetName;
            pathInfo = AssetUtility.StandardSceneProviderKey(pathInfo);

            if (m_SceneResourceMap.TryGetValue(pathInfo, out resourceHandle))
            {
                s_logger.WarnFormat("scene path {0} already loaded before", path);
                return resourceHandle;
            }
            var sceneInfo = new NewSceneInfo()
            {
                Asset = path.ToNewAsset(),
                IsAdditive = isAdditive,
                LoadMode = isAsync ? SceneLoadMode.Async : SceneLoadMode.Sync,
                LoadPriority = priority
            };
            resourceHandle = new SceneResourceHandle(sceneInfo, new SceneLoadOperation(m_UpdateMgr, sceneInfo, m_ManiFestProcider.GetLoadingPattern(path.BundleName)), assetNotifycation);
            resourceHandle.SceneLoadCompleteNotify += SceneLoadCompleteCallback;
            m_SceneResourceMap.Add(pathInfo, resourceHandle);
            m_LoadingSceneList.Add(resourceHandle.LoadSceneInfo.Asset.NewToAsset());

            return resourceHandle;
        }
        internal void GetInspectorData(List<SceneInspectorData> sceneList)
        {
            sceneList.Clear();
            foreach (var item in m_SceneResourceMap.Values)
            {
                sceneList.Add(new SceneInspectorData()
                {
                    LoadState  =item.LoadState,
                    Name = item.AssetInfoString,
                    SceneInfo = item.LoadSceneInfo,
                    BundleKey =  item.BundleKey,
                    AllDependenceBundleKeys = item.GetAllDependenceBundleKeys(),
                });
            }
        }


        public AsyncOperation GetSceneLoadAsyncOperation(string sceneName)
        {
            SceneResourceHandle resourceHandle;
            sceneName = AssetUtility.StandardSceneProviderKey(sceneName);
            if (m_SceneResourceMap.TryGetValue(sceneName, out resourceHandle))
            {
                return resourceHandle.GetAsyncOperation();
            }
            s_logger.WarnFormat("{0} dont has load request yet",sceneName);
            return null;
        }
    }
}
