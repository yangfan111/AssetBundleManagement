using System;
using System.Collections;
using System.Text;
using AssetBundleManagement.ObjectPool;
using UnityEngine;
using System.Collections.Generic;
using System.Data;
using System.IO;
//using UnityEngine.Assertions;
using Common;
using Core;
using Core.Utils;
using Utils.SettingManager;
using XmlConfig.BootConfig;
using Utils.AssetManager;
//using static ArtPlugins.CharaGen;

namespace AssetBundleManagement
{
    public interface IResourceProvider
    {
        //异步加载asset
        void LoadAssetAsync(AssetInfo path, LoadResourceOption loadOption);
        void LoadAssetAsync(AssetInfo path, OnLoadResourceComplete loadOption);
        void LoadAssetAsync(AssetInfo path, OnLoadResourceComplete loadOption, LoadPriority priority);
        void LoadSortableAssetAsync(AssetInfo path, ISortableJob job, LoadResourceOption loadOption);

        //同步加载asset,会直接返回asset原始资源,如果该资源需要被实例化需要通过InstantiateAsync接口加载
        void LoadAssetSync(AssetInfo path, LoadResourceOption loadOption);

        //批量加载assets
        BatchLoadResourceResult LoadBatchResources(BatchLoadResourceRequest request);
        void                    LoadBatchResources(BatchLoadResourceRequest request, BatchLoadResourceResult result);

        void LoadSortableBatchResources(BatchLoadResourceRequest request, BatchLoadResourceResult result,
                                        ISortableJob job);

        //加载实例化资源，原始资源加载后会通过Object.Instantiate返回实例化对象
        void InstantiateAsync(AssetInfo path, LoadResourceOption loadOption, Transform parent = null);
        void InstantiateAsync(AssetInfo path, OnLoadResourceComplete loadOption, Transform parent = null);

        void InstantiateSortableAsync(AssetInfo path, ISortableJob job, LoadResourceOption loadOption);

        void InstantiateSync(AssetInfo path, LoadResourceOption loadOption, Transform parent = null);

        //设置实例化对象池卸载参数:没有引用后持续多久时间卸载对象池
        void SetInstancePoolRetainTime(AssetInfo assetInfo, int retain);

        //设置实例化对象池卸载参数:是否禁止卸载对象池
        void SetInstancePoolForbidenUnload(AssetInfo assetInfo, bool forbidUnload);
        //预加载asset
        //  void PreloadAsset(AssetInfo path, LoadPriority loadPriority, OnPreloadComplete complete = null);

        //预加载单个资源,如果是gameobject资源加载完之后会自动在对象池创建一个
        void PreloadResource(AssetInfo path, LoadPriority loadPriority, OnPreloadComplete complete = null);

        //预加载批量资源
        void PreloadResources(BatchLoadResourceRequest requests);

        //加载场景
        void LoadScene(AssetInfo path, bool isAdditive, bool isAsync, LoadPriority priority = LoadPriority.P_MIDDLE,bool lqMatched = false);

        //协程异步卸载场景
        IEnumerator YiledUnloadScene(string sceneName);

        //异步卸载场景
        void UnloadScene(string sceneName);

        //  IResourceEventMonitor EventMonitor { get; }
        //提供一个直接卸载Asset的调用，主要是适配配置TextAsset的卸载，先只支持TextAsset类型Object
        void ForceUnloadTextAsset(AssetInfo assetInfo);
        void ForceUnloadTexture(AssetInfo assetInfo);

        //DestroyList
        void AddToDetroyList(GameObject parentObj);

        //查询Scene加载
        AsyncOperation GetSceneLoadAsyncOperation(string sceneName);

        //查询asset,bundle加载状态 
        AssetLoadState         QueryAsesetLoadState(AssetInfo asetInfo);
        AssetBundleOutputState QueryBundleLoadState(string bundleName);

        void LoadRequestImmediatelySync(AssetInfo assetInfo);

        //单独为之前适配一个查找tables资源的功能
        bool CheckTableExistAsset(string assetName);

        void SetAssetLockState(AssetInfo assetInfo, bool value);

        void SafeRelease(BatchLoadResourceResult resourceResult);
    }

    public interface ILoadOwner
    {
        ILoadOwner GetRootOwner();
    }

