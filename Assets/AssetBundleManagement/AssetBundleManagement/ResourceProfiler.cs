using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AssetBundleManagement.ObjectPool;
using Core.Utils;
using UnityEngine;
using Utils.AssetManager;

namespace AssetBundleManagement
{
  

    public  class ResourceProfiler
    {
        public ResourceProfiler( ResourceLoadManager s_ResourceMananger)
        {
            ProfileDataAdapter = new ProfileDataAdapter(s_ResourceMananger.Event);
            s_ResourceMananger.SetResourceReference(ProfileDataAdapter);
        }
        public  ProfileDataAdapter ProfileDataAdapter;
        
        
        
 


        
        public  void AddAssetObjectProfileDataNotify(ref AssetObjectHandler assetObjectHandler, ref LoadResourceOption loadResourceOption)
        {
            if(!ResourceConfigure.DebugMode)
                return;
            if(ProfileDataAdapter.AddAssetObjectDataNotify != null)
                ProfileDataAdapter.AddAssetObjectDataNotify(ref assetObjectHandler, ref loadResourceOption);
        }
        
        public  void RemoveAssetObjectProfileDataNotify(ref AssetObjectHandler assetObjectHandler)
        {
            if(!ResourceConfigure.DebugMode)
                return;
            if(ProfileDataAdapter.RemoveAssetObjectDataNotify != null)
                ProfileDataAdapter.RemoveAssetObjectDataNotify(ref assetObjectHandler);
        }
        
        public  void AddInstanceObjectProfileDataNotify(ref InstanceObjectHandler instanceObjectHandler, ref LoadResourceOption loadResourceOption)
        {
            if(!ResourceConfigure.DebugMode)
                return;
            if(ProfileDataAdapter.AddInstanceDataNotify != null)
                ProfileDataAdapter.AddInstanceDataNotify(ref instanceObjectHandler, ref loadResourceOption);
        }
        
        public  void RemoveInstanceObjectProfileDataNotify(ref InstanceObjectHandler instanceObjectHandler)
        {
            if(!ResourceConfigure.DebugMode)
                return;
            if(ProfileDataAdapter.RemoveInstanceDataNotify != null)
                ProfileDataAdapter.RemoveInstanceDataNotify(ref instanceObjectHandler);
        }
        
    }
    public struct BundleInspectorData
    {
        public string Name;
        public BundleKey BundleKey;
        public int Reference;
        public AssetBundleOutputState LoadState;
        public float WaitDestroyStart;
        public List<BundleKey> AllDependenceBundleKeys;
        public string GetState()
        {
            return LoadState == AssetBundleOutputState.WaitDestroy ? string.Format("{0}({1})", LoadState, (int)(ResourceConfigure.UnusedBundleDestroyWaitTime - (Time.realtimeSinceStartup - WaitDestroyStart))) : LoadState.ToString();
        }

        public void GetProfileDetailData(ref StringBuilder sb)
        {
            sb.AppendFormat("BundleName: {0}\n", Name);
            sb.AppendFormat("Reference: {0}\n", Reference);
            sb.AppendFormat("AssetBundleOutputState: {0}\n", LoadState);
            sb.AppendFormat("state: {0}\n", GetState());
        }
    }
    
    public struct SceneInspectorData
    {
        public string Name;
        public AssetLoadState LoadState;
        public NewSceneInfo SceneInfo;
        public BundleKey BundleKey;
        public List<BundleKey> AllDependenceBundleKeys;

    }
    public struct AssetInspectorData
    {
        public string Name;
        public int Reference;
        public AssetOutputState LoadState;
        public float WaitDestroyStart;
        public BundleKey BundleKey;
        public AssetInfo AssetInfo;
        public List<BundleKey> AllDependenceBundleKeys;

        public string GetState()
        {
            return LoadState == AssetOutputState.WaitDestroy ? string.Format("{0}({1})", LoadState, (int)(ResourceConfigure.UnusedBundleDestroyWaitTime - (Time.realtimeSinceStartup - WaitDestroyStart))) : LoadState.ToString();
        }

