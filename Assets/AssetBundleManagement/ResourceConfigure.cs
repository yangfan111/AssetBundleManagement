namespace AssetBundleManagement
{
    public class ResourceConfigure
    {
        public static string RootAssetBundleName { get; private set; }
        public static string LocalAssetPath { get; private set; }
        public static bool DebugMode;
        public static bool IsServer { get; private set; }

        public static bool SupportUnload;//是否支持卸载功能
        public static int UnusedBundleDestroyWaitTime = 5; //Asset卸载时会清除相关bundle引用计数->bundle引用计数到0之后加入卸载队列->之后等待多久时间后销毁(s)
        public static int UnusedAssetDestroyWaitTime = 10;//Asset引用计数为0之后加入卸载队列->之后等待多久时间后销毁(s)
        public static int OneFrameUnloadBundleLimit = 1;//一帧内最多卸载多少个bundle对象，防止出现卡顿
        public static int OneFrameLoadAssetLimit = 5;//一帧内执行资源加载请求的数量上限

        public static AssetBundleLoadingPattern LoadPattern { get; private set; }
        public static void Initialize(bool isServer, AssetBundleLoadingPattern loadingPattern, string localAssetPath, string rootAssetBundleName)
        {
            IsServer = isServer;
            LoadPattern = loadingPattern;
            LocalAssetPath = localAssetPath;
            RootAssetBundleName = rootAssetBundleName;
            SupportUnload = true;
            DebugMode = true;
            ResourceProfiler.Init();
        }

    }
}