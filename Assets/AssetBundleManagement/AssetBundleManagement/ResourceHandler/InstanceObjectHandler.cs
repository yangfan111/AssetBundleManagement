using AssetBundleManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using AssetBundleManagement.ObjectPool;
using UnityEngine;
using Core.Utils;
using Utils.AssetManager;
using Object = UnityEngine.Object;

namespace AssetBundleManagement
{
    
    public struct InstanceObjectHandler : IResourceHandler
    {
        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.InstanceObjectHandler",false);
        private UnityEngine.Object m_Instance;
        private InstanceObjectPool m_RecyclePool;
        private int m_HandleId;
        public int GetHandleId() { return m_HandleId; }
        internal InstanceObjectHandler(UnityEngine.Object instance, InstanceObjectPool recyclePool, int handleId)
        {
            m_HandleId = handleId;
            m_Instance = instance;
            m_RecyclePool = recyclePool;
         //   Context = context;
            
        }
        public ResourceHandlerType HandlerType
        {
            get { return ResourceHandlerType.Instance; }
        }
        public  AssetInfo AssetKey
        {
            get { return m_RecyclePool.AssetInfo; }
        }
        public bool IsValid()
        {
            return m_HandleId > 0 && m_Instance != null;
        }
        public  UnityEngine.Object ResourceObject
        {
            get { return m_Instance; }
        }
        public UnityEngine.GameObject AsGameObject { get { return ResourceObject as GameObject; } }
      //   public ResourceHandlerType HandlerType { get { return ResourceHandlerType.Instance; } }

      //  public object Context { get; private set; }

        void Reset()
        {
            m_Instance = null;
            m_RecyclePool = null;
    //        Context = null;
            m_HandleId = -1;
        }

        internal void SetInstancePoolRetainTime(int retainSeconds)
        {
            if (m_RecyclePool != null && !m_RecyclePool.IsDisposed)
            {
                m_RecyclePool.SetRetainTime(retainSeconds);
            }

        }
        public void Release()
        {
            if (m_HandleId>0)
            {
                if (m_RecyclePool != null && !m_RecyclePool.IsDisposed)
                {
                    if (m_RecyclePool.Release(this))
                    {
                        ResourceProvider.Profiler.RemoveInstanceObjectProfileDataNotify(ref this);
                    }
                    else
                    {
                        
                        s_logger.ErrorFormat("{0} InstanceObjectHandler exception:handler dont in recylePool,object exist {1}", AssetKey,m_Instance != null);
                        if (m_Instance != null)
                        {
                            Object.Destroy(m_Instance);
                        }
                    }
                    // m_RecyclePool = null;
                    // m_Instance = null;
                }
                else
                {
                     s_logger.ErrorFormat("{0} InstanceObjectHandler exception:RecyclePool has disposed,object exist {1}", AssetKey,m_Instance != null);
                     if (m_Instance != null)
                     {
                         Object.Destroy(m_Instance);
                     }
                }
                Reset();
            }
            else
            {
                s_logger.ErrorFormat("InstanceObjectHandler has been released");
                if(m_Instance != null)
                    Object.Destroy(m_Instance);
            }
        }

        public void ForceRelease()
        {
        }
    }
}
