using Assets.AssetBundleManagement.Test;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssetBundleManagement.ObjectPool;
using UnityEngine;

namespace AssetBundleManagement
{
    public  class AssetManagement
    {
        static AssetManagement testInstance;
        public static AssetManagement TestInstance
        {
            get
            { 
                if (testInstance == null) 
                    testInstance = new AssetManagement();
                return testInstance;
            }
        }

        private static ResourceLoadManager s_ResourceMananger = new ResourceLoadManager();
        private InstanceObjectPoolContainer defaultObjectPool;

        
        public AssetManagement()
        {
            HandleGameObjectAssetLoadedCache = HandleGameObjectAssetLoaded;
            defaultObjectPool = new InstanceObjectPoolContainer(s_ResourceMananger);
        }
        private  OnGameObjectInstantiateHandler HandleGameObjectAssetLoadedCache ;
        public static IEnumerator InitResourceMananger(ICoRoutineManager coRoutine)
        {
            yield return s_ResourceMananger.Init(coRoutine);
        }

        public  void Update()
        {
            ResourceRecorder.Get().FrameReset();
            s_ResourceMananger.Update();
            defaultObjectPool.Update();
        }

        public static StringBuilder CollectResourceDetailData()
        {
            StringBuilder stringBuilder = new StringBuilder(1024);
            s_ResourceMananger.CollectResourceDetailData(ref stringBuilder);
            AssetManagement.TestInstance.defaultObjectPool.CollectProfileInfo(ref stringBuilder);
            stringBuilder.AppendLine("----------------------end--------------------");
            AssetLogger.LogToFile(stringBuilder.ToString());
            return stringBuilder;
        }

        public static ResourceManangerStatus GetResourceManangerStatus()
        {
            return s_ResourceMananger.GetResourceStatus();
        }
        

        public  void LoadAssetAsync(AssetInfo path, LoadResourceOption loadOption)
        {
            loadOption.OnGameObjectAssetLoaded = HandleGameObjectAssetLoadedCache;
            s_ResourceMananger.LoadAsset(path,ref loadOption);
        }
         void HandleGameObjectAssetLoaded(AssetInfo path, LoadResourceOption loadResourceOption,AssetObjectHandler assetHandler)
        {
            //检测如果是GameObject类型&&通过LoadAssetAsync接口调用，需要再通过对象池给它一个实例化对象，gameobject类型对象一定要走对象池
            Debug.LogErrorFormat("{0} is gameobject asset,should use InstantiateAsync instead of LoadAssetAsync", path);
            assetHandler.Release();
            InstantiateAsync(path, loadResourceOption);
        }
        public  void InstantiateAsync(AssetInfo path, LoadResourceOption loadOption, Transform parent = null, InstanceObjectPoolContainer gameObjectPool = null)
        {
            loadOption.OnGameObjectAssetLoaded = null;
            if (gameObjectPool == null)
                defaultObjectPool.InstantiateAsync(path, ref loadOption, parent);

            else
                gameObjectPool.InstantiateAsync(path, ref loadOption, parent);
        }

        #region 测试代码要删除

        public static  StringBuilder TestGetCollectResourceDetailData()
        {
            return CollectResourceDetailData();
        }

        #endregion

    }
}
