
using System;
using AssetBundleManagement;

public delegate void OnLoadAssetSuccess(AsyncAssetOperationHandle assetAccessor, object context);

public delegate void OnLoadAssetFailure(string errorMessage, object context);

public struct AssetLoadOption
{
    //callbacks
    public OnLoadAssetSuccess OnSuccess;

    public OnLoadAssetFailure OnFailure;

    public LoadPriority Priority;

    public System.Object Context;
    
    public AssetLoadOption(OnLoadAssetSuccess onSuccess,LoadPriority priority,Object context = null)
    {
        OnSuccess = onSuccess;
        OnFailure = null;
        Priority  = priority;
        Context   = context;
    }
    public AssetLoadOption(OnLoadAssetSuccess onSuccess, OnLoadAssetFailure onFailure, LoadPriority priority, object context)
    {
        OnSuccess = onSuccess;
        OnFailure = onFailure;
        Priority  = priority;
        Context   = context;
    }
}