using Core.Utils;
using UnityEngine;

namespace AssetBundleManagement
{
    public class PersistentHandle : IBundleObserver
    {
        private static LoggerAdapter _loggerAdapter = new LoggerAdapter("PersistentHandle");
        public bool IsDone { get; private set; }
        public void OnBundleLoaded(AssetBundle bundle)
        {
            _loggerAdapter.InfoFormat("persistent bundle {0} loaded ");
            IsDone = true;
        }
    }
}