    public class ResourceProvider : IResourceProvider, ILoadOwner
    {
        /*
      

     
        public static void ReleaseBattleProvider()
        {
            if (Battle != null)
            {
                Battle.Destroy();
                Battle = null;
                s_logger.InfoFormat("ReleaseBattleProvider");

            }
        }

    public static ResourceProvider Hall { get; private set; }
    
    public static void InitHallProvider()
    {
        if(Hall != null)
            Hall.Destroy();
        Hall = new ResourceProvider("hall");
        s_logger.InfoFormat("InitHallProvider");
    }
*/
        static ResourceProvider()
        {
            s_ResourceMananger = new ResourceLoadManager();
            Profiler           = new ResourceProfiler(s_ResourceMananger);
        }

        public static readonly ResourceProfiler Profiler;

        private static ResourceLoadManager s_ResourceMananger;

        private InstanceObjectPoolContainer m_DefaultObjectPool;

        //     private InstanceObjectPoolContainer m_CustomObjectPool;
        private BatchResourcesLoader m_BatchLoader;
        private PreloadResourceLoader m_PreLoader;
        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.ResourceProvider");
        private string m_Name;
        private List<AssetObjectHandler> _refHandlers = new List<AssetObjectHandler>();
        
        public static IAssetBundleExternalAccess ExternalAccess
        {
            get { return s_ResourceMananger.ExternalAccess; }
        } 

        public ResourceProvider(string poolName)
        {
            s_logger.InfoFormat("Resource Provider {0} is created ", m_Name);
            m_Name              = poolName;
            m_DefaultObjectPool = new InstanceObjectPoolContainer(this, s_ResourceMananger, poolName);
            //     m_CustomObjectPool  = new InstanceObjectPoolContainer(s_ResourceMananger, poolName +"_custom");
            //   m_DefaultObjectPool.SetInstancePoolToProfileData(Profiler.ProfileDataAdapter);
            m_PreLoader   = new PreloadResourceLoader(this, s_ResourceMananger, m_DefaultObjectPool);
            m_BatchLoader = new BatchResourcesLoader(this, s_ResourceMananger, m_DefaultObjectPool);
            s_ResourceMananger.Event.Register(ResourceEventType.LoadAssetOptionComplete,
                DefaultHandleAssetLoadOptionComplete);
        }

        public static List<AssetInfo> GetFailedInfos()
        {
            if (s_ResourceMananger != null)
                return s_ResourceMananger.Recorder.FailedLoadAssets;
            return new List<AssetInfo>();
        }

        public static string GetRemainsAndFailed()
        {
            if(s_ResourceMananger != null)
                return s_ResourceMananger.GetRemainsAndFailed().ToString();
            return "";
        }

        public void SaveRecorder(string key = "")
        {
            s_ResourceMananger.SaveRecorder(key);
        }

        public void StopRecorder()
        {
            s_ResourceMananger.StopRecorder();
        }
        public bool CheckTableExistAsset(string assetName)
        {
            return s_ResourceMananger.CheckTableExistAsset(assetName);
          
        }

        public void SetAssetLockState(AssetInfo assetInfo,bool val)
        {
            s_ResourceMananger.SetAssetLockState(assetInfo,val);
        }

        private List<GameObject> m_DestroyList = new List<GameObject>();

        public void ForceUnloadTextAsset(AssetInfo assetInfo)
        {
            s_ResourceMananger.ForceUnloadAsset(assetInfo, typeof(TextAsset));
        }
        public void ForceUnloadTexture(AssetInfo assetInfo)
        {
            s_ResourceMananger.ForceUnloadAsset(assetInfo, typeof(Texture2D));
        }

        public void AddToDetroyList(GameObject go)
        {
            m_DestroyList.Add(go);
        }

        private void UpdateDestroyList()
        {
            foreach (var obj in m_DestroyList)
            {
                UnityEngine.Object.Destroy(obj);
            }

            m_DestroyList.Clear();
        }

        public AsyncOperation GetSceneLoadAsyncOperation(string sceneName)
        {
            return s_ResourceMananger.GetSceneLoadAsyncOperation(sceneName);
        }

        public AssetLoadState QueryAsesetLoadState(AssetInfo asetInfo)
        {
            return s_ResourceMananger.QueryAsesetLoadState(asetInfo);
        }

        public AssetBundleOutputState QueryBundleLoadState(string bundleName)
        {
            return s_ResourceMananger.QueryBundleLoadState(bundleName);
        }

        public void LoadRequestImmediatelySync(AssetInfo assetInfo)
        {
            s_ResourceMananger.LoadRequestImmediatelySync(assetInfo);
        }

        public static IEnumerator InitResourceManangerInit(ResourceConfig config, ICoRoutineManager coRoutineManager,
                                                           bool isServer, bool matI, bool isLQ)
        {
            yield return s_ResourceMananger.Init(isLQ, config, coRoutineManager, isServer, matI);
            s_logger.InfoFormat("ResourceMananger Init once,IsInitialized:{0} ", s_ResourceMananger.IsInitialized);
        }

