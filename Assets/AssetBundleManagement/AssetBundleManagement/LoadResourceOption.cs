
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using AssetBundleManagement;
using Common;
using Core.Utils;
using Utils.AssetManager;

public delegate void OnPreloadComplete(AssetInfo AssetInfo,bool isSucess);

/// <summary>
/// 加载回调
/// </summary>
/// <param name="context">加载参数</param>
/// <param name="resource">加载资源结果，如果加载失败resource.IsValid() = false </param>
public delegate void OnLoadResourceComplete(System.Object context, UnityObject resource);

public delegate void OnLoadResourceFailure(string errorMessage,AssetInfo path, object context);
//public delegate void OnGameObjectInstantiateHandler(AssetInfo path, LoadResourceOption loadResourceOption);

namespace AssetBundleManagement
{
    public struct LoadResourceOption
    {
        //callbacks
        private OnLoadResourceComplete m_OnComplete;

     //   private OnLoadResourceFailure m_OnFailure;

       // internal OnGameObjectInstantiateHandler OnGameObjectAssetLoaded;
        public LoadPriority Priority;
        public bool ImmeidateCallbackIfAssetExist;

        public System.Object Context;
        
        internal ILoadOwner Owner; //实际从哪个ResourceProvider加载的
        

        private static LoggerAdapter _loggerAdapter = new LoggerAdapter("AssetBundleManagement.LoadResourceOption");
        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="onComplete">加载回调</param>
        /// <param name="context">加载回调参数,最终会传到回调的参数里</param>
        /// <param name="priority">加载优先级</param>
        public LoadResourceOption(OnLoadResourceComplete onComplete,object context,LoadPriority priority = LoadPriority.P_MIDDLE,bool immeidateCallbackIfAssetExist = false)
        {
            if (onComplete == null)
                AssetUtility.ThrowException("OnSuccess callback should not be null");
            m_OnComplete = onComplete;
          //  m_OnFailure = null;
            Priority                      = LoadPriority.P_MIDDLE;
            Context                       = context;
            ImmeidateCallbackIfAssetExist = immeidateCallbackIfAssetExist;
            Owner                         = null;
            //   ResourceType = loadResourceType;
            //  OnGameObjectAssetLoaded = null;
        }


        public LoadResourceOption( OnLoadResourceComplete onComplete,LoadPriority priority = LoadPriority.P_MIDDLE, object context = null,bool immeidateCallbackIfAssetExist = false)
        {
            if (onComplete == null)
                AssetUtility.ThrowException("OnSuccess callback should not be null");
            m_OnComplete = onComplete;
         //   m_OnFailure = onFailure;
            Priority                      = priority;
            Context                       = context;
            ImmeidateCallbackIfAssetExist = immeidateCallbackIfAssetExist;
            Owner                         = null;
            //       ResourceType = loadResourceType;
            //    OnGameObjectAssetLoaded = null;
        }

        public void CallOnComplete(IResourceHandler objectHandler)
        {
                try
                {
                    if(m_OnComplete != null)
                        m_OnComplete(Context, new UnityObject(objectHandler));
                }
                catch (Exception e)
                {
                    _loggerAdapter.ErrorFormat("asset onComplete error,{0}:{1}",e.Message,e.StackTrace);
                }
        }
 

        /// <summary>
        /// 获得当前m_OnSuccess方法由应用层哪些函数持有，
        /// 用于查询ResourceHandler被应用层哪个对象持有，方便后续调试 handler未释放的问题
        /// </summary>
        /// <param name="stringBuilder"></param>
        public void GetOnSuccessCallMethodInfo(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("           [ResourceHandler]BindOnSuccessCallMethod:\n");
            foreach (var dDelegate in m_OnComplete.GetInvocationList())
            {
                stringBuilder.AppendFormat("            class:{0}  Method:{1}\n",  
                    dDelegate.Target.GetType().Name, dDelegate.Method.Name);
            }
        }

        public StringBuilder GetOnSuccessCallMethodInfo()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var dDelegate in m_OnComplete.GetInvocationList())
            {
                stringBuilder.AppendFormat("{0}/{1} \n",  
                    dDelegate.Target.GetType().Name, dDelegate.Method.Name);
            }

            return stringBuilder;
        }
    }
}
