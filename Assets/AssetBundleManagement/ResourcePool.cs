using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using UnityEditor;

namespace AssetBundleManagement
{
    
    public interface IResourcePoolObject
    {
        void Reset();
    }
    public class ResourcePoolContainer
    {
         static Dictionary<Type, Pool> poolDict = new Dictionary<Type, Pool>();
        internal static void Add(Type tp,Pool pool)
        {
            poolDict.Add(tp, pool);
        }
        public static void GetPoolInfos(StringBuilder sb)
        {
            sb.AppendLine("====>ResourcePoolContainer");
            foreach (var pool in poolDict)
            {
                sb.AppendFormat("[{0}]NewCount:{1},HoldCount:{2}\n", pool.Key, pool.Value.NewCount, pool.Value.HoldCount);
            }
            AssetLogger.LogToFile(sb.ToString());
        }
    }
    internal class ResourceObjectHolder<T> where T : IResourcePoolObject,new()
    {
        private static Pool poolCache;
      
        static ResourceObjectHolder()
        {
            if(poolCache == null)
            {
                poolCache = new Pool();
                ResourcePoolContainer.Add(typeof(T), poolCache);
            }
        }


        internal static T Allocate() 
        {
            return poolCache.Get<T>();
        }
        internal static void Free(T obj)
        {
            poolCache.Release(obj);
        }
    }

      class Pool
    {
          Stack<IResourcePoolObject> list = new Stack<IResourcePoolObject>(512);
        public int NewCount;
        public int HoldCount { get { return list.Count; } }
        public T Get<T>() where T : IResourcePoolObject,new()
        {
            if (list.Count == 0)
            {
                T obj = new T();
                ++NewCount;
                return obj;
            }
            return (T)list.Pop();
        }

        public void Release(IResourcePoolObject obj)
        {
            if(obj != null)
            {
                obj.Reset();
                list.Push(obj);

            }
        }
    }
}
