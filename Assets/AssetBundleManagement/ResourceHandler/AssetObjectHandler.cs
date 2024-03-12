using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetBundleManagement
{

    public class AssetObjectHandler : ResourceHandler
    {
        public override UnityEngine.Object ResourceObject
        {
            get { return m_AssetResource.AssetObject; }
        }
        public override AssetInfo AssetKey
        {
            get { return m_AssetResource.AssetKey; }
        }

        public override ResourceHandlerType HandlerType
        {
            get{ return ResourceHandlerType.Asset; }
        }

        private AssetResourceHandle m_AssetResource;
        
        public AssetObjectHandler() { }
        internal static AssetObjectHandler Alloc(LoadResourceOption resourceOption, AssetResourceHandle assetResource, int handleId)
        {
            AssetObjectHandler asyncAssetOperation = ResourceObjectHolder<AssetObjectHandler>.Allocate();
            asyncAssetOperation.m_AssetResource = assetResource;
            asyncAssetOperation.Context = resourceOption.Context;
            asyncAssetOperation.m_handleId = handleId;
            asyncAssetOperation.ResourceOption = resourceOption;
            return asyncAssetOperation;
        }

        public override void Release()
        {

            if (m_AssetResource != null && m_handleId > 0)
            {
                if (!m_AssetResource.ReleaseHandleId(m_handleId))
                {
                    throw new Exception("AsyncAssetOperationHandle handleId dont match AssetObject");
                }
                m_AssetResource = null;
                m_handleId = 0;
                ResourceObjectHolder<AssetObjectHandler>.Free(this);

            }
            else
            {
                Debug.LogError("AsyncAssetOperationHandle has been disposed or not initialize");
            }
          
        }
        
        public override void CollectProfileInfo(ref StringBuilder stringBuilder)
        {
            ResourceOption.GetOnSuccessCallMethodInfo(ref stringBuilder);
        }

        public override void Reset()
        {
            
        }
    }
}