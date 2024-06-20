using System.Collections.Generic;
using AssetBundleManagement;
using Core.Utils;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    public class BatchLoadResourceSet
    {

        public static readonly BatchLoadResourceSet s_Default = new BatchLoadResourceSet("default");
                        
        protected static LoggerAdapter s_logger = new LoggerAdapter(typeof(BatchLoadResourceSet));

        private Stack<BatchLoadResourceRequest> m_Reqeuest = new Stack<BatchLoadResourceRequest>(64);
        private Stack<BatchLoadResourceResult> m_Results = new Stack<BatchLoadResourceResult>(64);
        private string m_tag;
        public BatchLoadResourceSet(string tag)
        {
            m_tag = tag;
        }
        public BatchLoadResourceRequest AllocRequest()
        {
            if (m_Reqeuest.Count > 0)
                return m_Reqeuest.Pop();
            return new BatchLoadResourceRequest(true);
        }

        public void FreeRequest(BatchLoadResourceRequest request)
        {
            if (request != null)
            {
                request.Reset();
                if (request.IsRecyclable)
                {
                    m_Reqeuest.Push(request);
                   //  GMVariable.loggerAdapter.InfoFormat("{1} CurrRequest:{0}",m_Reqeuest.Count,m_tag);
                }
                else
                {
                    s_logger.WarnFormat("BatchLoadResourceRequest is not Recyable,Will case object leak!!");
                }
            }
           
        }

        public BatchLoadResourceResult AllocResult()
        {
            
            if (m_Results.Count > 0)
                return m_Results.Pop();
            return new BatchLoadResourceResult(true);
        }

        public void FreeResult(BatchLoadResourceResult result, IUnityAssetManager unityAssetManager)
        {
            if (result != null)
            {
                unityAssetManager.NewProvider.SafeRelease(result);
             //   result.Reset();
                if (result.IsRecyclable)
                {
                    m_Results.Push(result);
                  //  GMVariable.loggerAdapter.InfoFormat("{1} CurrResults:{0}",m_Results.Count,m_tag);
                }
                else
                {
                    s_logger.WarnFormat("BatchLoadResourceResult is not Recyable,Will case object leak!!");
                }
            }
           
        }
    }
}