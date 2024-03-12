using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AssetBundleManagement
{
    public abstract class ResourceHandler: IResourcePoolObject
    {
        public int m_handleId { get; protected set; }
        public abstract UnityEngine.Object ResourceObject { get; }
        public System.Object Context { get; protected set; }
        public abstract ResourceHandlerType HandlerType { get; }
        public LoadResourceOption ResourceOption;
        public abstract void Release();

        public abstract void Reset();

        public abstract void CollectProfileInfo(ref StringBuilder stringBuilder);

        public  UnityEngine.GameObject AsGameObject { get { return ResourceObject as GameObject; } }

        public abstract AssetInfo AssetKey { get; }


    }
}
