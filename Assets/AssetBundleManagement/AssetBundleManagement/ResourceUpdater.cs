using Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using App.Server.StatisticData;
using Core.Utils;
using UnityEngine;
using UnityEngine.Profiling;
using Utils.AssetManager;
using Utils.Utils;

namespace AssetBundleManagement
{
    internal interface IResourceUpdater
    {
        void AddLoadingOperation(ILoadOperation loadOperation);
        void RemoveLoadingOperation(ILoadOperation loadOperation);

        void InspectResourceDisposeState(IDisposableResourceHandle resourceHandle);


        //  void AddPendingLoadAssetReqeust(AssetLoadTask loadTask);
    }

    internal interface IAssetCompletePoster
    {
        void EnqueuePostAssetEvent(LoadAssetOptionCompleteEventArgs eventArgs);
    }

    public interface ILoadTask : IResourcePoolObject
    {
        //属于哪个加载队列，优先级
        LoadPriority Priority { get; }

        LoadTaskType TaskType { get; }

        //在某个具体加载队列中，获取内部的优先级
        float GetDisToCamera();

        AssetInfo Info { get; }

        bool Release(ILoadOwner owner);
    }

    //大地图专属
    public interface ISortableLoadTask : ILoadTask
    {
        ISortableJob Job { get; set; }
    }

    public interface ISortableJob
    {
        float GetDisToCamera();
    }

    public class DefaultSortableJob : ISortableJob
    {
        public Vector3 SelfTranPos;
        public Transform MainTran;
        public float Scaler = 1f;

        public DefaultSortableJob(Vector3 selfPos, Transform mainTran, float scaler = 1)
        {
            SelfTranPos = selfPos;
            MainTran    = mainTran;
            Scaler      = scaler;
        }

        public float GetDisToCamera()
        {
            if(MainTran == null) return ResourceConfigure.DefaultDistanceToCamera;
            var dis = Vector3.Distance(MainTran.position, SelfTranPos);
            return dis / Scaler;
        }
    }

    internal class AssetSortableLoadTask : AssetLoadTask, ISortableLoadTask
    {
        public ISortableJob Job { get; set; }

        public override float GetDisToCamera()
        {
            if (Job != null) return Job.GetDisToCamera();
            return ResourceConfigure.DefaultDistanceToCamera;
        }

        public override LoadTaskType TaskType
        {
            get { return LoadTaskType.SortableAsset; }
        }

        public override void Reset()
        {
            base.Reset();
            Job = null;
        }
    }

    internal class AssetLoadTask : ILoadTask
    {
        internal int HandleId;
        internal LoadResourceOption LoadOption;
        internal AssetResourceHandle ResourceObject;

        public AssetLoadTask()
        {
        }

        public AssetInfo Info
        {
            get { return ResourceObject.AssetKey; }
        }

        public bool Release(ILoadOwner owner)
        {
            if (LoadOption.Owner != null && LoadOption.Owner.GetRootOwner() == owner)
            {
                if (ResourceObject != null)
                {
                    ResourceObject.ReleaseHandleId(HandleId);
                }

                return true;
            }

            return false;
        }

        public LoadPriority Priority
        {
            get { return LoadOption.Priority; }
        }

        public virtual LoadTaskType TaskType
        {
            get { return LoadTaskType.Asset; }
        }

        public virtual float GetDisToCamera()
        {
            return ResourceConfigure.DefaultDistanceToCamera;
        }

        public virtual void Reset()
        {
            HandleId       = 0;
            ResourceObject = null;
            LoadOption     = default(LoadResourceOption);
        }

    }

    internal class SceneLoadTask : ILoadTask
    {
        internal NewSceneInfo LoadSceneInfo;
        internal SceneResourceHandle SceneHandle;

        public LoadPriority Priority
        {
            get { return LoadSceneInfo.LoadPriority; }
        }

        public LoadTaskType TaskType
        {
            get { return LoadTaskType.Scene; }
        }

        public bool Release(ILoadOwner owner)
        {
            return false;
        }

        public AssetInfo Info
        {
            get { return LoadSceneInfo.Asset.NewToAsset(); }
        }

        public void Reset()
        {
        }

        public virtual float GetDisToCamera()
        {
            return ResourceConfigure.MinDistanceToCamera;
        }
    }

    internal class AssetTaskGroup
    {
        List<ILoadTask> m_WaitLoadAssetReqeusts = new List<ILoadTask>(512);

        internal void AddTask(ILoadTask task)
        {
            m_WaitLoadAssetReqeusts.Add(task);
        }

