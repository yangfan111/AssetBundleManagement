using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AssetBundleManagement
{
    internal class ProfilerState
    {
        internal void LoadBundleDirectly(AssetBundleResourceHandle bundleHandle)
        {
            // Method Body Removed
        }

        internal void LoadBundleInDirectly(AssetBundleResourceHandle bundleHandle, AssetBundleResourceHandle fromBundle)
        {
            // Method Body Removed
        }

        internal void LoadBundleSelfComplete(AssetBundleResourceHandle bundleHandle)
        {
            // Method Body Removed
        }

        internal void BundleOneDepndencyDone(string bundleName, int remain)
        {
            // Method Body Removed
        }

        internal void LoadBundleFullComplete(AssetBundleResourceHandle bundleHandle)
        {
            // Method Body Removed
        }

        internal void RequestAsset(ref AssetInfo assetInfo)
        {
            // Method Body Removed
        }

        internal void CallOnAssetSuccess(AssetInfo assetInfo)
        {
            // Method Body Removed
        }

        internal void CallOnAssetFailed(AssetInfo assetInfo)
        {
            // Method Body Removed
        }

        internal void PushToAssetLoadTask(AssetResourceHandle resourceHandle)
        {
            // Method Body Removed
        }

        internal void PopAssetLoadTask(AssetResourceHandle resourceHandle)
        {
            // Method Body Removed
        }

        internal void LogSimpleMsg(string msg)
        {
            // Method Body Removed
        }

        internal void LoadAsset(AssetResourceHandle resourceHandle)
        {
            // Method Body Removed
        }

        internal void LoadAssetComplete(AssetResourceHandle resourceHandle)
        {
            // Method Body Removed
        }

        internal void AddResourceToDestroyList(IDisposableResourceHandle resourceHandle)
        {
            // Method Body Removed
        }

        internal void RemoveResourceFromDestroyList(IDisposableResourceHandle resourceHandle)
        {
            // Method Body Removed
        }

        internal void ExecuteResourceDestroy(IDisposableResourceHandle resourceHandle)
        {
            // Method Body Removed
        }
    }
    internal class SimulationProfilerState  : ProfilerState
    {
        new internal void LoadBundleDirectly(AssetBundleResourceHandle bundleHandle)
        {
            AssetLogger.LogToFile("Trigger LoadBundle:{0}", bundleHandle.BundleKey.BundleName);
        }

        new internal void LoadBundleInDirectly(AssetBundleResourceHandle bundleHandle, AssetBundleResourceHandle fromBundle)
        {
            AssetLogger.LogToFile("Trigger LoadBundleDependency:{0}->{1}", fromBundle.BundleKey.BundleName, bundleHandle.BundleKey.BundleName);
        }

        new internal void LoadBundleSelfComplete(AssetBundleResourceHandle bundleHandle)
        {
            if (bundleHandle.IsFaild())
            {
                AssetLogger.LogToFile("Bundle {0} LoadFailed", bundleHandle.BundleKey.BundleName);
            }
            else
            {
                AssetLogger.LogToFile("Bundle {0} LoadSucess", bundleHandle.BundleKey.BundleName);
            }
        }

        new internal void BundleOneDepndencyDone(string bundleName, int remain)
        {
            AssetLogger.LogToFile("Bundle {0} 'sOneDepndencyDone,remain:{1}", bundleName, remain);
        }

        new internal void LoadBundleFullComplete(AssetBundleResourceHandle bundleHandle)
        {
            if (bundleHandle.IsFullLoaded())
            {
                AssetLogger.LogToFile("Bundle {0} is NowUsable", bundleHandle.BundleKey.BundleName);
            }
        }

        new internal void RequestAsset(ref AssetInfo assetInfo)
        {
            AssetLogger.LogToFile("Request Asset {0} ", assetInfo);
        }

        new internal void CallOnAssetSuccess(AssetInfo assetInfo)
        {
            AssetLogger.LogToFile("Callback {0} Scuess", assetInfo.ToString());
        }

        new internal void CallOnAssetFailed(AssetInfo assetInfo)
        {
            AssetLogger.LogToFile("Callback {0} Failed", assetInfo.ToString());
        }

        new internal void PushToAssetLoadTask(AssetResourceHandle resourceHandle)
        {
            AssetLogger.LogToFile("Push AssetTask to WaitList,{0},{1}", resourceHandle.AssetInfoString, Time.frameCount);
        }

        new internal void PopAssetLoadTask(AssetResourceHandle resourceHandle)
        {
            AssetLogger.LogToFile("Pop AssetTask from WaitList,{0},{1}", resourceHandle.AssetInfoString, Time.frameCount);
        }

        new internal void LogSimpleMsg(string msg)
        {
            AssetLogger.LogToFile(msg);
        }

        new internal void LoadAsset(AssetResourceHandle resourceHandle)
        {
            AssetLogger.LogToFile("Trigger LoadAsset {0}", resourceHandle.AssetInfoString);
        }

        new internal void LoadAssetComplete(AssetResourceHandle resourceHandle)
        {
            if (resourceHandle.LoadState == AssetLoadState.Failed)
            {
                AssetLogger.LogToFile("AssetLoad {0} Failed", resourceHandle.AssetInfoString);
            }
            else
            {
                AssetLogger.LogToFile("AssetLoad {0} Sucess", resourceHandle.AssetInfoString);
            }
        }

        new internal void AddResourceToDestroyList(IDisposableResourceHandle resourceHandle)
        {
            AssetLogger.LogToFile("AddResource To DestroyList,{0} ", resourceHandle.ToStringDetail());
        }

        new internal void RemoveResourceFromDestroyList(IDisposableResourceHandle resourceHandle)
        {
            AssetLogger.LogToFile("RemoveResourcefrom DestroyList, {0}  ", resourceHandle.ToStringDetail());
        }

        new internal void ExecuteResourceDestroy(IDisposableResourceHandle resourceHandle)
        {
            AssetLogger.LogToFile("Do Unload Resource,{0}  ", resourceHandle.ToStringDetail());
        }
    }
    

    internal static class ResourceProfiler
    {
        static ProfilerState state;
        internal static void Init()
        {
            if (ResourceConfigure.DebugMode)
                state = new SimulationProfilerState();
            else
                state = new ProfilerState();

        }
        internal static void LoadBundleDirectly(AssetBundleResourceHandle bundleHandle)
        {
            state.LoadBundleDirectly(bundleHandle);
        }

        internal static void LoadBundleInDirectly(AssetBundleResourceHandle bundleHandle, AssetBundleResourceHandle fromBundle)
        {
            state.LoadBundleInDirectly(bundleHandle, fromBundle);
        }

        internal static void LoadBundleSelfComplete(AssetBundleResourceHandle bundleHandle)
        {
            state.LoadBundleSelfComplete(bundleHandle);
        }

        internal static void BundleOneDepndencyDone(string bundleName, int remain)
        {
            state.BundleOneDepndencyDone(bundleName, remain);
        }

        internal static void LoadBundleFullComplete(AssetBundleResourceHandle bundleHandle)
        {
            state.LoadBundleFullComplete(bundleHandle);
        }

        internal static void RequestAsset(ref AssetInfo assetInfo)
        {
            state.RequestAsset(ref assetInfo);
        }

        internal static void CallOnAssetSuccess(AssetInfo assetInfo)
        {
            state.CallOnAssetSuccess(assetInfo);
        }

        internal static void CallOnAssetFailed(AssetInfo assetInfo)
        {
            state.CallOnAssetFailed(assetInfo);
        }

        internal static void PushToAssetLoadTask(AssetResourceHandle resourceHandle)
        {
            state.PushToAssetLoadTask(resourceHandle);
        }

        internal static void PopAssetLoadTask(AssetResourceHandle resourceHandle)
        {
            state.PopAssetLoadTask(resourceHandle);
        }

        internal static void LogSimpleMsg(string msg)
        {
            state.LogSimpleMsg(msg);
        }

        internal static void LoadAsset(AssetResourceHandle resourceHandle)
        {
            state.LoadAsset(resourceHandle);
        }

        internal static void LoadAssetComplete(AssetResourceHandle resourceHandle)
        {
            state.LoadAssetComplete(resourceHandle);
        }

        internal static void AddResourceToDestroyList(IDisposableResourceHandle resourceHandle)
        {
            state.AddResourceToDestroyList(resourceHandle);
        }

        internal static void RemoveResourceFromDestroyList(IDisposableResourceHandle resourceHandle)
        {
            state.RemoveResourceFromDestroyList(resourceHandle);
        }

        internal static void ExecuteResourceDestroy(IDisposableResourceHandle resourceHandle)
        {
            state.ExecuteResourceDestroy(resourceHandle);
        }
    }
}
