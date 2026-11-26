using UnityEngine;
using System.Collections.Generic;
using static RescuePrincess.Gameplay.CannonSlot;
using DG.Tweening;

namespace RescuePrincess.Gameplay
{
    /// <summary>
    /// Quản lý đối tượng level, bao gồm cannon slots, color boxes và princess controller
    /// </summary>
    public class LevelObject : MonoBehaviour
    {
        public List<CannonSlot> cannonSlots = new List<CannonSlot>();
        public List<ColorBox> colorBoxes = new List<ColorBox>();
        public static LevelObject Instance { get; private set; }

        public int levelIndex; // Tham số level index
        public ChainController chainController;
        public BoxCollider2D pathCollider;
        public PrincessController princessController;

        [Header("Phạm vi Cannon")]
        [SerializeField] private List<BodySegment> targetsInRange = new List<BodySegment>();

        [Header("Tự động Spawn Cannon Slots")]
        [SerializeField] public float spaceDistance = 1.5f; // Khoảng cách giữa các slot
        [SerializeField] public int slotCount = 3; // Số lượng slot trong một hàng
        [SerializeField] public Vector2 centerPosition = Vector2.zero; // Vị trí trung tâm của hàng

        [Header("Tutorial Components (Level 1)")]
        [SerializeField] private GameObject handObject; // Hand object cho tutorial
        [SerializeField] private GameObject txtTutorial; // Tutorial text
        [SerializeField] private bool isTutorialActive = false; // Trạng thái tutorial
        [SerializeField] private Sequence handAnimationSequence; // Dotween sequence cho hand animation

        /// <summary>
        /// Lấy danh sách các segment trong phạm vi cannon
        /// </summary>
        public IReadOnlyList<BodySegment> GetTargetsInRange()
        {
            // Chỉ trả về các segment còn active
            return targetsInRange.FindAll(seg => seg != null && seg.gameObject.activeInHierarchy);
        }

        void Awake()
        {
            //Init();
        }

        void OnDestroy()
        {
            // Cleanup tutorial khi object bị destroy
            if (handAnimationSequence != null && handAnimationSequence.IsActive())
            {
                handAnimationSequence.Kill();
                handAnimationSequence = null;
            }
        }

        /// <summary>
        /// Khởi tạo level object
        /// </summary>
        public void Init()
        {
            Instance = this;
            // Tối ưu: chỉ lấy ColorBox dưới node "Boxes"
            Transform boxesRoot = transform.Find("Boxes");
            colorBoxes.Clear();
            if (boxesRoot != null)
            {
                // Tự động add ColorBox component nếu chưa có
                foreach (Transform child in boxesRoot)
                {
                    if (child.GetComponent<ColorBox>() == null)
                    {
                        child.gameObject.AddComponent<ColorBox>();
                        child.GetComponent<ColorBox>().Init();
                    }
                    else
                        child.GetComponent<ColorBox>().Init();
                }
                colorBoxes.AddRange(boxesRoot.GetComponentsInChildren<ColorBox>(true));
            }
            else
            {
                Debug.Log("Không tìm thấy node 'Boxes' trong LevelObject!");
            }

            // Tự động tìm pathCollider từ child PathContainer
            Transform pathContainer = transform.Find("PathContainer");
            if (pathContainer != null)
            {
                pathCollider = pathContainer.GetComponent<BoxCollider2D>();
                if (pathCollider == null)
                    Debug.LogWarning("PathContainer không có BoxCollider2D!");
            }
            else
            {
                Debug.LogWarning("Không tìm thấy child 'PathContainer' trong LevelObject!");
            }

            // Tự động spawn và init PrincessController
            SpawnAndInitPrincess();

            // Setup lock state cho cannon slots sau khi đã spawn
            SetupCannonSlotsLockState();

            // Khởi tạo tutorial cho level 1
            InitTutorial();
        }

        /// <summary>
        /// Khởi tạo tutorial cho level 1
        /// </summary>
        private void InitTutorial()
        {
            // Chỉ khởi tạo tutorial cho level 1
            if (levelIndex != 1)
            {
                Debug.Log($"Level {levelIndex}: Không phải level 1, bỏ qua tutorial");
                return;
            }

            Debug.Log("Level 1: Khởi tạo tutorial...");

            // Tìm hand object và tutorial text
            FindTutorialObjects();

            // Bắt đầu tutorial nếu tìm thấy các object
            if (handObject != null && txtTutorial != null)
            {
                StartTutorial();
            }
            else
            {
                Debug.LogWarning("Level 1: Không tìm thấy hand object hoặc tutorial text!");
            }
        }

