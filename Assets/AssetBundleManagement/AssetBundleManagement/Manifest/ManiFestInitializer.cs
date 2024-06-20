using System;
using Common;
using Core.Utils;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AssetBundleManager.Manifest;
using AssetBundleManager.Warehouse;
using AssetBundles;
using UnityEngine;

namespace AssetBundleManagement.Manifest
{
    
    public  class ManiFestInitializer:IAssetBundleExternalAccess
    {
        public ManiFestLoadState ManiFestLoadState = ManiFestLoadState.NotBegin;
        // AssetBundle Address
        private AssetBundleWarehouseAddr m_Addr;

        private AssetBundleWarehouse m_Default;
        private AssetBundleWarehouse m_Simulation;
        private AssetBundleWarehouse m_HotFix;
        private List<string> m_HotfixBundles = new List<string>();
        private static LoggerAdapter s_logger = new LoggerAdapter(typeof(ManiFestInitializer));


        #region //Manifest Init
        void SetDefaultWarehouse(AssetBundleWarehouseAddr addr)
        {
            if (addr == null)
                throw new ArgumentNullException();
            if (addr.Pattern == AssetBundleLoadingPattern.EndOfTheWorld)
            {
                throw new ArgumentException(addr.ToString());
            }
            else if (addr.Pattern == AssetBundleLoadingPattern.AsyncCacheWeb)
            {
                if (string.IsNullOrEmpty(addr.LocalPath) || string.IsNullOrEmpty(addr.WebUrl))
                    throw new ArgumentException(addr.ToString());
            }
            else if (addr.Pattern == AssetBundleLoadingPattern.AsyncWeb)
            {
                if (string.IsNullOrEmpty(addr.WebUrl))
                    throw new ArgumentException(addr.ToString());
            }
            else if (addr.Pattern != AssetBundleLoadingPattern.Simulation)
            {
                if (string.IsNullOrEmpty(addr.LocalPath))
                    throw new ArgumentException(addr.ToString());
            }   
            //if (string.IsNullOrEmpty(addr.Manifest))
            //    addr.Manifest = Utility.GetPlatformName();

            addr.LocalWithoutPlatform = addr.LocalPath;
            addr.LocalPath            = Utility.ProcessAssetBundleBasePath(addr.LocalPath);
            addr.WebUrl               = Utility.ProcessAssetBundleBasePath(addr.WebUrl);

            m_Addr = addr;
            s_logger.InfoFormat(m_Addr.ToString());
        }
        
         void IAssetBundleExternalAccess.SetHotfixBundles(List<string> bundles)
        {
            m_HotfixBundles = bundles;
            if (m_HotfixBundles == null)
                m_HotfixBundles = new List<string>();
        }
        
         List<string> IAssetBundleExternalAccess.GetHotfixBundles()
        {
            return m_HotfixBundles;
        }

        void IAssetBundleExternalAccess.SetQualityLow(bool isLow)
        {
            m_Default.SetSceneQuantityLevel(isLow);
        }

        bool IAssetBundleExternalAccess.IsQualityLow()
        {
            return m_Default.IsQualityLow();
        }
        //manifest md 或者 文件列表md5
        public void Init(AssetBundleWarehouseAddr addr, bool isLow, IAssetBundleManifest manifest)
        {
            SetDefaultWarehouse(addr);
            m_Default             = WarehouseFactory.CreateWarehouse(m_Addr, manifest, isLow);
            InitSupplementaryWarehouse(isLow);
            InitHotfixWarehouse(isLow, manifest);
            
#if ENABLE_ASSET_BUNDLE_HEADER_SERIALIZE 


            if (AssetBundle.isAssetBundleHeaderEnabled)
            {
                string headerFileName = m_Addr.LocalPath + "AssetBundleGroupHeader";
                s_logger.InfoFormat("----------------------Deserialize asset bundle header start: {0}", headerFileName);
                AssetBundle[] assetBundles = AssetBundle.DeserializeAssetBundleHeaderToBundles(headerFileName);
                foreach (var assetBundle in assetBundles)
                {
                    LoadedAssetBundleSet.Insert(assetBundle.name,assetBundle);
            }
                s_logger.InfoFormat("----------------------Deserialize asset bundle header count {0}", assetBundles.Length);
            }
#endif
        }
        private void InitSupplementaryWarehouse(bool isLow)
        {
#if UNITY_EDITOR //TODO 调试调试模式

            var addr = new AssetBundleWarehouseAddr()
            {
                Pattern = AssetBundleLoadingPattern.Simulation,
            };

            m_Simulation = WarehouseFactory.CreateWarehouse(addr, new SimulationAssetBundleManifest(m_Addr.Supplement), isLow);
#endif
        }
        private void InitHotfixWarehouse(bool isLow, IAssetBundleManifest manifest)
        {
            var addr = new AssetBundleWarehouseAddr()
            {
                Pattern              = AssetBundleLoadingPattern.Hotfix,
                LocalWithoutPlatform = m_Addr.LocalWithoutPlatform,
            };

            m_HotFix = WarehouseFactory.CreateWarehouse(addr, manifest, isLow);
        }
        HashSet<string> IAssetBundleExternalAccess.AllAssetBundleNames
        {
            get
            {
                var result = new HashSet<string>();

                foreach (var bundleName in m_Default.AllAssetBundleNames())
                {
                    result.Add(bundleName);
                }
                return result;
            }
        }
        private AssetBundleWarehouse FindWarehouse(string bundleName, AssetBundleWarehouse defaultWarehouse)
        {
            var warehouse = defaultWarehouse;

            var baseName = Utility.GetNameWithoutVariant(bundleName);

            if (m_Addr.Supplement.Contains(baseName))
                warehouse = m_Simulation;
            else
            {
                foreach (var name in m_Addr.Supplement)
                {
                    if (baseName.StartsWith(name))
                    {
                        warehouse = m_Simulation;
                        break;
                    }
                }
            }

            if (m_HotfixBundles.Contains(bundleName))
            {
                warehouse = m_HotFix;
            }

            return warehouse;
        }
        string IAssetBundleExternalAccess.GetBundleNameWithVariant(string bundleName) //todo:??
        {
            var preferedWarehouse = FindWarehouse(bundleName, m_Default);
            return BundleRenameHelper.GetBundleNameWithLQ(bundleName, preferedWarehouse);
        }
        IAssetBundleManifest IAssetBundleExternalAccess.GetWarhouseManifest(string bundleName)
        {
            var preferedWarehouse = FindWarehouse(bundleName, m_Default);
            return preferedWarehouse.BundleManifest;
        }
      

      
        
        bool IAssetBundleExternalAccess.IsSimulationWareHouse(string bundleName)
        {
            var preferedWarehouse = FindWarehouse(bundleName, m_Default);
            return preferedWarehouse is SimulationWarehouse;
        }
         string IAssetBundleExternalAccess.GetAssetBundleFileName(string name)
        {
            return m_Default.GetAssetBundleFileName(name);
        }

         public AssetBundleInfoItem GetAsssetBundleInfo(string bundleName)
         {
             var preferedWarehouse = FindWarehouse(bundleName, m_Default);
             AssetBundleInfoItem item = preferedWarehouse.GetAssetBundleInfo(bundleName);
             return item;

         }

         public AssetBundleWarehouse FindWarehouse(string bundleName)
         {
             return  FindWarehouse(bundleName, m_Default);
             
         }

         public AssetBundleLoadingPattern GetLoadingPattern(string bundleName)
         {
             return FindWarehouse(bundleName, m_Default).LoadingPattern;
           
         }

        #endregion
    }


   
}