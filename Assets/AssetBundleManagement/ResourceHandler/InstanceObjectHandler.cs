using AssetBundleManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using AssetBundleManagement.ObjectPool;
using UnityEngine;

namespace AssetBundleManagement
{
    public class InstanceObjectHandler : ResourceHandler
    {
        private UnityEngine.Object m_Instance;
        private InstanceObjectPool m_RecyclePool;

        public override AssetInfo AssetKey
        {
            get { return m_RecyclePool.AssetInfo; }
        }

        public override UnityEngine.Object ResourceObject
        {
            get { return m_Instance; }
        }
        public InstanceObjectHandler() { }

        public override ResourceHandlerType HandlerType { get { return ResourceHandlerType.Instance; } }
        

        internal static InstanceObjectHandler Alloc(UnityEngine.Object instance, InstanceObjectPool recyclePool)
        {
            InstanceObjectHandler instanceOperationHandler = ResourceObjectHolder<InstanceObjectHandler>.Allocate();
            instanceOperationHandler.m_Instance = instance;
            instanceOperationHandler.m_RecyclePool = recyclePool;
            return instanceOperationHandler;
        }

        public void RefreshHandlerId(int handlerId)
        {
            this.m_handleId = handlerId;
        }

        public override void Release()
        {
            if (m_RecyclePool != null && !m_RecyclePool.IsDisposed)
            {
                if (!m_RecyclePool.Release(this))
                {
                    throw new Exception("InstanceObjectHandler has been release can not release again");
                }
                // m_RecyclePool = null;
                // m_Instance = null;
            }
            else
            {
                throw new Exception("Instance Recycle Exception,objectPool dont exist");
            }
        }

        public override void Reset()
        {
            m_Instance = null;
            m_RecyclePool = null;
        }

        public override void CollectProfileInfo(ref StringBuilder stringBuilder)
        {
            ResourceOption.GetOnSuccessCallMethodInfo(ref stringBuilder);
        }
    }
}
