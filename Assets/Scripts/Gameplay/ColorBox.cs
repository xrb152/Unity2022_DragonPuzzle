using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using RescuePrincess.Managers;
using RescuePrincess.Utils;
using Lofelt.NiceVibrations;

namespace RescuePrincess.Gameplay
{
    public enum Direction8
    {
        Left,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft
    }

    public class ColorBox : MonoBehaviour
    {
        [HideInInspector] public SpriteRenderer shape;
        [HideInInspector] public Transform arrow;
        [HideInInspector] public Transform positivePoint;
        [HideInInspector] public Transform negativePoint;
        [HideInInspector] public Collider2D selectionCollider;
        [HideInInspector] public GameObject boxTrail;

        Color boxColor = Color.white;
        [HideInInspector] public float moveSpeed = 5f;
        [HideInInspector] public float pathDuration = 2f;
        public Direction8 direction = Direction8.Right;

        [Header("Color Index")]
        [SerializeField] private int colorIndex = 0;
        [SerializeField] private int ammoCount = 1;
        [SerializeField] private bool isBoxGroup = false;
        [SerializeField] private List<ColorBox> childColorBoxes = new List<ColorBox>();

        private Tween moveTween;
        private Vector3? finalTarget = null;
        private CannonSlot finalCannonSlot = null;
        private Tween rotationTween;
        private Tween shakeTween;

        // Thêm biến để lưu vị trí ban đầu
        private Vector3 originalPosition;
        private bool isLeaving = false;
        private BoardUI boardUI;

        void Awake()
        {
            //Init();
        }

        public void Init()
        {
            // Tự động lấy các component con nếu chưa gán
            // Kiểm tra xem có child object BoxContainer không
            Transform boxContainer = transform.Find("BoxContainer");
            isBoxGroup = boxContainer != null;
            //Debug.Log($"ColorBox {name}: isBoxGroup = {isBoxGroup} (BoxContainer found: {boxContainer != null})");
            if (shape == null)
                shape = transform.Find("shape")?.GetComponent<SpriteRenderer>();
            if (arrow == null)
                arrow = transform.Find("arrow");
            if (selectionCollider == null)
            {
                if (isBoxGroup)
                    selectionCollider = transform.Find("collider")?.GetComponent<Collider2D>();
                else
                    selectionCollider = transform.Find("collider-selection")?.GetComponent<Collider2D>();
            }

            if (boxTrail == null)
                boxTrail = transform.Find("BoxTrail")?.gameObject;


            // Nếu là box group, khởi tạo các child ColorBox
            if (isBoxGroup && boxContainer != null)
            {
                Debug.Log("Spawned UI Board for box group ");
                InitializeChildColorBoxes(boxContainer);
                // Spawn UI Board for box group
                SpawnUIBoard();
            }

            if (selectionCollider != null)
            {
                selectionCollider.gameObject.SetActive(true);
                // Set layer là BoxSelector
                int boxSelectorLayer = LayerMask.NameToLayer("BoxSelector");
                if (boxSelectorLayer >= 0)
                    selectionCollider.gameObject.layer = boxSelectorLayer;
                if (selectionCollider.GetComponent<SelectionColliderListener>() == null)
                {
                    selectionCollider.gameObject.AddComponent<SelectionColliderListener>();
                }
                // Thêm Rigidbody2D kinematic nếu chưa có
                Rigidbody2D rb = selectionCollider.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = selectionCollider.gameObject.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.simulated = true;
                    rb.useFullKinematicContacts = true;
                    rb.freezeRotation = true;
                }
                // Giảm size collider về 85% nếu là BoxCollider2D
                BoxCollider2D box = selectionCollider as BoxCollider2D;
                if (box != null)
                {
                    Vector2 originalSize = box.size;
                    box.size = originalSize * 0.85f;
                }
            }
            if (boxTrail != null)
                boxTrail.SetActive(true);
            // Không còn SetupCollisionDetection nữa

            // Lưu vị trí ban đầu
            originalPosition = transform.position;

            // Lấy colorIndex từ tên sprite của shape
            ExtractColorIndexFromSpriteName();

            // Lấy cấu hình từ GameConfig
            LoadGameConfig();

            // Tính toán direction từ arrow
            direction = GetDirectionFromArrow();

            // Tính toán và khởi tạo positivePoint và negativePoint
            InitializePoints();
        }

        void OnDestroy()
        {
            // Cleanup tweens khi object bị destroy
            if (moveTween != null && moveTween.IsActive())
                moveTween.Kill();
            if (rotationTween != null && rotationTween.IsActive())
                rotationTween.Kill();
            if (shakeTween != null && shakeTween.IsActive())
                shakeTween.Kill();
        }


        /// <summary>
        /// Quay về vị trí ban đầu khi va chạm
        /// </summary>
        private void ReturnToOriginalPosition()
        {
            // Dừng tất cả tweens hiện tại
            if (moveTween != null && moveTween.IsActive())
                moveTween.Kill();
            if (rotationTween != null && rotationTween.IsActive())
                rotationTween.Kill();

            // Reset trạng thái cannon slot nếu đã book
            if (finalCannonSlot != null)
            {
                finalCannonSlot.state = CannonSlotState.EMPTY;
                finalCannonSlot = null;
            }

            finalTarget = null;

            // Tạo tween quay về vị trí ban đầu
            float distance = Vector3.Distance(transform.position, originalPosition);
            float duration = distance / moveSpeed;

            // Lấy ease type từ GameConfig
            DG.Tweening.Ease returnEase = DG.Tweening.Ease.Linear;
            if (GameManager.Instance?.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                returnEase = GameManager.Instance.BoardManager.gameConfig.moveEase;
            }

            moveTween = transform.DOMove(originalPosition, duration)
                .SetEase(returnEase)
                .OnComplete(() =>
                {
                    Debug.Log($"ColorBox {name} returned to original position: {originalPosition}");
                    // Reset rotation về hướng ban đầu
                    isLeaving = false;
                    //ResetToOriginalRotation();
                });
        }

        /// <summary>
        /// Reset rotation về hướng ban đầu
        /// </summary>
        private void ResetToOriginalRotation()
        {
            isLeaving = false;
            Direction8 originalDirection = GetDirectionFromArrow();
            float targetAngle = GetBaseAngleForDirection(originalDirection);

            // Lấy ease type từ GameConfig
            DG.Tweening.Ease rotationEase = DG.Tweening.Ease.Linear;
            if (GameManager.Instance.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                rotationEase = GameManager.Instance.BoardManager.gameConfig.rotationEase;
            }

            rotationTween = transform.DORotate(new Vector3(0, 0, targetAngle), 0.5f)
                .SetEase(rotationEase);
        }