        internal ILoadTask PopTask()
        {
            //GetDisToCamera最小的优先
            var firstProprityTask = m_WaitLoadAssetReqeusts[0];
            for (int i = 1; i < m_WaitLoadAssetReqeusts.Count; i++)
            {
                if (firstProprityTask.GetDisToCamera() > m_WaitLoadAssetReqeusts[i].GetDisToCamera())
                {
                    firstProprityTask = m_WaitLoadAssetReqeusts[i];
                }
            }

            m_WaitLoadAssetReqeusts.Remove(firstProprityTask);
            return firstProprityTask;
        }

        internal ILoadTask GetLoadTask(AssetInfo assetInfo)
        {
            for (int i = 0; i < m_WaitLoadAssetReqeusts.Count; i++)
            {
                if (m_WaitLoadAssetReqeusts[i].Info == assetInfo)
                {
                    var ret = m_WaitLoadAssetReqeusts[i];
                    m_WaitLoadAssetReqeusts.RemoveAt(i);
                    return ret;
                }
            }

            return null;
        }

        internal List<ILoadTask> GetAllTasks()
        {
            return m_WaitLoadAssetReqeusts;
        }

        internal int GetTaskCount()
        {
            return m_WaitLoadAssetReqeusts.Count;
        }
        
        public List<AssetInfo> Clear(ILoadOwner owner)
        {
            var infos = new List<AssetInfo>();
            for (int i = 0; i < m_WaitLoadAssetReqeusts.Count; i++)
            {
                var re = m_WaitLoadAssetReqeusts[i];
                if (re != null && re.Release(owner))
                {
                    infos.Add(m_WaitLoadAssetReqeusts[i].Info);
                    m_WaitLoadAssetReqeusts.RemoveAt(i--);
                }
            }
            return infos;
        }
    }

    internal class ResourceUpdater : IResourceUpdater, IAssetCompletePoster
    {
        private readonly AssetTaskGroup[] m_WaitLoadAssetTasks;
        private HashSet<ILoadOperation> m_BackgroundLoadingOperation = new HashSet<ILoadOperation>();
        private List<ILoadOperation> m_PendingRemoveLoadingOperation = new List<ILoadOperation>();
        private List<ILoadOperation> m_PendingAddLoadingOperation = new List<ILoadOperation>();
        private HashSet<IDisposableResourceHandle> m_WaitDisposeResourceList = new HashSet<IDisposableResourceHandle>();
        private ResourceRecorder m_Recorder;
        private LoggerAdapter _loggerAdapter = new LoggerAdapter("AssetBundleManagement.ResourceUpdater");
        private FrametimeHandler m_FetchResult;
        private FrametimeHandler m_ProcessDispose;
        private FrametimeHandler m_ProcessExecution;
        internal ResourceUpdater(ResourceRecorder recorder)
        {
            m_Recorder           = recorder;
            m_WaitLoadAssetTasks = new AssetTaskGroup[(int) LoadPriority.P_Length];
            for (int i = 0; i < m_WaitLoadAssetTasks.Length; i++)
            {
                m_WaitLoadAssetTasks[i] = new AssetTaskGroup();
            }
            m_FetchResult = new FrametimeHandler();
            m_FetchResult.Registe("ResourceUpdater_FetchResult");
            m_ProcessDispose = new FrametimeHandler();
            m_ProcessDispose.Registe("ResourceUpdater_ProcessDispose");
            m_ProcessExecution = new FrametimeHandler();
            m_ProcessExecution.Registe("ResourceUpdater_ProcessExecution");
        }

        public List<AssetInfo> ClearWaitLoadAssetTask(ILoadOwner owner)
        {
            var waitLoadAssetList = new List<AssetInfo>();
            for (int i = 0; i < m_WaitLoadAssetTasks.Length; i++)
            {
                var infos = m_WaitLoadAssetTasks[i].Clear(owner);
                waitLoadAssetList.AddRange(infos);
            }
            return waitLoadAssetList;
        }

        public void AddWaitLoadAssetTask(AssetSortableLoadTask loadTask)
        {
            int priority = AssetUtility.GetLoadPrioritySequentialIndex(loadTask.Priority);
            m_WaitLoadAssetTasks[priority].AddTask(loadTask);
            // ResourceProfiler.PushToAssetLoadTask(loadTask.ResourceObject);
        }

        public void LoadRequestImmediatelySync(AssetInfo assetInfo, AssetBundleLoadProvider abProvider)
        {
            for (int i = m_WaitLoadAssetTasks.Length - 1; i >= 0; i--)
            {
                ILoadTask loadTask = m_WaitLoadAssetTasks[i].GetLoadTask(assetInfo);
                if (loadTask != null)
                {
                    
                    HandleTask(loadTask, abProvider,true);
                }
            }
        }

