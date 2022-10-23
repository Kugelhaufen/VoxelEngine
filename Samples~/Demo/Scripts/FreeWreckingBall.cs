using VoxelEngine;
using VoxelEngine.ConnectedComponent;
using VoxelEngine.VoroniStandard;
using UnityEngine;

namespace VoxelEngineDemo
{
    [RequireComponent(typeof(Rigidbody))]
    public class FreeWreckingBall : MonoBehaviour
    {
        public int minDestrucionVoxelRadius = 3;
        public int minVoxelsForFracture = 200;
        public bool useRenderedVoxelCount = true;
        public float myVelocityMultipierAfterFracture = 0.8f;
        [SerializeField] public SphereVoroniFracturer.SphereFractureSettings sphereFractureSettings;

        private int fracturerRadius;
        private float fracturerSeedRadius;
        private Vector3 startVelocity;
        private Vector3 lastVelocity;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private bool fracturing = false;

        private Rigidbody myRigidbody;
        private readonly SphereVoroniFracturer sphereFracturer = new SphereVoroniFracturer();

        private void Awake()
        {
            myRigidbody = gameObject.GetComponent<Rigidbody>();
        }

        private void Start()
        {
            startVelocity = myRigidbody.velocity;
            fracturerRadius = sphereFractureSettings.fractureVoxelRadius;
            fracturerSeedRadius = sphereFractureSettings.seedSpawnWorldSpaceRadius;
        }

        private void LateUpdate()
        {
            if (fracturing == false)
            {
                lastVelocity = myRigidbody.velocity;
                lastPosition = this.transform.position;
                lastRotation = this.transform.rotation;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (fracturing) return;

            float remainingSpeedPrecentage = lastVelocity.magnitude / startVelocity.magnitude;
            int destructionVoxelRadius = (int)(fracturerRadius * remainingSpeedPrecentage);

            if (destructionVoxelRadius < 5) return;

            sphereFractureSettings.fractureVoxelRadius = destructionVoxelRadius;
            sphereFractureSettings.seedSpawnWorldSpaceRadius = fracturerSeedRadius * remainingSpeedPrecentage;

            VoxelObjHolder holder = collision.collider.GetComponentInParent<VoxelObjHolder>();
            if (holder == null) return;

            VoxelObj colliderVoxelObj = holder.MyVoxelObj;
            if (colliderVoxelObj == null) return;

            if (colliderVoxelObj.UpdatingVoxelObj)
            {
                var voxelCount = useRenderedVoxelCount ? colliderVoxelObj.GetCurrentlyRenderedVoxelCount() : colliderVoxelObj.GetMapVoxelCount();
                if (voxelCount < minVoxelsForFracture) return;
            }
            else
            {
                if (colliderVoxelObj.GetCurrentlyRenderedVoxelCount() < minVoxelsForFracture) return;
            }


            myRigidbody.isKinematic = true;
            this.transform.position = lastPosition;
            this.transform.rotation = lastRotation;
            fracturing = true;

            sphereFracturer.Fracture(colliderVoxelObj, collision.GetContact(0).point, sphereFractureSettings, OnFracturerCompleted);
        }

        private void OnFracturerCompleted(VoxelObj[] shrapnelObjs)
        {
            foreach (VoxelObj shrapnel in shrapnelObjs)
            {
                VoxelObjAmountManager.Instance.AddVoxelObjToManagement(shrapnel);
            }

            if (myRigidbody != null)
            {
                myRigidbody.isKinematic = false;
                myRigidbody.velocity = lastVelocity * myVelocityMultipierAfterFracture;
            }
            fracturing = false;
        }
    }
}