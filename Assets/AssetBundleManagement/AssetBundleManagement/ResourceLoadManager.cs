using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using AssetBundleManagement.Manifest;
using AssetBundleManager.Manifest;
using AssetBundleManager.Warehouse;
using Common;
using Core;
using Core.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.AssetManager;
using Utils.AssetManager.Initialize;
using Utils.SettingManager;
using XmlConfig.BootConfig;

namespace AssetBundleManagement
{
    public interface IAssetLoadNotification
    {
        void OnAssetReferenceChangedCallback(AssetResourceHandle resourceHandle);
        void OnAssetLoadSucessCallback(AssetResourceHandle resourceHandle);
        void OnAssetLoadFailedCallback(AssetResourceHandle resourceHandle);
        void OnAssetWillDestroyCallback(AssetResourceHandle resourceHandle);
        void OnAssetLoadWaitForBundle(AssetResourceHandle resourceHandle);
        void OnAssetLoadStart(AssetResourceHandle resourceHandle);
        void OnSceneLoadStart(SceneResourceHandle resourceHandle);
        void OnSceneLoadSucessCallback(SceneResourceHandle resourceHandle);
        void OnSceneLoadFailedCallback(SceneResourceHandle resourceHandle);
        void OnLoadAssetComplete(IResourceHandler handler,LoadResourceOption LoadOption);
    }
    public class ResourceLoadManager: IAssetLoadNotification
    {

        
        private readonly Dictionary<AssetInfo, AssetResourceHandle> m_AsseReosurceMap = new Dictionary<AssetInfo, AssetResourceHandle>(2048, AssetInfo.AssetInfoComparer.Instance);

        private readonly AssetBundleLoadProvider m_AbProvider;
        private readonly SceneLoadProvider m_SceneLoadProvider;
        private readonly ResourceUpdater m_UpdateMgr;
        private readonly ResourceEventMonitor m_EventMananger;
        private readonly ResourceRecorder m_ResourceRecorder;
        internal ResourceRecorder Recorder
        {
            get { return m_ResourceRecorder; }
        }
        private readonly ManiFestInitializer m_Manifest;
        private readonly IRecorderSaver m_RecorderSaver;

        public  IAssetBundleExternalAccess ExternalAccess
        {
            get { return m_Manifest; }
        }
        internal IResourceEventMonitor Event               { get { return m_EventMananger; } }
        internal IAssetCompletePoster  AssetCompletePoster { get { return m_UpdateMgr; }}
     //   private readonly ClientGameController m_starGameController;
     private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.ResourceLoadManager",false);
     private static LoggerAdapter s_logger_invalidPath = new LoggerAdapter("AssetBundleManagement.ResourcePath");
        public bool IsInitialized
        {
            get
            {
                return m_Manifest.ManiFestLoadState == ManiFestLoadState.Loaded;
            }

        }
        public bool CheckTableExistAsset(string assetName)
        {
            return m_AbProvider.CheckTableExistAsset(assetName);
        }
        public void Update()
        {
            if (!IsInitialized)
                return;
            m_ResourceRecorder.FrameReset();
            m_UpdateMgr.Update(m_AbProvider);
        }

        private int m_SceneCounter =0;
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ++m_SceneCounter;
            s_logger.InfoFormat("[Scene Monitor]Load Scene {0},currentScene is {1}", scene.name, m_SceneCounter);
        }
        void OnSceneUnLoaded(Scene scene)
        {
            --m_SceneCounter;
            s_logger.InfoFormat("[Scene Monitor]Unload Scene {0},currentScene is {1}", scene.name, m_SceneCounter);


        }
        public ResourceLoadManager()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnLoaded;

            m_Manifest = new ManiFestInitializer();
            m_EventMananger     = new ResourceEventMonitor();
            m_RecorderSaver     = new RecorderSaver();
            m_ResourceRecorder  = new ResourceRecorder(m_EventMananger, m_RecorderSaver);

