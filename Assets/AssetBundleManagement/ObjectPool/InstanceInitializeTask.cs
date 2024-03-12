using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleManagement.ObjectPool
{
    public class InstanceInitializeTask:IResourcePoolObject
    {
        private InstanceObjectPool attachPool;
        private LoadResourceOption resourceOption;
        private Transform instanceObjectParent;

        public InstanceInitializeTask(){ }
        
        public void Init(InstanceObjectPool objectPool, Transform parent, ref LoadResourceOption loadOption)
        {
            attachPool = objectPool;
            resourceOption = loadOption;
            instanceObjectParent = parent;
        }

        public void Execute()
        {
            var instanceObjectHandler = attachPool.GetInstanceObjectHandler(instanceObjectParent, resourceOption);
            if (instanceObjectHandler != null)
            {
                resourceOption.CallOnSuccess(instanceObjectHandler, attachPool.AssetInfo);
            }
            else
            {
                resourceOption.CallOnFailed(attachPool.AssetInfo);
            }
        }

        public void Reset()
        {
            attachPool = null;
        }
    }
}