        //     public IResourceEventMonitor EventMonitor { get { return s_ResourceMananger.Event; } }
        internal IAssetCompletePoster AssetCompletePoster
        {
            get { return s_ResourceMananger.AssetCompletePoster; }
        }

        public void Update()
        {
            if (s_ResourceMananger.IsInitialized)
            {
                s_ResourceMananger.Update();
                m_DefaultObjectPool.Update();
                // m_CustomObjectPool.Update();
            }

            UpdateDestroyList();
        }


        public void PreloadResource(AssetInfo path, LoadPriority loadPriority, OnPreloadComplete complete = null)
        {
            m_PreLoader.PreloadResource(path, loadPriority, complete);
        }

        //异步加载asset
        public void LoadAssetAsync(AssetInfo path, OnLoadResourceComplete loadOption, LoadPriority priority)
        {
            LoadAssetAsync(path, new LoadResourceOption(loadOption, priority));
        }

        public void LoadAssetAsync(AssetInfo path, OnLoadResourceComplete loadOption)
        {
            LoadAssetAsync(path, new LoadResourceOption(loadOption));
        }

        //可排序
        public void LoadSortableAssetAsync(AssetInfo path, ISortableJob job, LoadResourceOption loadOption)
        {
            loadOption.Owner = this;
            s_ResourceMananger.LoadSortableAsset(path, job, ref loadOption);
        }

        public void LoadAssetAsync(AssetInfo path, LoadResourceOption loadOption)
        {
            loadOption.Owner = this;
            s_ResourceMananger.LoadAsset(path, false, ref loadOption);
        }

        //同步加载asset
        public void LoadAssetSync(AssetInfo path, LoadResourceOption loadOption)
        {
            loadOption.Owner = this;
            s_ResourceMananger.LoadAsset(path, true, ref loadOption);
        }

        public void PreloadResources(BatchLoadResourceRequest requests)
        {
            foreach (var pair in requests.RequestAssets)
            {
                PreloadResource(pair.Value.Asset, requests.DefaultPriority);
            }

            requests.Release();
        }

        void DefaultHandleAssetLoadOptionComplete(ResourceEventArgs eventArgs)
        {
            LoadAssetOptionCompleteEventArgs loadAssetOptionCompleteEvent =
                            eventArgs as LoadAssetOptionCompleteEventArgs;
            if (loadAssetOptionCompleteEvent.LoadOption.Owner.GetRootOwner() == this)
            {
                var res = loadAssetOptionCompleteEvent.LoadResult;
                if (res != null && res.HandlerType == ResourceHandlerType.Asset)
                {
                    _refHandlers.Add((AssetObjectHandler)res);
                }
            }

            if (loadAssetOptionCompleteEvent.LoadOption.Owner == this)
            {
                loadAssetOptionCompleteEvent.Handled = true;
                DoHandleLoadAssetComplete(AssetCompletePoster, loadAssetOptionCompleteEvent, m_DefaultObjectPool,
                    false);
            }
        }

        internal static void DoHandleLoadAssetComplete(IAssetCompletePoster poster,
                                                       LoadAssetOptionCompleteEventArgs loadAssetOptionCompleteEvent,
                                                       InstanceObjectPoolContainer poolContainer,
                                                       bool ignoreWarning = true)
        {
            //do action
            if (loadAssetOptionCompleteEvent.IsAssetHandler && poolContainer != null &&
                loadAssetOptionCompleteEvent.LoadResult.AsGameObject != null)
            {
                //检测如果是GameObject类型&&通过LoadAssetAsync接口调用，需要再通过对象池给它一个实例化对象，gameobject类型对象一定要走对象池
                if (!ignoreWarning)
                {
                    s_logger.WarnFormat("{0} is gameobject asset,should use InstantiateAsync instead of LoadAssetAsync",
                        loadAssetOptionCompleteEvent.LoadAsset);
                }

                var loadAssetPath = loadAssetOptionCompleteEvent.LoadResult.AssetKey;
                loadAssetOptionCompleteEvent.LoadResult.Release();
                poolContainer.Instantiate(loadAssetPath, ref loadAssetOptionCompleteEvent.LoadOption);
            }
            else
            {
                loadAssetOptionCompleteEvent.ProcessAssetComplete(poster);
            }
        }

