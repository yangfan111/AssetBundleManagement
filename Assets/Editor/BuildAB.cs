using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetBundleTools : UnityEditor.Editor
{
    //ab文件输出目录
    private static readonly string destPath = Application.streamingAssetsPath; 
    public static string DestPath
    {
        get {
            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);
            return destPath;
        }
    }

    /// <summary>
    /// 所选资源打包为一个ab文件
    /// </summary>
    [MenuItem("AssetBundleTools/BuildMix")]
    private static void PackageAssetBundleMix()
    {
        var                abList   = AssetDatabase.GetAllAssetBundleNames(); //提取打上assetbundle标签的所有ab名
        AssetBundleBuild[] buildMap = new AssetBundleBuild[abList.Length];
        for(int k=0;k<abList.Length;k++)
        {
            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(abList[k]); //提取打上指定assetbundle标签的资源列表路径

            buildMap[k].assetNames      = assetPaths;
            buildMap[k].assetBundleName = abList[k];

        }
        if (BuildPipeline.BuildAssetBundles(DestPath, buildMap, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64))
        {
            Debug.Log(buildMap[0].assetNames + "Package Success");
        }
        else {
            Debug.Log(buildMap[0].assetNames + "Package Faild");
        }
        AssetDatabase.Refresh();
    }

        /*
        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
        buildMap[0].assetBundleName = "bundlename"; //生成的ab名
        Object[] objs   = Selection.objects;
        string[] assets = new string[objs.Length];
        for (int i = 0; i < objs.Length; i++)
        {
            assets[i] = AssetDatabase.GetAssetPath(objs[i]); //获取文件工程相对路径
        }
        buildMap[0].assetNames = assets;
        if (BuildPipeline.BuildAssetBundles(DestPath, buildMap, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget))
        {
            Debug.Log(buildMap[0].assetNames + "Package Success");
        }
        else {
            Debug.Log(buildMap[0].assetNames + "Package Faild");
        }
        AssetDatabase.Refresh(); 
        */
    }
  
