using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetBundleManagement
{
    public interface ILoadOperation
    {
        void PollOperationResult();

    }
    public interface IAssetBundleLoadMananger
    {
        void AddLoadingOperation(ILoadOperation loadOperation);
        void RemoveLoadingOperation(ILoadOperation loadOperation);
    }
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
        P_FIRST_HIGH = 20
    }

    public enum AssetBundleLoadState
    {
        NotBegin,
        Loading,
        Loaded,
    }

  
    
}
