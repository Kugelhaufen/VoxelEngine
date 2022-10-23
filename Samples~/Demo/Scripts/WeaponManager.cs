using UnityEngine;

namespace VoxelEngineDemo
{
    public class WeaponManager : MonoBehaviour
    {
        public int startWeapon = 0;
        public int ActiveWeaponIndex { get; set; }
        public GameObject[] weaponPrefabs;

        private GameObject activeWeapon;

        private void Start()
        {
            ActivateWeapon(startWeapon);
        }

        public void NextWeapon()
        {
            int newWeapon = ActiveWeaponIndex + 1;
            if (newWeapon >= weaponPrefabs.Length)
            {
                newWeapon = 0;
            }
            ActivateWeapon(newWeapon);
        }

        public void ActivateWeapon(int weaponIndex)
        {
            if (weaponIndex >= weaponPrefabs.Length)
            {
                Debug.LogError("weaponIndex is >= weaponGameObjs.Length");
                return;
            }

            ActiveWeaponIndex = weaponIndex;

            if (activeWeapon != null) Destroy(activeWeapon);
            activeWeapon = Instantiate(weaponPrefabs[weaponIndex], this.transform);
        }
    }
}