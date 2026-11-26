using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Spine.Unity;

namespace RescuePrincess.Gameplay
{
    /// <summary>
    /// Điều khiển công chúa với animation spine, bị rồng đuổi và di chuyển theo waypoints
    /// </summary>
    public class PrincessController : MonoBehaviour
    {
        [Header("Cấu hình Công chúa")]
        [SerializeField] private GameObject princessPrefab; // Có thể để null nếu được spawn từ LevelObject
        
        [Header("Cài đặt Di chuyển")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float spawnDelay = 2f;
        [SerializeField] private float finalOffsetDistance = 2f; // Khoảng cách offset khi đến waypoint cuối
        
        [Header("Tên Animation")]
        [SerializeField] private string runAnimationName = "walk";
        [SerializeField] private string cryAnimationName = "cry";
        [SerializeField] private string idleAnimationName = "idle";
        
        [Header("Cài đặt Waypoint")]
        [SerializeField] private int minSpawnWaypointIndex = 3;
        [SerializeField] private int maxSpawnWaypointIndex = 5;
        
        // Biến private
        private GameObject princessInstance;
        [SerializeField] private SkeletonAnimation princessAnimation;
        private List<Transform> waypoints = new List<Transform>();
        private int currentWaypointIndex = 0;
        private bool isMoving = false;
        private bool isCrying = false;
        private bool isMovingToFinalPosition = false; // Đang di chuyển đến vị trí cuối
        private bool hasPlayedHelpSound = false; // Đã play help sound chưa
        private Vector3 finalPosition; // Vị trí cuối cùng sau offset
        private ChainController dragonController;
        
        // Trạng thái animation
        private enum PrincessState
        {
            IDLE,       // Đứng yên
            RUNNING,    // Chạy
            CRYING      // Khóc
        }
        
        private PrincessState currentState = PrincessState.IDLE;
        
        void Start()
        {
            InitializePrincess();
        }
        
        void Update()
        {
            if (isMoving && princessInstance != null)
            {
                UpdatePrincessMovement();
            }
        }
        
        /// <summary>
        /// Khởi tạo công chúa và bắt đầu chuỗi đuổi bắt
        /// </summary>
        private void InitializePrincess()
        {
            // Tìm waypoints từ LevelObject
            FindWaypoints();
            
            // Tìm dragon controller
            FindDragonController();
            
            // Kiểm tra xem princess đã được spawn từ LevelObject chưa
            if (princessInstance != null)
            {
                // Princess đã được spawn, chỉ cần bắt đầu di chuyển
                Debug.Log("Princess đã được spawn từ LevelObject, bắt đầu di chuyển");
                StartPrincessMovement();
            }
            else
            {
                // Princess chưa được spawn, tự spawn (fallback)
                Debug.LogWarning("Princess chưa được spawn từ LevelObject, tự spawn fallback");
                ValidatePrincessPrefab();
                StartCoroutine(SpawnPrincessSequence());
            }
        }
        
        /// <summary>
        /// Kiểm tra princess prefab có hợp lệ không
        /// </summary>
        private void ValidatePrincessPrefab()
        {
            if (princessPrefab == null)
            {
                Debug.LogError("Princess prefab chưa được gán! Vui lòng kéo princess prefab vào field 'princessPrefab' trong Inspector.");
                return;
            }
            
            // Kiểm tra xem prefab có SkeletonAnimation không
            var skeletonAnimation = princessPrefab.GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
            {
                skeletonAnimation = princessPrefab.GetComponentInChildren<SkeletonAnimation>();
            }
            
            if (skeletonAnimation == null)
            {
                Debug.LogError($"Princess prefab '{princessPrefab.name}' không có SkeletonAnimation component! Vui lòng thêm SkeletonAnimation vào prefab.");
            }
            else
            {
                Debug.Log($"Princess prefab '{princessPrefab.name}' hợp lệ - có SkeletonAnimation: {skeletonAnimation.name}");
            }
        }
        
        /// <summary>
        /// Tìm waypoints từ level hiện tại
        /// </summary>
        private void FindWaypoints()
        {
            waypoints.Clear();
            
            if (LevelObject.Instance != null)
            {
                // Tìm waypoints trong level
                Transform waypointsRoot = null;
                foreach (Transform t in LevelObject.Instance.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Waypoints")
                    {
                        waypointsRoot = t;
                        break;
                    }
                }
                
                if (waypointsRoot != null)
                {
                    foreach (Transform child in waypointsRoot)
                    {
                        waypoints.Add(child);
                    }
                    Debug.Log($"Tìm thấy {waypoints.Count} waypoints cho công chúa");
                }
                else
                {
                    Debug.LogWarning("Không tìm thấy waypoints trong LevelObject!");
                }
            }
            else
            {
                Debug.LogWarning("LevelObject.Instance là null!");
            }
        }
        
        /// <summary>
        /// Tìm dragon controller trong scene
        /// </summary>
        private void FindDragonController()
        {
            dragonController = FindObjectOfType<ChainController>();
            if (dragonController == null)
            {
                Debug.LogWarning("Không tìm thấy ChainController (rồng) trong scene!");
            }
        }
        
        /// <summary>
        /// Chuỗi spawn công chúa với delay
        /// </summary>
        private IEnumerator SpawnPrincessSequence()
        {
            yield return new WaitForSeconds(spawnDelay);
            
            // Chỉ số waypoint spawn ngẫu nhiên giữa min và max
            int spawnIndex = Random.Range(minSpawnWaypointIndex, maxSpawnWaypointIndex + 1);
            
            // Đảm bảo spawn index nằm trong phạm vi waypoints
            if (spawnIndex >= waypoints.Count)
            {
                spawnIndex = waypoints.Count - 1;
            }
            
            SpawnPrincess(spawnIndex);
        }
        
        /// <summary>
        /// Spawn công chúa tại chỉ số waypoint được chỉ định (fallback method)
        /// </summary>
        private void SpawnPrincess(int waypointIndex)
        {
            if (waypoints.Count == 0 || waypointIndex >= waypoints.Count)
            {
                Debug.LogError("Chỉ số waypoint không hợp lệ cho việc spawn công chúa!");
                return;
            }
            
            Vector3 spawnPosition = waypoints[waypointIndex].position;
            
            Debug.Log($"Bắt đầu spawn princess fallback tại waypoint {waypointIndex}, vị trí: {spawnPosition}");
            
            // Tạo instance công chúa từ prefab (chỉ khi chưa có)
            if (princessInstance == null && princessPrefab != null)
            {
                Debug.Log($"Đang spawn princess từ prefab: {princessPrefab.name}");
                princessInstance = Instantiate(princessPrefab, spawnPosition, Quaternion.identity);
                Debug.Log($"Đã tạo princess instance: {princessInstance.name}");
                
                // Thiết lập các component của công chúa
                SetupPrincessComponents();
            }
            else if (princessInstance != null)
            {
                // Di chuyển princess instance hiện có đến vị trí spawn
                princessInstance.transform.position = spawnPosition;
                Debug.Log($"Di chuyển princess instance hiện có đến waypoint {waypointIndex}");
            }
            else
            {
                Debug.LogError("Cần có princess prefab! Vui lòng gán princess prefab vào field princessPrefab trong Inspector hoặc đặt prefab trong Resources.");
                return;
            }
            
            // Đặt chỉ số waypoint hiện tại (spawn waypoint)
            currentWaypointIndex = waypointIndex;
            
            // Bắt đầu di chuyển đến waypoint cuối
            StartPrincessMovement();
            
            Debug.Log($"Hoàn thành spawn princess fallback tại waypoint {waypointIndex} tại vị trí {spawnPosition}");
            Debug.Log($"Princess sẽ di chuyển từ waypoint {waypointIndex} đến waypoint cuối ({waypoints.Count - 1})");
        }
        
        /// <summary>
        /// Thiết lập các component và tham chiếu của công chúa
        /// </summary>
        private void SetupPrincessComponents()
        {
            if (princessInstance == null) 
            {
                Debug.LogError("Princess instance là null!");
                return;
            }
            
            Debug.Log($"Đang thiết lập components cho princess: {princessInstance.name}");
            
            // Lấy component SkeletonAnimation từ công chúa hoặc con của nó
            princessAnimation = princessInstance.GetComponent<SkeletonAnimation>();
            if (princessAnimation == null)
            {
                Debug.Log("Không tìm thấy SkeletonAnimation trên princess, tìm trong children...");
                princessAnimation = princessInstance.GetComponentInChildren<SkeletonAnimation>();
            }
            
            if (princessAnimation == null)
            {
                Debug.LogError("Không tìm thấy component SkeletonAnimation trong princess prefab hoặc con của nó!");
                Debug.LogError($"Princess prefab cần có SkeletonAnimation component hoặc child object có SkeletonAnimation!");
                return;
            }
            
            Debug.Log($"Tìm thấy SkeletonAnimation: {princessAnimation.name}");
            
            // Kiểm tra và đặt animation ban đầu
            if (princessAnimation.AnimationState != null)
            {
                princessAnimation.AnimationState.SetAnimation(0, idleAnimationName, true);
                Debug.Log($"Đã đặt animation ban đầu: {idleAnimationName}");
            }
            else
            {
                Debug.LogError("AnimationState của SkeletonAnimation là null!");
            }
            
            // Kiểm tra các component khác nếu cần
            var collider = princessInstance.GetComponent<Collider2D>();
            if (collider == null)
            {
                Debug.LogWarning("Princess không có Collider2D - có thể cần thêm để tương tác!");
            }
        }
        
        /// <summary>
        /// Bắt đầu di chuyển công chúa đến waypoint cuối
        /// </summary>
        private void StartPrincessMovement()
        {
            if (waypoints.Count == 0) 
            {
                Debug.LogWarning("Không có waypoints để di chuyển!");
                return;
            }
            
            isMoving = true;
            currentState = PrincessState.RUNNING;
            
            // Phát animation chạy
            if (princessAnimation != null)
            {
                princessAnimation.AnimationState.SetAnimation(0, runAnimationName, true);
            }
            
            Debug.Log($"Công chúa bắt đầu di chuyển từ waypoint {currentWaypointIndex} đến waypoint cuối ({waypoints.Count - 1})");
        }
        
        /// <summary>
        /// Cập nhật di chuyển công chúa mỗi frame
        /// </summary>
        private void UpdatePrincessMovement()
        {
            // Nếu đang di chuyển đến vị trí cuối
            if (isMovingToFinalPosition)
            {
                UpdateFinalPositionMovement();
                return;
            }
            
            if (currentWaypointIndex >= waypoints.Count - 1)
            {
                // Đã đến waypoint cuối, bắt đầu di chuyển đến vị trí cuối
                StartMovingToFinalPosition();
                return;
            }
            
            // Lấy waypoint mục tiêu hiện tại
            Transform targetWaypoint = waypoints[currentWaypointIndex + 1];
            Vector3 targetPosition = targetWaypoint.position;
            
            // Di chuyển đến mục tiêu
            Vector3 direction = (targetPosition - princessInstance.transform.position).normalized;
            princessInstance.transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Flip horizontal theo hướng di chuyển (không rotate như dragon)
            UpdatePrincessFacing(direction);
            
            // Kiểm tra nếu đã đến waypoint hiện tại
            float distanceToTarget = Vector3.Distance(princessInstance.transform.position, targetPosition);
            if (distanceToTarget < 0.1f)
            {
                currentWaypointIndex++;
                
                // Kiểm tra nếu rồng gần và bắt đầu khóc
                CheckDragonProximity();
            }
        }
        
        /// <summary>
        /// Bắt đầu di chuyển đến vị trí cuối (sau offset)
        /// </summary>
        private void StartMovingToFinalPosition()
        {
            // Tính hướng di chuyển từ 2 waypoints cuối cùng
            Vector3 finalDirection = CalculateFinalDirection();
            
            // Tính vị trí cuối cùng sau offset
            finalPosition = waypoints[waypoints.Count - 1].position + (finalDirection * finalOffsetDistance);
            
            isMovingToFinalPosition = true;
            
            Debug.Log($"Bắt đầu di chuyển đến vị trí cuối: {finalPosition}, hướng: {finalDirection}");
        }
        
        /// <summary>
        /// Tính hướng di chuyển từ 2 waypoints cuối cùng
        /// </summary>
        private Vector3 CalculateFinalDirection()
        {
            if (waypoints.Count < 2)
            {
                // Nếu chỉ có 1 waypoint, sử dụng hướng mặc định
                return Vector3.right;
            }
            
            // Lấy 2 waypoints cuối cùng
            Vector3 lastWaypoint = waypoints[waypoints.Count - 1].position;
            Vector3 secondLastWaypoint = waypoints[waypoints.Count - 2].position;
            
            // Tính hướng từ waypoint thứ 2 cuối đến waypoint cuối
            Vector3 direction = (lastWaypoint - secondLastWaypoint).normalized;
            
            Debug.Log($"Hướng di chuyển cuối: từ {secondLastWaypoint} đến {lastWaypoint} = {direction}");
            return direction;
        }
        
        /// <summary>
        /// Cập nhật di chuyển đến vị trí cuối
        /// </summary>
        private void UpdateFinalPositionMovement()
        {
            // Di chuyển đến vị trí cuối
            Vector3 direction = (finalPosition - princessInstance.transform.position).normalized;
            princessInstance.transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Flip horizontal theo hướng di chuyển
            UpdatePrincessFacing(direction);
            
            // Kiểm tra nếu đã đến vị trí cuối
            float distanceToFinal = Vector3.Distance(princessInstance.transform.position, finalPosition);
            if (distanceToFinal < 0.1f)
            {
                // Đã đến vị trí cuối, dừng và bắt đầu khóc
                OnReachedFinalPosition();
            }
        }
        
        /// <summary>
        /// Cập nhật hướng nhìn của princess (flip horizontal)
        /// </summary>
        private void UpdatePrincessFacing(Vector3 direction)
        {
            if (direction == Vector3.zero || princessAnimation == null) return;
            
            // Lấy scale hiện tại của princessAnimation object
            Vector3 currentScale = princessAnimation.transform.localScale;
            
            // Flip horizontal dựa trên hướng di chuyển
            if (direction.x > 0)
            {
                // Di chuyển sang phải - flip sang phải
                currentScale.x = Mathf.Abs(currentScale.x);
            }
            else if (direction.x < 0)
            {
                // Di chuyển sang trái - flip sang trái
                currentScale.x = -Mathf.Abs(currentScale.x);
            }
            
            // Áp dụng scale mới cho princessAnimation object
            princessAnimation.transform.localScale = currentScale;
        }
        
        /// <summary>
        /// Kiểm tra nếu rồng gần công chúa và kích hoạt khóc
        /// </summary>
        private void CheckDragonProximity()
        {
            if (dragonController == null || princessInstance == null) return;
            
            // Lấy vị trí đầu rồng
            Transform dragonHead = dragonController.Head;
            if (dragonHead == null) return;
            
            float distanceToDragon = Vector3.Distance(princessInstance.transform.position, dragonHead.position);
            
            // Cập nhật hướng nhìn về phía rồng
            //UpdatePrincessFacingTowardsDragon(dragonHead.position);
            
            // Nếu rồng gần (trong vòng 3 đơn vị), bắt đầu khóc
            if (distanceToDragon < 3f && !isCrying)
            {
                StartCrying();
            }
            else if (distanceToDragon >= 3f && isCrying)
            {
                StopCrying();
            }
        }
        
        /// <summary>
        /// Cập nhật hướng nhìn của princess về phía rồng
        /// </summary>
        private void UpdatePrincessFacingTowardsDragon(Vector3 dragonPosition)
        {
            if (princessInstance == null || princessAnimation == null) return;
            
            // Tính hướng từ princess đến rồng
            Vector3 directionToDragon = (dragonPosition - princessInstance.transform.position).normalized;
            
            // Lấy scale hiện tại của princessAnimation object
            Vector3 currentScale = princessAnimation.transform.localScale;
            
            // Flip horizontal để nhìn về phía rồng
            if (directionToDragon.x > 0)
            {
                // Rồng ở bên phải - flip sang phải
                currentScale.x = Mathf.Abs(currentScale.x);
            }
            else if (directionToDragon.x < 0)
            {
                // Rồng ở bên trái - flip sang trái
                currentScale.x = -Mathf.Abs(currentScale.x);
            }
            
            // Áp dụng scale mới cho princessAnimation object
            princessAnimation.transform.localScale = currentScale;
        }
        
        /// <summary>
        /// Bắt đầu animation khóc
        /// </summary>
        private void StartCrying()
        {
            if (isCrying) return;
            
            isCrying = true;
            currentState = PrincessState.CRYING;
            
            if (princessAnimation != null)
            {
                princessAnimation.AnimationState.SetAnimation(0, cryAnimationName, true);
            }
            
            Debug.Log("Công chúa bắt đầu khóc - rồng đang gần!");
        }
        
        /// <summary>
        /// Dừng khóc và trở về trạng thái chạy
        /// </summary>
        private void StopCrying()
        {
            if (!isCrying) return;
            
            isCrying = false;
            currentState = PrincessState.RUNNING;
            
            if (princessAnimation != null)
            {
                princessAnimation.AnimationState.SetAnimation(0, runAnimationName, true);
            }
            
            Debug.Log("Công chúa dừng khóc - rồng đã di chuyển xa");
        }
        
        /// <summary>
        /// Được gọi khi công chúa đến waypoint cuối
        /// </summary>
        private void OnReachedFinalWaypoint()
        {
            Debug.Log("Công chúa đã đến waypoint cuối, chuẩn bị di chuyển đến vị trí cuối!");
            // Method này không còn được sử dụng, thay vào đó sử dụng StartMovingToFinalPosition()
        }
        
        /// <summary>
        /// Được gọi khi công chúa đến vị trí cuối cùng (sau offset)
        /// </summary>
        private void OnReachedFinalPosition()
        {
            isMoving = false;
            isMovingToFinalPosition = false;
            currentState = PrincessState.CRYING;
            
            // Bắt đầu khóc loop
            if (princessAnimation != null)
            {
                princessAnimation.AnimationState.SetAnimation(0, cryAnimationName, true);
            }
            
           // Debug.Log($"Công chúa đã đến vị trí cuối cùng: {finalPosition} - bắt đầu khóc loop!");
            
            // Play help sound 3 lần
            if (!hasPlayedHelpSound)
            {
                StartCoroutine(PlayHelpSoundSequence());
                hasPlayedHelpSound = true;
            }
        }
        
        
        /// <summary>
        /// Kiểm tra nếu công chúa đang di chuyển
        /// </summary>
        public bool IsMoving()
        {
            return isMoving;
        }
        
        /// <summary>
        /// Kiểm tra nếu công chúa đang khóc
        /// </summary>
        public bool IsCrying()
        {
            return isCrying;
        }
        
        /// <summary>
        /// Lấy instance công chúa
        /// </summary>
        public GameObject GetPrincessInstance()
        {
            return princessInstance;
        }
        
        /// <summary>
        /// Set princess instance (được gọi từ LevelObject)
        /// </summary>
        public void SetPrincessInstance(GameObject instance)
        {
            princessInstance = instance;
            Debug.Log($"Đã set princess instance: {instance?.name}");
            
            // Tự động setup components khi được set từ LevelObject
            SetupPrincessComponents();
        }
        
        /// <summary>
        /// Set spawn waypoint index (được gọi từ LevelObject)
        /// </summary>
        public void SetSpawnWaypointIndex(int spawnIndex)
        {
            currentWaypointIndex = spawnIndex;
            Debug.Log($"Đã set spawn waypoint index: {spawnIndex}");
            
            // Tự động bắt đầu di chuyển từ spawn waypoint đến waypoint cuối
            StartPrincessMovement();
        }
        
        /// <summary>
        /// Play help sound 3 lần, mỗi lần cách nhau 5 giây
        /// </summary>
        private IEnumerator PlayHelpSoundSequence()
        {
            Debug.Log("Bắt đầu play help sound sequence");
            
            for (int i = 0; i < 3; i++)
            {
                // Play help sound
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.helpSound, 1f);
                    Debug.Log($"Play help sound lần {i + 1}/3");
                }
                else
                {
                    Debug.LogWarning("Help sound hoặc SoundManager không có sẵn!");
                }
                
                // Đợi 5 giây trước khi play lần tiếp theo
                if (i < 2) // Không đợi sau lần cuối
                {
                    yield return new WaitForSeconds(5f);
                }
            }
            
            Debug.Log("Hoàn thành play help sound sequence");
        }
        
        /// <summary>
        /// Reset công chúa về trạng thái ban đầu
        /// </summary>
        public void ResetPrincess()
        {
            if (princessInstance != null)
            {
                Destroy(princessInstance);
                princessInstance = null;
            }
            
            isMoving = false;
            isCrying = false;
            isMovingToFinalPosition = false;
            hasPlayedHelpSound = false;
            currentState = PrincessState.IDLE;
            currentWaypointIndex = 0;
            finalPosition = Vector3.zero;
            
            // Khởi động lại chuỗi spawn
            StartCoroutine(SpawnPrincessSequence());
        }
        
        void OnDestroy()
        {
            // Dọn dẹp
            if (princessInstance != null)
            {
                Destroy(princessInstance);
            }
        }
    }
} 