        /// <summary>
        /// Load cấu hình từ GameConfig
        /// </summary>
        private void LoadGameConfig()
        {
            //Debug.Log("LoadGameConfig called");
            if (GameManager.Instance?.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                var config = GameManager.Instance.BoardManager.gameConfig;
                moveSpeed = config.colorBoxMoveSpeed;
                pathDuration = config.pathDuration;
                // Debug.Log($"ColorBox {name}: Loaded game config - moveSpeed: {moveSpeed}, pathDuration: {pathDuration}");
            }
        }

        /// <summary>
        /// Set the color of the box (and update shape renderer).
        /// </summary>
        public void SetColor(Color color)
        {
            boxColor = color;
            if (shape != null)
            {
                shape.color = color;
            }
        }

        /// <summary>
        /// Check if a point is inside the box collider.
        /// </summary>
        public bool IsPointInside(Vector2 point)
        {
            if (selectionCollider != null)
            {
                return selectionCollider.OverlapPoint(point);
            }
            return false;
        }

        /// <summary>
        /// Get the world position of the positive point.
        /// </summary>
        public Vector3 GetPositivePoint()
        {
            return positivePoint != null ? positivePoint.position : transform.position;
        }

        /// <summary>
        /// Get the world position of the negative point.
        /// </summary>
        public Vector3 GetNegativePoint()
        {
            return negativePoint != null ? negativePoint.position : transform.position;
        }

        /// <summary>
        /// Enable or disable the selection collider.
        /// </summary>
        public void SetSelectionActive(bool active)
        {
            if (selectionCollider != null)
            {
                selectionCollider.enabled = active;
            }
        }

        /// <summary>
        /// Extract colorIndex and ammoCount from shape sprite name format "a.b.c"
        /// </summary>
        private void ExtractColorIndexFromSpriteName()
        {
            if (shape != null && shape.sprite != null)
            {
                string spriteName = shape.sprite.name;
                if (!string.IsNullOrEmpty(spriteName))
                {
                    // Parse format "a.b.c" where a=colorIndex, b=ammoCount, c=other info
                    string[] parts = spriteName.Split('.');

                    if (parts.Length >= 1)
                    {
                        // Extract colorIndex (part a)
                        if (int.TryParse(parts[0], out int extractedColorIndex))
                        {
                            colorIndex = extractedColorIndex;
                            /// Debug.Log($"ColorBox {name}: Extracted colorIndex {colorIndex} from sprite name '{spriteName}'");
                        }
                        else
                        {
                            // Debug.LogWarning($"ColorBox {name}: First part '{parts[0]}' of sprite name '{spriteName}' is not a valid number");
                        }
                    }

                    if (parts.Length >= 2)
                    {
                        // Extract ammoCount (part b)
                        if (int.TryParse(parts[1], out int extractedAmmoCount))
                        {
                            ammoCount = extractedAmmoCount;
                            // Debug.Log($"ColorBox {name}: Extracted ammoCount {ammoCount} from sprite name '{spriteName}'");
                        }
                        else
                        {
                            Debug.LogWarning($"ColorBox {name}: Second part '{parts[1]}' of sprite name '{spriteName}' is not a valid number");
                        }
                    }

                    if (parts.Length >= 3)
                    {
                        // Log other info (part c) for future use
                        //Debug.Log($"ColorBox {name}: Other info '{parts[2]}' from sprite name '{spriteName}'");
                    }
                }
                else
                {
                    //Debug.LogWarning($"ColorBox {name}: Sprite name is null or empty");
                }
            }
            else
            {
                //Debug.LogWarning($"ColorBox {name}: Shape or sprite is null");
            }
            // if (boxTrail != null)
            // {
            //     boxTrail.GetComponent<TrailRenderer>().startColor = BoardManager.Instance.colorConfig.colorList[colorIndex - 1].colorTint;
            // }
        }

        /// <summary>
        /// Get the color index of this ColorBox
        /// </summary>
        public int GetColorIndex()
        {
            return colorIndex;
        }

        /// <summary>
        /// Set the color index of this ColorBox
        /// </summary>
        public void SetColorIndex(int newColorIndex)
        {
            colorIndex = newColorIndex;
        }

        /// <summary>
        /// Get the ammo count of this ColorBox
        /// </summary>
        public int GetAmmoCount()
        {
            return ammoCount;
        }

        /// <summary>
        /// Set the ammo count of this ColorBox
        /// </summary>
        public void SetAmmoCount(int newAmmoCount)
        {
            ammoCount = newAmmoCount;
        }

        /// <summary>
        /// Get whether this ColorBox is a box group
        /// </summary>
        public bool IsBoxGroup()
        {
            return isBoxGroup;
        }

        /// <summary>
        /// Set whether this ColorBox is a box group
        /// </summary>
        public void SetBoxGroup(bool newIsBoxGroup)
        {
            isBoxGroup = newIsBoxGroup;
        }

        /// <summary>
        /// Get the list of child ColorBoxes
        /// </summary>
        public List<ColorBox> GetChildColorBoxes()
        {
            return childColorBoxes;
        }

        /// <summary>
        /// Get the number of child ColorBoxes
        /// </summary>
        public int GetChildColorBoxCount()
        {
            return childColorBoxes.Count;
        }

        /// <summary>
        /// Enable or disable colliders for all child ColorBoxes
        /// </summary>
        public void SetChildColorBoxCollidersEnabled(bool enabled)
        {
            foreach (ColorBox childBox in childColorBoxes)
            {
                if (childBox != null)
                {
                    Collider2D collider = childBox.selectionCollider;
                    if (collider != null)
                    {
                        collider.enabled = enabled;
                    }
                }
            }
            Debug.Log($"Set child ColorBox colliders enabled: {enabled} for {childColorBoxes.Count} children");
        }

        /// <summary>
        /// Update the original position (useful when ColorBox is moved programmatically)
        /// </summary>
        public void UpdateOriginalPosition()
        {
            originalPosition = transform.position;
            Debug.Log($"ColorBox {name}: Updated original position to {originalPosition}");
        }

        /// <summary>
        /// Get the original position of this ColorBox
        /// </summary>
        public Vector3 GetOriginalPosition()
        {
            return originalPosition;
        }

