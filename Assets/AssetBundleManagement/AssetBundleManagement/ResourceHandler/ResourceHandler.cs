using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    public enum ResourceHandlerType
    {
        Asset,
        Instance,
        Failed,
    }
    public interface IResourceHandler
    {
        UnityEngine.Object ResourceObject { get; } //返回的资源加载对象
        AssetInfo AssetKey { get; }//资源名
        GameObject AsGameObject { get; } //返回的资源加载对象转为GameObject对象，非GameObject类型为null
        
        ResourceHandlerType HandlerType { get; }
        bool                IsValid(); //当前资源是否可用，加载失败或者Handler被Release()情况下值为false
        void                Release(); //释放资源回资源池,释放后当前handler不再持有资源,IsValid()变为false，
        void                ForceRelease();//Release()，但是不打印日志
        //  int GetHandleId();
    }
  
}
