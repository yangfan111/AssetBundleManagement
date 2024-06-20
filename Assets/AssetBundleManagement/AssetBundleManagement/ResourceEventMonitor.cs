

using System;

namespace AssetBundleManagement
{
    internal interface IResourceEventMonitor
    {
        void Register(ResourceEventType id, System.Action<ResourceEventArgs> handler);
        void UnRegister(ResourceEventType id, System.Action<ResourceEventArgs> handler);
        void RegisterDefault(System.Action<ResourceEventArgs> handler);
        void UnRegisterDefault(System.Action<ResourceEventArgs> handler);

    }
    internal class ResourceEventMonitor: IResourceEventMonitor
    {

        class EventItem
        {
            public event System.Action<ResourceEventArgs> ResourceEvent;
            public void Handle(ResourceEventArgs args)
            {
                if (ResourceEvent != null)
                    ResourceEvent(args);
            }
            public void Register(System.Action<ResourceEventArgs> newHandler)
            {
                ResourceEvent += newHandler;
            }
            public void UnRegister(System.Action<ResourceEventArgs> newHandler)
            {
                ResourceEvent -= newHandler;
            }


        }
        EventItem[] m_EventHandlers;
        EventItem m_DefaultHandler;
        public ResourceEventMonitor()
        {
            m_EventHandlers = new EventItem[(int)ResourceEventType.Length];
        }
        public void Register(ResourceEventType id, System.Action<ResourceEventArgs> handler)
        {
            if(id<ResourceEventType.Length)
            {
                if(m_EventHandlers[(int)id] == null)
                {
                    m_EventHandlers[(int)id] = new EventItem();
                }
                m_EventHandlers[(int)id].Register(handler);
            }
        }
        public void UnRegister(ResourceEventType id, System.Action<ResourceEventArgs> handler)
        {
            if (id < ResourceEventType.Length)
            {
                if (m_EventHandlers[(int)id] != null)
                {
                    m_EventHandlers[(int)id].UnRegister(handler);
                }
            }
        }
        public void FireEvent(ResourceEventArgs args)
        {
            if (m_EventHandlers[args.EventId] != null)
            {
                m_EventHandlers[args.EventId].Handle(args);
            }
            if(m_DefaultHandler != null)
                m_DefaultHandler.Handle(args);
            args.Free();


        }

        public void RegisterDefault(Action<ResourceEventArgs> handler)
        {
            if(m_DefaultHandler == null)
            {
                m_DefaultHandler = new EventItem();
            }
            m_DefaultHandler.Register(handler);
        }

        public void UnRegisterDefault(Action<ResourceEventArgs> handler)
        {
            if (m_DefaultHandler != null)
            {
                m_DefaultHandler.UnRegister(handler);
            }
        }
    }
}
