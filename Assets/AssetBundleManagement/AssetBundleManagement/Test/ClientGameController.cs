#if false
using System;
using System.Collections;
using System.Collections.Generic;
using AssetBundleManagement;
using UnityEngine;

namespace Assets.AssetBundleManagement.Test
{
    public interface ICoRoutineManager
    {
        Coroutine StartCoRoutine(IEnumerator enumerator);
    }
    public class ClientGameController : MonoBehaviour, ICoRoutineManager
    {
        private ResourceInitTest resourceInitTest = new ResourceInitTest();

        public int UnusedBundleDestroyWaitTime = 5; //Asset卸载时会清除相关bundle引用计数->bundle引用计数到0之后加入卸载队列->之后等待多久时间后销毁(s)
        public int UnusedAssetDestroyWaitTime = 10;//Asset引用计数为0之后加入卸载队列->之后等待多久时间后销毁(s)
        public int OneFrameUnloadBundleLimit = 1;//一帧内最多卸载多少个bundle对象，防止出现卡顿
        public int OneFrameLoadAssetLimit = 5;//一帧内执行资源加载请求的数量上限
        public int UnusedInstanceObjectPoolWaitTime = 3; //对象池多久没有对象使用之后销毁整个对象池
        public bool ABLoadAgent;
        public int TimeScale = 1;
        public int DamageScale = 100;
        public bool InfiniteHomeBlood = true;


        IEnumerator Start()
        {

            TowerTest.LoadAgent = ABLoadAgent;


            yield return resourceInitTest.TestInit(this);

            ResourceProvider.TestInstance.DestroyUnusedAssetsImmediately();

        }
        void OnEnable()
        {
            
        }
        void OnDisable()
        {
            
        }

        void Update()
        {
            Time.timeScale = TimeScale;
            TowerTest.DamageScale = DamageScale;
            TowerTest.InfiniteHomeBlood = InfiniteHomeBlood;
            ResourceConfigure.OneFrameLoadAssetLimit = OneFrameLoadAssetLimit;
            ResourceConfigure.OneFrameUnloadBundleLimit = OneFrameUnloadBundleLimit;
            ResourceConfigure.UnusedAssetDestroyWaitTime = UnusedAssetDestroyWaitTime;
            ResourceConfigure.UnusedBundleDestroyWaitTime = UnusedBundleDestroyWaitTime;
            ResourceConfigure.UnusedInstanceObjectPoolWaitTime = UnusedInstanceObjectPoolWaitTime;

            //   ProfilerInfoText.text = AssetManagement.TestGetCollectResourceDetailData().ToString();
            ResourceProvider.TestInstance.Update();
            ResourceProvider.TestInstance.CollectResourceInspectorData(ResourceProfiler.ProfileDataAdapter.InspectorDataSet);
            //InspectorDataSet.ConvertEventToString();
        }

        public Coroutine StartCoRoutine(IEnumerator enumerator)
        {
            return StartCoroutine(enumerator);
        }

        private void OnDestroy()
        {
            //AssetManagement.TestInstance.OnDestroy();
        }
    }
}
#endif