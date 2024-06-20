using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetBundleManagement
{

    //增加枚举值需要修改PriorityCount并且实现GetLoadPrioritySequentialIndex映射


    public enum AssetLoadState
    {
        NotBegin,
        LoadingAssetBundle,
        LoadingSelf,
        Loaded,
        Failed,
    }
  
    public enum AssetBundleLoadState
    {
        NotBegin,
        Loading,
        Loaded,
        Failed,
    }
    public enum AssetBundleOutputState
    {
        Unkown,
        Loading,
        Loaded,
        Failed,
        LoadedWaitForDep,
        WaitDestroy,
    }
    public enum AssetOutputState
    {
        NotBegin,
        LoadingAssetBundle,
        LoadingSelf,
        Loaded,
        Failed,
        WaitDestroy,
    }
    public enum ManiFestLoadState
    {
        NotBegin,
        Loading,
        Loaded,
        Failed,
    }
    


    public enum InstanceObjectPoolState
    {
        WaitAssetLoading,
        Failed,
        Loaded,
        WaitDestroy,
        Disposed,
    }


    public enum InstanceObjectPoolDestroyStrategyType
    {
        Permanent,  // 永远持有，不销毁的模式
        TimeDelay,  // 延迟一段时间后销毁模式
    }
    public enum ProfilerEventType
    {
        UnloadAsset,
        UnloadBundle,
        UnloadInstancePool,
    }
    public enum LoadTaskType
    {
        Asset,
        Scene,
        SortableAsset,
    }
    public enum ResourceEventReportType
    {
        Default,
        ToLocalFile,
        ToLogFile,
    }
}
