
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Text;
using AssetBundleManagement;

public delegate void OnLoadResourceSuccess(ResourceHandler assetAccessor);

public delegate void OnLoadResourceFailure(string errorMessage, object context);
public delegate void OnGameObjectInstantiateHandler(AssetInfo path, LoadResourceOption loadResourceOption, AssetObjectHandler resourceHandle);

namespace AssetBundleManagement
{
    public struct LoadResourceOption
    {
        //callbacks
        private OnLoadResourceSuccess m_OnSuccess;

        private OnLoadResourceFailure m_OnFailure;

        internal OnGameObjectInstantiateHandler OnGameObjectAssetLoaded;
        public LoadPriority Priority;

        public System.Object Context;

        public ResourceHandlerType ResourceType;


        public LoadResourceOption(OnLoadResourceSuccess onSuccess, Object context, ResourceHandlerType loadResourceType, LoadPriority priority = LoadPriority.P_MIDDLE)
        {
            if (onSuccess == null)
                throw new ArgumentException("OnSuccess callback should not be null");
            m_OnSuccess = onSuccess;
            m_OnFailure = null;
            Priority = priority;
            Context = context;
            ResourceType = loadResourceType;
            OnGameObjectAssetLoaded = null;
        }
        public LoadResourceOption(OnLoadResourceSuccess onSuccess, OnLoadResourceFailure onFailure, ResourceHandlerType loadResourceType, LoadPriority priority, object context)
        {
            if (onSuccess == null)
                throw new ArgumentException("OnSuccess callback should not be null");
            m_OnSuccess = onSuccess;
            m_OnFailure = onFailure;
            Priority = priority;
            Context = context;
            ResourceType = loadResourceType;
            OnGameObjectAssetLoaded = null;
        }
        public void CallOnSuccess(ResourceHandler objectHandler, AssetInfo assetInfo)
        {
            ResourceProfiler.CallOnAssetSuccess(assetInfo);
            if ( OnGameObjectAssetLoaded != null && objectHandler.AsGameObject != null )
            {
                OnGameObjectAssetLoaded(assetInfo, this, objectHandler as AssetObjectHandler);
            }
            else
            {
                m_OnSuccess(objectHandler);
            }
        }
        public void CallOnFailed(AssetInfo assetInfo)
        {
            ResourceProfiler.CallOnAssetFailed(assetInfo);
            if (m_OnFailure != null)
                m_OnFailure(string.Format("Asset Object {0} load failed", assetInfo), Context);
        }

        /// <summary>
        /// 获得当前m_OnSuccess方法由应用层哪些函数持有，
        /// 用于查询ResourceHandler被应用层哪个对象持有，方便后续调试 handler未释放的问题
        /// </summary>
        /// <param name="stringBuilder"></param>
        public void GetOnSuccessCallMethodInfo(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("           [ResourceHandler]BindOnSuccessCallMethod:\n");
            foreach (var dDelegate in m_OnSuccess.GetInvocationList())
            {
                stringBuilder.AppendFormat("            class:{0}  Method:{1}\n",  
                    dDelegate.Target.GetType().Name, dDelegate.Method.Name);
            }
        }
    }
}
