using AssetBundleManagement;
using AssetBundleManagement;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared.Scripts.Effect.Weapon.Specific;
using UnityEngine;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    internal class AssetLoadOperation : ILoadOperation
    {
        private AssetLoader m_AssetLoader;
        private AssetInfo m_AssetInfo;
        internal System.Action<UnityEngine.Object> OnAssetLoadComplete;
        private IResourceUpdater m_UpdateMgr;


        internal AssetLoadOperation(IResourceUpdater mgr,AssetInfo assetInfo, AssetLoader loader,bool isSync)
        {
            m_AssetLoader = loader;
            m_AssetLoader.SetIsSync(isSync);
            m_UpdateMgr      = mgr;
            m_AssetInfo      = assetInfo;
            m_AlreadyLoading = false;
        }

        private bool m_AlreadyLoading;
        internal void SetSyncLoadMode()
        {
            if (!m_AlreadyLoading && m_AssetLoader != null)
            {
                m_AssetLoader.SetIsSync(true);
            }
          
        }
        internal void BeginOperation(AssetBundle assetBundle)
        {
            m_AlreadyLoading = true;
            if(assetBundle == null && !m_AssetLoader.IsSimualtion) //load failed
            {
                m_UpdateMgr.RemoveLoadingOperation(this);
                if (OnAssetLoadComplete != null)
                    OnAssetLoadComplete(null);
                m_AssetLoader = null;
            }
            else
            {
                m_AssetLoader.Begin(assetBundle, m_AssetInfo.ToNewAsset());
                m_UpdateMgr.AddLoadingOperation(this);
            }
       
        }
   

        public void PollOperationResult()
        {
            if (m_AssetLoader == null)
            {
                m_UpdateMgr.RemoveLoadingOperation(this);
                return;
            }
                
            UnityEngine.Object assetObject;
            if (m_AssetLoader.GetResult(out assetObject))
            {
                m_UpdateMgr.RemoveLoadingOperation(this);
                m_AssetLoader = null;

                if (OnAssetLoadComplete != null)
                    OnAssetLoadComplete(assetObject);
            }
        }

        public LoadOpType LoadOp
        {
            get { return LoadOpType.Asset; }
        }

        public override string ToString()
        {
            return string.Format("LoadAsset:{0}", m_AssetInfo);
        }
    }
   
}