        /// <summary>
        /// Tìm các object tutorial trong scene
        /// </summary>
        private void FindTutorialObjects()
        {
            // Tìm hand object
            handObject = GameObject.Find("hand");
            if (handObject == null)
            {
                Debug.LogWarning("Level 1: Không tìm thấy object có tên 'hand'!");
            }
            else
            {
                Debug.Log("Level 1: Tìm thấy hand object");
            }

            // Tìm tutorial text
            txtTutorial = GameObject.Find("txtTutorial");
            if (txtTutorial == null)
            {
                Debug.LogWarning("Level 1: Không tìm thấy object có tên 'txtTutorial'!");
            }
            else
            {
                Debug.Log("Level 1: Tìm thấy tutorial text");
            }
        }

        /// <summary>
        /// Bắt đầu tutorial
        /// </summary>
        private void StartTutorial()
        {
            if (levelIndex != 1 || handObject == null || txtTutorial == null)
                return;

            isTutorialActive = true;
            
            // Hiển thị tutorial objects
            handObject.SetActive(true);
            txtTutorial.SetActive(true);

            // Tạo hand animation với Dotween
            CreateHandTapAnimation();

            Debug.Log("Level 1: Tutorial đã bắt đầu");
        }

        /// <summary>
        /// Tạo animation tap cho hand object sử dụng Dotween
        /// </summary>
        private void CreateHandTapAnimation()
        {
            if (handObject == null) return;

            // Dừng animation cũ nếu có
            if (handAnimationSequence != null && handAnimationSequence.IsActive())
            {
                handAnimationSequence.Kill();
            }

            // Tạo sequence animation tap
            handAnimationSequence = DOTween.Sequence();

            // Animation: scale down -> scale up (hiệu ứng tap)
            handAnimationSequence.Append(handObject.transform.DOScale(0.8f, 0.3f).SetEase(Ease.OutQuad))
                               .Append(handObject.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack))
                               .Append(handObject.transform.DOScale(1f, 0.1f).SetEase(Ease.OutQuad))
                               .SetLoops(-1, LoopType.Restart) // Lặp vô hạn
                               .SetDelay(0.2f); // Delay 1 giây giữa các lần tap

            Debug.Log("Level 1: Đã tạo hand tap animation");
        }

        /// <summary>
        /// Dừng tutorial và ẩn các object tutorial
        /// </summary>
        public void StopTutorial()
        {
            if (levelIndex != 1) return;

            isTutorialActive = false;

            // Dừng animation
            if (handAnimationSequence != null && handAnimationSequence.IsActive())
            {
                handAnimationSequence.Kill();
                handAnimationSequence = null;
            }

            // Ẩn tutorial objects
            if (handObject != null)
            {
                handObject.SetActive(false);
            }

            if (txtTutorial != null)
            {
                txtTutorial.SetActive(false);
            }

            Debug.Log("Level 1: Tutorial đã dừng");
        }

        /// <summary>
        /// Được gọi khi color box được tap (từ ColorBox script)
        /// </summary>
        public void OnColorBoxTapped()
        {
            if (levelIndex == 1 && isTutorialActive)
            {
                StopTutorial();
            }
        }

        /// <summary>
        /// Kiểm tra xem tutorial có đang active không
        /// </summary>
        public bool IsTutorialActive()
        {
            return levelIndex == 1 && isTutorialActive;
        }

        /// <summary>
        /// Reset tutorial về trạng thái ban đầu
        /// </summary>
        public void ResetTutorial()
        {
            if (levelIndex == 1)
            {
                StopTutorial();
                StartTutorial();
            }
        }

        /// <summary>
        /// Tự động spawn princess prefab và get PrincessController component
        /// </summary>
        private void SpawnAndInitPrincess()
        {
            // Kiểm tra xem đã có princess instance chưa
            if (princessController != null)
            {
                Debug.Log("PrincessController đã tồn tại!");
                return;
            }
            
            // Spawn princess prefab
            GameObject princessPrefab = LoadPrincessPrefabFromResources();
            if (princessPrefab == null)
            {
                Debug.LogError("Không tìm thấy princess prefab trong Resources!");
                return;
            }
            
            // Lấy random waypoint index và position
            int spawnWaypointIndex = GetRandomWaypointIndex();
            Vector3 spawnPosition = GetWaypointPosition(spawnWaypointIndex);
            
            // Spawn princess tại random waypoint
            GameObject princessInstance = Instantiate(princessPrefab, spawnPosition, Quaternion.identity);
            princessInstance.transform.SetParent(transform);
            
            // Get PrincessController component từ instance
            princessController = princessInstance.GetComponent<PrincessController>();
            if (princessController == null)
            {
                Debug.LogError("Princess prefab không có PrincessController component!");
                Destroy(princessInstance);
                return;
            }
            
            // Set princess instance và spawn waypoint index cho PrincessController
            princessController.SetPrincessInstance(princessInstance);
            princessController.SetSpawnWaypointIndex(spawnWaypointIndex);
            
            Debug.Log($"Đã spawn princess prefab tại waypoint {spawnWaypointIndex} và get PrincessController component: {princessInstance.name}");
        }
        
