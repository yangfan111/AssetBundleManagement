using System.Collections.Generic;
using AssetBundleManagement.ObjectPool;
using Common;
using UnityEngine;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    internal class PreloadResourceLoader:ILoadOwner
    {
        public ILoadOwner GetRootOwner()
        {
            return m_RootOwner;
        }
        private ResourceLoadManager m_ResourceLoad;
        private InstanceObjectPoolContainer m_Pool;
        private ILoadOwner m_RootOwner;
        internal PreloadResourceLoader(ILoadOwner rootRootOwner, ResourceLoadManager resourceLoad, InstanceObjectPoolContainer pool)
        {
            m_RootOwner        = rootRootOwner;
            m_ResourceLoad = resourceLoad;
            m_Pool         = pool;
            resourceLoad.Event.Register(ResourceEventType.LoadAssetOptionComplete,HandleLoadAssetOptionComplete);
        }
        void HandleLoadAssetOptionComplete(ResourceEventArgs eventArgs)
        {
            LoadAssetOptionCompleteEventArgs loadAssetOptionCompleteEvent = eventArgs as LoadAssetOptionCompleteEventArgs;
            if (loadAssetOptionCompleteEvent.LoadOption.Owner == this)
            {
                loadAssetOptionCompleteEvent.Handled = true;
                m_PreloadResourceList.Add(loadAssetOptionCompleteEvent.LoadResult);
                loadAssetOptionCompleteEvent.ProcessAssetComplete(m_ResourceLoad.AssetCompletePoster);
            }
        }

        public void Dispose()
        {
            m_ResourceLoad.Event.UnRegister(ResourceEventType.LoadAssetOptionComplete,HandleLoadAssetOptionComplete);

        }

        private List<IResourceHandler> m_PreloadResourceList = new List<IResourceHandler>();
     
        internal void PreloadResource(AssetInfo assetInfo, LoadPriority loadPriority, OnPreloadComplete onPreloadComplete)
        {
            LoadResourceOption resourceOption = new LoadResourceOption(OnPreloadObjectSucess, loadPriority, onPreloadComplete);
            resourceOption.Owner = this;
            m_ResourceLoad.LoadAsset(assetInfo, false, ref resourceOption);
            //   LoadResourceOption resourceOption = new LoadResourceOption(OnPreloadObjectSucess, loadPriority,onPreloadComplete);
            //       poolContainer.InstantiateAsync(assetInfo,ref resourceOption);

        }


        void OnPreloadObjectSucess(System.Object context, UnityObject resourceHandler)
        {

          //  m_PreloadInstanceList.Add(resourceHandler.GetNewHandler());
            OnPreloadComplete callback = context as OnPreloadComplete;
            if (callback != null)
                callback(resourceHandler.Address, resourceHandler.IsValid());
          if (resourceHandler.AsGameObject)
          {
               LoadResourceOption resourceOption = new LoadResourceOption(OnPreloadInstanceSucess, LoadPriority.P_MIDDLE);
               resourceOption.Owner = m_RootOwner;
               m_Pool.Instantiate(resourceHandler.Address, ref resourceOption);
          }

            //var go = resourceHandler.AsGameObject;
        
        }
        //预加载isntance实例，然后回池，再设置延迟对象池待机销毁时间
        void OnPreloadInstanceSucess(System.Object context, UnityObject resourceHandler)
        {
            m_Pool.SetInstancePoolRetainTime(resourceHandler.Address,ResourceConfigure.PreloadInstanceRetainTime);
            resourceHandler.Release();
        }
        public void Destroy()
        {
            foreach (var preloadHandler in m_PreloadResourceList)
            {
                preloadHandler.Release();
            }
            m_PreloadResourceList.Clear();
       //     foreach (var preloadHandler in m_PreloadInstanceList)
            {
         //       preloadHandler.Release();
            }
         //   m_PreloadInstanceList.Clear();
        }


       
    }
}