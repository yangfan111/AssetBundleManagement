
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Collections;

namespace AssetBundleManagement
{

    internal class AssetResourceHandle: IDisposableResourceHandle
    {
        //加载信息
        public readonly string AssetInfoString;
        public readonly AssetInfo AssetKey;

        //asset资源状态
        private AssetLoadState m_AssetLoadState;
        internal AssetLoadState LoadState { get { return m_AssetLoadState; } }
        //异步加载回调注册，加载完成后执行回调之后清空
        private List<KeyValuePair<int, LoadResourceOption>> m_LoadCompleteCallbacks = new List<KeyValuePair<int, LoadResourceOption>>();
        //资源对象，加载中或加载失败为null
        internal UnityEngine.Object AssetObject { get; private set; }
        //资源加载操作对象
        private AssetLoadOperation m_LoadOperation;
        //资源加载上层句柄列表，维护业务层持有资源引用计数，是在接收到加载请求后立即添加引用计数，不是在加载完成时，防止以后卸载检测到加载中的资源
        private List<int> m_HandleList = new List<int>();
        private static int s_HandleId;
        internal int NextHandleId() { return ++s_HandleId; }
        internal int ReferenceCount { get { return m_HandleList.Count; } }
        //资源依赖的assetbundle对象
        private AssetBundleResourceHandle m_BundleResource;
        //资源卸载相关状态
        internal System.Action<AssetResourceHandle> AssetReferenceChangedNotify;
        internal System.Action<AssetResourceHandle> AssetWillDestroyNotify;
        bool IDisposableResourceHandle.InDestroyList { get; set; }
        float IDisposableResourceHandle.WaitDestroyStartTime { get; set; }
        bool IDisposableResourceHandle.IsUnUsed() //failed asset一直保留，不持有ab引用计数
        {
            return ReferenceCount == 0 && m_AssetLoadState == AssetLoadState.Loaded;
        }
        //asset直到资源销毁时才减少引用计数，防止出现bundle比asset资源先销毁的情况
        void IDisposableResourceHandle.Destroy()
        {
            if(m_BundleResource != null)
                m_BundleResource.RemoveObserver(this);
            if(AssetWillDestroyNotify != null)
                AssetWillDestroyNotify(this);
        }
        int IDisposableResourceHandle.DestroyWaitTime { get { return ResourceConfigure.UnusedAssetDestroyWaitTime; } }
        internal AssetResourceHandle(AssetLoadOperation loadOperation, AssetInfo assetInfo)
        {
            m_AssetLoadState = AssetLoadState.NotBegin;
            m_LoadOperation = loadOperation;
            m_LoadOperation.OnAssetLoadComplete += OnAssetLoadCompleteCallback;
            AssetInfoString = assetInfo.ToString();
            AssetKey = assetInfo;

            //ResourceProfiler.OnAssetStructureCreate(this);
        }
        int AcquireHandleId()
        {
            int hid = NextHandleId();
            m_HandleList.Add(hid);
            AssetReferenceChangedNotify(this);
            return hid;
        }
        internal bool ReleaseHandleId(int handleId)
        {
            for (int i = 0; i < m_HandleList.Count; i++)
            {
                if (m_HandleList[i] == handleId)
                {
                    m_HandleList.RemoveAt(i);
                    AssetReferenceChangedNotify(this);
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
                    loadOption.CallOnFailed(AssetKey);
                    break;
                case AssetLoadState.Loaded:
                    loadOption.CallOnSuccess(AssetObjectHandler.Alloc(loadOption, this, AcquireHandleId()), AssetKey);
                    
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

        internal bool HandleAssetLoadTaskIfStartLoading(AssetLoadTask loadTask)
        {
            if (m_AssetLoadState == AssetLoadState.NotBegin)
                return false;
            ResourceProfiler.LogSimpleMsg("AssetTask has been handled before");
            switch (m_AssetLoadState)
            {
                case AssetLoadState.Failed:
                    ReleaseHandleId(loadTask.HandleId);
                    loadTask.LoadOption.CallOnFailed(AssetKey);
                    break;
                case AssetLoadState.Loaded:
                    loadTask.LoadOption.CallOnSuccess(AssetObjectHandler.Alloc(loadTask.LoadOption, this, loadTask.HandleId), AssetKey);
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
                ResourceProfiler.LogSimpleMsg("AssetTask Wait for bundle");
                m_AssetLoadState = AssetLoadState.LoadingAssetBundle;
                //  bundleResource.BundleFullLoadCompleteAction += BeginOperation;
            }
         
        }

        internal void BeginOperation(AssetBundle ab)
        {
            UnityEngine.Debug.Assert(m_AssetLoadState < AssetLoadState.LoadingSelf, "m_AssetLoadState error");
            m_AssetLoadState = AssetLoadState.LoadingSelf;
            ResourceProfiler.LoadAsset(this);
            m_LoadOperation.BeginOperation(ab);
        }
        void OnAssetLoadCompleteCallback(UnityEngine.Object assetObject)
        {
            AssetObject = assetObject;
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
            ResourceProfiler.LoadAssetComplete(this);
            for (int i = 0; i < m_LoadCompleteCallbacks.Count; i++)
            {
                int handleId = m_LoadCompleteCallbacks[i].Key;
                // m_HandleList.Add(handleId);
                m_LoadCompleteCallbacks[i].Value.CallOnSuccess(AssetObjectHandler.Alloc(m_LoadCompleteCallbacks[i].Value, this, handleId), AssetKey);
            }
            m_LoadCompleteCallbacks.Clear();
        }
        void OnAssetLoadFailed()
        {
            m_AssetLoadState = AssetLoadState.Failed;
            ResourceProfiler.LoadAssetComplete(this);
            for (int i = 0; i < m_LoadCompleteCallbacks.Count; i++)
            {
                m_LoadCompleteCallbacks[i].Value.CallOnFailed(AssetKey);
                ReleaseHandleId(m_LoadCompleteCallbacks[i].Key);
            }
            m_LoadCompleteCallbacks.Clear();
            if(m_BundleResource != null)
                m_BundleResource.RemoveObserver(this);
        }
        public  string ToStringDetail()
        {
            return string.Format("{0},{1},ref:{2}",AssetInfoString,m_AssetLoadState, ReferenceCount);
        }

      
    }
}
