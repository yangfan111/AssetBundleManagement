using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetBundleManagement
{
    
    public class ResourceRecorder
    {
        public static ResourceRecorder instance = new ResourceRecorder();
        public static ResourceRecorder Get() { return instance; }

        private int m_CurrentFrameUnloadABCount;
        private int m_CurrentFrameLoadAssetRequest;

        internal void FrameReset()
        {
            m_CurrentFrameUnloadABCount = 0;
            m_CurrentFrameLoadAssetRequest = 0;
        }
        internal void IncUnloadABCount() { m_CurrentFrameUnloadABCount++; }
        internal void IncLoadAssetWaitRequestCount() { m_CurrentFrameLoadAssetRequest++; }

        public bool CanUnloadAB()
        {
            return m_CurrentFrameUnloadABCount < ResourceConfigure.OneFrameUnloadBundleLimit;
        }
        public bool CanLoadAssetWaitRequest()
        {
            return m_CurrentFrameLoadAssetRequest < ResourceConfigure.OneFrameLoadAssetLimit;
        }
    }

}
