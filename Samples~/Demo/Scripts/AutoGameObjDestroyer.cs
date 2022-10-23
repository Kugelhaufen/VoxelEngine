using UnityEngine;

namespace VoxelEngineDemo
{
    public class AutoGameObjDestroyer : MonoBehaviour
    {
        [SerializeField] private float destroyAfterSeconds = 5;

        private void Awake()
        {
            Invoke(nameof(SelfDestory), destroyAfterSeconds);
        }

        private void SelfDestory()
        {
            Destroy(this.gameObject);
        }
    }
}