using System;
using System.Collections;
using AssetBundleManagement;
using UnityEngine;

namespace Assets.AssetBundleManagement.Test
{
    
    public interface ICoRoutineManager
    {
        Coroutine StartCoRoutine(IEnumerator enumerator);
    }
    public class ClientGameController:MonoBehaviour, ICoRoutineManager
    {
        private ResourceInitTest resourceInitTest  = new ResourceInitTest();
        IEnumerator Start()
        {
            yield return resourceInitTest.TestInit(this);
        }

        private void Update()
        {
            AssetManagement.TestInstance.Update();
        }
        
        public Coroutine StartCoRoutine(IEnumerator enumerator)
        {
            return StartCoroutine(enumerator);
        }

    }
}