        public void GetProfileDetailData(ref StringBuilder sb)
        {
            sb.AppendFormat("AssetName: {0}\n", Name);
            sb.AppendFormat("Reference: {0}\n", Reference);
            sb.AppendFormat("state: {0}\n", GetState());
            sb.AppendFormat("AssetBundleOutputState: {0}\n", LoadState);
        }
    }
    public struct ObjectPoolInspectorData
    {
        public string Name;
        public AssetInfo AssetInfo;
        public int UsedCount;
        public int PoolCount;
        public InstanceObjectPoolState LoadState;
        public float WaitDestroyStart;
        public string GetState()
        {
            return LoadState == InstanceObjectPoolState.WaitDestroy ? string.Format("{0}({1})", LoadState, (int)(ResourceConfigure.UnusedInstanceObjectPoolWaitTime - (Time.realtimeSinceStartup - WaitDestroyStart))) : LoadState.ToString();
        }

        public void GetProfileDetailData(ref StringBuilder sb)
        {
            sb.AppendFormat("ObjectPoolName: {0}\n", Name);
            sb.AppendFormat("UsedCount: {0}\n", UsedCount);
            sb.AppendFormat("PoolCount: {0}\n", PoolCount);
            sb.AppendFormat("state: {0}\n", GetState());
            sb.AppendFormat("InstanceObjectPoolState: {0}\n", LoadState);
        }
    }
 
    public struct ResourceProfilerEvent
    {
        public ProfilerEventType EventType;
        public string Name;

        public override string ToString()
        {
            string timeString = DateTime.Now.ToString("HH:mm:ss:fff");
            return string.Format("[{2}]{0}:{1}", EventType, Name, timeString);
        }
        public ResourceProfilerEvent(ProfilerEventType evet, string name)
        {
            this.EventType = evet;
            this.Name = name;
        }
    }
    public class ResourceInspectorDataSet
    {

        public List<BundleInspectorData> BundleList = new List<BundleInspectorData>();
        public List<ObjectPoolInspectorData> PoolList = new List<ObjectPoolInspectorData>();
        public List<AssetInspectorData> AssetList = new List<AssetInspectorData>();
        public List<SceneInspectorData> SceneList = new List<SceneInspectorData>();

        public List<ResourceProfilerEvent> EventList = new List<ResourceProfilerEvent>();
        public List<string> EventListString = new List<string>();

        internal IResourceEventMonitor EventMonitor;

        internal ResourceInspectorDataSet(IResourceEventMonitor eventMonitor)
        {
            EventMonitor = eventMonitor;
            EventMonitor.Register(ResourceEventType.WillDestroyAsset, OnAssetWillDestroy);
            EventMonitor.Register(ResourceEventType.WillDestroyBundle, OnBundleWillDestroy);
        }
    
        public void UnregisterProfilerEvent()
        {
            EventMonitor.UnRegister(ResourceEventType.WillDestroyAsset, OnAssetWillDestroy);
            EventMonitor.UnRegister(ResourceEventType.WillDestroyBundle, OnBundleWillDestroy);

        }
   
    void OnAssetWillDestroy(ResourceEventArgs eventArgs)
    {
         AssetRelatedEventArgs relatedEventArgs = eventArgs as AssetRelatedEventArgs;
         EventList.Add(new ResourceProfilerEvent(ProfilerEventType.UnloadAsset, relatedEventArgs.Asset.ToString()));
     }
    void OnBundleWillDestroy(ResourceEventArgs eventArgs)
    {
        BundleRelatedEventArgs relatedEventArgs = eventArgs as BundleRelatedEventArgs;
        EventList.Add(new ResourceProfilerEvent(ProfilerEventType.UnloadBundle, relatedEventArgs.Bundle.ToString()));
    }
    public void ConvertEventToString()
        {
            for (int i = 0; i < EventList.Count; i++)
            {
                EventListString.Add(EventList[i].ToString());

            }
            EventList.Clear();
            while (EventListString.Count > 100)
                EventListString.RemoveAt(0);
        }
     
    }
}