        //批量加载assets
        public BatchLoadResourceResult LoadBatchResources(BatchLoadResourceRequest request)
        {
            if (request.IsRecyclable)
                throw new Exception(
                    "IsRecyclable BatchRequest must get BatchLoadResourceResult from BatchResourcesLoader");
            if (request.IsRequestValid())
            {
                return m_BatchLoader.LoadBatchAssets(request);
            }
            else
            {
                //s_logger.ErrorFormat("BatchLoadResourceRequest is not valid");
            }

            return BatchLoadResourceResult.Alloc();
        }

        public void SafeRelease(BatchLoadResourceResult resourceResult)
        {
            m_BatchLoader.CancelBatchResults(resourceResult);
            resourceResult.Recycle();
            resourceResult.Reset();
        }
        public void LoadSortableBatchResources(BatchLoadResourceRequest request, BatchLoadResourceResult result,
                                               ISortableJob job)
        {
            if (result == null)
            {
                s_logger.ErrorFormat("BatchLoadResourceResult is null");
            }

            if (request.IsRequestValid())
            {
                m_BatchLoader.LoadBatchAssets(request, result, job);
            }
            else
            {
                s_logger.ErrorFormat("BatchLoadResourceRequest is not valid");
            }
        }

        public void LoadBatchResources(BatchLoadResourceRequest request, BatchLoadResourceResult result)
        {
            if (result == null)
            {
                s_logger.ErrorFormat("BatchLoadResourceResult is null");
            }

            if (request.IsRequestValid())
            {
                m_BatchLoader.LoadBatchAssets(request, result);
            }
            else
            {
                s_logger.ErrorFormat("BatchLoadResourceRequest is not valid");
            }
        }

        public void InstantiateSortableAsync(AssetInfo path, ISortableJob job, LoadResourceOption loadOption)
        {
            if (path.IsValid())
            {
                loadOption.Owner = this;
                //   loadOption.OnGameObjectAssetLoaded = null;
                m_DefaultObjectPool.Instantiate(path, ref loadOption, null, job);
            }
            else
            {
                s_logger.ErrorFormat("Instantiate path {0} is not valid", path);
            }
        }

        //加载实例化资源
        public void InstantiateAsync(AssetInfo path, LoadResourceOption loadOption, Transform parent = null)
        {
            if (path.IsValid())
            {
                loadOption.Owner = this;
                //   loadOption.OnGameObjectAssetLoaded = null;
                m_DefaultObjectPool.Instantiate(path, ref loadOption, parent);
            }
            else
            {
                s_logger.ErrorFormat("Instantiate path {0} is not valid", path);
            }
        }

        public void InstantiateAsync(AssetInfo path, OnLoadResourceComplete loadOption, Transform parent = null)
        {
            InstantiateAsync(path, new LoadResourceOption(loadOption), parent);
        }

        public void InstantiateSync(AssetInfo path, LoadResourceOption loadOption, Transform parent = null)
        {
            if (path.IsValid())
            {
                loadOption.Owner = this;
                //   loadOption.OnGameObjectAssetLoaded = null;
                m_DefaultObjectPool.Instantiate(path, ref loadOption, parent, null, true);
            }
            else
            {
                s_logger.ErrorFormat("Instantiate path {0} is not valid", path);
            }
        }

        public void SetInstancePoolRetainTime(AssetInfo assetInfo, int retain)
        {
            m_DefaultObjectPool.SetInstancePoolRetainTime(assetInfo, retain);
        }

        public void SetInstancePoolForbidenUnload(AssetInfo assetInfo, bool forbidUnload)
        {
            m_DefaultObjectPool.SetInstancePoolForbidenUnload(assetInfo, forbidUnload);
        }

        //加载场景
        public void LoadScene(AssetInfo path, bool isAdditive, bool isAsync,
                              LoadPriority priority = LoadPriority.P_MIDDLE,bool lqMatched= false)
        {
            if (lqMatched)
            {
                string previousPath = path.BundleName; 
                path.BundleName = ExternalAccess.GetBundleNameWithVariant(path.BundleName);
                if(previousPath !=path.BundleName)
                    s_logger.InfoFormat("SwitchScene bundle path from {0}->{1}",previousPath, path.BundleName);
            }
            s_ResourceMananger.LoadScene(path, isAdditive, isAsync, priority,lqMatched);
        }

        //协程异步卸载场景
        public IEnumerator YiledUnloadScene(string sceneName)
        {
            yield return s_ResourceMananger.YiledUnloadScene(sceneName);
        }

        //卸载场景
        public void UnloadScene(string sceneName)
        {
            s_ResourceMananger.UnloadScene(sceneName);
        }


        public void DestroyUnusedAssetsImmediately()
        {
            s_ResourceMananger.DestroyUnusedAssetsImmediately();
        }