            m_UpdateMgr         = new ResourceUpdater(m_ResourceRecorder);
            m_AbProvider        = new AssetBundleLoadProvider(m_UpdateMgr, m_Manifest, m_EventMananger);
            m_SceneLoadProvider = new SceneLoadProvider(m_UpdateMgr, m_Manifest);

        }
        public IEnumerator Init(bool isLQ,ResourceConfig config, ICoRoutineManager coRoutineManager, bool isServer ,bool matI )
        {
            if (m_Manifest.ManiFestLoadState == ManiFestLoadState.NotBegin )
            {
                s_logger.InfoFormat("ResourceLoadManager init:IsServer:{0} ,isLQ:{1}", isServer, isLQ);

                yield return coRoutineManager.StartCoRoutine(Init(config, isServer,
                    !isServer && isLQ));
            }
            else
            {
                s_logger.ErrorFormat("ResourceLoadManager Init is {0},do nothing",m_Manifest.ManiFestLoadState);
            }
         
            yield return null;

        }

        public void SaveRecorder(string key = "")
        {
            if(m_ResourceRecorder.GetRecord())
                m_RecorderSaver.Save(key);
        }

        public void StopRecorder()
        {
            m_ResourceRecorder.SetRecord(false);
        }

        private List<PersistentHandle> m_PersistentHandles;
        IEnumerator LoadPesistentBundles()
        {
            if (m_PersistentHandles != null)
                yield return null;
            m_PersistentHandles = new List<PersistentHandle>();
            foreach (var bundle in ResourceConfigure.PersistentUsedBundleList)
            {
                var bundleHandle = m_AbProvider.LoadAssetBundleAsync(new AssetInfo(bundle, "empty"));
                var persisHandle = new PersistentHandle();
                bundleHandle.AddObserver(persisHandle);
                m_PersistentHandles.Add(persisHandle);
            }
            while (true)
            {
                foreach (var item in m_PersistentHandles)
                {
                    if (!item.IsDone)
                    {
                        yield return null;
                    }
                }
                break;
            }
        }
        private IEnumerator Init(ResourceConfig config, bool isServer, bool isLow)
        {
            UnityAssetBundleManifest.IsServer = isServer;
            ResourceConfigure.IsServer        = isServer;
            m_Manifest.ManiFestLoadState      = ManiFestLoadState.Loading;
            {
                IAssetBundleManifest imanifest = null;
                if (config.BasePattern == AssetBundleLoadingPattern.Simulation)
                    imanifest = ManifestInitializeHelper.GetSimulationAssetBundleManifest(config.Supplement);
                else if (config.BasePattern == AssetBundleLoadingPattern.AsyncLocal)
                    yield return ManifestInitializeHelper.GetUnityAssetBundleManifest((manifest) => { imanifest = manifest; }, config);
                else
                    throw new Exception("Un excpeted pattern");
                if (imanifest == null)
                {
                    m_Manifest.ManiFestLoadState = ManiFestLoadState.Failed;
                    s_logger.InfoFormat(
                        "******************************* ResourceLoadManager Manifest init fail *******************************");
                    yield break;
                }
                s_logger.InfoFormat("Manifest Loaded");
                InitWarehouse(config, isLow, imanifest);
                m_Manifest.ManiFestLoadState = ManiFestLoadState.Loaded;
                s_logger.InfoFormat("Warehouse Initialized");
            }
            yield return LoadPesistentBundles();
            s_logger.InfoFormat("******************************* ResourceLoadManager init over, state: {0} *******************************", IsInitialized);
        }
        private void InitWarehouse(ResourceConfig config, bool isLow, IAssetBundleManifest manifest)
        {
            var defaultWarehouseAddr = new AssetBundleWarehouseAddr()
            {
                LocalPath  = config.LocalPath,
                WebUrl     = config.BaseUrl,
                Pattern    = config.BasePattern,
                Manifest   = config.Manifests,
                Supplement = config.Supplement != null?config.Supplement: new List<string>(),
            };
            m_Manifest.Init(defaultWarehouseAddr,isLow, manifest);
        }

        public void LoadScene(AssetInfo path, bool isAdditive, bool isAsync,
                              LoadPriority priority , bool lqMatched)
        {
            if (!path.IsValid())
            {
                s_logger_invalidPath.ErrorFormat("scene path {0} is not valid", path);
                return;
            }
            var sceneHandle = m_SceneLoadProvider.CreateOrGetSceneResource(this, path, isAdditive, isAsync, priority);
            var sceneLoadTask = ResourceObjectHolder<SceneLoadTask>.Allocate();
            sceneLoadTask.LoadSceneInfo = sceneHandle.LoadSceneInfo;
            sceneLoadTask.SceneHandle = sceneHandle;
            m_UpdateMgr.AddWaitLoadSceneTask(sceneLoadTask);
            FireSceneAddEvent(sceneHandle, ResourceEventType.AddQueueStart, priority, isAsync);

        }
        public IEnumerator YiledUnloadScene(string sceneName)
        {
            s_logger.InfoFormat("Start UnloadScene {0} Yiled", sceneName);
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
          
                m_SceneLoadProvider.UnloadSceneResourceData(sceneName);
                var tmpLoading = SceneManager.UnloadSceneAsync(sceneName);
                yield return tmpLoading;
            }
            else
            {
                s_logger.ErrorFormat("UnloadScene is not loaded {0}", sceneName);
            }
        }
        public void UnloadScene(string sceneName)
        {
            s_logger.InfoFormat("Start UnloadScene {0}", sceneName);
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                try
                {
                    m_SceneLoadProvider.UnloadSceneResourceData(sceneName);
                    SceneManager.UnloadSceneAsync(sceneName);
                }
                catch (Exception e)
                {
                    s_logger.ErrorFormat("UnloadScene execption {0},{1},{2}",sceneName,e.Message, e.StackTrace);
                }
             
            }
            else
            {
                s_logger.ErrorFormat("UnloadScene is not loaded {0}", sceneName);
            }

        }

        public void LoadSortableAsset(AssetInfo path, ISortableJob job, ref LoadResourceOption loadOption)
        {
            if (loadOption.Owner == null)
                AssetUtility.ThrowException("Owner should not be null");

            if (path.IsValid())
            {
                AssetResourceHandle objectResource = GetAssetResourceHandle(path, false);
                var loadTask = objectResource.HandleAssetSortableObjectLoadRequest(job, ref loadOption);
                if (loadTask != null)
                {
                    m_UpdateMgr.AddWaitLoadAssetTask(loadTask);
                    FireAssetAddEvent(objectResource, ResourceEventType.AddQueueStart, loadTask.Priority, true, RecordAssetType.SortableAsset);
                }
            }
            else
            {

                //s_logger.ErrorFormat("SortableAsset path {0} is not valid", path);
                OnLoadAssetComplete(new FailedObjectHandler(path), loadOption);
                //   loadOption.CallOnFailed(path);

            }
        }
        
        public void LoadAsset(AssetInfo path, bool isSync, ref LoadResourceOption loadOption)
        {
            if(loadOption.Owner == null)
                AssetUtility.ThrowException("Owner should not be null");
            if (path.IsValid())
            {
              //  ResourceProfiler.RequestAsset(ref path);
                AssetResourceHandle objectResource = GetAssetResourceHandle(path, isSync);
                AssetLoadTask loadTask = objectResource.HandleAssetObjectLoadRequest(ref loadOption);

                if (loadTask != null)
                {
                    m_UpdateMgr.AddWaitLoadAssetTask(loadTask);
                    FireAssetAddEvent(objectResource, ResourceEventType.AddQueueStart, loadTask.Priority, !isSync, RecordAssetType.Asset);
                }
            }
            else
            {

                s_logger_invalidPath.ErrorFormat("Asset path {0} is not valid", path);
                OnLoadAssetComplete(new FailedObjectHandler(path),loadOption);
             //   loadOption.CallOnFailed(path);

            }
        }

        private AssetResourceHandle GetAssetResourceHandle(AssetInfo path, bool isSync)
        {
            AssetResourceHandle objectResource;
            if (!m_AsseReosurceMap.TryGetValue(path, out objectResource))
            {
                var loader = m_Manifest.FindWarehouse(path.BundleName).CreateNewAssetLoader();
                objectResource = new AssetResourceHandle(new AssetLoadOperation(m_UpdateMgr, path,
                    loader, isSync), path, this);
                m_AsseReosurceMap.Add(path, objectResource);
            }
            return objectResource;
        }

        void FireSceneAddEvent(SceneResourceHandle resourceHandle, ResourceEventType eventType, LoadPriority priority, bool isSync)
        {
            var assetEvent = ResourceObjectHolder<SceneRelatedEventArgs>.Allocate();
            assetEvent.EventType = eventType;
            assetEvent.Scene = resourceHandle;
            assetEvent.Priority = priority;
            assetEvent.IsAync = isSync;
            m_EventMananger.FireEvent(assetEvent);
        }

        void FireAssetAddEvent(AssetResourceHandle resourceHandle, ResourceEventType eventType, LoadPriority priority, bool isAsync, RecordAssetType assetType)
        {
            var assetEvent = ResourceObjectHolder<AssetRelatedEventArgs>.Allocate();
            assetEvent.EventType = eventType;
            assetEvent.Asset = resourceHandle;
            assetEvent.IsAync = isAsync;
            assetEvent.RecordAssetType = assetType;
            assetEvent.Priority = priority;
            m_EventMananger.FireEvent(assetEvent);
        }
        void FireAssetEvent(SceneResourceHandle resourceHandle, ResourceEventType eventType)
        {
            var assetEvent = ResourceObjectHolder<SceneRelatedEventArgs>.Allocate();
            assetEvent.EventType = eventType;
            assetEvent.Scene = resourceHandle;
            m_EventMananger.FireEvent(assetEvent);
        }
        void FireAssetEvent(AssetResourceHandle resourceHandle,ResourceEventType eventType)
        {
            var assetEvent = ResourceObjectHolder<AssetRelatedEventArgs>.Allocate();
            assetEvent.EventType = eventType;
            assetEvent.Asset = resourceHandle;
            m_EventMananger.FireEvent(assetEvent);
        }
        void IAssetLoadNotification.OnAssetLoadSucessCallback(AssetResourceHandle resourceHandle)
        {
            FireAssetEvent(resourceHandle, ResourceEventType.LoadAssetSucess );
        }
        void IAssetLoadNotification.OnAssetLoadFailedCallback(AssetResourceHandle resourceHandle)
        {
            FireAssetEvent(resourceHandle, ResourceEventType.LoadAssetFailed);
        }

        void IAssetLoadNotification.OnSceneLoadSucessCallback(SceneResourceHandle resourceHandle)
        {
            FireAssetEvent(resourceHandle, ResourceEventType.LoadSceneSucess);
        }

        void IAssetLoadNotification.OnSceneLoadFailedCallback(SceneResourceHandle resourceHandle)
        {
            FireAssetEvent(resourceHandle, ResourceEventType.LoadSceneFailed);
        }

        void IAssetLoadNotification.OnAssetReferenceChangedCallback(AssetResourceHandle resourceHandle)
        {
            m_UpdateMgr.InspectResourceDisposeState(resourceHandle);
        }

        void IAssetLoadNotification.OnAssetWillDestroyCallback(AssetResourceHandle resourceHandle)
        {
            m_AsseReosurceMap.Remove(resourceHandle.AssetKey);
            FireAssetEvent(resourceHandle, ResourceEventType.WillDestroyAsset);
        }
        void IAssetLoadNotification.OnAssetLoadWaitForBundle(AssetResourceHandle resourceHandle)
        {
            FireAssetEvent(resourceHandle, ResourceEventType.LoadAssetWaitForBundle);

        }
        void IAssetLoadNotification.OnSceneLoadStart(SceneResourceHandle resourceHandle)
        {
            FireAssetEvent(resourceHandle, ResourceEventType.LoadSceneStart);
        }
        void IAssetLoadNotification.OnAssetLoadStart(AssetResourceHandle resourceHandle)
        {
            FireAssetEvent(resourceHandle, ResourceEventType.LoadAssetStart);
        }

        public void OnLoadAssetComplete(IResourceHandler handler, LoadResourceOption LoadOption)
        {
            var assetEvent = ResourceObjectHolder<LoadAssetOptionCompleteEventArgs>.Allocate();
            assetEvent.EventType  = ResourceEventType.LoadAssetOptionComplete;
            assetEvent.Handled    = false;
            assetEvent.WillDispose    = false;
            assetEvent.LoadResult = handler;
            assetEvent.LoadOption = LoadOption;
            m_EventMananger.FireEvent(assetEvent);
        }

        internal void DestroyUnusedAssetsImmediately()
        {
            ResourceConfigure.ImmediatelyDestroy = true;
            Update();
            Update();
            Update();
            ResourceConfigure.ImmediatelyDestroy = false;

        }
        
        //大厅和战斗内用同一套，owner判断表示哪个provider
        public void ClearWaitLoadAssetTask(ILoadOwner owner)
        {
            var infos= m_UpdateMgr.ClearWaitLoadAssetTask(owner);
            for (int i = 0; i < infos.Count; i++)
            {
                m_AsseReosurceMap.Remove(infos[i]);
            }
            s_logger.InfoFormat("ClearWaitLoadAssetTask {0}", infos.Count);
        }

        #region profiler
   
        internal void SetResourceReference(ProfileDataAdapter profileDataAdapter)
        {
            profileDataAdapter.InitAsseResourceMap(m_AsseReosurceMap);
            m_AbProvider.InitSetBundleData(profileDataAdapter);

        }

        public StringBuilder GetRemainsAndFailed()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-----------####----RemainsAndFailed----####-------------------");
            m_UpdateMgr.GetUpdaterAllInfos(sb);
            return sb;
        }

        public void CollectResourceDetailData(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("----------------------CollectResourceDetailData--------------------");
            GetAssetsAllInfo(stringBuilder);
            m_AbProvider.GetProviderAllInfo(stringBuilder);
            m_UpdateMgr.GetUpdaterAllInfos(stringBuilder);
            ResourcePoolContainer.GetPoolInfos(stringBuilder);
        }

        public void SaveResourceDetailData()
        {
            foreach (var pair in m_AsseReosurceMap)
            {
                m_RecorderSaver.AddAssetResHandle(pair.Value);
            }

            foreach (var pair in m_AbProvider.GetAssetBundleReosurceMap())
            {
                m_RecorderSaver.AddAssetBundleHandle(pair.Value);
            }
        }

        public void CollectResourceInspectorData(List<BundleInspectorData> datas)
        {
            m_AbProvider.GetInspectorData(datas);
        }
        public void CollectResourceInspectorData(List<SceneInspectorData> datas)
        {
            m_SceneLoadProvider.GetInspectorData(datas);
        }
        public void CollectResourceInspectorData(List<AssetInspectorData> datas)
        {
            datas.Clear();
            foreach (var item in m_AsseReosurceMap.Values)
            {
                datas.Add(new AssetInspectorData()
                {
                    LoadState = item.GetOutputState(),
                    WaitDestroyStart = item.WaitDestroyStartTime,
                    Name = item.AssetInfoString,
                    Reference = item.ReferenceCount,
                    BundleKey = item.BundleKey,
                    AssetInfo = item.AssetKey,
                    AllDependenceBundleKeys = item.GetAllDependenceBundleKeys(),
                });
            }
        }
        public ResourceManangerStatus GetResourceStatus()
        {
            ResourceManangerStatus status = new ResourceManangerStatus();
            int sum = 0;
            foreach (var pair in m_AsseReosurceMap.Values)
            {
                sum += pair.ReferenceCount;
                if (pair.LoadState == AssetLoadState.Failed)
                {
                    status.AllAssetFailedCont++;
                }
                else
                {
                    status.AllAssetSucessCont++;
                }
            }

            status.AllAssetHoldReference = sum;
            m_AbProvider.GetResourceStatus(ref status);
            m_UpdateMgr.GetResourceStatus(ref status);
            return status;
        }
        void GetAssetsAllInfo(StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("====>Assets LoadData,Size:{0}\n", m_AsseReosurceMap.Count);
            foreach (var pair in m_AsseReosurceMap)
            {
                stringBuilder.AppendFormat("    [AssetItem]{0}\n", pair.Value.ToStringDetail());
            }
        }

      
        #endregion

        public AsyncOperation GetSceneLoadAsyncOperation(string sceneName)
        {
            return m_SceneLoadProvider.GetSceneLoadAsyncOperation(sceneName);
        }

        public void ForceUnloadAsset(AssetInfo assetInfo,Type assetType)
        {
            AssetResourceHandle assetResourceHandle;
            if (m_AsseReosurceMap.TryGetValue(assetInfo, out assetResourceHandle))
            {
                if (assetResourceHandle.AssetLoadState == AssetLoadState.Loaded)
                {
                    if (assetResourceHandle.GetAssetObjectType() == assetType)
                    {
                        assetResourceHandle.InternalDestroy(true);
                       // s_logger.InfoFormat("ForceUnloadAsset {0} sucess", assetInfo);
                    }
                    else
                    {
                        s_logger.ErrorFormat("ForceUnloadAsset {0} failed,asset type dont match", assetInfo);
                    }

                 
                }
                else
                {
                    s_logger.ErrorFormat("ForceUnloadAsset {0} failed, asset state is: {1}",assetInfo,assetResourceHandle.AssetLoadState);
                }
            }
            else
            {
                s_logger.ErrorFormat("unload asset dont exist:"+assetInfo);
            }
        }

        public AssetLoadState QueryAsesetLoadState(AssetInfo asetInfo)
        {
            AssetResourceHandle assetResourceHandle;
            if (m_AsseReosurceMap.TryGetValue(asetInfo, out assetResourceHandle))
            {
               return assetResourceHandle.LoadState;
            }

            return AssetLoadState.NotBegin;
        }

        public AssetBundleOutputState QueryBundleLoadState(string bundleName)
        {
            return m_AbProvider.QueryBundleLoadState(bundleName);
        }

        public void LoadRequestImmediatelySync(AssetInfo assetInfo)
        {
            m_UpdateMgr.LoadRequestImmediatelySync(assetInfo,m_AbProvider);
        }

        public void SetAssetLockState(AssetInfo assetInfo,bool val)
        {
            AssetResourceHandle resourceHandle;
            if (m_AsseReosurceMap.TryGetValue(assetInfo, out resourceHandle))
            {
                resourceHandle.SetLocked(val);
            }
            else
            {
               s_logger.ErrorFormat("{0} resource dont exist or has been disposed",assetInfo);   
            }
        }


    }
}