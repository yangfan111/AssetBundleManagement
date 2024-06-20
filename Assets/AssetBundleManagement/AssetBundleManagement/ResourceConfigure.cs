using System.Collections.Generic;
using Common;

namespace AssetBundleManagement
{
    public class ResourceConfigure
    {
        public static bool DebugMode=true;
        public static bool IsServer;

        public static bool SupportUnload { get; private set; }//是否支持卸载功能
        public static int UnusedInstanceObjectPoolWaitTime = 30; //资源对象池30s不用后销毁
        public static int UnusedBundleDestroyWaitTime = 10; //Asset卸载时会清除相关bundle引用计数->bundle引用计数到0之后加入卸载队列->之后等待多久时间后销毁(s)
        public static int UnusedAssetDestroyWaitTime = 30;//Asset引用计数为0之后加入卸载队列->之后等待多久时间后销毁(s)
        public static int OneFrameUnloadBundleLimit = 1;//一帧内最多卸载多少个bundle对象，防止出现卡顿
        public static int LoadAssetMaxCount = 6;//同时最多7个LoadAsset异步加载对象
        public static int OneFrameLoadAssetLimit = 50;//一帧内执行资源加载请求的数量上限

        public static void SetMaxAssetLoad(int count)
        {
            LoadAssetMaxCount      = count;
            OneFrameLoadAssetLimit = count;
        }
        public static bool ImmediatelyDestroy = false;

        public static bool ErrorLogInsteadThrowExecption;
        public static ResourceEventReportType ReportType = ResourceEventReportType.Default;

        public static int PreloadInstanceRetainTime = 600;//secs

        public static int MinDistanceToCamera = 1;
        public static int DefaultDistanceToCamera = 100;

        public static List<string> PersistentUsedBundleList = new List<string>() {"shaders", "tables",};
        
      
        //不能被卸载的白名单
        public static List<string> CannotDestroyBundleStartList = new List<string>() { "ui/hall", "ui/icon", "configuration/interfaceanimation", "uires/", "maps/ui_3dscene001/3dbackground", "maps/ui_3dscene003/3dbackground" };
        //可以被卸载的白名单
        public static List<string> CanDestroyBundleStartList = new List<string>() { "ui/hall/prefabs/activity" };

        public static int AssetBundleRecorderDirLimitCount = 8;

        public static void SetHallLoadConfigure()
        {
            LoadAssetMaxCount = 100;
            OneFrameLoadAssetLimit = 500;
        }

        public static void SetBattleUnloadStart()
        {
            SupportUnload = true;
        }

        public static void SetBattleUnloadEnd()
        {
            SupportUnload = false;
        }

        public static void SetUnloadOpen(bool isOpen)
        {
            SupportUnload = isOpen;
        }
    }
}