
using Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    public class AssetResourceHandle: IDisposableResourceHandle,IBundleObserver
    {
        //加载信息
        private readonly string m_AssetInfoString;
        public string AssetInfoString { get { return m_AssetInfoString; } }   
        public readonly AssetInfo AssetKey;
        public ResourceType ResType { get { return ResourceType.Asset; } }

        public readonly BundleKey BundleKey;
 		//asset资源状态
        private AssetLoadState m_AssetLoadState;
        internal AssetLoadState LoadState { get { return m_AssetLoadState; } }
        //异步加载回调注册，加载完成后执行回调之后清空
        private List<KeyValuePair<int, LoadResourceOption>> m_LoadCompleteCallbacks = new List<KeyValuePair<int, LoadResourceOption>>();
        //资源对象，加载中或加载失败为null
        private static Stopwatch stopwatch = new Stopwatch();
        public Type GetAssetObjectType()
        {
            return m_AssetObject != null ? m_AssetObject.GetType() : null;
        }
        internal UnityEngine.Object GetAssetObjectOutside()
        {
            if (m_AssetObject != null)
            {
                return m_AssetObject;
            }

            if (m_AssetLoadState == AssetLoadState.Loaded && !IsDisposed) //此处说明被业务层异常卸载掉了，无法感知：重新从bundle同步加载出来
            {
                if (m_BundleResource != null)
                {
                    var bundle = m_BundleResource.GetAssetBundle();
                    if (bundle != null)
                    {
                        stopwatch.Reset();
                        stopwatch.Start();
                        if (AssetKey.AssetType != null)
                            m_AssetObject = bundle.LoadAsset(AssetKey.AssetName, AssetKey.AssetType);
                        else
                            m_AssetObject = bundle.LoadAsset(AssetKey.AssetName);
                        s_logger.WarnFormat("Asset {0} has been destroyed,Sync Reload Again,elapse:{1}ms", AssetKey,
                            stopwatch.ElapsedMilliseconds);
                    }
                }

            }

            return m_AssetObject;
        }

       
        private static LoggerAdapter s_logger = new LoggerAdapter("AssetResourceHandle");
        internal UnityEngine.Object m_AssetObject;
        //资源加载操作对象
        private AssetLoadOperation m_LoadOperation;
        //资源加载上层句柄列表，维护业务层持有资源引用计数，是在接收到加载请求后立即添加引用计数，不是在加载完成时，防止以后卸载检测到加载中的资源
        private List<int> m_HandleList = new List<int>();
        internal int NextHandleId() { return AssetUtility.GenerateNewHandle(); }
        internal int ReferenceCount { get { return m_HandleList.Count; } }
        //资源依赖的assetbundle对象
        private AssetBundleResourceHandle m_BundleResource;
        //资源卸载相关状态
        private IAssetLoadNotification m_AssetCallbacks;

        public bool IsDisposed { get; private set; }
        public bool InDestroyList { get; set; }
        
        private bool m_Locked;

        public float WaitDestroyStartTime { get; set; }


        internal void SetLocked(bool val)
        {
        
            if (m_Locked != val)
            {
                m_Locked = val;
                m_AssetCallbacks.OnAssetReferenceChangedCallback(this);
            }
        }

        bool IDisposableResourceHandle.IsUnUsed() //failed asset一直保留，不持有ab引用计数
        {
            return ReferenceCount == 0 && m_AssetLoadState == AssetLoadState.Loaded && !m_Locked;
        }
        //asset直到资源销毁时才减少引用计数，防止出现bundle比asset资源先销毁的情况
        void IDisposableResourceHandle.Destroy()
        {
            InternalDestroy();
        }

        internal void InternalDestroy(bool realUnloadAssets = false)
        {
            
            if (IsDisposed)
                return;
            // GMVariable.loggerAdapter.InfoFormat("[{1}] >>>>>> Destroy AssetResource, {0}", AssetKey, Time.frameCount);
            m_AssetCallbacks.OnAssetWillDestroyCallback(this);

            if (m_BundleResource != null )
            {
                m_BundleResource.RemoveObserver(this);
                m_BundleResource = null;
            }

            if (m_AssetObject != null) //如果是TextAsset直接释放掉节约资源
            {
                if (m_AssetObject is TextAsset || realUnloadAssets)
                {
                    UnityEngine.Resources.UnloadAsset(m_AssetObject);
                }
            }
            m_AssetObject   = null;
            m_LoadOperation = null;
            IsDisposed      = true;

        }
        internal void ForceUnloadAsset()
        { 
            if(m_AssetObject != null)
            {
                UnityEngine.Resources.UnloadAsset(m_AssetObject);
                m_AssetObject = null;

            }

        }
        int IDisposableResourceHandle.DestroyWaitTime { get { return ResourceConfigure.UnusedAssetDestroyWaitTime; } }
        internal AssetResourceHandle(AssetLoadOperation loadOperation, AssetInfo assetInfo, IAssetLoadNotification assetNotifycation)
        {
            m_AssetLoadState = AssetLoadState.NotBegin;
            m_LoadOperation = loadOperation;
            m_LoadOperation.OnAssetLoadComplete += OnAssetLoadCompleteCallback;
            m_AssetInfoString = assetInfo.ToString();
            AssetKey = assetInfo;
            BundleKey = AssetUtility.ConvertToBundleKey(assetInfo);
            IsDisposed = false;
            m_AssetCallbacks = assetNotifycation;
        //    CannotDestroy = AssetUtility.IsCannotDestroy(BundleKey.BundleName);
            //ResourceProfiler.OnAssetStructureCreate(this);
        }

        internal void SetSyncLoadMode()
        {
            if (m_AssetLoadState == AssetLoadState.NotBegin )
            {
                m_LoadOperation.SetSyncLoadMode();   
            }
        }
        int AcquireHandleId()
        {
            int hid = NextHandleId();
            m_HandleList.Add(hid);
            m_AssetCallbacks.OnAssetReferenceChangedCallback(this);
            return hid;
        }
        internal bool ReleaseHandleId(int handleId)
        {
            for (int i = 0; i < m_HandleList.Count; i++)
            {
                if (m_HandleList[i] == handleId)
                {
                    m_HandleList.RemoveAt(i);
                    m_AssetCallbacks.OnAssetReferenceChangedCallback(this);
                    return true;
                }
            }
   //         Debug.LogErrorFormat("Not")
            return false;
        }
        internal AssetLoadState AssetLoadState { get { return m_AssetLoadState; } }
        internal AssetLoadTask HandleAssetObjectLoadRequest(ref LoadResourceOption loadOption)
        {
            switch (m_AssetLoadState)
            {
                case AssetLoadState.Failed:
                    m_AssetCallbacks.OnLoadAssetComplete(new FailedObjectHandler(AssetKey),loadOption);
                 //   loadOption.CallOnFailed(AssetKey);
                    break;
                case AssetLoadState.Loaded:
                    var assetObjectHandler = new AssetObjectHandler(this, AcquireHandleId());

                    ResourceProvider.Profiler.AddAssetObjectProfileDataNotify(ref assetObjectHandler, ref loadOption);
                    m_AssetCallbacks.OnLoadAssetComplete(assetObjectHandler,loadOption);
                  //  loadOption.CallOnSuccess(assetObjectHandler, AssetKey);
                    break;

                case AssetLoadState.NotBegin:
                    var loadTask = ResourceObjectHolder<AssetLoadTask>.Allocate();
                    loadTask.HandleId = AcquireHandleId();
                    loadTask.LoadOption = loadOption;
                    loadTask.ResourceObject = this;
                    return loadTask;
                case AssetLoadState.LoadingSelf:
                case AssetLoadState.LoadingAssetBundle:
                    m_LoadCompleteCallbacks.Add(new KeyValuePair<int, LoadResourceOption>(AcquireHandleId(), loadOption));
                 //   ResourceProfiler.OnAssetWaitLoading(this);
                    break;

            }
            return null;

        }

        internal AssetSortableLoadTask HandleAssetSortableObjectLoadRequest(ISortableJob job, ref LoadResourceOption loadOption)
        {
            switch (m_AssetLoadState)
            {
                case AssetLoadState.Failed:
                    m_AssetCallbacks.OnLoadAssetComplete(new FailedObjectHandler(AssetKey), loadOption);
                    //   loadOption.CallOnFailed(AssetKey);
                    break;
                case AssetLoadState.Loaded:
                    var assetObjectHandler = new AssetObjectHandler(this, AcquireHandleId());

                    ResourceProvider.Profiler.AddAssetObjectProfileDataNotify(ref assetObjectHandler, ref loadOption);
                    m_AssetCallbacks.OnLoadAssetComplete(assetObjectHandler, loadOption);
                    //  loadOption.CallOnSuccess(assetObjectHandler, AssetKey);
                    break;

                case AssetLoadState.NotBegin:
                    var loadTask = ResourceObjectHolder<AssetSortableLoadTask>.Allocate();
                    loadTask.HandleId = AcquireHandleId();
                    loadTask.LoadOption = loadOption;
                    loadTask.ResourceObject = this;
                    loadTask.Job = job;
                    return loadTask;
                case AssetLoadState.LoadingSelf:
                case AssetLoadState.LoadingAssetBundle:
                    m_LoadCompleteCallbacks.Add(new KeyValuePair<int, LoadResourceOption>(AcquireHandleId(), loadOption));
                    //   ResourceProfiler.OnAssetWaitLoading(this);
                    break;

            }
            return null;

        }

        internal bool HandleAssetLoadTaskIfStartLoading(AssetLoadTask loadTask)
        {
            if (m_AssetLoadState == AssetLoadState.NotBegin)
                return false;
            switch (m_AssetLoadState)
            {
                case AssetLoadState.Failed:
                    ReleaseHandleId(loadTask.HandleId);
                    m_AssetCallbacks.OnLoadAssetComplete(new FailedObjectHandler(AssetKey),loadTask.LoadOption);
               //     loadTask.LoadOption.CallOnFailed(AssetKey);
                    break;
                case AssetLoadState.Loaded:
                    var assetObjectHandler = new AssetObjectHandler( this, loadTask.HandleId);

                    ResourceProvider.Profiler.AddAssetObjectProfileDataNotify(ref assetObjectHandler, ref loadTask.LoadOption);
                    m_AssetCallbacks.OnLoadAssetComplete(assetObjectHandler,loadTask.LoadOption);
                    //loadTask.LoadOption.CallOnSuccess(assetObjectHandler, AssetKey);
                
                    break;

                case AssetLoadState.LoadingSelf:
                case AssetLoadState.LoadingAssetBundle:
                    m_LoadCompleteCallbacks.Add(new KeyValuePair<int, LoadResourceOption>(loadTask.HandleId, loadTask.LoadOption));
                  //  ResourceProfiler.OnAssetWaitLoading(this);
                    break;

            }
            return true;

        }
        internal void InitalizeChainAttachedBundleObject(AssetBundleResourceHandle bundleResource, AssetLoadTask loadTask)
        {
            m_BundleResource = bundleResource;
            m_LoadCompleteCallbacks.Add(new KeyValuePair<int, LoadResourceOption>(loadTask.HandleId, loadTask.LoadOption));
            if (m_BundleResource == null)
            {
                BeginOperation(null);
                return;
               
            }
            m_BundleResource.AddObserver(this);

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
                m_AssetCallbacks.OnAssetLoadWaitForBundle(this);
                m_AssetLoadState = AssetLoadState.LoadingAssetBundle;
                //  bundleResource.BundleFullLoadCompleteAction += BeginOperation;
            }
         
        }
        public void OnBundleLoaded(AssetBundle bundle)
        {
            BeginOperation(bundle);
        }
        internal void BeginOperation(AssetBundle ab)
        {
            if (m_AssetLoadState < AssetLoadState.LoadingSelf)
            {
                m_AssetLoadState = AssetLoadState.LoadingSelf;
                //   ResourceProfiler.LoadAsset(this);
                m_AssetCallbacks.OnAssetLoadStart(this);
                m_LoadOperation.BeginOperation(ab);
            }

            
        }
        void OnAssetLoadCompleteCallback(UnityEngine.Object assetObject)
        {
            m_AssetObject = assetObject;
         
            if (assetObject != null)
            {

                OnAssetLoadSucess();
            }
            else
            {

                OnAssetLoadFailed();
            }
        }
        void OnAssetLoadSucess()
        {
            m_AssetLoadState = AssetLoadState.Loaded;
            m_AssetCallbacks.OnAssetLoadSucessCallback(this);
            for (int i = 0; i < m_LoadCompleteCallbacks.Count; i++)
            {
                int handleId = m_LoadCompleteCallbacks[i].Key;
                // m_HandleList.Add(handleId);
                var assetObjectHandler = new AssetObjectHandler( this, handleId);
                var loadResourceOptionOption = m_LoadCompleteCallbacks[i].Value;

                ResourceProvider.Profiler.AddAssetObjectProfileDataNotify(ref assetObjectHandler, ref loadResourceOptionOption);
                m_AssetCallbacks.OnLoadAssetComplete(assetObjectHandler,loadResourceOptionOption);
                //loadResourceOptionOption.CallOnSuccess(assetObjectHandler, AssetKey);
             
            }
            m_LoadCompleteCallbacks.Clear();
        }
        void OnAssetLoadFailed()
        {
            m_AssetLoadState = AssetLoadState.Failed;
            m_AssetCallbacks.OnAssetLoadFailedCallback(this);
            for (int i = 0; i < m_LoadCompleteCallbacks.Count; i++)
            {
                m_AssetCallbacks.OnLoadAssetComplete(new FailedObjectHandler(AssetKey),m_LoadCompleteCallbacks[i].Value);
             //   m_LoadCompleteCallbacks[i].Value.CallOnFailed(AssetKey);
                ReleaseHandleId(m_LoadCompleteCallbacks[i].Key);
            }
            m_LoadCompleteCallbacks.Clear();
            if(m_BundleResource != null)
            {
                m_BundleResource.RemoveObserver(this);
                m_BundleResource = null;
            }
        }
        public  string ToStringDetail()
        {
            return string.Format("{0},{1},ref:{2}",AssetInfoString,m_AssetLoadState, ReferenceCount);
        }
        public override string ToString()
        {
            return AssetInfoString;
        }

        internal AssetOutputState GetOutputState()
        {
            
            if (InDestroyList)
            {
                if (m_AssetLoadState != AssetLoadState.Loaded)
                    AssetUtility.ThrowException("Asset state excpetion!");
                return AssetOutputState.WaitDestroy;
            }
            return (AssetOutputState)m_AssetLoadState;
        }
        public List<BundleKey> GetAllDependenceBundleKeys()
        {
        
            if (m_BundleResource != null)
                return  m_BundleResource.GetAllDependenceBundleKeys();
            return null;
        }

      
    }
}
