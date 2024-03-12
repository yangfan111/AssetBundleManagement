using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AssetBundleManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Assets.AssetBundleManagement.Test
{
    public class TestPanelRunTimeGUI : MonoBehaviour
    {
        public Button ErrorTestBtn;
        public Button DuplicateTestBtn;
        public Button ReferenceLoadTestBtn;
        public Button ReferenceReleaseTestBtn;
        public Button PrintResourceStateBtn;
        public Button LoadInstanceHandlerBtn;
        public Button ReleaseInstanceHandlerBtn;

        public GameObject PlayerEntity;
        public GameObject UnityObjectPool;

        public Text ProfilerInfoText;

        public  int UnusedBundleDestroyWaitTime = 5; //Asset卸载时会清除相关bundle引用计数->bundle引用计数到0之后加入卸载队列->之后等待多久时间后销毁(s)
        public  int UnusedAssetDestroyWaitTime = 10;//Asset引用计数为0之后加入卸载队列->之后等待多久时间后销毁(s)
        public  int OneFrameUnloadBundleLimit = 1;//一帧内最多卸载多少个bundle对象，防止出现卡顿
        public  int OneFrameLoadAssetLimit = 5;//一帧内执行资源加载请求的数量上限

        public void Start()
        {
          
            ErrorTestBtn.onClick.AddListener(LoadErrorTest);
            DuplicateTestBtn.onClick.AddListener(LoadDuplicateTest);
            ReferenceLoadTestBtn.onClick.AddListener(LoadAssetReferenceTest);
            ReferenceReleaseTestBtn.onClick.AddListener(ReleaseAssetHandlersTest);
            PrintResourceStateBtn.onClick.AddListener(PrintResourceState);
            LoadInstanceHandlerBtn.onClick.AddListener(LoadInstanceHandler);
            ReleaseInstanceHandlerBtn.onClick.AddListener(ReleaseInstanceHandler);
        }

        #region AssetTest

        void PrintResourceState()
        {
            AssetManagement.CollectResourceDetailData();
        }
        void LoadErrorTest()
        {
            List<AssetInfo> testAssetList = new List<AssetInfo>()
        {
            new AssetInfo("prefabab","Sphere2"),
            new AssetInfo("prefabab","error0"),
            new AssetInfo("prefabab","Sphere"),
            new AssetInfo("prefabab","error1"),
            new AssetInfo("errorab","test1"),
            new AssetInfo("errorab","test2"),
            
        };
            LoadAssetTest(testAssetList);
        }
        void LoadDuplicateTest()
        {
            List<AssetInfo> testAssetList = new List<AssetInfo>()
        {
            new AssetInfo("prefabab","Sphere2"),
            new AssetInfo("prefabab","Sphere2"),
            new AssetInfo("prefabab","Sphere3"),
            new AssetInfo("prefabab","Sphere"),
            new AssetInfo("prefabab","Sphere3"),

        };
            LoadAssetTest(testAssetList);
        }

        void LoadAssetReferenceTest()
        {
            ResourceConfigure.SupportUnload                        = true;
           
            List<AssetInfo> testAssetList = new List<AssetInfo>()
            {
                new AssetInfo("prefabab","Sphere2"),
                new AssetInfo("prefabab","Sphere3"),
                new AssetInfo("prefabab","Sphere3"),

                new AssetInfo("matab","blue"),
               
                new AssetInfo("baseballab","WPN_BaseballBat00_P3"),
                new AssetInfo("knifeab","WPN_ChangDao0000_P3"),
                new AssetInfo("prefabab","Sphere2"),
                new AssetInfo("baseballab","WPN_BaseballBat00_P3"),
                new AssetInfo("matab","blue"),
                new AssetInfo("depsab1","Floor"),
                new AssetInfo("depsab2","WPNT_BaseballBat0000_N"),
                new AssetInfo("depsab2","error"),
                new AssetInfo("error","error"),
            

            };

            LoadAssetTest(testAssetList);
        }

        void ReleaseAssetHandlersTest()
        {
            StartCoroutine(ReleaseAssetHandlerCortinue());
        }
        IEnumerator CheckAllBundleUnloaded()
        {
            AssetManagement.TestInstance.Update();
            while (true)
            {
                var assetStatus = AssetManagement.GetResourceManangerStatus();
                if (assetStatus.ToBeDestroyCont == 0)
                {
                    if (assetStatus.AllBundleSucessCont == 0)
                    {
                        AssetLogger.MyLog("所有bundle对象被卸载成功", AssetLogger.DebugColor.Green);
                    }
                    else
                    {
                        AssetLogger.MyLog("bundle对象被卸载失败,剩余{0}", AssetLogger.DebugColor.Red, assetStatus.AllBundleSucessCont);
                    }
                    break;
                }
                else
                {
                    yield return null;
                }
            }


        }
        IEnumerator CheckAllAssetUnloaded()
        {
            AssetManagement.TestInstance.Update();
            while (true)
            {
                var assetStatus = AssetManagement.GetResourceManangerStatus();
             
                    if (assetStatus.AllAssetSucessCont == 0)
                    {
                        AssetLogger.MyLog("所有asset资源对象被卸载成功", AssetLogger.DebugColor.Green);
                         break;
                    }
                     if (assetStatus.ToBeDestroyCont == 0)
                     {
                    {
                        AssetLogger.MyLog("asset资源对象被卸载失败,剩余{0}", AssetLogger.DebugColor.Red,assetStatus.AllAssetSucessCont);
                        break;

                    }
                }
                yield return null;
            }

            
        }
        IEnumerator ReleaseAssetHandlerCortinue()
        {
            float secs = 2.0f / handlerList.Count;
            var newHandlerList = handlerList.ToList();
            handlerList.Clear();
            foreach (var handler in newHandlerList)
            {
                AssetLogger.LogToFile("Release Asset {0}",handler.ResourceObject.name);
                handler.Release();
           //     AssetManagement.TestInstance.CollectResourceDetailData();
                yield return new WaitForSeconds(secs);
            }

            AssetManagement.CollectResourceDetailData();
            ResourceManangerStatus status = AssetManagement.GetResourceManangerStatus();
            if(status.AllAssetHoldReference ==0)
            {
                AssetLogger.MyLog("所有asset资源引用计数清零成功",AssetLogger.DebugColor.Green);
            }
            else
            {
                AssetLogger.MyLog("一些资源引用计数没有被卸载,存在问题!",AssetLogger.DebugColor.Red);
            }
            yield return StartCoroutine(CheckAllAssetUnloaded()) ;
            yield return StartCoroutine(CheckAllBundleUnloaded()) ;
            AssetLogger.MyLog("卸载结束,当前资源状态:{0}", AssetLogger.DebugColor.Green, AssetManagement.GetResourceManangerStatus());

            yield return null;
        }
        private void LoadAssetTest(List<AssetInfo> testAssetList)
        {
            loadAssetCount+= testAssetList.Count;

            foreach (AssetInfo asset in testAssetList)
            {
                LoadAssetDefaultConfig(asset);
            }
        }
   
        private List<ResourceHandler> handlerList = new List<ResourceHandler>();
     
        private int loadAssetCount;
        void LoadAssetDefaultConfig(AssetInfo asset)
        {
        
            AssetManagement.TestInstance.LoadAssetAsync(asset, new LoadResourceOption(OnLoadAssetSuccess, OnLoadAssetFailure, ResourceHandlerType.Asset,LoadPriority.P_MIDDLE, this));
        }
        void OnLoadAssetSuccess(ResourceHandler assetAccessor)
        {
            Debug.Log("Asset Sucess:"+ assetAccessor.ResourceObject.name);
            handlerList.Add(assetAccessor);
            CheckTest();

        }
        
        void OnLoadAssetFailure(string errorMessage, object context)
        {
            Debug.Log(errorMessage);
            CheckTest();

        }

        void CheckTest()
        {
            --loadAssetCount;
            if (loadAssetCount == 0)
            {
                AssetLogger.MyLog("加载列表回调返回正确",AssetLogger.DebugColor.Green);
            }
        }

        #endregion


        #region InstanceTest

        
        private List<ResourceHandler> instanceHandlerList = new List<ResourceHandler>();
        void LoadInstanceHandler()
        {
            AssetInfo assetInfo = new AssetInfo("baseballab", "WPN_BaseballBat00_P3");
            LoadResourceOption loadResourceOption = new LoadResourceOption(OnLoadInstanceSuccess, OnLoadInstanceFailure, 
                ResourceHandlerType.Instance, LoadPriority.P_MIDDLE, this);

            AssetManagement.TestInstance.InstantiateAsync(assetInfo, loadResourceOption, PlayerEntity.transform);
            //AssetManagement.LoadAssetAsync(assetInfo, loadResourceOption);

            AssetInfo OtherAssetInfo = new AssetInfo("knifeab", "WPN_ChangDao0000_P3");
            LoadResourceOption OtherloadResourceOption = new LoadResourceOption(OnLoadInstanceSuccess, OnLoadInstanceFailure, 
                ResourceHandlerType.Instance, LoadPriority.P_MIDDLE, this);
            AssetManagement.TestInstance.InstantiateAsync(OtherAssetInfo, OtherloadResourceOption, PlayerEntity.transform);
        }

        void ReleaseInstanceHandler()
        {
            if(instanceHandlerList.Count>0)
            {
                instanceHandlerList[instanceHandlerList.Count - 1].Release();
                instanceHandlerList.RemoveAt(instanceHandlerList.Count - 1);
            }
     
        }
        
        void OnLoadInstanceSuccess(ResourceHandler instanceAccessor)
        {
            instanceHandlerList.Add(instanceAccessor);
            var gameObeject = instanceAccessor.AsGameObject;
            gameObeject.transform.position = new Vector3((instanceHandlerList.Count - 1) * 0.3f, 0, 1);
        }

        void OnLoadInstanceFailure(string errorMessage, object context)
        {


        }

        #endregion

        private void Update()
        {
            ResourceConfigure.OneFrameLoadAssetLimit = OneFrameLoadAssetLimit;
            ResourceConfigure.OneFrameUnloadBundleLimit = OneFrameUnloadBundleLimit;
            ResourceConfigure.UnusedAssetDestroyWaitTime = UnusedAssetDestroyWaitTime;
            ResourceConfigure.UnusedBundleDestroyWaitTime = UnusedBundleDestroyWaitTime;
            ProfilerInfoText.text = AssetManagement.TestGetCollectResourceDetailData().ToString();
        }
    }
}