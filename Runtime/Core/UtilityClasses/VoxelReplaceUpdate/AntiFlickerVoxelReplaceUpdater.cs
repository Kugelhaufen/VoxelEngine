using System;
using UnityEngine;

namespace VoxelEngine
{
    [CreateAssetMenu(fileName = "Anti Flicker Voxel Replace Updater", menuName = "Voxel Engine/Core/Voxel Replace Updaters/Anti Flicker Voxel Replace Updater", order = 0)]
    public class AntiFlickerVoxelReplaceUpdater : VoxelReplaceUpdater
    {
        public Vector3 antiMeshOverlapOffset = new Vector3(0.005f, 0.005f, 0.005f);

        public override void ReplaceUpdate(VoxelObj existingVoxelObj, Action applyExistingVoxelObjChanges, VoxelObj[] newVoxelObjs, Action callOnCompletion = null)
        {
            VoxelReplaceUpdateData antiFlickerUpdateData = new VoxelReplaceUpdateData();
            SetDefaultReplaceDataValues(antiFlickerUpdateData, existingVoxelObj, newVoxelObjs);
            ReplaceUpdate(antiFlickerUpdateData, applyExistingVoxelObjChanges, callOnCompletion);
        }

        public override void ReplaceUpdate(VoxelReplaceUpdateData updateData, Action applyExistingVoxelObjChanges, Action callOnCompletion)
        {
            int amountCurrentlyUpdatingVoxelObjs = updateData.NewVoxelObjs.Length;
            bool[] replacedVoxelsWasKinematic = new bool[updateData.NewVoxelObjs.Length];

            if (updateData.NewVoxelObjs.Length > 0)
            {
                for (int i = 0; i != updateData.NewVoxelObjs.Length; i++)
                {
                    VoxelObj voxelObj = updateData.NewVoxelObjs[i];

                    //This line prevents Mesh overlap. Overlapping meshes with identical textures create flickering effects as well under certain lighting conditions
                    // The position is later going to be changed to normal again
                    voxelObj.GetVoxelObjHolder().transform.position += antiMeshOverlapOffset;
                    voxelObj.MyVoxelObjPhysicsManager.MyBoxColliderHolder.SetActive(false);

                    if (voxelObj.MyVoxelObjPhysicsManager.PhysicsEnabled)
                    {
                        bool kinematic = voxelObj.MyVoxelObjPhysicsManager.MyRigidBody.isKinematic;
                        replacedVoxelsWasKinematic[i] = kinematic;
                        if (kinematic == false)
                        {
                            voxelObj.MyVoxelObjPhysicsManager.ToggleRigidbodyKinematic(true);
                        }
                    }

                    updateData.NewVoxelObjUpdates[i](newVoxelObjUpdateCompletion);
                }
            }
            else
            {
                updateExistingVoxelObj();
            }

            void newVoxelObjUpdateCompletion()
            {
                amountCurrentlyUpdatingVoxelObjs -= 1;
                if (amountCurrentlyUpdatingVoxelObjs == 0)
                {
                    updateExistingVoxelObj();
                }
            }

            void updateExistingVoxelObj()
            {
                applyExistingVoxelObjChanges.Invoke();

                if (updateData.ExistingVoxelObj != null)
                {
                    if (updateData.ExistingVoxelObj.GetNextUpdateChunkBatchCunt() > 0)
                    {
                        updateData.ExistingVoxelObjUpdate(finishUpdate);
                    }
                    else
                    {
                        finishUpdate();
                    }
                }
                else
                {
                    finishUpdate();
                }

                void finishUpdate()
                {
                    for (int i = 0; i != updateData.NewVoxelObjs.Length; i++)
                    {
                        VoxelObj newVoxelObj = updateData.NewVoxelObjs[i];
                        if (newVoxelObj == null) continue;

                        //Resets the position that was changed in order to prevent mesh overlapping flicker effects
                        newVoxelObj.GetVoxelObjHolder().transform.position -= antiMeshOverlapOffset;
                        newVoxelObj.MyVoxelObjPhysicsManager.MyBoxColliderHolder.SetActive(true);

                        if (replacedVoxelsWasKinematic[i] == false) newVoxelObj.MyVoxelObjPhysicsManager.ToggleRigidbodyKinematic(false);
                    }

                    if (callOnCompletion != null)
                    {
                        callOnCompletion.Invoke();
                    }
                }
            }
        }
    }
}