using UnityEngine;

namespace AssetBundleManagement.Manifest
{
    public class ManiFestFactory
    {
        public static ManiFestInitializer CreateManiFestInitializer(AssetBundleLoadingPattern pattern)
        {
            if(pattern == AssetBundleLoadingPattern.AsyncLocal)
                return new LocalManiFestInitializer(pattern);
            
            if (pattern == AssetBundleLoadingPattern.Simulation) 
                return new SimulationManiFestInitializer(pattern);
            
            return new LocalManiFestInitializer(pattern);
        }

        public static ManiFestResourceHandle CreateManiFestResourceHandle(AssetBundleManifest assetBundleManifest, AssetBundleLoadingPattern pattern)
        {
            if(pattern == AssetBundleLoadingPattern.AsyncLocal)
                return new LocalManiFestResourceHandle(assetBundleManifest, pattern);
            
            if (pattern == AssetBundleLoadingPattern.Simulation)
                return new SimulationManiFestResourceHandle(assetBundleManifest, pattern);
            
            return new LocalManiFestResourceHandle(assetBundleManifest, pattern);
        }
    }
}