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

        public int UnusedBundleDestroyWaitTime = 5; //Assetж��ʱ��������bundle���ü���->bundle���ü�����0֮�����ж�ض���->֮��ȴ����ʱ�������(s)
        public int UnusedAssetDestroyWaitTime = 10;//Asset���ü���Ϊ0֮�����ж�ض���->֮��ȴ����ʱ�������(s)
        public int OneFrameUnloadBundleLimit = 1;//һ֡�����ж�ض��ٸ�bundle���󣬷�ֹ���ֿ���
        public int OneFrameLoadAssetLimit = 5;//һ֡��ִ����Դ�����������������
        public int UnusedInstanceObjectPoolWaitTime = 3; //����ض��û�ж���ʹ��֮���������������
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