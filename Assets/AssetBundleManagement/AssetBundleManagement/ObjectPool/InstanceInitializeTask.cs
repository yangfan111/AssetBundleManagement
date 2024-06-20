using System;
using System.Collections.Generic;
using System.Diagnostics;
using Core.Utils;
using UnityEngine;

namespace AssetBundleManagement.ObjectPool
{
    public class InstanceInitializeTask:IResourcePoolObject
    {
        private InstanceObjectPool attachPool;
        private LoadResourceOption resourceOption;
        private Transform instanceObjectParent;
        private bool isDisposed;
        public InstanceInitializeTask(){ }
        private static LoggerAdapter _logger = new LoggerAdapter("InstanceInitializeTask");
        public void Init(InstanceObjectPool objectPool, Transform parent, ref LoadResourceOption loadOption)
        {
            attachPool           = objectPool;
            resourceOption       = loadOption;
            instanceObjectParent = parent;
            isDisposed           = false;
            attachPool.IncrWaitExecuteTask();
        }

        public void LoadCancel(System.Object ctx)
        {
            if (resourceOption.Context == ctx)
            {
                isDisposed = true;
                GMVariable.loggerAdapter.InfoFormat("{0} LoadTask contexts has been destroyed", attachPool.AssetInfo,new StackTrace());
            }
        }
        public void ExecuteAndFree(ResourceLoadManager resourceLoadManager)
        {
            try
            {
                if (!attachPool.IsUsableState || isDisposed ||resourceLoadManager == null)
                {
                    if(isDisposed)
                    {
                        GMVariable.loggerAdapter.InfoFormat("{0} LoadTask return loadFailed", attachPool.AssetInfo);
                    }
                    FailedAndFree(resourceLoadManager);
                    return;
                }
                var instanceObjectHandler = attachPool.GetInstanceObjectHandler(instanceObjectParent, resourceOption,true);
                AssertUtility.Assert(instanceObjectHandler != null);
                resourceLoadManager.OnLoadAssetComplete(instanceObjectHandler,resourceOption);
                //   resourceOption.CallOnSuccess(instanceObjectHandler, attachPool.AssetInfo);
                attachPool.DecrWaitExecuteTask();
                ResourceObjectHolder<InstanceInitializeTask>.Free(this);
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("err:{0},stack:{1}", e.Message, e.StackTrace);
            }
          
        }
        void FailedAndFree(ResourceLoadManager resourceLoadManager)
        {
            if(resourceLoadManager != null)
                resourceLoadManager.OnLoadAssetComplete(new FailedObjectHandler(attachPool.AssetInfo),resourceOption);
            //      resourceOption.CallOnFailed(attachPool.AssetInfo);
            attachPool.DecrWaitExecuteTask();
            ResourceObjectHolder<InstanceInitializeTask>.Free(this);
        }

        public void Reset()
        {
            attachPool           = null;
            resourceOption       = default(LoadResourceOption);
            instanceObjectParent = null;
            isDisposed           = false;
        }
    }
}