using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleManagement
{
    internal class AssetBundleResourceHandle: IDisposableResourceHandle
    {
        internal bool IsSelfDoneOrFailed()
        {
            return m_SelfLoadState == AssetBundleLoadState.Loaded || m_SelfLoadState == AssetBundleLoadState.Failed;
        }
   
        internal bool IsLoading()
        {
            return m_SelfLoadState == AssetBundleLoadState.Loading || m_DependenciesLoadState == AssetBundleLoadState.Loading;
        }
        internal bool IsFullLoaded()
        {
            return m_SelfLoadState == AssetBundleLoadState.Loaded &&
                            m_DependenciesLoadState == AssetBundleLoadState.Loaded;
        }
        internal bool IsIndirectBundle()
        {
            if (m_DependenciesLoadState == AssetBundleLoadState.NotBegin)
            {
                UnityEngine.Debug.Assert(m_AllDependencies.Count == 0, "m_AllDependencies size should be 0");
                return true;
            }
            return false;
        }
      
        internal bool IsFaild()
        {
            return m_SelfLoadState == AssetBundleLoadState.Failed;
        }
        //资源加载操作对象
        AssetBundleLoadOperation m_LoadOperation;
        //ab自身加载状态
        private AssetBundleLoadState m_SelfLoadState;
        //ab依赖加载状态
        private AssetBundleLoadState m_DependenciesLoadState;
        //manifest表里依赖列表对应的ab对象列表
        List<AssetBundleResourceHandle> m_AllDependencies = new List<AssetBundleResourceHandle>(16);
        //资源对象，加载中或加载失败为null
        AssetBundle m_AssetBundle;
        internal AssetBundle GetAssetBundle() { return m_AssetBundle; }
        //ab对应的asset加载对象列表
        private readonly List<AssetResourceHandle> m_AssetResourceList = new List<AssetResourceHandle>();
        //加载key值
        internal readonly BundleKey BundleKey;
        //引用计数
        private int m_ReferenceCount;

        public int GetReferenceCount()
        {
            return m_ReferenceCount;
        }
        bool IDisposableResourceHandle.IsUnUsed() //failed assetbundle一直保留
        {
            return m_ReferenceCount == 0 && !IsLoading() && m_SelfLoadState != AssetBundleLoadState.Failed;
        }
        //卸载状态
        bool IDisposableResourceHandle.InDestroyList { get; set; }
        float IDisposableResourceHandle.WaitDestroyStartTime { get; set; }
   

        //外部回调事件注册
        internal System.Action<AssetBundleResourceHandle> BundleSelfLoadCompleteNotify;
        internal System.Action<AssetBundleResourceHandle> BundleFullLoadCompleteNotify;
        internal System.Action<AssetBundleResourceHandle> BundleReferenceChangedNotify;
        internal System.Action<AssetBundleResourceHandle> BundleWillDestroyNotify;
        internal void AddObserver(AssetResourceHandle assetOb)
        {
            m_AssetResourceList.Add(assetOb);
            AddReference(true);
        }
        internal void RemoveObserver(AssetResourceHandle assetOb)
        {
            if(m_AssetResourceList.Remove(assetOb))
            {
                RemoveReference(true);

            }
            else
            {
                throw new System.Exception("asset observer dont exist");
            }
        }
  
        internal AssetBundleResourceHandle(AssetBundleLoadOperation loadOperation, BundleKey bundleKey)
        {
            m_LoadOperation = loadOperation;
            BundleKey = bundleKey;
            m_SelfLoadState = AssetBundleLoadState.Loading;
            m_DependenciesLoadState = AssetBundleLoadState.NotBegin;
            loadOperation.OnSelfLoadComplete += OnSelfLoadCompleteCallback;
            loadOperation.OnDependenciesLoadComplete += OnDependenciesLoadCompleteCallback;
            BundleFullLoadCompleteNotify += DefaultFullCompleteCallback;

            // m_AllDependenciess = allDependenciess;
            //  if (autoAddAssetReference)
            //        AddReference(true);
        }
        internal void SetLoadingDependencie()
        {
            m_DependenciesLoadState = AssetBundleLoadState.Loading;
        }
        void DefaultFullCompleteCallback(AssetBundleResourceHandle bundleHandle)
        {
            //Debug.log("DefaultFullComplete");
        }

        internal void BeginOperation()
        {
            m_LoadOperation.BeginOperation();
        }
        
        void OnSelfLoadCompleteCallback(AssetBundle assetBundle)
        {
            m_AssetBundle = assetBundle;
            m_SelfLoadState = assetBundle != null ? AssetBundleLoadState.Loaded : AssetBundleLoadState.Failed;
            ResourceProfiler.LoadBundleSelfComplete(this);
            if (BundleSelfLoadCompleteNotify != null)
            {
                BundleSelfLoadCompleteNotify(this);
                BundleSelfLoadCompleteNotify = null;
            }
          
            CheckBundleFullLoadComplete();
        }

        void OnDependenciesLoadCompleteCallback()
        {
            m_DependenciesLoadState = AssetBundleLoadState.Loaded;
            CheckBundleFullLoadComplete();
        }
        void CheckBundleFullLoadComplete()
        {
            if (IsFullLoaded() || IsFaild())
            {
                if (BundleFullLoadCompleteNotify != null)
                {
                    BundleFullLoadCompleteNotify(this);
                    BundleFullLoadCompleteNotify = null;
                    ResourceProfiler.LoadBundleFullComplete(this);
                    foreach (var assetOb in m_AssetResourceList)
                    {
                        assetOb.BeginOperation(m_AssetBundle);
                    }
                }
            }
        }
        internal void ChainBundleDependencies(AssetBundleResourceHandle depBundle)
        {
            m_AllDependencies.Add(depBundle);
            if (depBundle.IsSelfDoneOrFailed())
            {
                m_LoadOperation.ReduceDependenciesRemain();
            }
            else
            {
                depBundle.BundleSelfLoadCompleteNotify += OnDepBundleCompleteCallback;
            }
        }

        void OnDepBundleCompleteCallback(AssetBundleResourceHandle assetBundleResource)
        {
            m_LoadOperation.ReduceDependenciesRemain();
        }

         void AddReference(bool includeDependencies)
        {
            ++m_ReferenceCount;
            if (includeDependencies)
            {
                foreach (var dependItem in m_AllDependencies)
                {
                    dependItem.AddReference(false);
                }
            }
            BundleReferenceChangedNotify(this);
        }
        void RemoveReference(bool includeDependencies)
        {
            if (m_ReferenceCount == 0)
                throw new System.Exception("RemoveReference exception,referenceCount match failed");
            --m_ReferenceCount;
            if (includeDependencies)
            {
                foreach (var dependItem in m_AllDependencies)
                {
                    dependItem.RemoveReference(false);
                }
            }
            BundleReferenceChangedNotify(this);

        }
        public string ToStringDetail()
        {
            return string.Format("{0},{1},{2},ref:{3}", BundleKey.BundleName,m_SelfLoadState,m_DependenciesLoadState, m_ReferenceCount);
        }

        void IDisposableResourceHandle.Destroy()
        {
            if(BundleWillDestroyNotify != null)
                BundleWillDestroyNotify(this);
            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
                ResourceRecorder.Get().IncUnloadABCount();
            }
        }
        int IDisposableResourceHandle.DestroyWaitTime { get { return ResourceConfigure.UnusedBundleDestroyWaitTime; } }

    }
}