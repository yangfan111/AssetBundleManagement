using AssetBundleManagement;
using Assets.AssetBundleManagement.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AssetBundleManagement
{
    internal class AssetLoadOperation : ILoadOperation
    {
        private AssetLoader m_AssetLoader;
        private AssetInfo m_AssetInfo;
        internal System.Action<UnityEngine.Object> OnAssetLoadComplete;
        private IResourceUpdater m_UpdateMgr;


        internal AssetLoadOperation(IResourceUpdater mgr,AssetInfo assetInfo, AssetBundleLoadingPattern pattern)
        {
            m_AssetLoader = AssetBundleFactoryFacade.CreateAssetLoader(pattern);
            m_UpdateMgr = mgr;
            m_AssetInfo = assetInfo;
        }
        internal void BeginOperation(AssetBundle assetBundle)
        {
            if(assetBundle == null) //load failed
            {
                m_UpdateMgr.RemoveLoadingOperation(this);
                if (OnAssetLoadComplete != null)
                    OnAssetLoadComplete(null);
                m_AssetLoader = null;
            }
            else
            {
                m_AssetLoader.Begin(assetBundle, m_AssetInfo);
                m_UpdateMgr.AddLoadingOperation(this);
            }
       
        }
   

        public void PollOperationResult()
        {
            UnityEngine.Object assetObject;
            if (m_AssetLoader.GetResult(out assetObject))
            {
                m_UpdateMgr.RemoveLoadingOperation(this);
                if (OnAssetLoadComplete != null)
                    OnAssetLoadComplete(assetObject);
                m_AssetLoader = null;
            }
        }

        public override string ToString()
        {
            return string.Format("LoadAsset:{0}", m_AssetInfo);
        }
    }
}
