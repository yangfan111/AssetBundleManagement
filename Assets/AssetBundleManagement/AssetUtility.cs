using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using AssetBundleManagement.Manifest;

namespace AssetBundleManagement
{


    public static class AssetUtility
    {
        public static BundleKey ConvertToBundleKey(AssetInfo path)
        {
            return new BundleKey(path.BundleName);
        }
        
        public static BundleKey ConvertToBundleKey(string bundleName)
        {
            return new BundleKey(bundleName);
        }

        public static int GetLoadPrioritySequentialIndex(LoadPriority priority)
        {
            int sequentialIndex;
            switch (priority)
            {
                case LoadPriority.P_NoKown:
                    sequentialIndex = 0;
                    break;
                case LoadPriority.P_LOW:
                    sequentialIndex = 1;
                    break;
                case LoadPriority.P_SINGLE:
                    sequentialIndex = 2;
                    break;
                case LoadPriority.P_Art_HIGH:
                    sequentialIndex = 3;
                    break;
                case LoadPriority.P_MIDDLE:
                    sequentialIndex = 4;
                    break;
                case LoadPriority.P_GROUP_JOB:
                    sequentialIndex = 5;
                    break;
                case LoadPriority.P_NORMAL_HIGH:
                    sequentialIndex = 6;
                    break;
                case LoadPriority.P_THIRD_MODEL_HIGH:
                    sequentialIndex = 7;
                    break;
                case LoadPriority.P_FIRST_PRELOAD_HIGH:
                    sequentialIndex = 8;
                    break;
                case LoadPriority.P_BIGMAP_UI_HIGH:
                    sequentialIndex = 9;
                    break;
                case LoadPriority.P_FIRST_HIGH:
                    sequentialIndex = 10;
                    break;
                default:
                    sequentialIndex = 0; // 处理未知枚举值的情况
                    break;
            }

            return sequentialIndex;
        }
    }
}
