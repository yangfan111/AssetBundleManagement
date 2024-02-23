using UnityEngine;

namespace AssetBundleManagement
{
    public abstract class AssetBundleLoader
    {
        public abstract void Begin(AssetBundleRequestOptions loadRequestOptions);

        public abstract AssetBundle GetResult();

    }

    public class AssetBundleFileAsyncLoader : AssetBundleLoader
    {
        private AssetBundleCreateRequest m_LoadFromFileRequest;
        public override void Begin(AssetBundleRequestOptions loadRequestOptions)
        {
            m_LoadFromFileRequest = AssetBundle.LoadFromFileAsync(loadRequestOptions.RealName, loadRequestOptions.Crc);
        }

        public override AssetBundle GetResult()
        {
            if (m_LoadFromFileRequest.isDone)
            {
                return m_LoadFromFileRequest.assetBundle;
            }

            return null;
        }
    }

    public static class AssetBundleLoadFactory
    {
        public static AssetBundleLoader CreateAssetBundleLoader()
        {
            return new AssetBundleFileAsyncLoader();
        }
    }
}