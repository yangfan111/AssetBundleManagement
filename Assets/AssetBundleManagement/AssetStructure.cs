using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace AssetBundleManagement
{
    internal interface ILoadOperation
    {
        void PollOperationResult();

    }
    internal interface IDisposableResourceHandle
    {
        bool InDestroyList { get; set; }
        float WaitDestroyStartTime { get; set; }
        bool IsUnUsed();
        void Destroy();

        int DestroyWaitTime { get; }

        string ToStringDetail();
    }
    public struct AssetInfo
    {
        public static readonly AssetInfo EmptyInstance = new AssetInfo(string.Empty, string.Empty);
        public string BundleName;
        public string AssetName;

        public AssetInfo(string bundle, string asset)
        {
            this.BundleName = bundle;
            this.AssetName = asset;
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(BundleName) || string.IsNullOrEmpty(AssetName))
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", BundleName, AssetName);
        }

        public class AssetInfoComparer : IEqualityComparer<AssetInfo>
        {

            public bool Equals(AssetInfo x, AssetInfo y)
            {
                return string.Equals(x.AssetName, y.AssetName, System.StringComparison.Ordinal)
                       && string.Equals(x.BundleName, y.BundleName, System.StringComparison.Ordinal);
            }

            public int GetHashCode(AssetInfo obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + (obj.BundleName != null ? obj.BundleName.GetHashCode() : 0);
                    hash = hash * 23 + (obj.AssetName != null ? obj.AssetName.GetHashCode() : 0);
                    return hash;
                }
            }

            public static readonly AssetInfoComparer Instance = new AssetInfoComparer();
        }



    }
    public struct BundleKey : IEqualityComparer<BundleKey>
    {
        public string BundleName;
        public int BundleVersion;

        public BundleKey(string bundleName, int bundleVersion = 0)
        {
            BundleName = bundleName;
            BundleVersion = bundleVersion;
        }
        
        public bool Equals(BundleKey x, BundleKey y)
        {
            return string.Equals(x.BundleName, y.BundleName, System.StringComparison.Ordinal)
                  && x.BundleVersion == y.BundleVersion;
        }

        public int GetHashCode(BundleKey obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (obj.BundleName != null ? obj.BundleName.GetHashCode() : 0);
                hash = hash * 23 + obj.BundleVersion.GetHashCode();
                return hash;
            }
        }
    }
    public class AssetBundleRequestOptions
    {
        public string RealName;
        public string[] Dependencies;
        public AssetBundleLoadingPattern Pattern;

        public int GetDependenciesCount()
        {
            return Dependencies != null ? Dependencies.Length : 0;
        }
    }

    public struct ResourceManangerStatus
    {
        public int AllAssetSucessCont;
        public int AllBundleSucessCont;
        public int AllAssetHoldReference;
        public int AllBundleHoldReference;
        public int LoadingOperationCont;
        public int WaitRequestAssetTaskCount;
        public int ToBeDestroyCont;
        public int AllAssetFailedCont;
        public int AllBundleFailedCont;

        public bool IsAllClear()
        {
            return AllAssetSucessCont == 0 && AllBundleSucessCont == 0 && LoadingOperationCont == 0 && ToBeDestroyCont == 0;
        }
        public override string ToString()
        {
            return string.Format("AllAssetCont: {0}, AllBundleCont: {1}, AllAssetHoldReference: {2}, AllBundleHoldReference: {3}, LoadingOperationCont: {4}, " +
                "WaitRequestAssetTaskCount: {5}, ToBeDestroyCont: {6},AllAssetFailedCont:{7},AllBundleFailedCont:{8}",
                AllAssetSucessCont, AllBundleSucessCont, AllAssetHoldReference, AllBundleHoldReference, LoadingOperationCont, WaitRequestAssetTaskCount, ToBeDestroyCont, AllAssetFailedCont, AllBundleFailedCont);
        }
    }
}