        /// <summary>
        /// Lấy random waypoint index từ 3-5
        /// </summary>
        private int GetRandomWaypointIndex()
        {
            // Tìm waypoints trong level
            List<Transform> waypoints = FindWaypointsInLevel();
            
            if (waypoints.Count == 0)
            {
                Debug.LogWarning("Không tìm thấy waypoints, trả về index 0");
                return 0;
            }
            
            // Chọn random waypoint index từ 3-5
            int minIndex = Mathf.Min(3, waypoints.Count - 1);
            int maxIndex = Mathf.Min(5, waypoints.Count - 1);
            
            int randomIndex = Random.Range(minIndex, maxIndex + 1);
            
            Debug.Log($"Chọn random waypoint index {randomIndex} cho princess spawn");
            return randomIndex;
        }
        
        /// <summary>
        /// Lấy vị trí của waypoint theo index
        /// </summary>
        private Vector3 GetWaypointPosition(int waypointIndex)
        {
            List<Transform> waypoints = FindWaypointsInLevel();
            
            if (waypoints.Count == 0 || waypointIndex >= waypoints.Count)
            {
                Debug.LogWarning($"Waypoint index {waypointIndex} không hợp lệ, trả về Vector3.zero");
                return Vector3.zero;
            }
            
            Vector3 position = waypoints[waypointIndex].position;
            Debug.Log($"Lấy vị trí waypoint {waypointIndex}: {position}");
            return position;
        }
        