        /// <summary>
        /// Setup collision detection for this ColorBox
        /// </summary>
        private void SetupCollisionDetection()
        {
            // KHÔNG LÀM GÌ NỮA, ĐỂ TRỐNG HOẶC XÓA HẲN
        }
        /// <summary>
        /// Called when ColorBox reaches the final target (CannonSlot)
        /// </summary>
        private void OnReachedFinalTarget()
        {
            //Debug.Log($"OnReachedFinalTarget called for {gameObject.name}");
            //Debug.Log($"ColorBox active state before hiding: {gameObject.activeSelf}");

            // Hide the ColorBox after reaching the target
            gameObject.SetActive(false);

            //Debug.Log($"ColorBox active state after hiding: {gameObject.activeSelf}");

            // TODO: Add any additional cleanup or effects here
            // For example: play particle effects, play sound, etc.
        }

        /// <summary>
        /// Call this method to start the movement test (e.g. from UI button or OnMouseDown).
        /// </summary>
        [ContextMenu("Test OnTap")]
        public void OnTap()
        {
            //Debug.Log("ColorBox OnTap called!");

            // Thông báo cho LevelObject về việc color box được tap (cho tutorial)
            if (LevelObject.Instance != null)
            {
                LevelObject.Instance.OnColorBoxTapped();
            }

            // Kiểm tra xem có phải box group không
            if (isBoxGroup)
            {
                OnTapBoxGroup();
                return;
            }
            if (isLeaving) return;
            // Cleanup các tween cũ
            if (moveTween != null && moveTween.IsActive())
                moveTween.Kill();
            if (rotationTween != null && rotationTween.IsActive())
                rotationTween.Kill();

            // Kiểm tra xem có bị cản bởi Box khác theo hướng arrow không
            if (IsBlockedByOtherBox())
            {
                Debug.Log("Box is blocked by another box in arrow direction. Movement cancelled.");
                SpawnCollisionEffect();
                return;
            }

            // Tìm CannonSlot EMPTY đầu tiên
            finalCannonSlot = null;
            finalTarget = null;
            if (LevelObject.Instance != null)
            {
                // Debug.Log($"LevelObject found, checking {LevelObject.Instance.cannonSlots.Count} cannon slots");
                foreach (var slot in LevelObject.Instance.cannonSlots)
                {
                    if (slot != null)
                    {
                        //Debug.Log($"CannonSlot state: {slot.state}");
                        if (slot.state == CannonSlotState.EMPTY)
                        {
                            slot.state = CannonSlotState.BOOKED;
                            finalCannonSlot = slot;
                            finalTarget = slot.transform.position;
                            //Debug.Log($"Found EMPTY slot at position: {finalTarget}");
                            break;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("LevelObject.Instance is null!");
            }

            if (finalTarget == null)
            {
                Debug.LogWarning("No EMPTY cannon slot found! Cannot move ColorBox.");
                GameManager.Instance.UIManager.notifyUI.ShowNotify("No EMPTY cannon slots found!", true, 2f);
                return;
            }


            Debug.Log("Starting movement to cannon slot");
            SoundManager.Instance.PlaySFX(SoundManager.Instance.slideBoxClip);
            MoveInDirection();
        }

        /// <summary>
        /// Xử lý tap cho box group
        /// </summary>
        private void OnTapBoxGroup()
        {
            Debug.Log($"OnTapBoxGroup called for {name} with {childColorBoxes.Count} child ColorBoxes");

            // Kiểm tra xem còn child ColorBox nào không
            if (childColorBoxes.Count == 0)
            {
                Debug.Log("No child ColorBoxes available to spawn");
                return;
            }

            // Tìm spawnArea
            Transform spawnArea = transform.Find("spawnArea");
            if (spawnArea == null)
            {
                Debug.LogWarning("spawnArea not found in box group!");
                return;
            }

            // Kiểm tra va chạm tại spawnArea
            BoxCollider2D spawnAreaCollider = spawnArea.GetComponent<BoxCollider2D>();
            if (spawnAreaCollider == null)
            {
                Debug.LogWarning("spawnArea does not have BoxCollider2D!");
                return;
            }

            // Kiểm tra xem có ColorBox nào khác trong spawnArea không
            if (IsSpawnAreaOccupied(spawnAreaCollider))
            {
                Debug.Log("SpawnArea is occupied by another ColorBox. Cannot spawn child.");
                return;
            }

            // Spawn child ColorBox đầu tiên trong danh sách
            SpawnChildColorBox(spawnArea.position);
        }

        /// <summary>
        /// Kiểm tra xem spawnArea có bị chiếm bởi ColorBox khác không
        /// </summary>
        private bool IsSpawnAreaOccupied(BoxCollider2D spawnAreaCollider)
        {
            // Tạm thời enable collider để kiểm tra va chạm
            bool wasEnabled = spawnAreaCollider.enabled;
            spawnAreaCollider.enabled = true;

            // Tạo OverlapBox để kiểm tra va chạm
            Vector2 center = spawnAreaCollider.transform.TransformPoint(spawnAreaCollider.offset);
            Vector2 size = Vector2.Scale(spawnAreaCollider.size, spawnAreaCollider.transform.lossyScale);
            float angle = spawnAreaCollider.transform.eulerAngles.z;

            // Kiểm tra tất cả Collider2D trong spawnArea
            Collider2D[] colliders = Physics2D.OverlapBoxAll(center, size, angle);

            bool isOccupied = false;
            foreach (Collider2D collider in colliders)
            {
                // Kiểm tra xem có phải ColorBox khác không (không phải child của box group này)
                ColorBox otherColorBox = collider.GetComponentInParent<ColorBox>();
                if (otherColorBox != null && otherColorBox.gameObject != gameObject && !childColorBoxes.Contains(otherColorBox))
                {
                    Debug.Log($"SpawnArea occupied by ColorBox: {otherColorBox.name}");
                    isOccupied = true;
                    break;
                }
            }

            // Restore trạng thái collider
            spawnAreaCollider.enabled = wasEnabled;

            return isOccupied;
        }

        /// <summary>
        /// Spawn child ColorBox tại vị trí spawnArea
        /// </summary>
        private void SpawnChildColorBox(Vector3 spawnPosition)
        {
            if (childColorBoxes.Count == 0)
            {
                Debug.LogWarning("No child ColorBoxes available to spawn");
                return;
            }

            // Lấy child ColorBox đầu tiên
            ColorBox childToSpawn = childColorBoxes[0];
            childColorBoxes.RemoveAt(0);

            if (childToSpawn == null)
            {
                Debug.LogWarning("Child ColorBox is null, skipping spawn");
                return;
            }
            if (boardUI != null)
            {
                if (childColorBoxes.Count > 0)
                    boardUI.SetBoard(childColorBoxes.Count, childColorBoxes[0].colorIndex);
                else
                    boardUI.SetBoardDefaultColor(childColorBoxes.Count);
            }

            // Lưu vị trí ban đầu của child
            Vector3 originalChildPosition = childToSpawn.transform.position;

            // Tính toán thời gian di chuyển dựa trên khoảng cách
            float distance = Vector3.Distance(originalChildPosition, spawnPosition);
            float duration = distance / moveSpeed;

            // Lấy ease type từ GameConfig
            DG.Tweening.Ease spawnEase = DG.Tweening.Ease.OutBack; // Ease đẹp cho spawn effect
            if (GameManager.Instance?.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                spawnEase = GameManager.Instance.BoardManager.gameConfig.moveEase;
            }

            // Tạo sequence cho spawn effect
            Sequence spawnSequence = DOTween.Sequence();

            // Scale effect: bắt đầu nhỏ, phóng to, rồi về bình thường
            Vector3 originalScale = childToSpawn.transform.localScale;
            spawnSequence.Append(childToSpawn.transform.DOScale(originalScale * 0.5f, 0.1f));
            spawnSequence.Append(childToSpawn.transform.DOScale(originalScale * 1.2f, 0.2f));
            spawnSequence.Append(childToSpawn.transform.DOScale(originalScale, 0.1f));

            // Di chuyển đến spawnArea với animation mượt mà
            spawnSequence.Join(childToSpawn.transform.DOMove(spawnPosition, duration)
                .SetEase(spawnEase)
                .OnComplete(() =>
                {
                    childToSpawn.selectionCollider.enabled = true;

                    // Cập nhật original position cho child
                    childToSpawn.UpdateOriginalPosition();

                    Debug.Log($"Spawned child ColorBox: {childToSpawn.name} at position: {spawnPosition}. Remaining children: {childColorBoxes.Count}");
                }));

            // Play sound effect
            if (SoundManager.Instance != null && SoundManager.Instance.slideBoxClip != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.slideBoxClip);
            }
        }

        /// <summary>
        /// Kiểm tra xem Box có bị cản bởi Box khác theo hướng arrow không
        /// </summary>
        private bool IsBlockedByOtherBox()
        {
            // Lấy hướng chính xác từ rotation của arrow
            Vector2 rayDirection = GetArrowDirection();

            // Tạo raycast từ vị trí hiện tại theo hướng arrow, kiểm tra tất cả collider
            float rayDistance = 0.5f; // Khoảng cách kiểm tra
            RaycastHit2D[] hits = Physics2D.RaycastAll(positivePoint.position, rayDirection, rayDistance);

            foreach (RaycastHit2D hit in hits)
            {
                // Kiểm tra xem hit có phải là ColorBox khác không
                ColorBox otherBox = hit.collider.GetComponentInParent<ColorBox>();
                if (otherBox != null && otherBox.gameObject != gameObject)
                {
                    // Kiểm tra vibration setting trước khi sử dụng
                    TriggerVibrationIfEnabled(HapticPatterns.PresetType.MediumImpact);
                    otherBox.ShakeEffect();
                    Debug.Log($"Box blocked by other ColorBox: {otherBox.name}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Chuyển đổi Direction8 thành Vector2
        /// </summary>
        private Vector2 GetVectorFromDirection(Direction8 direction)
        {
            switch (direction)
            {
                case Direction8.Right: return Vector2.right;
                case Direction8.Left: return Vector2.left;
                case Direction8.Top: return Vector2.up;
                case Direction8.Bottom: return Vector2.down;
                case Direction8.TopRight: return (Vector2.right + Vector2.up).normalized;
                case Direction8.TopLeft: return (Vector2.left + Vector2.up).normalized;
                case Direction8.BottomRight: return (Vector2.right + Vector2.down).normalized;
                case Direction8.BottomLeft: return (Vector2.left + Vector2.down).normalized;
                default: return Vector2.right;
            }
        }

        /// <summary>
        /// Tính toán và khởi tạo positivePoint và negativePoint dựa trên transform, direction và size của shape
        /// </summary>
        private void InitializePoints()
        {
            // Ưu tiên dùng selectionCollider nếu có
            BoxCollider2D box = selectionCollider as BoxCollider2D;
            if (box == null)
            {
                Debug.LogError("selectionCollider không phải là BoxCollider2D! Không thể tính toán points chính xác.");
                return;
            }

            if (positivePoint == null)
                positivePoint = transform.Find("positivePoint");
            if (negativePoint == null)
                negativePoint = transform.Find("negativePoint");

            if (positivePoint == null || negativePoint == null)
            {
                Debug.LogError("positivePoint hoặc negativePoint không tồn tại!");
                return;
            }

            // Tính center và size trong world space
            Vector2 center = box.transform.TransformPoint(box.offset);
            Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);

            // Tính 4 góc local (chưa xoay)
            Vector2[] localCorners = new Vector2[4];
            localCorners[0] = new Vector2(-size.x / 2, -size.y / 2); // Bottom Left
            localCorners[1] = new Vector2(-size.x / 2, size.y / 2);  // Top Left
            localCorners[2] = new Vector2(size.x / 2, size.y / 2);   // Top Right
            localCorners[3] = new Vector2(size.x / 2, -size.y / 2);  // Bottom Right

            // Xoay các điểm quanh center theo rotation của box
            float angle = box.transform.eulerAngles.z;
            Quaternion rot = Quaternion.Euler(0, 0, angle);
            Vector2[] worldCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                worldCorners[i] = (Vector2)(rot * (Vector3)localCorners[i]) + center;
            }

            // Tính midpoints của 4 cạnh: bottom, left, top, right (theo thứ tự)
            Vector2[] edgeMids = new Vector2[4];
            edgeMids[0] = (worldCorners[0] + worldCorners[3]) / 2f; // Bottom
            edgeMids[1] = (worldCorners[0] + worldCorners[1]) / 2f; // Left
            edgeMids[2] = (worldCorners[1] + worldCorners[2]) / 2f; // Top
            edgeMids[3] = (worldCorners[2] + worldCorners[3]) / 2f; // Right

            // Vector pháp tuyến (outward) của từng cạnh (bottom, left, top, right)
            Vector2[] edgeNormals = new Vector2[4];
            edgeNormals[0] = (Vector2)(rot * Vector3.down);  // Bottom
            edgeNormals[1] = (Vector2)(rot * Vector3.left);  // Left
            edgeNormals[2] = (Vector2)(rot * Vector3.up);    // Top
            edgeNormals[3] = (Vector2)(rot * Vector3.right); // Right

            // Lấy hướng arrow (vector2, đã chuẩn hóa)
            Vector2 arrowDir = GetArrowDirection();

            // Tìm cạnh có pháp tuyến gần nhất với hướng arrow
            float maxDot = float.NegativeInfinity;
            int bestEdge = 0;
            for (int i = 0; i < 4; i++)
            {
                float dot = Vector2.Dot(edgeNormals[i], arrowDir);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestEdge = i;
                }
            }

            // Cạnh đối diện
            int oppositeEdge = (bestEdge + 2) % 4;

            // Đặt positivePoint ở cạnh cùng hướng arrow, negativePoint ở cạnh đối diện
            positivePoint.position = edgeMids[bestEdge];
            negativePoint.position = edgeMids[oppositeEdge];
        }

        /// <summary>
        /// Tính toán offset cho điểm dựa trên direction và size
        /// </summary>
        private Vector2 GetPointOffset(Direction8 dir, float halfWidth, float halfHeight)
        {
            switch (dir)
            {
                case Direction8.Right: return new Vector2(halfWidth, 0);
                case Direction8.Left: return new Vector2(-halfWidth, 0);
                case Direction8.Top: return new Vector2(0, halfHeight);
                case Direction8.Bottom: return new Vector2(0, -halfHeight);
                case Direction8.TopRight: return new Vector2(halfWidth, halfHeight);
                case Direction8.TopLeft: return new Vector2(-halfWidth, halfHeight);
                case Direction8.BottomRight: return new Vector2(halfWidth, -halfHeight);
                case Direction8.BottomLeft: return new Vector2(-halfWidth, -halfHeight);
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// Lấy hướng ngược lại
        /// </summary>
        private Direction8 GetOppositeDirection(Direction8 dir)
        {
            switch (dir)
            {
                case Direction8.Right: return Direction8.Left;
                case Direction8.Left: return Direction8.Right;
                case Direction8.Top: return Direction8.Bottom;
                case Direction8.Bottom: return Direction8.Top;
                case Direction8.TopRight: return Direction8.BottomLeft;
                case Direction8.TopLeft: return Direction8.BottomRight;
                case Direction8.BottomRight: return Direction8.TopLeft;
                case Direction8.BottomLeft: return Direction8.TopRight;
                default: return Direction8.Right;
            }
        }

        /// <summary>
        /// Spawn hiệu ứng va chạm tại vị trí negativePoint
        /// </summary>
        private void SpawnCollisionEffect()
        {
            if (GameManager.Instance?.BoardManager == null || GameManager.Instance.BoardManager.gameConfig == null ||
                GameManager.Instance.BoardManager.gameConfig.boxCollisionFx == null)
            {
                Debug.LogWarning("Box collision effect prefab not found in GameConfig");
                return;
            }

            Vector3 spawnPosition = positivePoint != null ? positivePoint.position : transform.position;
            GameObject effect = Instantiate(GameManager.Instance.BoardManager.gameConfig.boxCollisionFx, spawnPosition, Quaternion.identity);

            // Tự động destroy effect sau một thời gian
            Destroy(effect, 2f);
        }

        private Direction8 GetDirectionFromArrow()
        {
            if (arrow == null)
                return Direction8.Right;
            float angle = arrow.eulerAngles.z;
            // Đưa về [0, 360)
            angle = (angle + 360) % 360;

            // Đưa về hệ quy chiếu bắt đầu từ 90 độ (Right)
            float shifted = (angle - 90 + 360) % 360;

            if (shifted >= 337.5f || shifted < 22.5f)
                return Direction8.Right;
            if (shifted >= 22.5f && shifted < 67.5f)
                return Direction8.TopRight;
            if (shifted >= 67.5f && shifted < 112.5f)
                return Direction8.Top;
            if (shifted >= 112.5f && shifted < 157.5f)
                return Direction8.TopLeft;
            if (shifted >= 157.5f && shifted < 202.5f)
                return Direction8.Left;
            if (shifted >= 202.5f && shifted < 247.5f)
                return Direction8.BottomLeft;
            if (shifted >= 247.5f && shifted < 292.5f)
                return Direction8.Bottom;
            if (shifted >= 292.5f && shifted < 337.5f)
                return Direction8.BottomRight;
            return Direction8.Right;
        }

        private void MoveInDirection()
        {
            // Set trạng thái đang di chuyển
            isLeaving = true;
            if (GameManager.Instance?.BoardManager == null || GameManager.Instance.BoardManager.pathCollider == null)
            {
                Debug.LogError("BoardManager or pathCollider is null!");
                return;
            }

            // Lấy rotation của arrow để tính hướng chính xác
            Vector2 direction = GetArrowDirection();

            // Tìm giao điểm với pathCollider bằng raycast
            Vector3 target = FindIntersectionWithPathCollider(direction);

            float distance = Vector3.Distance(transform.position, target);
            float duration = distance / moveSpeed;

            // Lấy ease type từ GameConfig
            DG.Tweening.Ease moveEase = DG.Tweening.Ease.Linear;
            if (GameManager.Instance?.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                moveEase = GameManager.Instance.BoardManager.gameConfig.moveEase;
            }

            moveTween = transform.DOMove(target, duration)
                .SetEase(moveEase)
                .OnComplete(MoveAlongPath);
        }

        /// <summary>
        /// Lấy hướng vector từ rotation của arrow
        /// </summary>
        private Vector2 GetArrowDirection()
        {
            if (arrow == null)
                return Vector2.right;

            // Lấy góc rotation của arrow (đơn vị độ)
            float angle = arrow.eulerAngles.z;

            // Đưa về [0, 360)
            angle = (angle + 360) % 360;

            // Đưa về hệ quy chiếu bắt đầu từ 90 độ (Right) - giống như GetDirectionFromArrow()
            float shifted = (angle - 90 + 360) % 360;

            // Chuyển đổi từ độ sang radian
            float angleRad = shifted * Mathf.Deg2Rad;

            // Tính vector hướng từ góc
            Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            return direction.normalized;
        }

        /// <summary>
        /// Tìm giao điểm giữa tia từ vị trí ColorBox theo hướng arrow với pathCollider
        /// </summary>
        private Vector3 FindIntersectionWithPathCollider(Vector2 direction)
        {
            var pathCollider = GameManager.Instance.BoardManager.pathCollider;
            Vector2 center = pathCollider.transform.TransformPoint(pathCollider.offset);
            Vector2 size = Vector2.Scale(pathCollider.size, pathCollider.transform.lossyScale);

            // Tính các góc của pathCollider
            Vector2 halfSize = size / 2f;
            Vector2[] corners = new Vector2[4];
            corners[0] = center + new Vector2(-halfSize.x, -halfSize.y); // Bottom Left
            corners[1] = center + new Vector2(-halfSize.x, halfSize.y);  // Top Left
            corners[2] = center + new Vector2(halfSize.x, halfSize.y);   // Top Right
            corners[3] = center + new Vector2(halfSize.x, -halfSize.y);  // Bottom Right

            // Vị trí bắt đầu của tia (vị trí ColorBox)
            Vector2 rayOrigin = transform.position;

            // Tìm giao điểm gần nhất với các cạnh của pathCollider
            Vector2 closestIntersection = Vector2.zero;
            float closestDistance = float.MaxValue;

            // Kiểm tra giao điểm với từng cạnh
            for (int i = 0; i < 4; i++)
            {
                Vector2 edgeStart = corners[i];
                Vector2 edgeEnd = corners[(i + 1) % 4];

                Vector2 intersection = GetLineIntersection(rayOrigin, rayOrigin + direction * 1000f, edgeStart, edgeEnd);

                if (intersection != Vector2.zero)
                {
                    float distance = Vector2.Distance(rayOrigin, intersection);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIntersection = intersection;
                    }
                }
            }

            // Nếu không tìm thấy giao điểm, trả về vị trí mặc định
            if (closestIntersection == Vector2.zero)
            {
                Debug.LogWarning("No intersection found with pathCollider, clamping fallback position to path bounds");
                // Clamp fallback point vào bounds
                Vector2 fallback = (Vector2)transform.position + direction * 10f;
                float minX = center.x - halfSize.x;
                float maxX = center.x + halfSize.x;
                float minY = center.y - halfSize.y;
                float maxY = center.y + halfSize.y;
                fallback.x = Mathf.Clamp(fallback.x, minX, maxX);
                fallback.y = Mathf.Clamp(fallback.y, minY, maxY);
                return fallback;
            }
            return closestIntersection;
        }

        /// <summary>
        /// Tính giao điểm giữa hai đường thẳng
        /// </summary>
        private Vector2 GetLineIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
        {
            Vector2 line1Dir = (line1End - line1Start).normalized;
            Vector2 line2Dir = (line2End - line2Start).normalized;

            // Tính cross product để kiểm tra song song
            float cross = line1Dir.x * line2Dir.y - line1Dir.y * line2Dir.x;

            if (Mathf.Abs(cross) < 0.0001f)
            {
                // Hai đường thẳng song song hoặc trùng nhau
                return Vector2.zero;
            }

            // Tính tham số t cho giao điểm
            Vector2 diff = line2Start - line1Start;
            float t = (diff.x * line2Dir.y - diff.y * line2Dir.x) / cross;

            // Tính giao điểm
            Vector2 intersection = line1Start + t * line1Dir;

            // Kiểm tra xem giao điểm có nằm trên cả hai đoạn thẳng không
            if (IsPointOnLineSegment(intersection, line1Start, line1End) &&
                IsPointOnLineSegment(intersection, line2Start, line2End))
            {
                return intersection;
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Kiểm tra xem một điểm có nằm trên đoạn thẳng không
        /// </summary>
        private bool IsPointOnLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            // Kiểm tra xem điểm có nằm trong bounding box của đoạn thẳng không
            float minX = Mathf.Min(lineStart.x, lineEnd.x);
            float maxX = Mathf.Max(lineStart.x, lineEnd.x);
            float minY = Mathf.Min(lineStart.y, lineEnd.y);
            float maxY = Mathf.Max(lineStart.y, lineEnd.y);

            if (point.x < minX || point.x > maxX || point.y < minY || point.y > maxY)
            {
                return false;
            }

            // Kiểm tra xem điểm có nằm trên đường thẳng không (với độ chính xác)
            float tolerance = 0.01f;
            float distance = Mathf.Abs((lineEnd.y - lineStart.y) * point.x -
                                      (lineEnd.x - lineStart.x) * point.y +
                                      lineEnd.x * lineStart.y - lineEnd.y * lineStart.x) /
                           Vector2.Distance(lineStart, lineEnd);

            return distance <= tolerance;
        }

        private void MoveAlongPath()
        {
            isLeaving = false;
            if (GameManager.Instance?.BoardManager == null || GameManager.Instance.BoardManager.pathCollider == null || finalTarget == null) return;
            var pathCollider = GameManager.Instance.BoardManager.pathCollider;
            Vector2 center = pathCollider.transform.TransformPoint(pathCollider.offset);
            Vector2 size = Vector2.Scale(pathCollider.size, pathCollider.transform.lossyScale);
            Vector3[] corners = new Vector3[4];
            corners[0] = center + new Vector2(-size.x / 2, -size.y / 2); // Bottom Left
            corners[1] = center + new Vector2(-size.x / 2, size.y / 2);  // Top Left
            corners[2] = center + new Vector2(size.x / 2, size.y / 2);   // Top Right
            corners[3] = center + new Vector2(size.x / 2, -size.y / 2);  // Bottom Right

            // Tìm temp target: giao điểm giữa tia từ final target hướng Vector2.down với cạnh trên (Top)
            Vector3 tempTarget = finalTarget.Value;
            Vector3 A = corners[1]; // Top Left
            Vector3 B = corners[2]; // Top Right
            // Đoạn AB: y = A.y (cạnh trên là ngang)
            if (finalTarget.Value.x >= Mathf.Min(A.x, B.x) && finalTarget.Value.x <= Mathf.Max(A.x, B.x))
            {
                tempTarget = new Vector3(finalTarget.Value.x, A.y, A.z);
            }

            // Tìm đường ngắn nhất trên path từ vị trí hiện tại đến temp target
            Vector3[] pathPoints = GetShortestPathOnRectPath(transform.position, tempTarget, corners);
            float totalDistance = 0;
            for (int i = 1; i < pathPoints.Length; i++)
                totalDistance += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            float duration = totalDistance / moveSpeed;

            // Lấy ease type từ GameConfig
            DG.Tweening.Ease pathEase = DG.Tweening.Ease.Linear;
            if (GameManager.Instance?.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                pathEase = GameManager.Instance.BoardManager.gameConfig.moveEase;
            }

            // Tween đến temp target với xoay mượt mà theo từng waypoint
            moveTween = transform.DOPath(pathPoints, duration, PathType.Linear, PathMode.TopDown2D)
                .SetEase(pathEase)
                .OnWaypointChange(waypointIndex =>
                {
                    // Xoay box theo hướng di chuyển đến waypoint tiếp theo
                    if (waypointIndex < pathPoints.Length - 1)
                    {
                        Vector3 currentPos = pathPoints[waypointIndex];
                        Vector3 nextPos = pathPoints[waypointIndex + 1];
                        float segmentDuration = Vector3.Distance(currentPos, nextPos) / moveSpeed;
                        RotateTowardsDirection(currentPos, nextPos, segmentDuration);
                    }
                })
                .OnComplete(() => MoveToFinalTarget());
        }

        private void MoveToFinalTarget()
        {
            Debug.Log("MoveToFinalTarget called");

            if (finalTarget == null)
            {
                Debug.LogError("finalTarget is null! Cannot move to final target.");
                return;
            }

            float distance = Vector3.Distance(transform.position, finalTarget.Value);
            float duration = distance / moveSpeed;

            Debug.Log($"Moving to final target: {finalTarget.Value}, distance: {distance}, duration: {duration}");

            // Xoay box theo hướng di chuyển cuối cùng
            RotateTowardsDirection(transform.position, finalTarget.Value, duration);

            // Lấy ease type từ GameConfig
            DG.Tweening.Ease finalMoveEase = DG.Tweening.Ease.Linear;
            if (GameManager.Instance.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                finalMoveEase = GameManager.Instance.BoardManager.gameConfig.moveEase;
            }

            moveTween = transform.DOMove(finalTarget.Value, duration)
                .SetEase(finalMoveEase)
                .OnComplete(() =>
                {
                    Debug.Log("Final target reached! OnComplete called.");

                    if (finalCannonSlot != null)
                    {
                        Debug.Log($"finalCannonSlot found: {finalCannonSlot.name}");

                        try
                        {
                            // Start the complete cannon activation process with colorIndex and ammoCount
                            finalCannonSlot.StartCannonActivation(colorIndex, ammoCount);
                            Debug.Log($"StartCannonActivation called successfully with colorIndex: {colorIndex}, ammoCount: {ammoCount}");

                            OnReachedFinalTarget();
                            Debug.Log("OnReachedFinalTarget called successfully");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Exception in final target completion: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogError("finalCannonSlot is null! Cannot activate cannon.");
                    }
                });
        }

        // Hàm hỗ trợ: tìm đường ngắn nhất trên path hình chữ nhật
        private Vector3[] GetShortestPathOnRectPath(Vector3 from, Vector3 to, Vector3[] corners)
        {
            int n = corners.Length;
            float epsilon = 0.01f;
            // Tìm cạnh chứa from và to
            int fromEdge = -1, toEdge = -1;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[(i + 1) % n];
                if (PointOnEdge(from, a, b, epsilon)) fromEdge = i;
                if (PointOnEdge(to, a, b, epsilon)) toEdge = i;
            }
            // Nếu không tìm được cạnh, trả về đường thẳng
            if (fromEdge == -1 || toEdge == -1)
                return new Vector3[] { from, to };

            // Path thuận chiều
            List<Vector3> path1 = new List<Vector3>();
            path1.Add(from);
            int idx = fromEdge;
            while (idx != toEdge)
            {
                idx = (idx + 1) % n;
                path1.Add(corners[idx]);
            }
            path1.Add(to);

            // Path ngược chiều
            List<Vector3> path2 = new List<Vector3>();
            path2.Add(from);
            idx = fromEdge;
            while (idx != toEdge)
            {
                idx = (idx - 1 + n) % n;
                path2.Add(corners[(idx + 1) % n]);
            }
            path2.Add(to);

            // Tính tổng độ dài
            float dist1 = 0, dist2 = 0;
            for (int i = 1; i < path1.Count; i++) dist1 += Vector3.Distance(path1[i - 1], path1[i]);
            for (int i = 1; i < path2.Count; i++) dist2 += Vector3.Distance(path2[i - 1], path2[i]);

            return (dist1 < dist2) ? path1.ToArray() : path2.ToArray();
        }

        // Kiểm tra điểm p có nằm trên đoạn ab không
        private bool PointOnEdge(Vector3 p, Vector3 a, Vector3 b, float epsilon)
        {
            float minX = Mathf.Min(a.x, b.x) - epsilon;
            float maxX = Mathf.Max(a.x, b.x) + epsilon;
            float minY = Mathf.Min(a.y, b.y) - epsilon;
            float maxY = Mathf.Max(a.y, b.y) + epsilon;
            if (p.x >= minX && p.x <= maxX && p.y >= minY && p.y <= maxY)
            {
                // Kiểm tra đồng phẳng
                float cross = Mathf.Abs((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x));
                if (cross < epsilon * 10f) return true;
            }
            return false;
        }

        /// <summary>
        /// Tính toán góc xoay dựa trên hướng di chuyển và hướng ban đầu của box
        /// Arrow chỉ hướng từ đuôi xe đến đầu xe
        /// </summary>
        private float CalculateRotationAngle(Vector3 from, Vector3 to)
        {
            Vector3 direction = (to - from).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Điều chỉnh góc dựa trên hướng ban đầu của box
            Direction8 boxDirection = GetDirectionFromArrow();
            float baseAngle = GetBaseAngleForDirection(boxDirection);

            // Tính góc xoay tương đối so với hướng ban đầu
            float relativeAngle = targetAngle - baseAngle;

            // Đảm bảo góc nằm trong khoảng [-180, 180]
            while (relativeAngle > 180f) relativeAngle -= 360f;
            while (relativeAngle < -180f) relativeAngle += 360f;

            return relativeAngle;
        }

        /// <summary>
        /// Lấy góc cơ bản cho từng hướng của box
        /// </summary>
        private float GetBaseAngleForDirection(Direction8 dir)
        {
            switch (dir)
            {
                case Direction8.Right: return 0f;      // 0 độ
                case Direction8.Left: return 180f;     // 180 độ (ngược với Right)
                case Direction8.Top: return 90f;       // 90 độ
                case Direction8.Bottom: return 270f;   // 270 độ (ngược với Top)
                case Direction8.TopRight: return 45f;  // 45 độ
                case Direction8.TopLeft: return 135f;  // 135 độ
                case Direction8.BottomRight: return 315f; // 315 độ
                case Direction8.BottomLeft: return 225f;  // 225 độ
                default: return 0f;
            }
        }

        /// <summary>
        /// Xoay box theo hướng di chuyển với animation mượt mà
        /// </summary>
        private void RotateTowardsDirection(Vector3 from, Vector3 to, float duration)
        {
            if (rotationTween != null && rotationTween.IsActive())
                rotationTween.Kill();

            float relativeAngle = CalculateRotationAngle(from, to);

            // Lấy hệ số tốc độ xoay từ GameConfig
            float rotationSpeedMultiplier = 10f; // Default value
            if (GameManager.Instance.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                rotationSpeedMultiplier = GameManager.Instance.BoardManager.gameConfig.colorBoxRotationSpeedMultiplier;
            }

            float rotationDuration = duration / rotationSpeedMultiplier;

            // Tính góc xoay tuyệt đối bằng cách cộng với góc hiện tại
            float currentZ = transform.eulerAngles.z;
            float targetZ = currentZ + relativeAngle;

            // Lấy ease type từ GameConfig
            DG.Tweening.Ease rotationEase = DG.Tweening.Ease.Linear;
            if (GameManager.Instance.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                rotationEase = GameManager.Instance.BoardManager.gameConfig.rotationEase;
            }

            rotationTween = transform.DORotate(new Vector3(0, 0, targetZ), rotationDuration)
                .SetEase(rotationEase);
        }

        /// <summary>
        /// Spawn UI Board for box group
        /// </summary>
        private void SpawnUIBoard()
        {
            // Load Board prefab from Resources
            GameObject boardPrefab = Resources.Load<GameObject>("prefabs/ui/Board");
            if (boardPrefab == null)
            {
                Debug.LogError($"Board prefab not found at path: prefabs/ui/Board");
                return;
            }

            // Spawn at specified position
            Vector3 spawnPosition = new Vector3(-0.7f, -0.7f, 0.0f);
            GameObject boardInstance = Instantiate(boardPrefab, Vector3.zero, Quaternion.identity);
            boardInstance.transform.SetParent(transform);
            boardInstance.transform.localPosition = spawnPosition;
            // Get BoardUI component and set it up
            boardUI = boardInstance.GetComponent<BoardUI>();
            if (boardUI != null)
            {
                // Set the board with child count and color index từ ColorBox
                boardUI.SetBoard(childColorBoxes.Count, childColorBoxes[0].colorIndex);
                //Debug.Log($"Spawned UI Board for box group {name} with {childColorBoxes.Count} children, colorIndex: {colorIndex}");
            }
            else
            {
                //Debug.LogWarning($"BoardUI component not found on spawned Board prefab");
            }
        }

        /// <summary>
        /// Khởi tạo các ColorBox con trong BoxContainer
        /// </summary>
        private void InitializeChildColorBoxes(Transform boxContainer)
        {
            Debug.Log($"Initializing child ColorBoxes for {boxContainer.name}");
            childColorBoxes.Clear();

            // Disable collider của spawnArea
            Transform spawnArea = transform.Find("spawnArea");
            if (spawnArea != null)
            {
                BoxCollider2D spawnAreaCollider = spawnArea.GetComponent<BoxCollider2D>();
                if (spawnAreaCollider != null)
                {
                    spawnAreaCollider.enabled = false;
                    Debug.Log($"Disabled spawnArea collider for {name}");
                }
            }

            foreach (Transform child in boxContainer)
            {
                // Add ColorBox component nếu chưa có
                ColorBox childColorBox = child.GetComponent<ColorBox>();
                if (childColorBox == null)
                {
                    Debug.Log($"Adding ColorBox component to child: {child.name}");
                    childColorBox = child.gameObject.AddComponent<ColorBox>();
                }
                // KHÔNG ADD RIGIDBODY2D VÀ BOXCOLLIDER2D NỮA
                // Init ColorBox con
                childColorBox.Init();
                childColorBox.selectionCollider.enabled = false;
                childColorBoxes.Add(childColorBox);
                Debug.Log($"Added child ColorBox: {childColorBox.name} (total: {childColorBoxes.Count})");
            }

            Debug.Log($"Initialized {childColorBoxes.Count} child ColorBoxes in {boxContainer.name}");
        }

        // Gọi khi selectionCollider va chạm với ColorBox khác
        public void OnSelectionColliderHit(ColorBox otherBox)
        {
            if (isLeaving && otherBox != null && otherBox != this)
            {
                SpawnCollisionEffect();
                otherBox.ShakeEffect();
                // Kiểm tra vibration setting trước khi sử dụng
                TriggerVibrationIfEnabled(HapticPatterns.PresetType.MediumImpact);
                ReturnToOriginalPosition();
            }
        }

        /// <summary>
        /// Tạo hiệu ứng rung cho ColorBox sử dụng DOTween
        /// </summary>
        /// <param name="duration">Thời gian rung (giây)</param>
        /// <param name="strength">Độ mạnh của hiệu ứng rung</param>
        /// <param name="vibrato">Số lần rung</param>
        /// <param name="randomness">Độ ngẫu nhiên của rung (0-90)</param>
        public void ShakeEffect(float duration = 0.5f, float strength = 0.3f, int vibrato = 10, float randomness = 90f)
        {
            // Dừng các tween rung hiện tại nếu có
            if (shakeTween != null && shakeTween.IsActive())
            {
                shakeTween.Kill();
            }

            // Disable selectionCollider trong quá trình rung
            if (selectionCollider != null)
            {
                selectionCollider.enabled = false;
            }

            // Lưu vị trí ban đầu
            Vector3 originalPos = transform.position;

            // Tạo hiệu ứng rung
            shakeTween = transform.DOShakePosition(duration, strength, vibrato, randomness, false, true)
                .OnComplete(() =>
                {
                    // Enable lại selectionCollider sau khi hết hiệu ứng
                    if (selectionCollider != null)
                    {
                        selectionCollider.enabled = true;
                    }

                    // Đảm bảo box trở về vị trí chính xác
                    transform.position = originalPos;

                    Debug.Log($"Shake effect completed for {name}");
                });

            Debug.Log($"Shake effect started for {name} - Duration: {duration}s, Strength: {strength}");
        }

        /// <summary>
        /// Tạo hiệu ứng rung với tham số mặc định
        /// </summary>
        public void ShakeEffect()
        {
            ShakeEffect(0.5f, 0.3f, 10, 90f);
        }

        /// <summary>
        /// Dừng hiệu ứng rung hiện tại
        /// </summary>
        public void StopShakeEffect()
        {
            if (shakeTween != null && shakeTween.IsActive())
            {
                shakeTween.Kill();

                // Enable lại selectionCollider
                if (selectionCollider != null)
                {
                    selectionCollider.enabled = true;
                }

                Debug.Log($"Shake effect stopped for {name}");
            }
        }

        /// <summary>
        /// Trigger vibration chỉ khi vibration setting được bật
        /// </summary>
        /// <param name="presetType">Loại vibration preset</param>
        private void TriggerVibrationIfEnabled(HapticPatterns.PresetType presetType)
        {
            VibrationHelper.TriggerVibrationIfEnabled(presetType);
        }
    }
}