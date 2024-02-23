using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace AssetBundleManagement
{
    public struct AssetLocation
    {
        public string AssetName;
        public string BundleName;
    }
    public struct BundleKey : IEqualityComparer<BundleKey>
    {
        public string BundleName;
        public int BundleVersion;
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
                hash = hash * 31 + BundleName.GetHashCode();
                hash = hash * 31 + BundleVersion;
                return hash;
            }
        }
    }
    public class AssetBundleRequestOptions
    {
        public string RealName;
        public string Md5;
        public uint Crc;
        public string[] Dependencies;

        public int GetDependenciesCount()
        {
            return Dependencies != null ? Dependencies.Length : 0;
        }
    }
  
    public static class AssetUtility
    {
        public static AssetBundleRequestOptions ConvertToAssetBundleRequest(BundleKey bundleKey)
        {
            return default(AssetBundleRequestOptions);
        }
        public static BundleKey ConvertToBundleKey(string bundleKey)
        {
            return default(BundleKey);
        }
    
      
    }
}
