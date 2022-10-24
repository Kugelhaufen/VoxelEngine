using System;
using UnityEngine;

namespace VoxelEngine.ConnectedComponent
{
    [AddComponentMenu("VoxelEngine/" + nameof(ConnectedComponentPhysics))]
    [RequireComponent(typeof(VoxelObj))]
    public class ConnectedComponentPhysics : MonoBehaviour
    {
        public delegate void CclCompleteEventHandler(VoxelObj[] newVoxelObjs);

        public ConnectedComponentExtractor extractor;
        public VoxelReplaceUpdater voxelReplaceUpdater;
        public bool onVoxelObjUpdate = true;
        public bool immediateConnectedComponentPhysics;
        public int minVoxelsForNewExtraction = 10;
        public bool newVoxelObjsPhysicsEnabled = true;

        public static event CclCompleteEventHandler anyCclNewVoxelObjsUpdateCompletion;

        public bool CclInProgress { get; private set; }
        private bool blockNextOnVoxelObjUpdate = false;
        private bool updateRequired;

        private VoxelObj myVoxelObj;

        private void Awake()
        {
            myVoxelObj = this.gameObject.GetComponent<VoxelObj>();
            myVoxelObj.UpdateStarted += OnVoxelObjUpdateStarted;
        }

        private void OnVoxelObjUpdateStarted(VoxelObj sender)
        {
            if (onVoxelObjUpdate == false)
            {
                return;
            }

            if (blockNextOnVoxelObjUpdate)
            {
                blockNextOnVoxelObjUpdate = false;
                return;
            }

            if (immediateConnectedComponentPhysics)
            {
                ConnectedComponentLabelingImmediate();
            }
            else
            {
                ConnectedComponentLabeling();
            }
        }

        public void ConnectedComponentLabeling(Action callBack = null)
        {
            InternalConnectedComponentLabeling(false, callBack);
        }

        public void ConnectedComponentLabelingImmediate(Action callBack = null)
        {
            InternalConnectedComponentLabeling(true, callBack);
        }

        public void ConnectedComponentFullVoxelObjUpdate(Action callBack = null)
        {
            Action<Action> forcedUpdate = (Action _callBack) =>
            {
                if(onVoxelObjUpdate) blockNextOnVoxelObjUpdate = true;
                myVoxelObj.FullVoxelObjUpdate(_callBack);
            };
            InternalConnectedComponentLabeling(false, callBack, forcedUpdate);
        }

        public void ConnectedComponentFullVoxelObjUpdateImmediate(Action callBack = null)
        {
            Action<Action> forcedUpdate = (Action _callBack) =>
            {
                if (onVoxelObjUpdate) blockNextOnVoxelObjUpdate = true;
                myVoxelObj.FullVoxelObjUpdateImmediate();
                _callBack?.Invoke();
            };
            InternalConnectedComponentLabeling(true, callBack, forcedUpdate);
        }

        /// <summary>
        /// </summary>
        /// <param name="immediate"></param>
        /// <param name="onCompletionWithNoChanges">Is called when Connected Component Labeling is done and no changes were made to the voxelobj. Takes a callback Action as argument</param>
        /// <param name="callBack"></param>
        private void InternalConnectedComponentLabeling(bool immediate, Action callBack = null, Action<Action> forcedUpdate = null)
        {
            if (CclInProgress)
            {
                updateRequired = true;
                return;
            }
            else CclInProgress = true;

            if(immediate)
            {
                extractor.ConnectedComponentExtractionImmediate(myVoxelObj, minVoxelsForNewExtraction, onCcExtractorComplete);
            }
            else
            {
                extractor.ConnectedComponentExtraction(myVoxelObj, minVoxelsForNewExtraction, onCcExtractorComplete);
            }

            void onCcExtractorComplete(VoxelObj originalVoxelObj, bool originalVoxelObjEdited, Action applyVoxelObjMapChanges, VoxelObj[] newVoxelObjs)
            {
                if (originalVoxelObjEdited)
                {
                    UpdateVoxelObjsAfterExtraction(applyVoxelObjMapChanges, newVoxelObjs, callBack, forcedUpdate);
                }
                else
                {
                    if(forcedUpdate == null)
                    {
                        callBack?.Invoke();
                    }
                    else
                    {
                        forcedUpdate(callBack);
                    }
                }

                if (updateRequired)
                {
                    CclInProgress = false;
                    updateRequired = false;
                    ConnectedComponentLabeling();
                }
                else
                {
                    CclInProgress = false;
                }
            }
        }

        private void UpdateVoxelObjsAfterExtraction(Action applyVoxelObjMapChanges, VoxelObj[] newVoxelObjs, Action callBack, Action<Action> overwrittenUpdateMethod = null)
        {
            bool preventinfintieCclLoop = onVoxelObjUpdate;
            if (preventinfintieCclLoop)
            {
                if (myVoxelObj.UpdatingVoxelObj == false)
                {
                    blockNextOnVoxelObjUpdate = true;
                }

                foreach (VoxelObj obj in newVoxelObjs)
                {
                    if (newVoxelObjsPhysicsEnabled)
                    {
                        obj.MyVoxelObjPhysicsManager.EnablePhysics();
                    }
                    else
                    {
                        obj.MyVoxelObjPhysicsManager.DisablePhysics();
                    }
                    
                    obj.GetComponent<ConnectedComponentPhysics>().blockNextOnVoxelObjUpdate = true;
                }
            }

            VoxelReplaceUpdateData voxelReplaceUpdateData = new VoxelReplaceUpdateData();

            if (overwrittenUpdateMethod == null)
            {
                voxelReplaceUpdater.SetDefaultReplaceDataValues(voxelReplaceUpdateData, myVoxelObj, newVoxelObjs);
            }
            else
            {
                voxelReplaceUpdater.SetDefaultReplaceDataValues(voxelReplaceUpdateData, newVoxelObjs);
                voxelReplaceUpdateData.ExistingVoxelObj = myVoxelObj;
                voxelReplaceUpdateData.ExistingVoxelObjUpdate = (Action myVoxelObjUpdateCallBack) =>
                {
                    applyVoxelObjMapChanges();
                    overwrittenUpdateMethod(myVoxelObjUpdateCallBack);
                };
            }

            voxelReplaceUpdater.ReplaceUpdate(myVoxelObj, applyVoxelObjMapChanges, newVoxelObjs, onUpdateCompletion);

            void onUpdateCompletion()
            {
                callBack?.Invoke();
                anyCclNewVoxelObjsUpdateCompletion?.Invoke(newVoxelObjs);
            }
        }
    }
}