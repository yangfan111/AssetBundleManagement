using System;
using System.Collections.Generic;
using Assets.ABSystem.Scripts.ResourceProvider;
using UnityEngine;

namespace AssetBundleManagement
{
        
    public class AsyncAssetOperationHandle
    {
        public System.Object AssetObject
        {
            get { return m_AssetResource.AssetObject; }
        }
        public System.Object Context       { get; private set; }
        private AssetObjectResource m_AssetResource;

        internal void Allocate( System.Object ctx,AssetObjectResource assetResource)
        {
            m_AssetResource = assetResource;
            Context         = ctx;
        }
 

        public void Release()
        {
            if (m_AssetResource != null)
            {
                m_AssetResource.ReleaseReference();
                m_AssetResource = null;

            }
            else
            {
                Debug.LogError("AsyncAssetOperationHandle has been disposed");
            }
        }
    }
}