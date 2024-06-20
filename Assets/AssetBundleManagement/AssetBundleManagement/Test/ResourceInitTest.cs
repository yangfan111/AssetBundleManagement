#if false
using AssetBundleManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.AssetBundleManagement.Test
{
    public class ResourceInitTest
    {
        public IEnumerator TestInit(ICoRoutineManager coRoutine)
        {
            AssetLogger.Init();
            // 用自己及建的测试
              ResourceConfigure.Initialize(false, AssetBundleLoadingPattern.AsyncLocal, "Assets/StreamingAssets", "StreamingAssets");
        //   ResourceConfigure.Initialize(false, AssetBundleLoadingPattern.AsyncLocal, "D:/client_for_nr_dev/AssetBundles/Windows", "Windows");
            yield return ResourceProvider.InitResourceMananger(coRoutine);
       

            // 用ssjj2的资源测试
            //ResourceConfigure.Initialize(false, AssetBundleLoadingPattern.AsyncLocal, "D:/client_for_nr_dev/AssetBundles/Windows", "Windows");
        }
        
        // 测试一下同步加载manifest的时间
        public void LoadRootManiFest()
        {
            var time1 = DateTime.UtcNow.Ticks;
            var rootManiFestPath = Path.Combine(ResourceConfigure.LocalAssetPath, ResourceConfigure.RootAssetBundleName); 
            var assetBundleCreateRequest = AssetBundle.LoadFromFile(rootManiFestPath);
            var time2 = DateTime.UtcNow.Ticks;
            Debug.Log("step1"+ (time2-time1)/10000);



            var manifestRequest = assetBundleCreateRequest.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            var time3 = DateTime.UtcNow.Ticks;
            Debug.Log("step2"+ (time3-time2)/10000);
            if (manifestRequest == null)  // 加载失败 
            {
                // todo: 报错
                return;
            }

            var manifest = manifestRequest as AssetBundleManifest; 
            
            string[] allAssetBundlesNameArray = manifest.GetAllAssetBundles();
            var assetBundleInfoDict = new Dictionary<string, AssetBundleRequestOptions>(allAssetBundlesNameArray.Length);
            foreach (var bundleName in allAssetBundlesNameArray)
            {
                AssetBundleRequestOptions bundleInfo = new AssetBundleRequestOptions();
                bundleInfo.RealName = bundleName;
                bundleInfo.Dependencies = manifest.GetAllDependencies(bundleName);
                bundleInfo.Pattern = AssetBundleLoadingPattern.AsyncLocal;
                assetBundleInfoDict.Add(bundleName, bundleInfo);
            }
            var time4 = DateTime.UtcNow.Ticks;
            Debug.Log("step3"+ (time4-time3)/10000);
        }
    }
}
#endif