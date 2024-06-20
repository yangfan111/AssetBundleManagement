using Core.Utils;
using UnityEngine;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    
    public struct AssetObjectHandler : IResourceHandler
    {
        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.AssetObjectHandler");
        internal AssetObjectHandler(AssetResourceHandle assetResource, int handleId)
        {
            m_handleId = handleId;
            m_AssetResource = assetResource;
          //  Context = context;
        }
        private int m_handleId;
        public int GetHandleId() { return m_handleId; }
        public AssetResourceHandle m_AssetResource;
        

        public UnityEngine.Object ResourceObject
        {
            get { return m_AssetResource.GetAssetObjectOutside(); }
        }
        public AssetInfo AssetKey
        {
            get { return m_AssetResource.AssetKey; }
        }

        public string GetAssetResourceInfo()
        {
            return string.Format("===>Asset:{0},{1},{2}",m_AssetResource.AssetKey,m_AssetResource.LoadState,m_AssetResource.IsDisposed);
        }
 
     //   public  ResourceHandlerType HandlerType
    //    {
    //        get{ return ResourceHandlerType.Asset; }
    //    }

       // public object Context { get; private set; }

        public GameObject          AsGameObject { get { return m_AssetResource.GetAssetObjectOutside() as GameObject; } }
        public ResourceHandlerType HandlerType
        {
            get { return ResourceHandlerType.Asset; }
        }

        public  void Release()
        {
            Release(true);
        }

        public void AssetResForceRelease()
        {
            if (IsValid())
                if(AsGameObject == null)
                    ForceRelease();
        }

        public void ForceRelease()
        {

            Release(false);
        }

        private void Release(bool hasLog)
        {
            if (m_handleId > 0)
            {
                if (m_AssetResource.IsDisposed)
                {
                    if(hasLog)
                        s_logger.ErrorFormat("AsyncAssetOperationHandle exception");
                }
                else if (!m_AssetResource.ReleaseHandleId(m_handleId))
                {
                    if(hasLog)
                        s_logger.ErrorFormat("{0} release failed,dont has handleId", AssetKey);
                }

                ResourceProvider.Profiler.RemoveAssetObjectProfileDataNotify(ref this);
                Reset();
            }
            else
            {
                if(hasLog)
                    s_logger.DebugFormat("AssetObjectHandler has been released");
            }
        }

        void Reset()
        {
            m_AssetResource = null;
            m_handleId = 0;
            //Context = null;
        }

        public bool IsValid()
        {
            return m_handleId > 0 && m_AssetResource != null && ResourceObject != null;
        }
    }
}