using AssetBundleManagement.ObjectPool;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using App.PerfCollect;
using Core.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    public delegate void OnBatchLoadResourceComplete(BatchLoadResourceResult assetAccessor);
    public enum ResourceReqType
    {
        Asset,
        Instance,
    }
    public struct LoadAssetItem
    {
        public AssetInfo Asset;
        public ResourceReqType ResType;
        public LoadPriority ItemPriority;
        public Transform Parent;
        public bool NeedAssetLoad() { return Asset.IsValid() && ResType == ResourceReqType.Asset; }
        public bool NeedObjectPoolLoad() { return Asset.IsValid() && ResType == ResourceReqType.Instance; }

        public LoadAssetItem(AssetInfo asset, ResourceReqType resType = ResourceReqType.Asset, LoadPriority priority = LoadPriority.P_NoKown, Transform parent = null)
        {
            Asset = asset;
            ResType = resType;
            ItemPriority = priority;
            Parent = parent;
        }

        public LoadAssetItem(string bundleName, string assetName, ResourceReqType resType = ResourceReqType.Asset, LoadPriority priority = LoadPriority.P_NoKown, Transform parent = null)
        {
            Asset = new AssetInfo(bundleName, assetName);
            ResType = resType;
            ItemPriority = priority;
            Parent = parent;
        }
    }

    public class BatchLoadResourceRequestContext
    {
        public int WeaponAvatarId;
        //在Instantiate之后是否先隐藏，不然会有一帧出现在（0，0，0）点
        public bool InstantiateShow = true;
        public bool ForceReactive = false;

        public AssetInfoExtra InfoExtraData;

        public BatchLoadResourceRequestContext Clone()
        {
            return this;
        }
    }

    public class BatchLoadResourceRequest: IResourcePoolObject
    {
        public LoadPriority DefaultPriority = LoadPriority.P_MIDDLE;
        public System.Object Context;
        public Dictionary<int, LoadAssetItem> RequestAssets = new Dictionary<int, LoadAssetItem>();
        public Dictionary<int, BatchLoadResourceRequestContext> RequestContexts = new Dictionary<int, BatchLoadResourceRequestContext>();
        public OnBatchLoadResourceComplete OnBatchLoadResourceComplete;
        public bool IsSync;
        public readonly bool IsRecyclable;
        public Transform Parent;
        private int _key;

        public BatchLoadResourceRequest()
        {
            IsRecyclable = false;
        }
        public BatchLoadResourceRequest(bool isRecyclable)
        {
            IsRecyclable = isRecyclable;
        }
        
        public bool ContainsKey(int key)
        {
            return RequestAssets.ContainsKey(key);
        }

        public void AddReqAutoKey(AssetInfo info)
        {
            AddReq(_key++, info, new AssetInfoExtra());
        }

        public void AddReq(int key, AssetInfo info, AssetInfoExtra infoExtra)
        {
            ResourceReqType resType = ResourceReqType.Asset;
            if (Parent != null)
            {
                resType = ResourceReqType.Instance;
            }

            if (RequestAssets.ContainsKey(key))
            {
                RequestAssets[key] = new LoadAssetItem(info, resType,parent: Parent);
                RequestContexts[key] = new BatchLoadResourceRequestContext();
            }
            else
            {
                RequestAssets.Add(key, new LoadAssetItem(info, resType,parent: Parent));
                RequestContexts.Add(key, new BatchLoadResourceRequestContext());
            }

            if (infoExtra.IsValid())
            {
                RequestContexts[key].InfoExtraData = infoExtra.Clone();
            }
        }


        public void AddReq(int key, AssetInfo info)
        {
            AddReq(key, info, default(AssetInfoExtra));
        }

        public bool IsRequestValid()
        {
            return RequestAssets.Count > 0;
        }

        public void Reset()
        {
            Context = null;
            RequestAssets.Clear();
            RequestContexts.Clear();
            OnBatchLoadResourceComplete = null;
            IsSync = false;
            Parent = null;

        }
        public  static BatchLoadResourceRequest Alloc()
        {
            return new BatchLoadResourceRequest();
            //return ResourceObjectHolder<BatchLoadResourceRequest>.Allocate();
        }
        public void Release()
        {
            this.Reset();
            //ResourceObjectHolder<BatchLoadResourceRequest>.Free(this);
        }
    }
    public class BatchLoadResourceResult : IResourcePoolObject
    {
        private BatchLoadResourceRequest _request;
        public Dictionary<int,UnityObject> ResultAssets = new Dictionary<int, UnityObject>();
        public Dictionary<int,BatchLoadResourceRequestContext> ResultContexts = new Dictionary<int, BatchLoadResourceRequestContext>();
        public int WaitTaskCount;
        public readonly bool IsRecyclable;
        public bool m_IsDisposed;

        public void SetDispose()
        {
            m_IsDisposed = true;
        }
        //public OnGameObjectInstantiateHandler OnGameObjectAssetLoaded;
        private int _intKey;
        
        public int IntKey
        {
            get { return _intKey; }
        }

        public BatchLoadResourceResult(bool recyclable)
        {
            IsRecyclable = recyclable;
        }
        public BatchLoadResourceResult()
        {
            IsRecyclable = false;
        }
        public void SetRequest(BatchLoadResourceRequest req)
        {
            _request = req;
        }

        public BatchLoadResourceRequest GetRequest()
        {
            return _request;
        }

        public void CallOnComplete()
        {
            try
            {
                if (_request.OnBatchLoadResourceComplete != null)
                    _request.OnBatchLoadResourceComplete(this);
            }
            catch (System.Exception e)
            {
                BatchResourcesLoader.s_logger.ErrorFormat("CallOnComplete err {0}:stack {1}",e.Message,e.StackTrace);
            }
          
        }

        internal void SetKey(int key)
        {
            _intKey = key;
        }

        public bool IsComplete
        {
            get { return WaitTaskCount == 0; }
        }

        public static BatchLoadResourceResult Alloc()
        {
            return new BatchLoadResourceResult();
            //  return ResourceObjectHolder<BatchLoadResourceResult>.Allocate();
        }

        public UnityObject Get(int key)
        {
            UnityObject result;
            if (ResultAssets.TryGetValue(key, out result))
            {
                ResultAssets.Remove(key);
                return null == result ? null : result;
            }
            return null;
        }

        public BatchLoadResourceRequestContext GetContext(int key)
        {
            BatchLoadResourceRequestContext result;
            if (ResultContexts.TryGetValue(key, out result))
            {
                ResultContexts.Remove(key);
                return null == result ? null : result;
            }
            return null;
        }

        public void Init(BatchLoadResourceRequest request)
        {
            m_IsDisposed  = false;
            WaitTaskCount = 0;
            _request      = request;
            ResultAssets.Clear();
            ResultContexts.Clear();

            foreach (var pair in _request.RequestAssets)
            {
                ResultAssets.Add(pair.Key, null);
            }

            foreach (var pair in request.RequestContexts)
            {
                ResultContexts.Add(pair.Key, pair.Value.Clone());
            }
        }

        public bool IsKeyMatch(int key)
        {
            return _intKey == key;
        }

        public bool AddSucessResult(int key, UnityObject resourceHandler)
        {
            //不属于他的加载回调，直接返回，不设置ResultAssets
            if (!IsKeyMatch(key))
                return false;

            bool excpetion = true;

            WaitTaskCount--;
            foreach (var pair in _request.RequestAssets)
            {
                if (pair.Value.Asset.Equals(resourceHandler.GetNewHandler().AssetKey))
                {
                    if (ResultAssets.ContainsKey(pair.Key) && ResultAssets[pair.Key] == null)
                    {
                        ResultAssets[pair.Key] = resourceHandler;

                        //原代码逻辑
                        var resContext = ResultContexts[pair.Key];
                        if (resourceHandler.AsGameObject != null)
                        {
                            if(!resContext.InstantiateShow)
                            {
                                resourceHandler.AsGameObject.SetActive(false);
                            }
                            else if (resContext.ForceReactive)
                            {
                                resourceHandler.AsGameObject.SetActive(true);
                            }
                        }
                        excpetion = false;
                        break;
                    }
                }
            }
            if (excpetion)
                AssetUtility.ThrowException("Batch Loader result dont match any requests");
            return true;
        }
        public void Release()
        {
            this.Reset();
            
            //ResourceObjectHolder<BatchLoadResourceResult>.Free(this);
        }
        public void Reset()
        {
            WaitTaskCount = 0;
            _request = null;
            ResultAssets.Clear();
            ResultContexts.Clear();
            _intKey = -1;
        }

        public void Recycle()
        {
            foreach (var pair in ResultAssets)
            {
                if(pair.Value != null)
                    pair.Value.Release();
            }
        }
    }
    internal class BatchResourcesLoader:ILoadOwner
    {
        public ILoadOwner GetRootOwner()
        {
            return m_RootOwner;
        }

        public static LoggerAdapter s_logger = new LoggerAdapter("BatchResourcesLoader");

        private ResourceLoadManager m_ResourceLoad;
        private InstanceObjectPoolContainer m_Pool;
        private ILoadOwner m_RootOwner;
        internal BatchResourcesLoader(ILoadOwner owner, ResourceLoadManager resourceLoadManager, InstanceObjectPoolContainer objectPool)
        {
            m_RootOwner                     = owner;
            m_ResourceLoad                  = resourceLoadManager;
            m_Pool                          = objectPool;
            OnBatchLoadAssetItemSucessCache = OnBatchLoadAssetItemSucess;
            m_ResourceLoad.Event.Register(ResourceEventType.LoadAssetOptionComplete,HandleLoadAssetOptionComplete);
        }

        public void Dispose()
        {
            m_ResourceLoad.Event.UnRegister(ResourceEventType.LoadAssetOptionComplete,HandleLoadAssetOptionComplete);
        }
        void HandleLoadAssetOptionComplete(ResourceEventArgs eventArgs)
        {
            LoadAssetOptionCompleteEventArgs loadAssetOptionCompleteEvent = eventArgs as LoadAssetOptionCompleteEventArgs;
            if (loadAssetOptionCompleteEvent.LoadOption.Owner == this)
            {
                loadAssetOptionCompleteEvent.Handled = true;
                ResourceProvider.DoHandleLoadAssetComplete(m_ResourceLoad.AssetCompletePoster,loadAssetOptionCompleteEvent, m_Pool,true);
            }
        }

        internal void Destroy()
        {
            m_BatchLoadRequestMap.Clear();
        }
        private OnLoadResourceComplete OnBatchLoadAssetItemSucessCache;
        private Dictionary<int, BatchLoadResourceResult> m_BatchLoadRequestMap = new Dictionary<int, BatchLoadResourceResult>();
        private static int _autoSeq;
        private int m_InIterationProcessCount=0;
        private Queue<BatchLoadResourceResult> m_PendingCompleteResults = new Queue<BatchLoadResourceResult>();
        internal void LoadBatchAssets(BatchLoadResourceRequest request, BatchLoadResourceResult resourceResult, ISortableJob job = null)

        {
            // if (m_BatchLoadRequestMap.ContainsKey(request))
            // {
            //     AssetUtility.ThrowException("Load Batch Assets execption,muti BatchLoadResourceRequest come in");
            // }
            ++m_InIterationProcessCount;
            try
            {
                resourceResult.Init(request);
                foreach (var pair in request.RequestAssets)
                {
                    if (pair.Value.Asset.IsValid())
                    {
                        ++resourceResult.WaitTaskCount;
                    }
                }

                m_BatchLoadRequestMap.Add(_autoSeq, resourceResult);
                resourceResult.SetKey(_autoSeq);
            
                foreach (var pair in request.RequestAssets)
                {
                    if (pair.Value.NeedAssetLoad())
                    {
                        var priority = pair.Value.ItemPriority == LoadPriority.P_NoKown ? request.DefaultPriority : pair.Value.ItemPriority;
                        var loadOpt = new LoadResourceOption(OnBatchLoadAssetItemSucessCache, priority, _autoSeq,true);
                        loadOpt.Owner = this;
                        if(job == null)
                            m_ResourceLoad.LoadAsset(pair.Value.Asset, request.IsSync, ref loadOpt);
                        else
                            m_ResourceLoad.LoadSortableAsset(pair.Value.Asset, job, ref loadOpt);
                    }
                    else if (pair.Value.NeedObjectPoolLoad())
                    {
                        var priority = pair.Value.ItemPriority == LoadPriority.P_NoKown ? request.DefaultPriority : pair.Value.ItemPriority;
                        var loadOpt = new LoadResourceOption(OnBatchLoadAssetItemSucessCache, priority, _autoSeq, true);
                        loadOpt.Owner = this;
                        m_Pool.Instantiate(pair.Value.Asset, ref loadOpt, pair.Value.Parent, job);
                    }
                }

            }
            catch (Exception e)
            {
                s_logger.ErrorFormat("LoadBatchAssets err {0}:stack {1}",e.Message,e.StackTrace);
            }

            while (m_PendingCompleteResults.Count > 0)
            {
                BatchLoadResourceResult pendingResult = m_PendingCompleteResults.Dequeue();
                pendingResult.CallOnComplete();
            }

            //每次加载, Key +1
            _autoSeq++;
            
            --m_InIterationProcessCount;

        }

        internal BatchLoadResourceResult LoadBatchAssets(BatchLoadResourceRequest request)
        {
            // if (m_BatchLoadRequestMap.ContainsKey(request))
            // {
            //     AssetUtility.ThrowException("Load Batch Assets execption,muti BatchLoadResourceRequest come in");
            // }
            BatchLoadResourceResult resourceResult = BatchLoadResourceResult.Alloc();
            LoadBatchAssets(request, resourceResult);
            return resourceResult;
        }

        public void CancelBatchResults(BatchLoadResourceResult results)
        {
            m_BatchLoadRequestMap.Remove(results.IntKey);

        }

        void OnBatchLoadAssetItemSucess(System.Object context, UnityObject resourceHandler)
        {
            var resultKey = (int)context;
            BatchLoadResourceResult resourceResults;
            if (m_BatchLoadRequestMap.TryGetValue(resultKey, out resourceResults))
            {
                if (resourceResults.AddSucessResult(resultKey, resourceHandler))
                {
                    Assert.IsTrue(resourceResults.WaitTaskCount >= 0, "OnBatchLoadAssetItem execption");
                    if (resourceResults.WaitTaskCount == 0)
                    {
                        m_BatchLoadRequestMap.Remove(resultKey);
                        if (m_InIterationProcessCount>0)
                        {
                            m_PendingCompleteResults.Enqueue(resourceResults);
                        }
                        else
                        {
                            resourceResults.CallOnComplete();
                        }
                     
                    }
                }
                else
                {
                    //result和Key不对应，说明是result被release过
                    m_BatchLoadRequestMap.Remove(resultKey);
                    resourceHandler.Release();
                }
            }
            else
            {
                resourceHandler.Release();
                //AssetUtility.ThrowException("OnBatchLoadAssetItem execption");
            }
        }
        void OnBatchLoadAssetItemFailed(string errorMessage,AssetInfo assetInfo, object context)
        {
            var resultKey = (int)context;
            BatchLoadResourceResult resourceResults;
            if (m_BatchLoadRequestMap.TryGetValue(resultKey, out resourceResults))
            {
                if (resourceResults.IsKeyMatch(resultKey))
                {
                    --resourceResults.WaitTaskCount;
                    Assert.IsTrue(resourceResults.WaitTaskCount >= 0, "OnBatchLoadAssetItem execption");
                    if (resourceResults.WaitTaskCount == 0)
                    {
                        m_BatchLoadRequestMap.Remove(resultKey);
                        if (m_InIterationProcessCount>0)
                        {
                            m_PendingCompleteResults.Enqueue(resourceResults);
                        }
                        else
                        {
                            resourceResults.CallOnComplete();
                        }
                    }
                }
                else
                {
                    //result和Key不对应，说明是result被release过
                    m_BatchLoadRequestMap.Remove(resultKey);
                }
            }
            else
            {
                AssetUtility.ThrowException("OnBatchLoadAssetItem execption");
            }

        }

      
    }
}
