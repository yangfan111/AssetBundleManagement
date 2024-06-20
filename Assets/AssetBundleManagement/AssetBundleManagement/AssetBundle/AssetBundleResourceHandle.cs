using Core.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleManagement
{
    public class AssetBundleResourceHandle: IDisposableResourceHandle
    {
        internal bool IsSelfDoneOrFailed()
        {
            return m_SelfLoadState == AssetBundleLoadState.Loaded || m_SelfLoadState == AssetBundleLoadState.Failed;
        }
        public ResourceType ResType { get { return ResourceType.Bundle; } }
        internal AssetBundleOutputState GetOutputState()
        {
            if (InDestroyList)
                return AssetBundleOutputState.WaitDestroy;
            if (m_SelfLoadState == AssetBundleLoadState.Failed)
                return AssetBundleOutputState.Failed;
            if (m_SelfLoadState == AssetBundleLoadState.Loading)
                return AssetBundleOutputState.Loading;
            if(m_SelfLoadState == AssetBundleLoadState.Loaded)
            {
                if (m_DependenciesLoadState == AssetBundleLoadState.Loading)
                    return AssetBundleOutputState.Loading;
                if (m_DependenciesLoadState == AssetBundleLoadState.NotBegin|| m_DependenciesLoadState == AssetBundleLoadState.Loaded)
                    return AssetBundleOutputState.Loaded;
            }
           
            return AssetBundleOutputState.Unkown;
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
            if (m_DependenciesLoadState == AssetBundleLoadState.NotBegin || m_HasDependenciesDestroyed)
            {
               // AssertUtility.Assert(m_AllDependencies.Count == 0, "m_AllDependencies size should be 0");
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
        public AssetBundleLoadState LoadState
        {
            get { return m_SelfLoadState; }
        }
        private AssetBundleLoadState m_SelfLoadState;
        //ab依赖加载状态
        private AssetBundleLoadState m_DependenciesLoadState;

        private bool m_HasDependenciesDestroyed;
        
        //manifest表里依赖列表对应的ab对象列表
        List<AssetBundleResourceHandle> m_AllDependencies = new List<AssetBundleResourceHandle>(16);
        //资源对象，加载中或加载失败为null
        AssetBundle m_AssetBundle;
        internal AssetBundle GetAssetBundle() { return m_AssetBundle; }
        //ab对应的asset加载对象列表
        private readonly List<IBundleObserver> m_AssetResourceList = new List<IBundleObserver>();
        //加载key值
        public readonly BundleKey BundleKey;
        //引用计数
        private int m_ReferenceCount;
        private BundleLoadCallbacks m_Callbacks;
        internal BundleLoadCallbacks GetCallback() { return m_Callbacks; }
        public   bool                CannotDestroy { get; set; }

        private bool m_IsDisposed;
        public int GetReferenceCount()
        {
            return m_ReferenceCount;
        }
        bool IDisposableResourceHandle.IsUnUsed() //failed assetbundle一直保留
        {
            return m_ReferenceCount == 0 && !IsLoading() && m_SelfLoadState != AssetBundleLoadState.Failed;
        }

        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleResourceHandle");
        //卸载状态
        public bool InDestroyList { get; set; }
        public float WaitDestroyStartTime { get; set; }

        internal void AddObserver(IBundleObserver assetOb)
        {
            m_AssetResourceList.Add(assetOb);
            AddReference(true);
        }
        internal void RemoveObserver(IBundleObserver assetOb)
        {
            if(m_AssetResourceList.Remove(assetOb))
            {
                RemoveReference(true);

            }
            else
            {
                AssetUtility.ThrowException("asset observer dont exist");
            }
        }
  
        internal AssetBundleResourceHandle(AssetBundleLoadOperation loadOperation, BundleKey bundleKey,BundleLoadCallbacks callbacks)
        {
            m_LoadOperation                          =  loadOperation;
            BundleKey                                =  bundleKey;
            m_SelfLoadState                          =  AssetBundleLoadState.Loading;
            m_DependenciesLoadState                  =  AssetBundleLoadState.NotBegin;
            m_Callbacks                              =  callbacks;
            loadOperation.OnSelfLoadComplete         += OnSelfLoadCompleteCallback;
            loadOperation.OnDependenciesLoadComplete += OnDependenciesLoadCompleteCallback;
            CannotDestroy                             =  AssetUtility.IsCannotDestroy(BundleKey.BundleName);
            // m_AllDependenciess = allDependenciess;
            //  if (autoAddAssetReference)
            //        AddReference(true);
        }
        internal void StartLoadingDependencie()
        {
            m_DependenciesLoadState               = AssetBundleLoadState.Loading;
            m_Callbacks.HasInvokeBundleFullLoaded = false;
            m_LoadOperation.InitDependenciesRemain();
        }

        internal void EndLoadingDependencie()
        {
            m_HasDependenciesDestroyed = false;
            m_LoadOperation.CheckDependenciesRemain();
        }
  

        internal void BeginOperation()
        {
            m_LoadOperation.BeginOperation();
        }
        
        void OnSelfLoadCompleteCallback(AssetBundle assetBundle,bool isSimulation)
        {
            m_AssetBundle = assetBundle;
            m_SelfLoadState = assetBundle != null || isSimulation ? AssetBundleLoadState.Loaded : AssetBundleLoadState.Failed;
           // ResourceProfiler.LoadBundleSelfComplete(this);
            m_Callbacks.InvokeBundleSelfLoadComplete(this);
          
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
                if(!m_Callbacks.HasInvokeBundleFullLoaded)
                {
                    m_Callbacks.HasInvokeBundleFullLoaded = true;
               //     m_Callbacks.InvokeBundleFullLoadComplete(this);
                    //ResourceProfiler.LoadBundleFullComplete(this);
                    for (int i = 0; i < m_AssetResourceList.Count; i++)
                    {
                        m_AssetResourceList[i].OnBundleLoaded(m_AssetBundle);
                    }
                   
                }
            }
        }

    
        internal bool ContainsBundleDependencies(AssetBundleResourceHandle depBundle)
        {
            foreach (var ownBundle in m_AllDependencies)
            {
                if (ownBundle == depBundle)
                {
                    return true;
                }
                
            }
            return false;
       
        }
        internal void ChainBundleDependencies(AssetBundleResourceHandle depBundle)
        {
            if (ContainsBundleDependencies(depBundle))
            {
                return;
            }
        
#if dev
             if (m_HasDependenciesDestroyed)
            {
                s_logger.InfoFormat("Dependencies Bundle {0} will reload",depBundle.BundleKey.BundleName);
            }
#endif 
            m_AllDependencies.Add(depBundle);
            if (depBundle.IsSelfDoneOrFailed())
            {
              //  m_LoadOperation.ReduceDependenciesRemain();
            }
            else
            {
                m_LoadOperation.IncreaseDependenciesRemain();
                depBundle.GetCallback().SetBundleSelfLoadComplete(OnDepBundleCompleteCallback);
            }
            depBundle.GetCallback().SetBundleWillDestroy(OnDepBundleWillDestroyCallback);
        }
         void OnDepBundleWillDestroyCallback(AssetBundleResourceHandle depencies)
        {
            if (m_IsDisposed)
                return;
            for (int i = m_AllDependencies.Count - 1; i >= 0; i--)
            {
                if (m_AllDependencies[i] == depencies)
                {
                    m_AllDependencies.RemoveAt(i);
                    m_HasDependenciesDestroyed = true;
                    break;
                }
            }

            if (!m_HasDependenciesDestroyed)
            {
                AssetUtility.ThrowException("depencies List dont has current bundle,logic error");
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
            m_Callbacks.InvokeBundleReferenceChanged(this);
        }
        void RemoveReference(bool includeDependencies)
        {
            if (CannotDestroy)
            {
                return;
            }
            if (m_ReferenceCount == 0)
                AssetUtility.ThrowException("RemoveReference exception,referenceCount match failed");
            --m_ReferenceCount;
            if (includeDependencies)
            {
                foreach (var dependItem in m_AllDependencies)
                {
                    dependItem.RemoveReference(false);
                }
            }
            m_Callbacks.InvokeBundleReferenceChanged(this);

        }
        public string ToStringDetail()
        {
            return string.Format("{0},{1},{2},ref:{3}", BundleKey.BundleName,m_SelfLoadState,m_DependenciesLoadState, m_ReferenceCount);
        }
        public override string ToString()
        {
            return BundleKey.BundleName;
        }

        void IDisposableResourceHandle.Destroy()
        {
            if (CannotDestroy)
            {
                s_logger.ErrorFormat("Bundle  {0} Canot be destroy!!",this.ToString());
                return;
            }

            m_Callbacks.InvokeBundleWillDestroy(this);
            m_LoadOperation = null;
            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
                LoadedAssetBundleSet.Remove(BundleKey.BundleName);
            }
            
            m_IsDisposed = true;

        }
        int IDisposableResourceHandle.DestroyWaitTime { get { return ResourceConfigure.UnusedBundleDestroyWaitTime; } }
        List<BundleKey> m_Temp = new List<BundleKey>();

        public List<BundleKey> GetAllDependenceBundleKeys()
        {
            m_Temp.Clear();
            for (int i = 0; i < m_AllDependencies.Count; i++)
            {
                var AssetBundleResourceHandle = m_AllDependencies[i];
                m_Temp.Add(AssetBundleResourceHandle.BundleKey);
            }
            return m_Temp;

        }
    }
}