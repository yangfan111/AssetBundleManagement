using Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using App.Server.StatisticData;
using UnityEngine;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    public enum RecordAssetType
    {
        Asset,
        SortableAsset,
        NormalScene,
    }

    internal class ResourceRecorder
    {

        private static LoggerAdapter s_logger = new LoggerAdapter("AssetBundleManagement.ResourceRecorder",false);

        private int m_CurrentFrameUnloadABCount;
        private int m_CurrentFrameLoadAssetRequest;
        private int lastUpdateFrame;

        private HashSet<AssetResourceHandle> m_LoadErrorAssets = new HashSet<AssetResourceHandle>();
        public HashSet<AssetBundleResourceHandle> m_LoadErrorBundles = new HashSet<AssetBundleResourceHandle>();

        private IResourceEventMonitor m_EventMonitor;
        private IRecorderSaver m_RecorderSaver;
        private bool m_IsRecord;

        public  int LoadingAssetCount;
        public  int LoadingBundleCount;
        public  int LoadingSceneCount;

        public List<AssetInfo> FailedLoadAssets = new List<AssetInfo>();
        
        internal ResourceRecorder(IResourceEventMonitor eventMonitor, IRecorderSaver saver)
        {
#if dev
            m_IsRecord = true;
#endif
            m_RecorderSaver = saver;

            m_EventMonitor = eventMonitor;
            m_EventMonitor.RegisterDefault(ResourceEventHandler);
            m_EventMonitor.Register(ResourceEventType.LoadAssetFailed, OnErrorAssetLoadFailed);
            m_EventMonitor.Register(ResourceEventType.LoadBundleFailed, OnErrorBundleLoadFailed);

            //
            m_EventMonitor.Register(ResourceEventType.LoadAssetStart, OnLoadAssetStart);
            m_EventMonitor.Register(ResourceEventType.LoadAssetSucess, OnLoadAssetSuccess);
            m_EventMonitor.Register(ResourceEventType.LoadBundleDirectly, OnLoadBundleStart);
            m_EventMonitor.Register(ResourceEventType.LoadBundleIndirectly, OnLoadBundleStart);
            m_EventMonitor.Register(ResourceEventType.LoadBundleSelfSucess, OnLoadBundleSuccess);
            m_EventMonitor.Register(ResourceEventType.AddQueueStart, OnAddQueue);
            m_EventMonitor.Register(ResourceEventType.LoadSceneStart, OnLoadSceneStart);
            m_EventMonitor.Register(ResourceEventType.LoadSceneSucess, OnLoadSceneSuccess);
            m_EventMonitor.Register(ResourceEventType.LoadSceneFailed, OnLoadSceneFailed);

            m_EventMonitor.Register(ResourceEventType.WillDestroyBundle, OnDestroyBundle);
            m_EventMonitor.Register(ResourceEventType.WillDestroyAsset, OnDestroyAsset);
        }

        void OnAddQueue(ResourceEventArgs eventArgs)
        {
            if (eventArgs is AssetRelatedEventArgs)
            {
                var args = eventArgs as AssetRelatedEventArgs;
                if (m_IsRecord)
                    m_RecorderSaver.AddQueue(args.Asset.AssetKey, args.IsAync, args.Priority, args.RecordAssetType);

            }
            else
            {
                var args = eventArgs as SceneRelatedEventArgs;
                if (m_IsRecord)
                    m_RecorderSaver.AddQueue(args.Scene.LoadSceneInfo.Asset.NewToAsset(), args.IsAync, args.Priority, RecordAssetType.NormalScene);
            }
        }

        void OnLoadBundleStart(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as BundleRelatedEventArgs;
            if(m_IsRecord)
                m_RecorderSaver.LoadBundleStart(args.Bundle.BundleKey.BundleName);
        }

        void OnLoadBundleSuccess(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as BundleRelatedEventArgs;
            if(m_IsRecord)
                m_RecorderSaver.LoadBundleEnd(args.Bundle.BundleKey.BundleName, true);
        }

        void OnLoadSceneStart(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as SceneRelatedEventArgs;
            if (m_IsRecord)
                m_RecorderSaver.LoadAssetStart(args.Scene.LoadSceneInfo.Asset.NewToAsset());
        }

        void OnLoadSceneSuccess(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as SceneRelatedEventArgs;
            if (m_IsRecord)
                m_RecorderSaver.LoadAssetEnd(args.Scene.LoadSceneInfo.Asset.NewToAsset(), true);
        }

        void OnLoadSceneFailed(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as SceneRelatedEventArgs;
            var info = args.Scene.LoadSceneInfo.Asset.NewToAsset();
            if (m_IsRecord)
                m_RecorderSaver.LoadAssetEnd(args.Scene.LoadSceneInfo.Asset.NewToAsset(), false);

            if(!FailedLoadAssets.Contains(info))
                FailedLoadAssets.Add(info);
        }

        void OnLoadAssetStart(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as AssetRelatedEventArgs;
            if(m_IsRecord)
                m_RecorderSaver.LoadAssetStart(args.Asset.AssetKey);
        }

        void OnLoadAssetSuccess(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as AssetRelatedEventArgs;
            if(m_IsRecord)
                m_RecorderSaver.LoadAssetEnd(args.Asset.AssetKey, true);
        }

        void OnErrorAssetLoadFailed(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as AssetRelatedEventArgs;

            s_logger.Error(eventArgs.ToString());

            m_LoadErrorAssets.Add(args.Asset);
            if(m_IsRecord)
                m_RecorderSaver.LoadAssetEnd(args.Asset.AssetKey, false);

            if (!FailedLoadAssets.Contains(args.Asset.AssetKey))
            {
                FailedLoadAssets.Add(args.Asset.AssetKey);
            }
        }

        void OnErrorBundleLoadFailed(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as BundleRelatedEventArgs;

            s_logger.Error(eventArgs.ToString());

            m_LoadErrorBundles.Add(args.Bundle);
            if(m_IsRecord)
                m_RecorderSaver.LoadBundleEnd(args.Bundle.BundleKey.BundleName, false);
        }

        void OnDestroyBundle(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as BundleRelatedEventArgs;

            var bundleName = args.Bundle.BundleKey.BundleName;
            if (m_IsRecord)
                m_RecorderSaver.UnloadBundle(bundleName);

#if dev
            s_logger.InfoFormat("Bundle {0} has been destroyed",bundleName);
#endif
        }

        void OnDestroyAsset(ResourceEventArgs eventArgs)
        {
            var args = eventArgs as AssetRelatedEventArgs;
            if (m_IsRecord)
                m_RecorderSaver.UnloadAsset(args.Asset.AssetKey);
        }

        void ResourceEventHandler(ResourceEventArgs eventArgs)
        {
            if (ResourceConfigure.ReportType == ResourceEventReportType.ToLocalFile)
            {
                DebugUtil.AppendLocalText("abProfiler", eventArgs.ToString());
            }
            else if (ResourceConfigure.ReportType == ResourceEventReportType.ToLogFile)
            {
                s_logger.Info(eventArgs.ToString());
            }
           
        }
        internal void FrameReset()
        {
            if (lastUpdateFrame != Time.frameCount)
            {
                lastUpdateFrame                      = Time.frameCount;
                m_CurrentFrameUnloadABCount          = 0;
                m_InstantiateTotalTime = 0;
                m_CurrentFrameLoadAssetRequest       = 0;
                FrameTimeRecordManager.InstanceCount = 0;
            }
            else
            {
                //s_logger.Debug("Resource Mananger Update More than once in frame");
            }

        }
        internal void IncUnloadABCount() { m_CurrentFrameUnloadABCount++; }
        internal void IncLoadAssetWaitRequestCount() { m_CurrentFrameLoadAssetRequest++; }
        internal void IncreaseAssetCount() { LoadingAssetCount++;}

        internal bool CanUnloadAB()
        {
            if (!ResourceConfigure.SupportUnload) return false;
            return ResourceConfigure.ImmediatelyDestroy || m_CurrentFrameUnloadABCount < ResourceConfigure.OneFrameUnloadBundleLimit;
        }
        internal bool ReachDestroyTime(IDisposableResourceHandle resource)
        {
            if (!ResourceConfigure.SupportUnload) return false;
            return ResourceConfigure.ImmediatelyDestroy ||  Time.realtimeSinceStartup - resource.WaitDestroyStartTime > resource.DestroyWaitTime;
        }
        public bool CanLoadAssetWaitRequest()
        {
            return m_CurrentFrameLoadAssetRequest < ResourceConfigure.OneFrameLoadAssetLimit;
        }

        public bool LoadAssetIsExceedingLimit()
        {
            return LoadingAssetCount > ResourceConfigure.LoadAssetMaxCount;
        }

        public void SetRecord(bool isRec)
        {
#if dev
            m_IsRecord = isRec;
#endif
        }

        public bool GetRecord()
        {
            return m_IsRecord;
        }

        private FrametimeHandler m_InstantiateHandler;
        public void StartInstantiate()
        {
          

            if (m_InstantiateHandler == null)
            {
                m_InstantiateHandler = new FrametimeHandler();
                m_InstantiateHandler.Registe("ResourceInstantitate");
            }
            m_InstantiateHandler.Start();
        }
        public bool CanAssetInstantiate()
        {
            if (m_InstantiateTotalTime < DurationHelp.InstantiateThreshold) return true;
            s_logger.DebugFormat("load next frame,{0}>{1}", m_InstantiateTotalTime, DurationHelp.InstantiateThreshold);
            return false;

        }
        private double m_InstantiateTotalTime;
        public void StopInstantiate()
        {
            if (m_InstantiateHandler != null)
            {
                var delta = m_InstantiateHandler.Stop();
                
                if (delta > 100)
                    delta = 100;
                m_InstantiateTotalTime += delta;

                ++FrameTimeRecordManager.InstanceCount;
            }
            else
            {
                s_logger.InfoFormat("instantiateHandler dont init yet");

            }
            
        }
    }

}
