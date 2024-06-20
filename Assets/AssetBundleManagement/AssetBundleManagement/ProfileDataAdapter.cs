using System;
using System.Collections.Generic;
using AssetBundleManagement.ObjectPool;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    public delegate void AddAssetObjectDataNotify(ref AssetObjectHandler assetObjectHandler, ref LoadResourceOption loadResourceOption);
    public delegate void RemoveAssetObjectDataNotify(ref AssetObjectHandler assetObjectHandler);
    public delegate void AddInstanceDataNotify(ref InstanceObjectHandler instanceObjectHandler, ref LoadResourceOption loadResourceOption);
    public delegate void RemoveInstanceDataNotify(ref InstanceObjectHandler instanceObjectHandler);
    
    public class ProfileDataAdapter
    {
        public Dictionary<BundleKey, AssetBundleResourceHandle> AssetBundleResourceMapAdapter { get; private set; }
        
        public Dictionary<AssetInfo, AssetResourceHandle> AsseResourceMap { get; private set; }
        
        public ResourceInspectorDataSet InspectorDataSet;   
        public AddAssetObjectDataNotify AddAssetObjectDataNotify;
        public RemoveAssetObjectDataNotify RemoveAssetObjectDataNotify;
        public AddInstanceDataNotify AddInstanceDataNotify;
        public RemoveInstanceDataNotify RemoveInstanceDataNotify;

        internal ProfileDataAdapter(IResourceEventMonitor eventMonitor)
        {
            InspectorDataSet = new ResourceInspectorDataSet(eventMonitor);
        }


        public void InitSetBundleData(Dictionary<BundleKey, AssetBundleResourceHandle> bundleResourceMap)
        {
            AssetBundleResourceMapAdapter = bundleResourceMap;
        }

        public void InitAsseResourceMap(Dictionary<AssetInfo, AssetResourceHandle> assetResourceMap)
        {
            AsseResourceMap = assetResourceMap;
        }
        
      
    }
}