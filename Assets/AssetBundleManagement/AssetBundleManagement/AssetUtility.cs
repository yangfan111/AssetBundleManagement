using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using AssetBundleManagement.Manifest;
using Common;
using Core.Utils;
using UnityEngine;
using Utils.AssetManager;
using Utils.Singleton;

namespace AssetBundleManagement
{


    public static class AssetUtility
    {
        static int s_handleIdGenerator=1;
        internal static int GenerateNewHandle()
        {
            return ++s_handleIdGenerator;
        }
        static LoggerAdapter execptionLogger = new LoggerAdapter("AssetBundleManagement.AssetUtility");
        public static BundleKey ConvertToBundleKey(AssetInfo path)
        {
            return new BundleKey(path.BundleName);
        }
   
        private static readonly string WeaponBundleStartString = "weapon/";
        private static readonly string WeaponAttachmentBundleStartString = "attachment/";
     
        public static bool IsWeaponOrWeaponAttachment(this AssetInfo assetInfo)
        {
            return assetInfo.BundleName.StartsWith(WeaponBundleStartString) || assetInfo.BundleName.StartsWith(WeaponAttachmentBundleStartString);;
        }
        public static Transform ParentOrDefault(Transform parent)
        {
            return parent != null ? parent : UnityObject.DefaultParent;
        }
        public static void SetParentOrDefaultWithoutWorldPosStay(this GameObject target, Transform parent)
        {
            parent = ParentOrDefault(parent);
            if(parent != target.transform.parent)
                target.transform.SetParent(parent,false);

        }

        public static void InitializeGameObjectAssetTemplate(GameObject go,AssetInfo AssetInfo)
        {
            if (AssetInfo.IsWeaponOrWeaponAttachment())
            {
                //Tricky Code: Do not auot-drive Animator or Animation, Disable it
                //It's better to disable animator or animation in Prefabs of Weapon or WeaponAttachment
               if (go)
                {
                    var animator = go.GetComponent<UnityEngine.Animator>();
                    if (animator && animator.enabled)
                        animator.enabled = false;

                    var animation = go.GetComponent<UnityEngine.Animation>();
                    if (animation && animation.enabled)
                        animation.enabled = false;
                }
            }

            if (go)
            {
                CommonUtil.AutoAttachRigidbodyIfNeeded(go);
            }

        }

        public static NewAssetInfo ToNewAsset(this AssetInfo assetInfo)
        {
            return new NewAssetInfo(assetInfo.BundleName, assetInfo.AssetName, assetInfo.AssetType);
        }

        public static AssetInfo NewToAsset(this NewAssetInfo assetInfo)
        {
            return new AssetInfo(assetInfo.BundleName, assetInfo.AssetName, assetInfo.AssetType);
        }

        public static BundleKey ConvertToBundleKey(string bundleName)
        {
            return new BundleKey(bundleName);
        }
        public static void ThrowException(string msg)
        {
            if (ResourceConfigure.ErrorLogInsteadThrowExecption)
                execptionLogger.Error("AssetBundleManagement process exception:" + msg);
            else 
                throw new Exception("AssetBundleManagement process exception:" + msg);
        }

        public static int GetLoadPrioritySequentialIndex(LoadPriority priority)
        {
            return (int)priority;
        }
        public static BatchLoadResourceRequest AllocBatchRequestFromList(List<AssetInfo> list,LoadPriority loadPriority,OnBatchLoadResourceComplete onLoadResourceComplete = null,Transform parent = null)
        {
            var request = BatchLoadResourceRequest.Alloc();
            request.Context = (int) list.Count;
            request.DefaultPriority             = loadPriority;
            request.OnBatchLoadResourceComplete = onLoadResourceComplete;
            request.Parent = parent;

            for (int i = 0; i < list.Count; i++)
            {
                request.AddReq(i, list[i]);
            }
            return request;
        }

        public static List<UnityObject> FetchBatchResultToList(BatchLoadResourceResult results)
        {
            List<UnityObject> ret   = new List<UnityObject>();
            int               count = (int)results.GetRequest().Context;
            for (int i = 0; i < count; i++)
            {
                UnityObject uo =  results.Get(i);
                ret.Add(uo);
            }
            results.GetRequest().Release();
            results.Release();
            return ret;
        }

        public static AsyncOperation UnloadUnusedAssets(bool includeGcCollect)
        {
#if dev
            execptionLogger.InfoFormat(">>>> Call UnloadUnusedAssets \n {0}", new StackTrace());
#endif
            if(includeGcCollect)
                SingletonManager.Get<gc_manager>().gc_collect();
            return new AsyncOperation();

            // return Resources.UnloadUnusedAssets();
        }

        public static bool IsCannotDestroy(string bundleName)
        {
            bundleName = bundleName.ToLower();
            for (int i = 0; i < ResourceConfigure.CanDestroyBundleStartList.Count; i++)
                if (bundleName.StartsWith(ResourceConfigure.CanDestroyBundleStartList[i]))
                    return false;
            for (int i = 0; i < ResourceConfigure.CannotDestroyBundleStartList.Count; i++)
                if (bundleName.StartsWith(ResourceConfigure.CannotDestroyBundleStartList[i]))
                    return true;
            return false;
        }

        public static string StandardSceneProviderKey(string sceneInfo)
        {
            var index = sceneInfo.LastIndexOf('/');
            if (index > 0)
            {
                sceneInfo = sceneInfo.Substring(index + 1, sceneInfo.Length - index - 1);
            }
            sceneInfo = sceneInfo.Replace(".unity", "");
            return sceneInfo;
        }
    }
}
