using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
/*

public class AssetLogger
{
    public static string ArchieveFilePath;
    public static void Init()
    {
      //  var fileName = string.Format("{0}.log", DateTime.Now.ToString("Debug"));
        ArchieveFilePath = Path.GetDirectoryName(UnityEngine.Application.dataPath) + "/Log/";
        if(!Directory.Exists(ArchieveFilePath))
            Directory.CreateDirectory(ArchieveFilePath);
        ArchieveFilePath = ArchieveFilePath + "AssetmentDebug.log";
        if(File.Exists(ArchieveFilePath))
        {
            File.WriteAllText(ArchieveFilePath, string.Empty);
        }
    }
   
    public static void LogToFile(string format, params object[] args)
    {
        string timeString = DateTime.Now.ToString("HH:mm:ss:fff");
        File.AppendAllText(ArchieveFilePath,string.Format("[{0}]{1}\n",timeString, string.Format(format, args)));
    }
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void MyLog(object str, DebugColor color, params object[] args)
    {
        UnityEngine.Debug.LogFormat("{0}{1}</color>", GetColorStr(color), string.Format(str.ToString(), args));
    }
    public enum DebugColor
    {
        Default,
        Red,
        Green,
        Blue,
        Black,
        Grey
    }
    public static string GetColorStr(DebugColor color)
    {
        string colStr = "<color=#000000>";
        switch (color)
        {
            default:
            case DebugColor.Black:
                colStr = "<color=#000000>";
                break;
            case DebugColor.Blue:
                colStr = "<color=#0000ff>";
                break;
            case DebugColor.Green:
                colStr = "<color=#00ff00>";
                break;
            case DebugColor.Grey:
                colStr = "<color=#888888>";
                break;
            case DebugColor.Red:
                colStr = "<color=#ff000>";
                break;
        }

        return colStr;
    }

}
*/