        public void AddWaitLoadAssetTask(AssetLoadTask loadTask)
        {
            int priority = AssetUtility.GetLoadPrioritySequentialIndex(loadTask.Priority);
            m_WaitLoadAssetTasks[priority].AddTask(loadTask);
            // ResourceProfiler.PushToAssetLoadTask(loadTask.ResourceObject);
        }

        public void AddWaitLoadSceneTask(SceneLoadTask loadTask)
        {
            int priority = AssetUtility.GetLoadPrioritySequentialIndex(loadTask.Priority);
            m_WaitLoadAssetTasks[priority].AddTask(loadTask);
        }

        public void Update(AssetBundleLoadProvider abMgr)
        {
            try
            {
                m_FetchResult.Start();
                PollLoadingOperationResult();
                UpdatePollRecorder();
                UpdateWaitLoadRequests(abMgr);
                PollLoadingOperationResult(); //再次PollOperation,更新UpdateWaitLoadRequests的加载结果

                m_FetchResult.Stop();

                m_ProcessDispose.Start();
                UpdateWaitDisposedResource();
                m_ProcessDispose.Stop();
                
                m_ProcessExecution.Start();
                PostAssetCompleteEvents();
                m_ProcessExecution.Stop();

#if dev
            if (GMVariable.LevelStreamingShowPreload)
                RemoveInstance();
#endif
            }
            catch (System.Exception e)
            {
                _loggerAdapter.ErrorFormat("err:{0},stack:{1}", e.Message, e.StackTrace);
            }
           
        }

        private List<IDisposableResourceHandle> m_TempList1 = new List<IDisposableResourceHandle>();
        private List<IDisposableResourceHandle> m_TempList2 = new List<IDisposableResourceHandle>();

        private Queue<LoadAssetOptionCompleteEventArgs> m_PostAssetCompleteEvents =
                        new Queue<LoadAssetOptionCompleteEventArgs>();

        //  private List<LoadAssetOptionCompleteEventArgs> m_PostAssetCompleteEventsPending =
        //                    new List<LoadAssetOptionCompleteEventArgs>();
        // private bool m_InPostAssetProcess;
        private void PostAssetCompleteEvents()
        {
            while (m_PostAssetCompleteEvents.Count > 0)
            {
                var evt = m_PostAssetCompleteEvents.Dequeue();
                evt.ProcessAssetComplete(null);
                evt.Free();
            }

            //  m_InPostAssetProcess = false;
        }

        void IAssetCompletePoster.EnqueuePostAssetEvent(LoadAssetOptionCompleteEventArgs eventArgs)
        {
            m_PostAssetCompleteEvents.Enqueue(eventArgs);
        }

        private void UpdateWaitDisposedResource()
        {
            m_TempList1.Clear();
            m_TempList2.Clear();
            foreach (var resource in m_WaitDisposeResourceList)
            {
                if (!resource.IsUnUsed())
                {
                    m_TempList1.Add(resource);
                }
                else if (m_Recorder.ReachDestroyTime(resource))
                {
                    m_TempList2.Add(resource);
                }
            }

            foreach (var resource in m_TempList1)
            {
                resource.InDestroyList = false;
                m_WaitDisposeResourceList.Remove(resource);
            }

            foreach (var resource in m_TempList2)
            {
                if (!m_Recorder.CanUnloadAB())
                {
                    break;
                }

                resource.Destroy();
                if (resource.ResType == ResourceType.Bundle)
                    m_Recorder.IncUnloadABCount();
                //   ResourceProfiler.ExecuteResourceDestroy(resource);
                m_WaitDisposeResourceList.Remove(resource);
            }
        }

        private void UpdateWaitLoadRequests(AssetBundleLoadProvider abMgr)
        {
            for (int i = m_WaitLoadAssetTasks.Length - 1; i >= 0; i--)
            {
                while (m_WaitLoadAssetTasks[i].GetTaskCount() > 0)
                {
                    //每帧上限
                    if (!m_Recorder.CanLoadAssetWaitRequest())
                    {
                        break;
                    }

                    //总的上限
                    if (m_Recorder.LoadAssetIsExceedingLimit())
                    {
                        break;
                    }

                    ILoadTask task = m_WaitLoadAssetTasks[i].PopTask();
                    HandleTask(task, abMgr,false);

                    m_Recorder.IncreaseAssetCount();
                }
            }
        }

