using UnityEngine;

namespace AssetBundleManagement.ObjectPool
{
    /// <summary>
    /// 采用策略模式，采用不同策略支持对象池卸载
    /// </summary>
    public abstract class AbstractInstanceObjectPoolDestroyStrategy
    {
        public abstract InstanceObjectPoolDestroyStrategyType StrategyType { get;}  // 策略类型
        public InstanceObjectPool InstanceObjectPool { get; set; }  // 策略持有的对象池
        public bool InWaitDestroyList { get; set; }  //是否进了等待销毁队列
        public abstract bool CheckCanDestroy();  // 检查是否要销毁对象池
        public abstract void Destroy();  // 销毁对象池
        public abstract bool LoadedCheckCanInWaitDestroyList();  // 对象池是InstanceObjectPoolState.Loaded状态下检查是否要进等待销毁队列

        public bool CheckCanInWaitDestroyList()
        {
            if (InstanceObjectPool.PoolState == InstanceObjectPoolState.WaitAssetLoading)
                return false;
            
            // 加载失败了也不销毁，为了下一次加载时候直接返回结果，不用重新走一遍对象池
            if (InstanceObjectPool.PoolState == InstanceObjectPoolState.Failed)  
                return false;

            if (InstanceObjectPool.PoolState == InstanceObjectPoolState.Loaded)
            {
                return LoadedCheckCanInWaitDestroyList();
            }
                 
                    
            return false;
        }
    }

    public class PermanentObjectPoolDestroyStrategy : AbstractInstanceObjectPoolDestroyStrategy
    {
        public override InstanceObjectPoolDestroyStrategyType StrategyType
        {
            get { return InstanceObjectPoolDestroyStrategyType.Permanent; }
        }

        public override bool CheckCanDestroy()
        {
            return false;
        }

        public override void Destroy()
        {
            
        }

        public override bool LoadedCheckCanInWaitDestroyList()
        {
            return false;
        }
    }
    
    public class TimeDelayObjectPoolDestroyStrategy : AbstractInstanceObjectPoolDestroyStrategy
    {
        public override InstanceObjectPoolDestroyStrategyType StrategyType
        {
            get { return InstanceObjectPoolDestroyStrategyType.TimeDelay; }
        }

        public TimeDelayObjectPoolDestroyStrategy(float timeDelayS)
        {
            DestroyTimeDelayS = timeDelayS;
        }

        private float WaitDestroyStartTimeS;
        private float DestroyTimeDelayS;
        private bool IsSetWaitDestroyStartTimeS;


        public override bool LoadedCheckCanInWaitDestroyList()
        {
            if (InstanceObjectPool.RefCount == 0)
            {                
                if(!IsSetWaitDestroyStartTimeS)
                {
                    WaitDestroyStartTimeS = Time.realtimeSinceStartup;
                    IsSetWaitDestroyStartTimeS = true;
                }
                    
                return true;
            }
            
            if (InstanceObjectPool.RefCount != 0)
            {
                IsSetWaitDestroyStartTimeS = false;
            }

            return false;
        }

        public override bool CheckCanDestroy()
        {
            if (Time.realtimeSinceStartup - WaitDestroyStartTimeS > DestroyTimeDelayS)
            {
                return true;
            }

            return false;
        }

        public override void Destroy()
        {
            InstanceObjectPool.InstantDestroy();
            InstanceObjectPool.OnObjectPoolDestroyNotify(InstanceObjectPool);
        }
    }
}