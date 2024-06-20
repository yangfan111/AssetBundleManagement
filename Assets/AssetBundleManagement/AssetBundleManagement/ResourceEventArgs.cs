using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Core.Utils;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    public enum ResourceEventType
    {
        LoadBundleDirectly,
        LoadBundleIndirectly,
        LoadBundleSelfSucess,
        LoadBundleFullSucess,
        LoadBundleFailed,
        WillDestroyBundle,
        LoadAssetWaitForBundle,
        LoadAssetStart,
        LoadAssetSucess,
        LoadAssetFailed,
        WillDestroyAsset,
        LoadAssetOptionComplete,

        LoadSceneStart,
        LoadSceneSucess,
        LoadSceneFailed,
        AddQueueStart,

        Length,
    }
    internal abstract class ResourceEventArgs : System.EventArgs, IResourcePoolObject
    {
        public ResourceEventArgs() { }
        public  void Reset() { }
        public ResourceEventType EventType;
        public int EventId { get { return (int)EventType; } } 
        

        public abstract void Free();
    }

    internal class SceneRelatedEventArgs : ResourceEventArgs
    {
        public SceneResourceHandle Scene;
        public LoadPriority Priority;
        public bool IsAync;

        public override void Free()
        {
            Scene = null;
            ResourceObjectHolder<SceneRelatedEventArgs>.Free(this);
        }
        public override string ToString()
        {
            return String.Format("{0}:{1}", EventType, Scene);
        }
    }
    internal class BundleRelatedEventArgs : ResourceEventArgs
    {
       
        public AssetBundleResourceHandle Bundle;

        public override void Free()
        {
            Bundle = null;
            ResourceObjectHolder<BundleRelatedEventArgs>.Free(this);
        }
        public override string ToString()
        {
            return String.Format("{0}:{1}", EventType, Bundle);
        }
    }
    internal class AssetRelatedEventArgs : ResourceEventArgs
    {
        public AssetResourceHandle Asset;
        public LoadPriority Priority;
        public bool IsAync;
        public RecordAssetType RecordAssetType;

        public override void Free()
        {
            Asset = null;
            ResourceObjectHolder<AssetRelatedEventArgs>.Free(this);
        }
        public override string ToString()
        {
            return String.Format("{0}:{1}", EventType, Asset);
        }
    }

    internal class LoadAssetOptionCompleteEventArgs : ResourceEventArgs
    {
        public LoadResourceOption LoadOption;
        public IResourceHandler LoadResult;
        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.LoadAssetOptionCompleteEventArgs",false);
        public bool IsLoadSucess
        {
            get { return LoadResult.IsValid(); }
        }
        public bool IsAssetHandler
        {
            get { return LoadResult.HandlerType == ResourceHandlerType.Asset; }
        }
        public AssetInfo LoadAsset
        {
            get { return LoadResult.AssetKey; }
        }
        public bool Handled;
        public bool WillDispose;

      

        public void ProcessAssetComplete(IAssetCompletePoster poster)
        {
            if (poster == null || LoadOption.ImmeidateCallbackIfAssetExist)
            {
                LoadOption.CallOnComplete(LoadResult);
                WillDispose = true;
            }
            else
            {
                poster.EnqueuePostAssetEvent(this);
            }
        }
        public override void Free()
        {
            if (!Handled)
            {
                s_logger.ErrorFormat("LoadAssetOptionComplete {0} dont has been handled,LoadResourceOption.Owner may be disposed",LoadAsset);
                LoadResult.Release();
                Release();
                return;
            }

            if (WillDispose)
            {
                Release();
          
            }
        }

         void Release()
         {
             WillDispose = false;
             Handled     = false;
            LoadOption   = default(LoadResourceOption);
            LoadResult   = null;
            ResourceObjectHolder<LoadAssetOptionCompleteEventArgs>.Free(this);
        }

    
    }
}
