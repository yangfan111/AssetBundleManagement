using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AssetBundleManagement
{
    internal interface IResourceUpdater
    {
        void AddLoadingOperation(ILoadOperation loadOperation);
        void RemoveLoadingOperation(ILoadOperation loadOperation);

        void InspectResourceDisposeState(IDisposableResourceHandle resourceHandle);

        //  void AddPendingLoadAssetReqeust(AssetLoadTask loadTask);
    }
    internal class AssetLoadTask:IResourcePoolObject
    {
        internal LoadResourceOption LoadOption;
        internal int HandleId;
        internal AssetResourceHandle ResourceObject;

        
        public AssetLoadTask() {}

        public void Reset()
        {
        }
    }
    internal class AssetTaskGroup
    {
        Queue<AssetLoadTask> m_WaitLoadAssetReqeusts = new Queue<AssetLoadTask>(512);
        internal void AddTask(AssetLoadTask task)
        {
            m_WaitLoadAssetReqeusts.Enqueue(task);
        }
        internal AssetLoadTask PopTask()
        {
            return m_WaitLoadAssetReqeusts.Dequeue();
        }
        internal int GetTaskCount()
        {
            return m_WaitLoadAssetReqeusts.Count;
        }
        

    }
    internal class ResourceUpdater : IResourceUpdater
    {

        private readonly AssetTaskGroup[] m_WaitLoadAssetTasks;
        private HashSet<ILoadOperation> m_BackgroundLoadingOperation = new HashSet<ILoadOperation>();
        private List<ILoadOperation> m_PendingRemoveLoadingOperation = new List<ILoadOperation>();
        private List<ILoadOperation> m_PendingAddLoadingOperation = new List<ILoadOperation>();
        private HashSet<IDisposableResourceHandle> m_WaitDisposeResourceList = new HashSet<IDisposableResourceHandle>();
        internal ResourceUpdater()
        {
            m_WaitLoadAssetTasks = new AssetTaskGroup[(int)LoadPriority.PriorityCount];
            for (int i = 0; i < m_WaitLoadAssetTasks.Length; i++)
            {
                m_WaitLoadAssetTasks[i] = new AssetTaskGroup();
            }
        }
        public void AddWaitLoadAssetTask(AssetLoadTask loadTask)
        {
            int priority = AssetUtility.GetLoadPrioritySequentialIndex(loadTask.LoadOption.Priority);
            m_WaitLoadAssetTasks[priority].AddTask(loadTask);
            ResourceProfiler.PushToAssetLoadTask(loadTask.ResourceObject);
        }
        public void Update(AssetBundleLoadProvider abMgr)
        {
            PollLoadingOperationResult();
            UpdateWaitLoadRequests(abMgr);
            UpdateWaitDisposedResource();
        }
        private List<IDisposableResourceHandle> m_TempList1 = new List<IDisposableResourceHandle>();
        private List<IDisposableResourceHandle> m_TempList2 = new List<IDisposableResourceHandle>();
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
                else if (Time.realtimeSinceStartup - resource.WaitDestroyStartTime > resource.DestroyWaitTime)
                {
                    m_TempList2.Add(resource);
                }
            }
            foreach (var resource in m_TempList1)
            {
                resource.InDestroyList = false;
                ResourceProfiler.RemoveResourceFromDestroyList(resource);
                m_WaitDisposeResourceList.Remove(resource);

            }
            foreach (var resource in m_TempList2)
            {
                if(!ResourceRecorder.Get().CanUnloadAB())
                {
                    break;
                }
                resource.Destroy();
                ResourceProfiler.ExecuteResourceDestroy(resource);
                m_WaitDisposeResourceList.Remove(resource);


            }

        }
        private void UpdateWaitLoadRequests(AssetBundleLoadProvider abMgr)
        {
            for (int i = m_WaitLoadAssetTasks.Length - 1; i >= 0; i--)
            {
                while (m_WaitLoadAssetTasks[i].GetTaskCount()>0)
                {
                    if(!ResourceRecorder.Get().CanLoadAssetWaitRequest())
                    {
                        break;
                    }
                    var task = m_WaitLoadAssetTasks[i].PopTask();
                    ResourceProfiler.PopAssetLoadTask(task.ResourceObject);
                    //assettask执行前再判断一次Asset的加载状态，可能有前面相同assetinfo的task执行过初始化过了
                    if (!task.ResourceObject.HandleAssetLoadTaskIfStartLoading(task))
                    { //当前asset资源没有被加载请求过
                        ResourceRecorder.Get().IncLoadAssetWaitRequestCount();
                        var bundleResource = abMgr.LoadAssetBundleAsync(task.ResourceObject.AssetKey);
                        task.ResourceObject.InitalizeChainAttachedBundleObject(bundleResource, task);
                     
                    }
                    ResourceObjectHolder<AssetLoadTask>.Free(task);
                }
            }

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
                loadOperation.PollOperationResult();
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
         
        public void InspectResourceDisposeState(IDisposableResourceHandle resourceHandle)
        {
            if (!ResourceConfigure.SupportUnload)
                return;
            if (resourceHandle.IsUnUsed())
            {
                if (!resourceHandle.InDestroyList)
                {
                    ResourceProfiler.AddResourceToDestroyList(resourceHandle);
                    m_WaitDisposeResourceList.Add(resourceHandle);
                    resourceHandle.InDestroyList = true;
                    resourceHandle.WaitDestroyStartTime = Time.realtimeSinceStartup;
                }
            }
            else
            {
                if (resourceHandle.InDestroyList)
                {
                    resourceHandle.InDestroyList = false;
                    m_WaitDisposeResourceList.Remove(resourceHandle);
                    ResourceProfiler.RemoveResourceFromDestroyList(resourceHandle);
                }
            }
        }

        internal void GetUpdaterAllInfos(StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("====>Unity Loading Option:{0}\n", m_BackgroundLoadingOperation.Count);
            foreach (var loadOperation in m_BackgroundLoadingOperation)
            {
                stringBuilder.AppendFormat("    [LoadingTask]{0}\n", loadOperation);
                
            }

            int allCount = 0;
            for (int i = m_WaitLoadAssetTasks.Length - 1; i >= 0; i--)
            {
                allCount += m_WaitLoadAssetTasks[i].GetTaskCount();
            }
            stringBuilder.AppendFormat("====>WaitLoadAssetTask :{0}\n", allCount);
            stringBuilder.AppendFormat("====>WaitDestroyTask :{0}\n", m_WaitDisposeResourceList.Count);
            foreach (var wd in m_WaitDisposeResourceList)
            {
                stringBuilder.AppendFormat("    [DestroyTask]{0}\n", wd.ToStringDetail());
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
    }
}