        /// <summary>
        /// Tìm tất cả waypoints trong level
        /// </summary>
        private List<Transform> FindWaypointsInLevel()
        {
            List<Transform> waypoints = new List<Transform>();
            
            // Tìm waypoints trong level
            Transform waypointsRoot = null;
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
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
                //Debug.Log($"Tìm thấy {waypoints.Count} waypoints trong level");
            }
          
            
            return waypoints;
        }
        
        /// <summary>
        /// Load princess prefab từ Resources
        /// </summary>
        private GameObject LoadPrincessPrefabFromResources()
        {
            // Đường dẫn chính xác đến princess prefab
            string princessPath = "prefabs/games/Princess";
            
            GameObject prefab = Resources.Load<GameObject>(princessPath);
            if (prefab != null && prefab.GetComponent<PrincessController>() != null)
            {
                Debug.Log($"Tìm thấy princess prefab tại: {princessPath}");
                return prefab;
            }
            
            // Fallback: tìm trong các đường dẫn khác nếu không tìm thấy
            string[] fallbackPaths = {
                "prefabs/Princess",
                "prefabs/princess",
                "prefabs/games/princess"
            };

            foreach (string path in fallbackPaths)
            {
                prefab = Resources.Load<GameObject>(path);
                if (prefab != null && prefab.GetComponent<PrincessController>() != null)
                {
                    Debug.Log($"Tìm thấy princess prefab tại fallback path: {path}");
                    return prefab;
                }
            }

            Debug.LogError($"Không tìm thấy princess prefab tại {princessPath} hoặc các đường dẫn fallback! Vui lòng đặt princess prefab vào Resources/prefabs/games/Princess.prefab");
            return null;
        }

        /// <summary>
        /// Tự động spawn các Cannon Slot theo hàng ngang
        /// </summary>
        public void AutoSpawnCannonSlots()
        {
            Transform cannonRoot = GetCannonRootTransform();
            if (cannonRoot == null)
            {
                Debug.LogError("Không tìm thấy Cannon Root Transform (child 'Cannons')!");
                return;
            }

            // Xóa các slot hiện có
            ClearExistingCannonSlots();

            // Spawn các slot mới trong một hàng ngang
            for (int i = 0; i < slotCount; i++)
            {
                Vector2 position = CalculateSlotPosition(i);
                SpawnCannonSlot(position, i, cannonRoot);
            }

            // Sắp xếp các slot theo index
            cannonSlots.Sort((a, b) => ExtractCannonIndex(a.name).CompareTo(ExtractCannonIndex(b.name)));
            
            Debug.Log($"Tự động spawn {cannonSlots.Count} cannon slots trong một hàng ngang");
        }

        /// <summary>
        /// Tìm child object "Cannons" làm root transform
        /// </summary>
        private Transform GetCannonRootTransform()
        {
            // Tìm child object có tên "Cannons"
            Transform cannonRoot = transform.Find("Cannons");
            if (cannonRoot == null)
            {
                Debug.LogWarning("Không tìm thấy child object 'Cannons'. Đang tạo...");
                GameObject cannonsObj = new GameObject("Cannons");
                cannonRoot = cannonsObj.transform;
                cannonRoot.SetParent(transform);
                cannonRoot.localPosition = Vector3.zero;
            }
            return cannonRoot;
        }

        /// <summary>
        /// Tính toán vị trí của slot trong hàng ngang với centerPosition là điểm chính giữa
        /// </summary>
        private Vector2 CalculateSlotPosition(int index)
        {
            // Tính toán offset từ điểm chính giữa
            float totalWidth = (slotCount - 1) * spaceDistance;
            float startX = centerPosition.x - totalWidth / 2f;
            
            float x = startX + index * spaceDistance;
            float y = centerPosition.y;
            
            return new Vector2(x, y);
        }

        /// <summary>
        /// Spawn một Cannon Slot tại vị trí cụ thể
        /// </summary>
        private void SpawnCannonSlot(Vector2 position, int index, Transform cannonRoot)
        {
            // Load prefab từ Resources
            GameObject cannonPrefab = LoadCannonPrefabFromResources();
            if (cannonPrefab == null)
            {
                Debug.LogError("Không tìm thấy Cannon prefab trong Resources/prefabs/games/");
                return;
            }

            GameObject slotObject = Instantiate(cannonPrefab, cannonRoot);
            slotObject.name = $"CannonSlot{index}";
            slotObject.transform.localPosition = position;

            CannonSlot slot = slotObject.GetComponent<CannonSlot>();
            if (slot != null)
            {
                cannonSlots.Add(slot);
            }
            else
            {
                Debug.LogError($"Không tìm thấy CannonSlot component trên prefab!");
            }
        }

        /// <summary>
        /// Load cannon prefab từ Resources/prefabs/games/
        /// </summary>
        private GameObject LoadCannonPrefabFromResources()
        {
            // Tìm prefab trong thư mục Resources/prefabs/games/
            string[] possiblePaths = {
                "prefabs/games/Cannon",
                "prefabs/games/CannonSlot",
                "prefabs/games/cannon",
                "prefabs/games/cannonslot"
            };

            foreach (string path in possiblePaths)
            {
                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab != null && prefab.GetComponent<CannonSlot>() != null)
                {
                    return prefab;
                }
            }

            return null;
        }

        /// <summary>
        /// Xóa tất cả Cannon Slot hiện có
        /// </summary>
        public void ClearExistingCannonSlots()
        {
            // Xóa các slot GameObject hiện có
            foreach (var slot in cannonSlots)
            {
                if (slot != null && slot.gameObject != null)
                {
                    DestroyImmediate(slot.gameObject);
                }
            }
            cannonSlots.Clear();
        }

        /// <summary>
        /// Làm mới danh sách Cannon Slots từ scene
        /// </summary>
        public void RefreshCannonSlots()
        {
            cannonSlots.Clear();
            cannonSlots.AddRange(GetComponentsInChildren<CannonSlot>(true));
            cannonSlots.Sort((a, b) => ExtractCannonIndex(a.name).CompareTo(ExtractCannonIndex(b.name)));
        }

        /// <summary>
        /// Cập nhật cấu hình tự động spawn
        /// </summary>
        public void UpdateAutoSpawnConfig(int newSlotCount, float newSpaceDistance)
        {
            slotCount = newSlotCount;
            spaceDistance = newSpaceDistance;
            
            // Luôn luôn spawn lại khi cập nhật cấu hình
            AutoSpawnCannonSlots();
        }

        /// <summary>
        /// Spawn lại cannon slots (luôn luôn bật)
        /// </summary>
        public void RespawnCannonSlots()
        {
            AutoSpawnCannonSlots();
        }

        /// <summary>
        /// Được gọi khi có object vào phạm vi cannon
        /// </summary>
        public void OnCannonRangeEnter(Collider2D other)
        {
            var segment = other.GetComponent<BodySegment>();
            if (segment != null && segment.Type == BodySegment.SegmentType.BODY && !targetsInRange.Contains(segment))
            {
                targetsInRange.Add(segment);
            }
        }

        /// <summary>
        /// Được gọi khi có object rời khỏi phạm vi cannon
        /// </summary>
        public void OnCannonRangeExit(Collider2D other)
        {
            var segment = other.GetComponent<BodySegment>();
            if (segment != null && targetsInRange.Contains(segment))
            {
                targetsInRange.Remove(segment);
            }
        }

        /// <summary>
        /// Hàm hỗ trợ: lấy số thứ tự ở cuối tên (ví dụ: Cannon1 -> 1)
        /// </summary>
        private int ExtractCannonIndex(string name)
        {
            int idx = name.Length - 1;
            while (idx >= 0 && char.IsDigit(name[idx])) idx--;
            if (idx < name.Length - 1)
            {
                string number = name.Substring(idx + 1);
                if (int.TryParse(number, out int result))
                    return result;
            }
            return int.MaxValue; // Nếu không có số, cho xuống cuối
        }

        /// <summary>
        /// Lấy Cannon Slot theo index
        /// </summary>
        public CannonSlot GetCannonSlot(int index)
        {
            if (index >= 0 && index < cannonSlots.Count)
                return cannonSlots[index];
            return null;
        }

        /// <summary>
        /// Lấy Color Box theo index
        /// </summary>
        public ColorBox GetColorBox(int index)
        {
            if (index >= 0 && index < colorBoxes.Count)
                return colorBoxes[index];
            return null;
        }

        /// <summary>
        /// Lấy instance PrincessController
        /// </summary>
        public PrincessController GetPrincessController()
        {
            return princessController;
        }

        /// <summary>
        /// Setup lock state cho tất cả cannon slots dựa trên level hiện tại
        /// </summary>
        public void SetupCannonSlotsLockState()
        {
            int totalSlots = cannonSlots.Count;
            
            for (int i = 0; i < totalSlots; i++)
            {
                if (cannonSlots[i] != null)
                {
                    cannonSlots[i].SetupLockState(i, totalSlots);
                }
            }
            
            Debug.Log($"LevelObject: Đã setup lock state cho {totalSlots} cannon slots");
        }

        /// <summary>
        /// Lấy số lượng slots đã unlock
        /// </summary>
        public int GetUnlockedSlotsCount()
        {
            int count = 0;
            foreach (var slot in cannonSlots)
            {
                if (slot != null && slot.state != CannonSlotState.LOCK)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Lấy số lượng slots đã lock
        /// </summary>
        public int GetLockedSlotsCount()
        {
            int count = 0;
            foreach (var slot in cannonSlots)
            {
                if (slot != null && slot.state == CannonSlotState.LOCK)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Unlock tất cả slots (cho testing)
        /// </summary>
        [ContextMenu("Unlock All Cannon Slots")]
        public void UnlockAllCannonSlots()
        {
            foreach (var slot in cannonSlots)
            {
                if (slot != null)
                {
                    slot.SetState(CannonSlotState.EMPTY);
                }
            }
            Debug.Log("LevelObject: Đã unlock tất cả cannon slots");
        }

        /// <summary>
        /// Lock tất cả slots (cho testing)
        /// </summary>
        [ContextMenu("Lock All Cannon Slots")]
        public void LockAllCannonSlots()
        {
            foreach (var slot in cannonSlots)
            {
                if (slot != null)
                {
                    slot.SetState(CannonSlotState.LOCK);
                }
            }
            Debug.Log("LevelObject: Đã lock tất cả cannon slots");
        }

        /// <summary>
        /// Reset công chúa về trạng thái ban đầu
        /// </summary>
        public void ResetPrincess()
        {
            if (princessController != null)
            {
                princessController.ResetPrincess();
            }
        }

        /// <summary>
        /// Bắt đầu tutorial (public method để có thể gọi từ bên ngoài)
        /// </summary>
        public void StartTutorialPublic()
        {
            if (levelIndex == 1)
            {
                StartTutorial();
            }
        }

        /// <summary>
        /// Dừng tutorial (public method để có thể gọi từ bên ngoài)
        /// </summary>
        public void StopTutorialPublic()
        {
            if (levelIndex == 1)
            {
                StopTutorial();
            }
        }

        /// <summary>
        /// Lấy trạng thái tutorial
        /// </summary>
        public bool GetTutorialActive()
        {
            return isTutorialActive;
        }

        // Có thể mở rộng thêm các hàm khởi tạo, reset, load level...
    }
} 