using AssetBundleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetBundleManagement
{
 
    internal class BundleLoadCallbacks
    {
        // 外部回调事件注册
        private System.Action<AssetBundleResourceHandle> m_BundleSelfLoadComplete;
      //  private System.Action<AssetBundleResourceHandle> m_BundleFullLoadComplete;
        private System.Action<AssetBundleResourceHandle> m_BundleReferenceChanged;
        private System.Action<AssetBundleResourceHandle> m_BundleWillDestroy;
        public bool HasInvokeBundleFullLoaded { get;  set; }
        public BundleLoadCallbacks(System.Action<AssetBundleResourceHandle> bundleSelfLoadComplete,
                              //      System.Action<AssetBundleResourceHandle> bundleFullLoadComplete,
                                    System.Action<AssetBundleResourceHandle> bundleReferenceChanged,
                                    System.Action<AssetBundleResourceHandle> bundleWillDestroy)
        {
            m_BundleSelfLoadComplete += bundleSelfLoadComplete;
          //  m_BundleFullLoadComplete += bundleFullLoadComplete;
            m_BundleReferenceChanged += bundleReferenceChanged;
            m_BundleWillDestroy += bundleWillDestroy;
        }
        public void SetBundleSelfLoadComplete(System.Action<AssetBundleResourceHandle> action)
        {
            m_BundleSelfLoadComplete += action;
        }

        // public void SetBundleFullLoadComplete(System.Action<AssetBundleResourceHandle> action)
        // {
        //     m_BundleFullLoadComplete += action;
        // }

        public void SetBundleReferenceChanged(System.Action<AssetBundleResourceHandle> action)
        {
            m_BundleReferenceChanged += action;
        }

        public void SetBundleWillDestroy(System.Action<AssetBundleResourceHandle> action)
        {
            m_BundleWillDestroy += action;
        }
        public void InvokeBundleSelfLoadComplete(AssetBundleResourceHandle handle)
        {
            if (m_BundleSelfLoadComplete != null)
            {
                m_BundleSelfLoadComplete(handle);
                m_BundleSelfLoadComplete = null;
            }
        }

        // public void InvokeBundleFullLoadComplete(AssetBundleResourceHandle handle)
        // {
        //     HasInvokeBundleFullLoaded = true;
        // }

        public void InvokeBundleReferenceChanged(AssetBundleResourceHandle handle)
        {
            if (m_BundleReferenceChanged != null)
            {
                m_BundleReferenceChanged(handle);
            }
        }

        public void InvokeBundleWillDestroy(AssetBundleResourceHandle handle)
        {
            if (m_BundleWillDestroy != null)
            {
                m_BundleWillDestroy(handle);
                m_BundleWillDestroy = null;
            }
        }
    }
}