        void HandleTask(ILoadTask task, AssetBundleLoadProvider abMgr,bool isSync)
        {
            if (task.TaskType == LoadTaskType.Asset || task.TaskType == LoadTaskType.SortableAsset)
            {
                HandleAssetTask(task as AssetLoadTask, abMgr,isSync);
            }
            else
            {
                HandleSceneTask(task as SceneLoadTask, abMgr,isSync);
            }
        }

        private Dictionary<GameObject, float> _instance = new Dictionary<GameObject, float>();

        private void RemoveInstance()
        {
            List<GameObject> removeList = new List<GameObject>();
            foreach (var pair in _instance)
            {
                if (Time.realtimeSinceStartup - pair.Value > 0.5f)
                {
                    removeList.Add(pair.Key);
                }
            }

            foreach (var re in removeList)
            {
                _instance.Remove(re);
                re.gameObject.SetActive(false);
            }
        }

        void HandleSceneTask(SceneLoadTask loadTask, AssetBundleLoadProvider abMgr,bool isSync)
        {
            var bundleResource = abMgr.LoadAssetBundleAsync(loadTask.LoadSceneInfo.Asset.NewToAsset());
            if(isSync)
                loadTask.SceneHandle.SetIsSync();
            loadTask.SceneHandle.InitalizeChainAttachedBundleObject(bundleResource);
            ResourceObjectHolder<SceneLoadTask>.Free(loadTask);
        }

        void HandleAssetTask(AssetLoadTask task, AssetBundleLoadProvider abMgr, bool isSync)
        {
#if dev
            if (GMVariable.LevelStreamingShowPreload)
            {
                if (task is AssetSortableLoadTask)
                {
                    var sortable = task as AssetSortableLoadTask;
                    var job      = sortable.Job as DefaultSortableJob;
                    if (job != null)
                    {
                        var instance = GameObjectExt.CloneCube(Color.white, job.SelfTranPos, new Vector3(2f, 20f, 2f));
                        _instance.Add(instance, Time.realtimeSinceStartup);
                    }
                }
            }
#endif

            //   ResourceProfiler.PopAssetLoadTask(task.ResourceObject);
            //assettask执行前再判断一次Asset的加载状态，可能有前面相同assetinfo的task执行过初始化过了
            if (!task.ResourceObject.HandleAssetLoadTaskIfStartLoading(task))
            {
                //当前asset资源没有被加载请求过
                m_Recorder.IncLoadAssetWaitRequestCount();
                var bundleResource = abMgr.LoadAssetBundleAsync(task.ResourceObject.AssetKey);
                if(isSync)
                    task.ResourceObject.SetSyncLoadMode();
                task.ResourceObject.InitalizeChainAttachedBundleObject(bundleResource, task);
            }

            ResourceObjectHolder<AssetLoadTask>.Free(task);
        }

        private bool m_PollUpdateLoadingState;

        public void AddLoadingOperation(ILoadOperation loadOperation)
        {
            if (m_PollUpdateLoadingState)
                m_PendingAddLoadingOperation.Add(loadOperation);
            else
                m_BackgroundLoadingOperation.Add(loadOperation);
        }

        public void RemoveLoadingOperation(ILoadOperation loadOperation)
        {
            if (m_PollUpdateLoadingState)
                m_PendingRemoveLoadingOperation.Add(loadOperation);
            else
                m_BackgroundLoadingOperation.Remove(loadOperation);
        }

        private void PollLoadingOperationResult()
        {
            m_PollUpdateLoadingState = true;

            foreach (var loadOperation in m_BackgroundLoadingOperation)
            {
                try
                {
                    loadOperation.PollOperationResult();
                }
                catch (Exception e)
                {
                    _loggerAdapter.ErrorFormat("poll reuslt error:{0}:{1}", e.Message, e.StackTrace);
                }
            }

            foreach (var loadOperation in m_PendingRemoveLoadingOperation)
            {
                m_BackgroundLoadingOperation.Remove(loadOperation);
            }

            foreach (var loadOperation in m_PendingAddLoadingOperation)
            {
                m_BackgroundLoadingOperation.Add(loadOperation);
            }

            m_PendingRemoveLoadingOperation.Clear();
            m_PendingAddLoadingOperation.Clear();

            m_PollUpdateLoadingState = false;
        }

        void UpdatePollRecorder()
        {
            m_Recorder.LoadingAssetCount  = 0;
            m_Recorder.LoadingBundleCount = 0;
            m_Recorder.LoadingSceneCount  = 0;
            foreach (var loadOperation in m_BackgroundLoadingOperation)
            {
                switch (loadOperation.LoadOp)
                {
                    case LoadOpType.Asset:
                        ++m_Recorder.LoadingAssetCount;
                        break;
                    case LoadOpType.Bundle:
                        ++m_Recorder.LoadingBundleCount;
                        break;
                    case LoadOpType.Scene:
                        ++m_Recorder.LoadingSceneCount;
                        break;
                }
            }
        }

