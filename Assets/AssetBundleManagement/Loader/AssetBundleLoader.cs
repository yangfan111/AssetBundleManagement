using System.IO;
using Assets.AssetBundleManagement.Loader;
using UnityEngine;

namespace AssetBundleManagement
{
    internal abstract class AssetBundleLoader
    {
        internal abstract void Begin(AssetBundleRequestOptions loadRequestOptions);

        internal abstract bool GetResult(out AssetBundle ab);

    }

    internal class AssetBundleFileAsyncLoader : AssetBundleLoader
    {
        private AssetBundleCreateRequest m_LoadFromFileRequest;
        internal override void Begin(AssetBundleRequestOptions loadRequestOptions)
        {
            string loadPath = Path.Combine(ResourceConfigure.LocalAssetPath, loadRequestOptions.RealName);
            m_LoadFromFileRequest = AssetBundle.LoadFromFileAsync(loadPath);
        }

        internal override bool GetResult(out AssetBundle ab)
        {
            ab = null;
            if (m_LoadFromFileRequest.isDone)
            {
                ab = m_LoadFromFileRequest.assetBundle;
                m_LoadFromFileRequest = null;
                return true;
            }

            return false;
        }
    }

    internal class AssetBundleHotFixLoader : AssetBundleLoader
    {
        private AssetBundleCreateRequest m_LoadFromFileRequest;
        internal override void Begin(AssetBundleRequestOptions loadRequestOptions)
        {
            string loadPath = Path.Combine(ResourceConfigure.LocalAssetPath+"_hotfix", loadRequestOptions.RealName);
            m_LoadFromFileRequest = AssetBundle.LoadFromFileAsync(loadPath);
        }

        internal override bool GetResult(out AssetBundle ab)
        {
            ab = null;
            if (m_LoadFromFileRequest.isDone)
            {
                ab = m_LoadFromFileRequest.assetBundle;
                m_LoadFromFileRequest = null;
                return true;
            }

            return false;
        }
    }

    internal class AssetBundleSimulateLoader : AssetBundleLoader
    {
        private static AssetBundle emptyAssetBundle = new AssetBundle();
        internal override void Begin(AssetBundleRequestOptions loadRequestOptions)
        {
            // simulate方式无需加载ab
        }

        internal override bool GetResult(out AssetBundle ab)
        {
            // 返回一个没有用的AssetBundle对象， 主要是为了框架内有些判断保持一致，实际这个AssetBundle对象没有用处
            ab = emptyAssetBundle;
            return true;
        }
    }


}