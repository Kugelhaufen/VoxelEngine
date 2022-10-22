using VoxelEngine;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelEngineDemo
{
    [RequireComponent(typeof(Camera))]
    public class PlayerController : MonoBehaviour
    {
        public VoxelObj rotateAround;

        //Shoot if Camera rotation canges less then this value
        public float ShootCameraRotationLimit = 1;
        public float rotationSensitivity = 180;
        public float distanceToObj = 15;
        public Vector3 cameraRotationCenterOffset;
        public float cameraYPostionLimit = 1;
        private float shootRayLengh = 1000;

        public delegate void ShootEventHandler(Vector3 target);
        public static event ShootEventHandler Shoot;
        public static PlayerController Instance { get; private set; }
        private Camera playerCamera;
        private WeaponManager weaponManager;

        private VoxelMap rotateAroundObjVoxelMap;
        private float minRotationRadius = 0;
        private Vector3 rotationCenter = new Vector3();

        private Vector3 myTransformRotation;
        private Vector3 currentTouchTotalRotationChange;

        private void Awake()
        {
            playerCamera = this.gameObject.GetComponent<Camera>();
            weaponManager = this.gameObject.GetComponent<WeaponManager>();
        }

        private void Start()
        {
            CheckIfNewVoxelMap();
        }

        private void CheckIfNewVoxelMap()
        {
            if (rotateAround == null) return;

            if (rotateAroundObjVoxelMap != rotateAround.VoxelMap)
            {
                rotateAroundObjVoxelMap = rotateAround.VoxelMap;
                CalculateRotationData();
            }
        }

        private void CalculateRotationData()
        {
            if (rotateAroundObjVoxelMap == null) return;

            int3 mapSize = rotateAroundObjVoxelMap.dimensions;

            minRotationRadius = Mathf.Max(mapSize.x, mapSize.y, mapSize.z) / 2 * VoxelObj.voxelSize;
            rotationCenter = new Vector3(mapSize.x * VoxelObj.voxelSize / 2, mapSize.y * VoxelObj.voxelSize / 2, mapSize.z * VoxelObj.voxelSize / 2);
            rotationCenter.x += cameraRotationCenterOffset.x;
            rotationCenter.y += cameraRotationCenterOffset.y;
            rotationCenter.z += cameraRotationCenterOffset.z;

            this.transform.LookAt(rotationCenter);
            this.transform.position = rotationCenter - (this.transform.forward * (minRotationRadius + distanceToObj));
            myTransformRotation = this.transform.rotation.eulerAngles;
        }


#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        private float lastFrameMousePosX;
        private float lastFrameMousePosY;
#endif

        private void Update()
        {
            CheckIfNewVoxelMap();

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            if (Input.GetMouseButtonDown(0))
            {
                lastFrameMousePosX = Input.mousePosition.x;
                lastFrameMousePosY = Input.mousePosition.y;
                currentTouchTotalRotationChange = new Vector3();
            }

            if (Input.GetMouseButton(0))
            {
                float addRotY = ((Input.mousePosition.x - lastFrameMousePosX) / Screen.width) * 90;
                float addRotX = -((Input.mousePosition.y - lastFrameMousePosY) / Screen.height) * 90;
                AddToRotaion(addRotX, addRotY);

                lastFrameMousePosX = Input.mousePosition.x;
                lastFrameMousePosY = Input.mousePosition.y;
            }

            if (Input.GetMouseButtonUp(0))
            {
                Vector2 lastScreenPos = new Vector2(lastFrameMousePosX, lastFrameMousePosY);
                CheckForShootTrigger(lastScreenPos);
            }

            int scrollWheel = (int)Input.mouseScrollDelta.y;
            if (scrollWheel != 0)
            {
                weaponManager.NextWeapon();
            }
#endif

#if UNITY_ANDROID || UNITY_IOS

            if (Input.touchCount > 0)
            {
                Touch touchOne = Input.GetTouch(0);

                if (touchOne.phase == TouchPhase.Began)
                {
                    currentTouchTotalRotationChange = new Vector3();
                }

                if (touchOne.phase == TouchPhase.Moved)
                {
                    float addRotY = touchOne.deltaPosition.x / Screen.width * rotationSensitivity;
                    float addRotX = -(touchOne.deltaPosition.y / Screen.height * rotationSensitivity);

                    AddToRotaion(addRotX, addRotY);
                }

                if (touchOne.phase == TouchPhase.Ended)
                {
                    CheckForShootTrigger(touchOne.position);
                }
            }
#endif
        }

        private void AddToRotaion(float addRotX, float addRotY)
        {
            currentTouchTotalRotationChange.y += addRotY;
            currentTouchTotalRotationChange.x += addRotX;

            myTransformRotation.y += addRotY;
            myTransformRotation.x += addRotX;
            this.transform.rotation = Quaternion.Euler(myTransformRotation);

            Vector3 newPos = rotationCenter - (this.transform.forward * (minRotationRadius + distanceToObj));

            if (newPos.y < cameraYPostionLimit)
            {
                myTransformRotation.x -= addRotX;
                this.transform.rotation = Quaternion.Euler(myTransformRotation);

                newPos = rotationCenter - (this.transform.forward * (minRotationRadius + distanceToObj));
            }
            this.transform.position = newPos;
        }

        private void CheckForShootTrigger(Vector2 lastInputScreenPos)
        {
            if (currentTouchTotalRotationChange.magnitude <= ShootCameraRotationLimit)
            {
                if (Shoot != null)
                {
                    Ray ray = playerCamera.ScreenPointToRay(lastInputScreenPos);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        Shoot.Invoke(hit.point);
                    }
                    else
                    {
                        Shoot.Invoke(this.transform.position + ray.direction * shootRayLengh);
                    }
                }
            }
        }
    }
}