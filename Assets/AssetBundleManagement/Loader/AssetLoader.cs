using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssetBundleManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.AssetBundleManagement.Loader
{
    internal abstract class AssetLoader
    {
        internal abstract void Begin(AssetBundle attachedAssetBundle, AssetInfo assetInfo);
        internal abstract bool GetResult(out UnityEngine.Object result);
    }

    internal class AssetLoadFromFileAsyncLoader: AssetLoader
    {
        private AssetBundleRequest m_LoadAssetRequest;

        internal AssetLoadFromFileAsyncLoader()
        { }
        internal override void  Begin(AssetBundle attachedAssetBundle, AssetInfo assetInfo)
        {
            m_LoadAssetRequest = attachedAssetBundle.LoadAssetAsync(assetInfo.AssetName);
        }
        internal override bool GetResult(out UnityEngine.Object result)
        {
            result = null;
            if (m_LoadAssetRequest.isDone)
            {
                result = m_LoadAssetRequest.asset;
                m_LoadAssetRequest = null;
                return true;
            }
            return false;
        }
    }
    
    internal class AssetLoadFromHotfixLoader:AssetLoadFromFileAsyncLoader
    {
        private AssetBundleRequest m_LoadAssetRequest;

        internal override void  Begin(AssetBundle attachedAssetBundle, AssetInfo assetInfo)
        {
            m_LoadAssetRequest = attachedAssetBundle.LoadAssetAsync(assetInfo.AssetName);
        }
        internal override bool GetResult(out UnityEngine.Object result)
        {
            result = null;
            if (m_LoadAssetRequest.isDone)
            {
                result = m_LoadAssetRequest.asset;
                m_LoadAssetRequest = null;
                return true;
            }
            return false;
        }
    }

    internal class AssetLoadFromSimulateLoader : AssetLoader
    {
        private UnityEngine.Object asset;
            

        /// <param name="attachedAssetBundle">该值常年为一个空的AssetBundle， 因为Simulate方式无法获得AB</param>
        internal override void Begin(AssetBundle attachedAssetBundle, AssetInfo assetInfo)
        {
#if UNITY_EDITOR
            string[] assetPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(attachedAssetBundle.name, assetInfo.AssetName);
            if (assetPaths.Length == 0)
            {
                if (assetInfo.AssetName.StartsWith("Assets/"))
                    asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetInfo.AssetName);
            }
            else
                asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPaths[0]);
#endif
        }

        internal override bool GetResult(out Object result)
        {
            result = asset;
            asset = null;
            return true;
        }
    }
}
