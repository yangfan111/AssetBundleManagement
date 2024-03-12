using AssetBundleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.AssetBundleManagement.Loader
{
    internal interface IAssetBundleLoadFactory
    {
        AssetBundleLoader CreateAssetBundleLoader();
        AssetLoader CreateAssetLoader();
    }
    internal class AssetBundleLocalAsyncFactory: IAssetBundleLoadFactory
    {
        public  AssetBundleLoader CreateAssetBundleLoader()
        {
            return new AssetBundleFileAsyncLoader();
        }
        public AssetLoader CreateAssetLoader()
        {
            return new AssetLoadFromFileAsyncLoader();
        }
    }

    internal class AssetBundleSimulateFactory : IAssetBundleLoadFactory
    {
        public AssetBundleLoader CreateAssetBundleLoader()
        {
            return new AssetBundleSimulateLoader();
        }

        public AssetLoader CreateAssetLoader()
        {
            return new AssetLoadFromSimulateLoader();
        }
    }
    
    internal class AssetBundleHotFixFactory : IAssetBundleLoadFactory
    {
        public AssetBundleLoader CreateAssetBundleLoader()
        {
            return new AssetBundleHotFixLoader();
        }

        public AssetLoader CreateAssetLoader()
        {
            return new AssetLoadFromHotfixLoader();
        }
    }
    
    
    
    internal static class AssetBundleFactoryFacade
    {
        private static Dictionary<AssetBundleLoadingPattern, IAssetBundleLoadFactory> loadFactoryDic =
            new Dictionary<AssetBundleLoadingPattern, IAssetBundleLoadFactory>();
        
        private static IAssetBundleLoadFactory GetFactory(AssetBundleLoadingPattern pattern)
        {
            IAssetBundleLoadFactory factory;
            if (!loadFactoryDic.TryGetValue(pattern, out factory))
            {
                if (pattern == AssetBundleLoadingPattern.AsyncLocal)
                    factory = new AssetBundleLocalAsyncFactory();
                else if(pattern == AssetBundleLoadingPattern.Simulation)
                    factory = new AssetBundleSimulateFactory();
                else if(pattern == AssetBundleLoadingPattern.Hotfix)
                    factory = new AssetBundleHotFixFactory();

                loadFactoryDic[pattern] = factory;
            }

            return factory;
        }
        public static AssetBundleLoader CreateAssetBundleLoader(AssetBundleLoadingPattern pattern)
        {
            return GetFactory(pattern).CreateAssetBundleLoader();
        }
        public static AssetLoader CreateAssetLoader(AssetBundleLoadingPattern pattern)
        {
            return GetFactory(pattern).CreateAssetLoader();
        }
    }
}
