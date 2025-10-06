
using System;
using UnityEngine;

namespace EventFramework
{
    public interface IEventProxy
    {
        void RegisterEvent(string eventName, Action action);

        void RegisterEvent<T>(string eventName, Action<T> action);

        void RegisterEvent<T1, T2>(string eventName, Action<T1, T2> action);

        void RegisterEvent<T1, T2, T3>(string eventName, Action<T1, T2, T3> action);


        void UnRegisterEvent(string eventName, Action action);

        void UnRegisterEvent<T>(string eventName, Action<T> action);

        void UnRegisterEvent<T1, T2>(string eventName, Action<T1, T2> action);

        void UnRegisterEvent<T1, T2, T3>(string eventName, Action<T1, T2, T3> action);



        void InVokeEvent(string eventName);
        void InVokeEvent<T>(string eventName, T data);
        void InVokeEvent<T1, T2>(string eventName, T1 data, T2 data2);
        void InVokeEvent<T1, T2, T3>(string eventName, T1 data, T2 data2, T3 data3);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class EventProxyAttribute : Attribute
    {
        public string proxyName { get; private set; }
        public EventProxyAttribute(string proxyName)
        {
            if (string.IsNullOrWhiteSpace(proxyName))
            {
                this.proxyName = null;
                Debug.LogError("Empty Field Name");
                return;
            }
            this.proxyName = proxyName;
        }
    }


}
