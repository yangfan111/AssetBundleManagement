using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AssetBundleManagement.Manifest
{
    public class ManiFestProvider
    {
        private ManiFestResourceHandle m_maniFestResourceHandle;
        private List<string> _hotfixBundleNameList;
        private List<string> _simulateBundleNameList;
        public override string ToString()
        {
            return m_maniFestResourceHandle.ToString();
        }
        public ManiFestLoadState ManiFestLoadState
        {
            get;
            set;
        }
        
        public ManiFestProvider()
        {
            ManiFestLoadState = ManiFestLoadState.NotBegin;
        }

        public void SetSimulateBundleNameList(List<string> simulateBundleNameList)
        {
            _simulateBundleNameList = simulateBundleNameList;
        }
        
        public void SetHotfixBundleNameList(List<string> hotfixBundleNameList)
        {
            _hotfixBundleNameList = hotfixBundleNameList;
        }

        private bool IsHotfixBundleName(string bundleName)
        {
            if (_hotfixBundleNameList == null)
                return false;
            
            return _hotfixBundleNameList.Contains(bundleName);
        }
        
        private bool IsSimulateBundleName(string bundleName)
        {
            if (_simulateBundleNameList == null)
                return false;

            if (_simulateBundleNameList.Contains(bundleName))
                return true;
            else
            {
                foreach (var name in _simulateBundleNameList)
                {
                    if (bundleName.StartsWith(name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private AssetBundleLoadingPattern GetAssetBundleLoadingPattern(string bundleName)
        {
            if (IsHotfixBundleName(bundleName))
                return AssetBundleLoadingPattern.Hotfix;
            
            if (IsSimulateBundleName(bundleName))
                return AssetBundleLoadingPattern.Simulation;

            return AssetBundleLoadingPattern.AsyncLocal;
        }


        public void ManiFestInitializerSuccess(AssetBundleManifest assetBundleManifest, AssetBundleLoadingPattern pattern)
        {
            ManiFestResourceHandle maniFestResourceHandle = ManiFestFactory.CreateManiFestResourceHandle(assetBundleManifest, pattern);
            maniFestResourceHandle.ParseAssetBundleDependency();

            m_maniFestResourceHandle = maniFestResourceHandle;
            ManiFestLoadState = ManiFestLoadState.Loaded;
        }

        public AssetBundleRequestOptions GetABRequsetOptions(BundleKey bundleKey)
        {
            var assetBundleRequestOptions = m_maniFestResourceHandle.GetAssetBundleRequestOptions(bundleKey.BundleName);
            if(assetBundleRequestOptions == null)
            {
                Debug.LogErrorFormat("BundleName {0} is not valid", bundleKey.BundleName);
                return null;
            }
            var pattern = GetAssetBundleLoadingPattern(bundleKey.BundleName);
            assetBundleRequestOptions.Pattern = pattern;
            return assetBundleRequestOptions;
        }

        public AssetBundleLoadingPattern GetLoadingPattern(string bundleName)
        {
            return GetAssetBundleLoadingPattern(bundleName);
        }
    }
}