        public void InspectResourceDisposeState(IDisposableResourceHandle resourceHandle)
        {
            // if (!ResourceConfigure.SupportUnload)
            //     return;
            if (resourceHandle.IsUnUsed())
            {
                if (!resourceHandle.InDestroyList)
                {
                    m_WaitDisposeResourceList.Add(resourceHandle);
                    resourceHandle.InDestroyList        = true;
                    resourceHandle.WaitDestroyStartTime = Time.realtimeSinceStartup;
                }
            }
            else
            {
                if (resourceHandle.InDestroyList)
                {
                    resourceHandle.InDestroyList = false;
                    m_WaitDisposeResourceList.Remove(resourceHandle);
                }
            }
        }

        #region //profiler

        internal void GetUpdaterAllInfos(StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("====>BackgroundLoadingOperationCount:{0}\n", m_BackgroundLoadingOperation.Count);
            int maxPrintOperationCount = 20;
            int printOperationCount = 0;

            if (m_BackgroundLoadingOperation.Count > 0)
            {
                stringBuilder.AppendFormat("====>BackgroundLoadingOperationDetail:");
                foreach (var loadOperation in m_BackgroundLoadingOperation)
                {
                    if (printOperationCount++ >= maxPrintOperationCount) break;
                    stringBuilder.AppendFormat("[LoadingTask:{0}], ", loadOperation);
                }
                stringBuilder.AppendFormat("\n");
            }


            int allCount = 0;
            for (int i = m_WaitLoadAssetTasks.Length - 1; i >= 0; i--)
            {
                allCount += m_WaitLoadAssetTasks[i].GetTaskCount();
            }

            stringBuilder.AppendFormat("====>WaitLoadAssetTaskCount:{0}\n", allCount);

            if (allCount > 0)
            {
                stringBuilder.AppendFormat("====>WaitLoadAssetTaskDetail:");
                for (int i = m_WaitLoadAssetTasks.Length - 1; i >= 0; i--)
                {
                    var tasks = m_WaitLoadAssetTasks[i].GetAllTasks();
                    for (int j = 0; j < tasks.Count; j++)
                    {
                        stringBuilder.AppendFormat("[Priority:{0}, AssetInfo:{1}], ", tasks[j].Priority, tasks[j].Info);
                    }
                }
                stringBuilder.AppendFormat("\n");
            }

            // stringBuilder.AppendFormat("====>WaitDestroyTask :{0}\n", m_WaitDisposeResourceList.Count);
            // foreach (var wd in m_WaitDisposeResourceList)
            // {
            //     stringBuilder.AppendFormat("    [DestroyTask]{0}\n", wd.ToStringDetail());
            // }

            var errorBundleCount = m_Recorder.m_LoadErrorBundles.Count;
            stringBuilder.AppendFormat("====>FailedBundleCount:{0}\n", errorBundleCount);
            if (errorBundleCount > 0)
            {
                stringBuilder.AppendFormat("====>FailedBundleDetail:");
                foreach (var bundle in m_Recorder.m_LoadErrorBundles)
                {
                    stringBuilder.AppendFormat("[{0}], ", bundle.BundleKey.BundleName);
                }
                stringBuilder.AppendFormat("\n");
            }

            var errorInfoCount = m_Recorder.FailedLoadAssets.Count;
            stringBuilder.AppendFormat("====>FailedAssetInfosCount:{0}\n", errorInfoCount);
            if (errorInfoCount > 0)
            {
                stringBuilder.AppendFormat("====>FailedAssetInfosDetail:");
                foreach (var asset in m_Recorder.FailedLoadAssets)
                {
                    stringBuilder.AppendFormat("{0}, ", asset);
                }
                stringBuilder.AppendFormat("\n");
            }
        }

        public void GetResourceStatus(ref ResourceManangerStatus status)
        {
            status.LoadingOperationCont = m_BackgroundLoadingOperation.Count;
            int allCount = 0;
            for (int i = m_WaitLoadAssetTasks.Length - 1; i >= 0; i--)
            {
                allCount += m_WaitLoadAssetTasks[i].GetTaskCount();
            }

            status.WaitRequestAssetTaskCount = allCount;
            status.ToBeDestroyCont           = m_WaitDisposeResourceList.Count;
        }

        #endregion
    }
}