using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace VoxelEngine
{
    public class JobCallbackManager : MonoBehaviour
    {
        private static JobCallbackManager instance;
        private Queue<CallbackData> callbackDataQueue = new Queue<CallbackData>();

        struct CallbackData
        {
            public JobHandle jobHandle;
            public Action callOnCompletion;
            public ICollection<IDisposable> disposeOnCancel;
            public Action callOnCancel;
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this) Destroy(this);
        }

        private void OnDestroy()
        {
            CancelCallBack();
        }

        private void CancelCallBack()
        {
            foreach (CallbackData callbackData in callbackDataQueue)
            {
                callbackData.jobHandle.Complete();
            }

            foreach (CallbackData callbackData in callbackDataQueue)
            {
                if (callbackData.disposeOnCancel != null)
                {
                    foreach (IDisposable disposable in callbackData.disposeOnCancel)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (ObjectDisposedException)
                        {
                            //ignore if already disposed
                        }
                    }
                }

                if (callbackData.callOnCancel != null)
                {
                    callbackData.callOnCancel();
                }
            }
        }

        private static void CreateInstanceIfNull()
        {
            if (instance == null)
            {
                GameObject obj = new GameObject
                {
                    name = nameof(JobCallbackManager) + " Instance (Do not destroy)"
                };
                instance = obj.AddComponent<JobCallbackManager>();
            }
        }

        public static void Register(JobHandle jobHandle, Action callOnCompletion, Action callOnCancel)
        {
            CreateInstanceIfNull();
            instance._Register(jobHandle, callOnCompletion, null, callOnCancel);
        }

        public static void Register(JobHandle jobHandle, Action callOnCompletion, ICollection<IDisposable> disposeOnCancel = null, Action callOnCancel = null)
        {
            CreateInstanceIfNull();
            instance._Register(jobHandle, callOnCompletion, disposeOnCancel, callOnCancel);
        }

        private void _Register(JobHandle jobHandle, Action callOnCompletion, ICollection<IDisposable> disposeOnCancel = null, Action callOnCancel = null)
        {
            var callbackData = new CallbackData
            {
                jobHandle = jobHandle,
                callOnCompletion = callOnCompletion,
                disposeOnCancel = disposeOnCancel,
                callOnCancel = callOnCancel
            };

            callbackDataQueue.Enqueue(callbackData);
        }

        private void LateUpdate()
        {
            int callBackDataCount = callbackDataQueue.Count;
            for(int i = 0; i < callBackDataCount; i++)
            {
                CallbackData callbackData = callbackDataQueue.Dequeue();
                if(callbackData.jobHandle.IsCompleted)
                {
                    callbackData.callOnCompletion();
                }
                else
                {
                    callbackDataQueue.Enqueue(callbackData);
                }
            }
        }
    }
}