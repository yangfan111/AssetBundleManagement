using UnityEngine;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    public struct FailedObjectHandler:IResourceHandler
    {
        public Object ResourceObject
        {
            get { return null; }
        }
        public ResourceHandlerType HandlerType
        {
            get { return ResourceHandlerType.Failed; }
        }


        public FailedObjectHandler(AssetInfo assetInfo)
        {
            AssetKey = assetInfo;
        }
      public  AssetInfo               AssetKey     { get; private set; }
      //  public ResourceHandlerType HandlerType  { get; set; }
        public GameObject          AsGameObject
        {
            get { return null; }
        }
        public bool                IsValid()
        {
            return false;
            //throw new System.NotImplementedException();
        }

        public void Release()
        {
        }

        public void ForceRelease()
        {
            
        }

        public int   GetHandleId()
        {
            return 0;
        }
    }
}