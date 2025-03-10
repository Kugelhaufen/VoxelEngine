using UnityEngine;

namespace VoxelEngineDemo
{
    public class StandardWeapon : MonoBehaviour
    {
        [SerializeField] private GameObject projectilePrefab = default;
        public Vector3 projectileSpawnPosOffset = default;
        public Vector3 projectileRotationOffset = default;
        public float projectileSpeed;

        private void Start()
        {
            PlayerController.Shoot += OnPlayerControllerShoot;
        }

        private void OnPlayerControllerShoot(Vector3 target)
        {
            Vector3 rotation = this.transform.rotation.eulerAngles + projectileRotationOffset;
            Vector3 position = this.transform.position + projectileSpawnPosOffset;

            GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.Euler(rotation));

            Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();
            if (projectileRigidbody != null)
            {
                Vector3 velocity = Vector3.Normalize(target - projectile.transform.position) * projectileSpeed;
#if UNITY_6000_0_OR_NEWER
                projectileRigidbody.linearVelocity = velocity;
#else
                projectileRigidbody.velocity = velocity;
#endif
            }
            else Debug.LogWarning("StandardWeapon cant apply speed to projectile because the projectile does not have a rigidbody");
        }

        private void OnDestroy()
        {
            PlayerController.Shoot -= OnPlayerControllerShoot;
        }
    }
}