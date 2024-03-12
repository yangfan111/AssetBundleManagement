using System.Collections;
using System.Collections.Generic;
using System.Text;
using AssetBundleManagement.Manifest;
using Assets.AssetBundleManagement.Test;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetBundleManagement
{
    public interface IResourceLoader
    {
        void LoadAsset(AssetInfo path, ref LoadResourceOption loadOption);
    }
    public class ResourceLoadManager:IResourceLoader
    {

        private readonly Dictionary<AssetInfo, AssetResourceHandle> m_AsseReosurceMap = new Dictionary<AssetInfo, AssetResourceHandle>(2048,AssetInfo.AssetInfoComparer.Instance);
      
        private readonly AssetBundleLoadProvider m_AbProvider ;
        private readonly ResourceUpdater m_UpdateMgr ;
        private readonly ClientGameController m_startGameController;
        private readonly ManiFestProvider m_ManiFestProcider;

        private bool IsInitialized {
            get
            {
                return m_ManiFestProcider.ManiFestLoadState == ManiFestLoadState.Loaded;
            }
            
        }
        
        public void Update()
        {
            if(!IsInitialized)
                return;
            
            m_UpdateMgr.Update(m_AbProvider);
        }
        
        public ResourceLoadManager()
        {
            m_UpdateMgr = new ResourceUpdater();
            m_ManiFestProcider = new ManiFestProvider();
            m_AbProvider = new AssetBundleLoadProvider(m_UpdateMgr, m_ManiFestProcider);
        }
        
        public IEnumerator Init(ICoRoutineManager coRoutineManager)
        {
            ManiFestInitializer maniFestInitializer = ManiFestFactory.CreateManiFestInitializer(ResourceConfigure.LoadPattern);
            m_ManiFestProcider.ManiFestLoadState = ManiFestLoadState.Loading;

            yield return coRoutineManager.StartCoRoutine(maniFestInitializer.LoadRootManiFest(m_ManiFestProcider.ManiFestInitializerSuccess));

            AssetLogger.LogToFile("ManiFestInitializer :{0},content:\n{1}", m_ManiFestProcider.ManiFestLoadState, m_ManiFestProcider.ToString());
            yield return null;
        }
     
        public void LoadAsset(AssetInfo path,ref LoadResourceOption loadOption)
        {
            if (path.IsValid())
            {
                ResourceProfiler.RequestAsset(ref path);
                AssetResourceHandle objectResource;
                if (!m_AsseReosurceMap.TryGetValue(path, out objectResource))
                {
                    objectResource = new AssetResourceHandle(new AssetLoadOperation(m_UpdateMgr, path, m_ManiFestProcider.GetLoadingPattern(path.BundleName)), path);
                    objectResource.AssetReferenceChangedNotify += OnAssetReferenceChangedCallback;
                    objectResource.AssetWillDestroyNotify += OnAssetWillDestroyCallback;
                    m_AsseReosurceMap.Add(path, objectResource);
                  
                 
                }
                var loadTask = objectResource.HandleAssetObjectLoadRequest(ref loadOption);
                if (loadTask != null)
                    m_UpdateMgr.AddWaitLoadAssetTask(loadTask);
             
            }
            else 
            {
                loadOption.CallOnFailed(path);
             
            }
        }
        void OnAssetReferenceChangedCallback(AssetResourceHandle resourceHandle)
        {
            m_UpdateMgr.InspectResourceDisposeState(resourceHandle);
        }

        void OnAssetWillDestroyCallback(AssetResourceHandle resourceHandle)
        {
            m_AsseReosurceMap.Remove(resourceHandle.AssetKey);
        }

        public void CollectResourceDetailData(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("----------------------CollectResourceDetailData--------------------");
            GetAssetsAllInfo(stringBuilder);
            m_AbProvider.GetProviderAllInfo(stringBuilder);
            m_UpdateMgr.GetUpdaterAllInfos(stringBuilder);
            ResourcePoolContainer.GetPoolInfos(stringBuilder);
        }

        public ResourceManangerStatus GetResourceStatus()
        {
            ResourceManangerStatus status = new ResourceManangerStatus();
            int                    sum    = 0;
            foreach (var pair in m_AsseReosurceMap.Values)
            {
                sum += pair.ReferenceCount;
                if(pair.LoadState == AssetLoadState.Failed)
                {
                    status.AllAssetFailedCont++;
                }
                else
                {
                    status.AllAssetSucessCont++;
                }
            }

            status.AllAssetHoldReference = sum;
            m_AbProvider.GetResourceStatus(ref status);
            m_UpdateMgr.GetResourceStatus(ref status);
            return status;
        }
        void GetAssetsAllInfo(StringBuilder stringBuilder)
        {
            stringBuilder.AppendFormat("====>Assets LoadData,Size:{0}\n", m_AsseReosurceMap.Count);
            foreach (var pair in m_AsseReosurceMap)
            {
                stringBuilder.AppendFormat("    [AssetItem]{0}\n", pair.Value.ToStringDetail());
            }
        }
    }
}