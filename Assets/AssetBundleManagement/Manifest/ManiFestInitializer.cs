using System.Collections;
using System.IO;
using UnityEngine;

namespace AssetBundleManagement.Manifest
{
    public delegate void ManifestInitializeSuccess(AssetBundleManifest assetBundleManifest, AssetBundleLoadingPattern pattern);
    
    public abstract class ManiFestInitializer
    {
        protected readonly AssetBundleLoadingPattern AssetBundleLoadingPattern;

        public ManiFestInitializer(AssetBundleLoadingPattern pattern)
        {
            AssetBundleLoadingPattern = pattern;
        }

        public abstract IEnumerator LoadRootManiFest(ManifestInitializeSuccess SuccessCallback);
    }


    internal class LocalManiFestInitializer : ManiFestInitializer
    {

        public LocalManiFestInitializer(AssetBundleLoadingPattern pattern) : base(pattern)
        {
            
        }
        
        public override IEnumerator LoadRootManiFest(ManifestInitializeSuccess SuccessCallback)
        {
            var rootManiFestPath = Path.Combine(ResourceConfigure.LocalAssetPath, ResourceConfigure.RootAssetBundleName); 
            var assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(rootManiFestPath);
            yield return assetBundleCreateRequest;

            if (assetBundleCreateRequest.assetBundle == null) // 加载失败
            {
                AssetLogger.LogToFile("Error, ManiFest File Load Failed");
                yield break;
            }

            var manifestRequest = assetBundleCreateRequest.assetBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
            yield return manifestRequest;

            if (manifestRequest.asset == null)  // 加载失败 
            {
                AssetLogger.LogToFile("Error, AssetBundleManifest Is Null");
                yield break;
            }

            var manifest = manifestRequest.asset as AssetBundleManifest;
            yield return null;
            
            SuccessCallback(manifest, AssetBundleLoadingPattern);
        }
    }

    internal class SimulationManiFestInitializer : ManiFestInitializer
    {
        public SimulationManiFestInitializer(AssetBundleLoadingPattern pattern) : base(pattern)
        {
        }

        public override IEnumerator LoadRootManiFest(ManifestInitializeSuccess SuccessCallback)
        {
            yield return null;
            // 编辑器里用AssetDatabase,AssetBundleManifest传null即可
            SuccessCallback(null, AssetBundleLoadingPattern); 
        }
    }
}