        public void HallDestroy()
        {
            GMVariable.LogMemoryDetail("CleanHallBefore");

            ResourceConfigure.SetUnloadOpen(true);

            s_ResourceMananger.ClearWaitLoadAssetTask(this);
            m_BatchLoader.Destroy();
            m_PreLoader.Destroy();
            m_DefaultObjectPool.HallDestroyAll();

            //大厅资源引用计数，非GameObject强制release
            for (int i = 0; i < _refHandlers.Count; i++)
            {
                _refHandlers[i].AssetResForceRelease();
            }

            //暂时先全部卸载
            DestroyUnusedAssetsImmediately();
            ResourceConfigure.SetUnloadOpen(false);
            s_ResourceMananger.SaveResourceDetailData();

            SaveRecorder("HallToBattle");

            Resources.UnloadUnusedAssets();
            GMVariable.LogMemoryDetail("CleanHallAfter");
        }

        public void Destroy()
        {
            GMVariable.LogMemoryDetail("CleanBattleBefore");

            ResourceConfigure.SetUnloadOpen(true);

            s_ResourceMananger.ClearWaitLoadAssetTask(this);
            m_BatchLoader.Destroy();
            m_PreLoader.Destroy();
            m_DefaultObjectPool.DestroyAll();

            //战斗资源引用计数，强制release
            for (int i = 0; i < _refHandlers.Count; i++)
            {
                _refHandlers[i].ForceRelease();
            }

            DestroyUnusedAssetsImmediately();
            ResourceConfigure.SetUnloadOpen(false);
            _refHandlers.Clear();

            s_ResourceMananger.SaveResourceDetailData();

            GMVariable.LogMemoryDetail("CleanBattleAfter");
        GMVariable.loggerAdapter.Info("====>Begin Save Reocorder");
            SaveRecorder("BattleToHall");
        GMVariable.loggerAdapter.Info("====>End Save Reocorder");

        }

        public void Dispose()
        {
            s_logger.InfoFormat("Resource Provider step1 {0} ", m_Name);

            Destroy();
            s_logger.InfoFormat("Resource Provider step2 {0} ", m_Name);
            s_ResourceMananger.Event.UnRegister(ResourceEventType.LoadAssetOptionComplete,
                DefaultHandleAssetLoadOptionComplete);
            m_PreLoader.Dispose();
            m_BatchLoader.Dispose();
            m_DefaultObjectPool.Dispose();
            s_logger.InfoFormat("Resource Provider stepend {0} ", m_Name);
          //  s_logger.InfoFormat("Resource Provider {0} is Disposed ", m_Name);
        }

        #region Profiler

        public static ResourceManangerStatus GetResourceManangerStatus()
        {
            return s_ResourceMananger.GetResourceStatus();
        }

        public StringBuilder CollectResourceDetailData()
        {
            StringBuilder stringBuilder = new StringBuilder(1024);
            s_ResourceMananger.CollectResourceDetailData(ref stringBuilder);
            m_DefaultObjectPool.CollectProfileInfo(ref stringBuilder);
            stringBuilder.AppendLine("----------------------end--------------------");
            //     DebugUtil.AppendLocalText("abProfiler", stringBuilder.ToString());
            return stringBuilder;
        }

        public static void CollectResourceInspectorData(ResourceInspectorDataSet inspectorData)
        {
            if (!s_ResourceMananger.IsInitialized)
                return;

            s_ResourceMananger.CollectResourceInspectorData(inspectorData.BundleList);
            s_ResourceMananger.CollectResourceInspectorData(inspectorData.AssetList);
            s_ResourceMananger.CollectResourceInspectorData(inspectorData.SceneList);
            inspectorData.PoolList.Clear();
            foreach (var container in InstanceObjectPoolContainer.S_PoolContainerSet)
            {
                container.CollectProfileInfo(inspectorData.PoolList);
            }
        }

        public void CollectObjectPool(List<ObjectPoolInspectorData> poolList)
        {
            m_DefaultObjectPool.CollectProfileInfo(poolList);
        }

        public StringBuilder TestGetCollectResourceDetailData()
        {
            return CollectResourceDetailData();
        }

        #endregion


        public ILoadOwner GetRootOwner()
        {
            return this;
        }

        public void ClearBundleRelatedReferences(string bundleName)
        {
            m_DefaultObjectPool.ForceDestroyInstancePoolWithBundleName(bundleName);
            //TODO:清理 指定assets引用技术
        }

        public void LoadCancel(object source)
        {
            m_DefaultObjectPool.LoadCancel(source);
        }
    }
}