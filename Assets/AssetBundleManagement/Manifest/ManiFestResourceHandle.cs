using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AssetBundleManagement.Manifest
{
    public abstract class ManiFestResourceHandle
    {
        protected AssetBundleLoadingPattern assetBundleLoadingPattern;
        protected AssetBundleManifest m_assetBundleManifest;
        protected Dictionary<string, AssetBundleRequestOptions> assetBundleInfoDict;

        public ManiFestResourceHandle(AssetBundleManifest assetBundleManifest, AssetBundleLoadingPattern pattern)
        {
            assetBundleLoadingPattern = pattern;
            m_assetBundleManifest = assetBundleManifest;
        }

        public abstract void ParseAssetBundleDependency();

        public AssetBundleRequestOptions GetAssetBundleRequestOptions(string bundleName)
        {
            AssetBundleRequestOptions assetBundleRequestOptions;
            assetBundleInfoDict.TryGetValue(bundleName, out assetBundleRequestOptions);
            return assetBundleRequestOptions;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var item in assetBundleInfoDict.Values)
            {
                int i = 0;
                sb.AppendFormat("Name:{0},Dep:{1}", item.RealName, item.Dependencies.Length);
                foreach (var dependency in item.Dependencies)
                {
                    sb.AppendFormat(",{0}", dependency);
                    if(++i>10)
                    {
                        sb.Append("...");
                        break;
                    }

                }
                sb.Append("\n");
            }
            return sb.ToString();   
        }

    }


    internal class LocalManiFestResourceHandle:ManiFestResourceHandle
    {
        public LocalManiFestResourceHandle(AssetBundleManifest assetBundleManifest, AssetBundleLoadingPattern pattern) 
            : base(assetBundleManifest, pattern)
        {
            
        }

        public override void ParseAssetBundleDependency()
        {
            if (m_assetBundleManifest == null)
                throw new ArgumentNullException("m_assetBundleManifest");

            string[] allAssetBundlesNameArray = m_assetBundleManifest.GetAllAssetBundles();
            assetBundleInfoDict = new Dictionary<string, AssetBundleRequestOptions>(allAssetBundlesNameArray.Length);
            foreach (var bundleName in allAssetBundlesNameArray)
            {
                AssetBundleRequestOptions bundleInfo = new AssetBundleRequestOptions();
                bundleInfo.RealName = bundleName;
                bundleInfo.Dependencies = m_assetBundleManifest.GetAllDependencies(bundleName);
                bundleInfo.Pattern = assetBundleLoadingPattern;
                assetBundleInfoDict.Add(bundleName, bundleInfo);
            }
        }
    }

    internal class SimulationManiFestResourceHandle : ManiFestResourceHandle
    {
        public SimulationManiFestResourceHandle(AssetBundleManifest assetBundleManifest, AssetBundleLoadingPattern pattern) : base(assetBundleManifest, pattern)
        {
        }

        public override void ParseAssetBundleDependency()
        {
#if UNITY_EDITOR
            string[] allAssetBundlesNameArray = AssetDatabase.GetAllAssetBundleNames();
            assetBundleInfoDict = new Dictionary<string, AssetBundleRequestOptions>(allAssetBundlesNameArray.Length);
            foreach (var bundleName in allAssetBundlesNameArray)
            {
                AssetBundleRequestOptions bundleInfo = new AssetBundleRequestOptions();
                bundleInfo.RealName = bundleName;
                bundleInfo.Dependencies = AssetDatabase.GetDependencies(bundleName);
                bundleInfo.Pattern = assetBundleLoadingPattern;
                assetBundleInfoDict.Add(bundleName, bundleInfo);
            }
#endif
        }
    }
}