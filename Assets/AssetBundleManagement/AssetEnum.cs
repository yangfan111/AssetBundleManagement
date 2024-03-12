using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetBundleManagement
{

    //增加枚举值需要修改PriorityCount并且实现GetLoadPrioritySequentialIndex映射
    public enum LoadPriority : byte
    {
        P_NoKown = 0,
        P_LOW = 1,
        P_SINGLE = 2,
        P_Art_HIGH = 3,
        P_MIDDLE = 5,
        P_GROUP_JOB = 6,
        P_NORMAL_HIGH = 10,
        P_THIRD_MODEL_HIGH = 12,
        P_FIRST_PRELOAD_HIGH = 0xF,
        P_BIGMAP_UI_HIGH = 18,
        P_FIRST_HIGH = 20,
        PriorityCount = 11,
    }

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

    public enum ManiFestLoadState
    {
        NotBegin,
        Loading,
        Loaded,
        Failed,
    }
    
    public enum AssetBundleLoadingPattern
    {
        Simulation,
        AsyncLocal,
        Hotfix
    }

    public enum InstanceObjectPoolState
    {
        WaitAssetLoading,
        Loaded,
        Failed,
    }

    public enum ResourceHandlerType
    {
        Asset,
        Instance,
    }

    public enum InstanceObjectPoolDestroyStrategyType
    {
        Permanent,  // 永远持有，不销毁的模式
        TimeDelay,  // 延迟一段时间后销毁模式
    }

}
