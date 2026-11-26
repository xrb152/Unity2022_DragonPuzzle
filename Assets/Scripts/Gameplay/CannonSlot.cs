using UnityEngine;
using Spine.Unity;
using UnityEngine.UI;
using System.Collections;
using RescuePrincess.Managers;

namespace RescuePrincess.Gameplay
{
    public enum CannonSlotState
    {
        LOCK,
        EMPTY,
        BOOKED,
        READY
    }

    public class CannonSlot : MonoBehaviour
    {
     
        [Header("Spawn Point")]
        [HideInInspector]public Transform spawnPoint;

        [Header("Spine Animations")]
        [SerializeField] private SkeletonAnimation boxAnimation;
        [SerializeField] private SkeletonAnimation barrelAnimation;

        [Header("Animation Names")]
        [SerializeField] private string boxOpenAnimationName = "open";
        [SerializeField] private string barrelIdleAnimationName = "idle";
        [SerializeField] private string barrelFireAnimationName = "fire";

        [Header("Auto Fire Settings")]
        [SerializeField] private GameObject bulletPrefab; // Bullet prefab

        private GameObject currentCannon;
        [SerializeField] private int cannonColorIndex = 0;
        [SerializeField] private int cannonAmmoCount = 1;
        [SerializeField] private int maxAmmoCount = 1; // Maximum ammo count

        [Header("Slot State")]
        public CannonSlotState state = CannonSlotState.EMPTY;

        [Header("UI References")]
        [SerializeField] private GameObject canvasObject;
        [SerializeField] private Text txtQuantity;
        [SerializeField] private GameObject txtAD;
        [SerializeField] private GameObject slotLock; // Lock icon
        [SerializeField] private Button unlockButton; // Button để unlock slot

        // Auto fire variables - replaced Coroutine with state-based system
        private float lastFireTime = 0f;
        private BodySegment currentTarget;
        private bool isAutoFiring = false;

        private void Awake()
        {
            // Auto-find UI references if not assigned
            FindUIReferences();
            
            // Setup unlock button listener
            if (unlockButton != null)
            {
                unlockButton.onClick.AddListener(ShowUnlockPanel);
            }
        }

        /// <summary>
        /// Auto-find UI references based on hierarchy structure
        /// </summary>
        private void FindUIReferences()
        {
            // Find slotLock (direct child)
            if (slotLock == null)
            {
                slotLock = transform.Find("slotLock")?.gameObject;
                //if (slotLock != null)
                   // Debug.Log($"CannonSlot {name}: Auto-found slotLock");
            }

            // Find Canvas and UI elements inside
            Transform canvasTransform = transform.Find("Canvas");
            if (canvasTransform != null)
            {
                // Find txtQuantity
                if (txtQuantity == null)
                {
                    txtQuantity = canvasTransform.Find("txtQuantity")?.GetComponent<Text>();
                   // if (txtQuantity != null)
                  //      Debug.Log($"CannonSlot {name}: Auto-found txtQuantity");
                }

                // Find txtAD
                if (txtAD == null)
                {
                    txtAD = canvasTransform.Find("txtAD")?.gameObject;
                   // if (txtAD != null)
                    //    Debug.Log($"CannonSlot {name}: Auto-found txtAD");
                }

                // Find unlockButton (Button)
                if (unlockButton == null)
                {
                    unlockButton = canvasTransform.Find("Button")?.GetComponent<Button>();
                   // if (unlockButton != null)
                    //    Debug.Log($"CannonSlot {name}: Auto-found unlockButton");
                }

                // Find canvasObject if not found
                if (canvasObject == null)
                {
                    canvasObject = canvasTransform.gameObject;
                    //if (canvasObject != null)
                      //  Debug.Log($"CannonSlot {name}: Auto-found canvasObject");
                }
            }
            else
            {
                //Debug.LogWarning($"CannonSlot {name}: Canvas child object not found");
            }
        }

        /// <summary>
        /// Get fire delay from GameConfig
        /// </summary>
        private float GetFireDelay()
        {
            if (GameManager.Instance != null && GameManager.Instance.BoardManager != null && GameManager.Instance.BoardManager.gameConfig != null)
            {
                return GameManager.Instance.BoardManager.gameConfig.cannonFireDelay;
            }
            return 1.0f; // Default value if no GameConfig
        }

        private void Update()
        {
            // Handle auto fire logic in Update - stop when game state is GameOver
            if (state == CannonSlotState.READY && isAutoFiring && cannonAmmoCount > 0 && 
                GameManager.Instance?.CurrentGameState != GameState.GameOver)
            {
                //Debug.Log($"Auto fire for {name}");
                HandleAutoFire();
            }
        }

        /// <summary>
        /// Handle auto fire logic in Update
        /// </summary>
        private void HandleAutoFire()
        {
            // Check if enough time has passed since last fire
            if (Time.time - lastFireTime < GetFireDelay())
            {
                return;
            }

            // Find target with matching colorIndex
            BodySegment target = FindTargetWithMatchingColor();

            if (target != null)
            {
                currentTarget = target;
                target.isTarget = true;

                // Rotate barrel towards target
                RotateBarrelTowardsTarget(target);

                // Play fire animation
                PlayBarrelFireAnimation();
                // Spawn bullet towards target
                SpawnBulletTowardsTarget(target);
                // Decrease ammo count
                cannonAmmoCount--;
                //Debug.Log($"Spawned bullet towards target {target.name} : " + cannonAmmoCount);
                UpdateAmmoUI();

                // Update last fire time
                lastFireTime = Time.time;

                // Check if out of ammo
                if (cannonAmmoCount <= 0)
                {
                    // Hide barrel
                    if (barrelAnimation != null && barrelAnimation.gameObject != null)
                        barrelAnimation.gameObject.SetActive(false);

                    // Hide AmmoUI
                    if (canvasObject != null)
                        canvasObject.SetActive(false);

                    // Set state to EMPTY
                    SetState(CannonSlotState.EMPTY);
                    return;
                }
            }
            else
            {
                currentTarget = null;
            }
        }

        /// <summary>
        /// Set the state of the CannonSlot.
        /// </summary>
        public void SetState(CannonSlotState newState)
        {
            state = newState;

            // Start/stop auto fire based on state
            if (newState == CannonSlotState.READY)
            {
                StartAutoFire();
            }
            else
            {
                StopAutoFire();
            }

            // Update visuals based on state
            UpdateVisualsForState(newState);
        }

        /// <summary>
        /// Start auto fire
        /// </summary>
        private void StartAutoFire()
        {
            isAutoFiring = true;
            lastFireTime = 0f; // Reset fire timer
        }

        /// <summary>
        /// Stop auto fire
        /// </summary>
        private void StopAutoFire()
        {
            isAutoFiring = false;
            currentTarget = null;
        }

        /// <summary>
        /// Update visuals based on state
        /// </summary>
        private void UpdateVisualsForState(CannonSlotState newState)
        {
            switch (newState)
            {
                case CannonSlotState.LOCK:
                    LockSlot();
                    break;
                case CannonSlotState.EMPTY:
                    EmptySlot();
                    break;
                case CannonSlotState.BOOKED:
                case CannonSlotState.READY:
                    UnlockSlot();
                    break;
            }
        }

        /// <summary>
        /// Lock the slot - show lock icon and unlock button, hide AD text
        /// </summary>
        private void LockSlot()
        {
            // Show lock icon
            if (slotLock != null)
                slotLock.SetActive(true);

            // Show unlock button
            if (unlockButton != null)
                unlockButton.gameObject.SetActive(true);

            // Hide AD text
            if (txtAD != null)
                txtAD.SetActive(true);

            // Hide ammo UI
            if (canvasObject != null)
                canvasObject.SetActive(true);
            if (txtQuantity != null)
                txtQuantity.gameObject.SetActive(false);

            // Hide barrel and box animations
            if (barrelAnimation != null && barrelAnimation.gameObject != null)
                barrelAnimation.gameObject.SetActive(false);
            if (boxAnimation != null && boxAnimation.gameObject != null)
                boxAnimation.gameObject.SetActive(false);
        }

        /// <summary>
        /// Empty slot - hide lock icon and unlock button, show AD text, hide ammo UI
        /// </summary>
        private void EmptySlot()
        {
            // Hide lock icon
            if (slotLock != null)
                slotLock.SetActive(false);

            // Hide unlock button
            if (unlockButton != null)
                unlockButton.gameObject.SetActive(false);

            // Show AD text
            if (txtAD != null)
                txtAD.SetActive(true);

            // Hide ammo UI (will be shown when cannon is activated)
            if (canvasObject != null)
                canvasObject.SetActive(false);
            if (txtQuantity != null)
                txtQuantity.gameObject.SetActive(false);

            // Hide barrel and box animations
            if (barrelAnimation != null && barrelAnimation.gameObject != null)
                barrelAnimation.gameObject.SetActive(false);
            if (boxAnimation != null && boxAnimation.gameObject != null)
                boxAnimation.gameObject.SetActive(false);
        }

        /// <summary>
        /// Unlock the slot - hide lock icon and unlock button, show AD text
        /// </summary>
        private void UnlockSlot()
        {
            // Hide lock icon
            if (slotLock != null)
                slotLock.SetActive(false);

            // Hide unlock button
            if (unlockButton != null)
                unlockButton.gameObject.SetActive(false);

            // Show AD text
            if (txtAD != null)
                txtAD.SetActive(false);

            // Hide ammo UI initially (will be shown when cannon is activated)
            if (canvasObject != null)
                canvasObject.SetActive(true);
            if (txtQuantity != null)
                txtQuantity.gameObject.SetActive(true);
        }

        /// <summary>
        /// Show unlock panel - called by unlock button
        /// </summary>
        public void ShowUnlockPanel()
        {
            //Debug.Log($"ShowUnlockPanel called for {name}");
            
            // Get slot index from name (CannonSlot0 -> 0, CannonSlot1 -> 1, etc.)
            int slotIndex = GetSlotIndexFromName();
            
            // Show Unlock Slot UI through UIManager
            if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            {
                GameManager.Instance.UIManager.ShowUnlockSlotUI(slotIndex, 
                    (unlockedSlotIndex) => {
                        // Callback when unlock successful
                        //Debug.Log($"Slot {unlockedSlotIndex} has been successfully unlocked!");
                        UnlockSlotManually();
                    }, 
                    () => {
                        // Callback when unlock failed or user cancelled
                       // Debug.Log($"Unlock slot {slotIndex} failed or user cancelled");
                    });
            }
            else
            {
                //Debug.LogError("GameManager or UIManager does not exist!");
            }
        }
        
        /// <summary>
        /// Get slot index from GameObject name
        /// </summary>
        /// <returns>Slot index (0-based)</returns>
        private int GetSlotIndexFromName()
        {
            // Find number at the end of name (CannonSlot0 -> 0, CannonSlot1 -> 1, etc.)
            string slotName = name;
            int lastDigitIndex = -1;
            
            for (int i = slotName.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(slotName[i]))
                {
                    lastDigitIndex = i;
                }
                else if (lastDigitIndex != -1)
                {
                    break;
                }
            }
            
            if (lastDigitIndex != -1)
            {
                string numberStr = slotName.Substring(lastDigitIndex);
                if (int.TryParse(numberStr, out int slotIndex))
                {
                    return slotIndex;
                }
            }
            
            // Fallback: find index in cannon slots list
            if (LevelObject.Instance != null)
            {
                return LevelObject.Instance.cannonSlots.IndexOf(this);
            }
            
            return 0; // Default fallback
        }

        /// <summary>
        /// Setup lock/unlock state based on current level and slot index
        /// </summary>
        /// <param name="slotIndex">Index of this slot (0-based)</param>
        /// <param name="totalSlots">Total number of slots</param>
        public void SetupLockState(int slotIndex, int totalSlots)
        {
            // Check if slot has been manually unlocked
            if (IsSlotManuallyUnlocked(slotIndex))
            {
                SetState(CannonSlotState.EMPTY);
                //Debug.Log($"Slot {name} (index {slotIndex}) is MANUALLY UNLOCKED");
                return;
            }
            
            int currentLevel = GetCurrentLevel();
            int lockedSlotsCount = GetLockedSlotsCount(currentLevel);
            
            // Calculate if this slot should be locked
            bool shouldBeLocked = slotIndex >= (totalSlots - lockedSlotsCount);
            
            if (shouldBeLocked)
            {
                SetState(CannonSlotState.LOCK);
                //Debug.Log($"Slot {name} (index {slotIndex}) is LOCKED for level {currentLevel}");
            }
            else
            {
                SetState(CannonSlotState.EMPTY);
               // Debug.Log($"Slot {name} (index {slotIndex}) is UNLOCKED for level {currentLevel}");
            }
        }
        
        /// <summary>
        /// Check if slot has been manually unlocked for current level
        /// </summary>
        /// <param name="slotIndex">Index of slot to check</param>
        /// <returns>True if manually unlocked</returns>
        private bool IsSlotManuallyUnlocked(int slotIndex)
        {
            if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            {
                return GameManager.Instance.UIManager.IsSlotUnlocked(slotIndex);
            }
            return false;
        }

        /// <summary>
        /// Get current level from GameManager
        /// </summary>
        private int GetCurrentLevel()
        {
            if (GameManager.Instance != null)
            {
                return GameManager.Instance.CurrentLevelIndex;
            }
            return 1; // Default to level 1
        }

        /// <summary>
        /// Get number of locked slots based on level
        /// </summary>
        private int GetLockedSlotsCount(int level)
        {
            if (level >= 1 && level <= 5)
            {
                return 1; // Lock 1 last slot
            }
            else if (level >= 6 && level <= 20)
            {
                return 2; // Lock 2 last slots
            }
            else
            {
                return 3; // Lock 3 last slots
            }
        }

        /// <summary>
        /// Unlock this slot manually (called after successful unlock)
        /// </summary>
        public void UnlockSlotManually()
        {
            SetState(CannonSlotState.EMPTY);
            Debug.Log($"Slot {name} has been manually unlocked");
        }

        /// <summary>
        /// Find target with matching colorIndex from targets in range
        /// </summary>
        private BodySegment FindTargetWithMatchingColor()
        {
            var targetsInRange = LevelObject.Instance.GetTargetsInRange();

            foreach (var target in targetsInRange)
            {
                if (target != null && target.Type == BodySegment.SegmentType.BODY && !target.isTarget)
                {
                    // Get colorIndex from target (you may need to implement this in BodySegment)
                    int targetColorIndex = GetTargetColorIndex(target);

                    if (targetColorIndex == cannonColorIndex)
                    {
                        return target;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get colorIndex from target BodySegment
        /// </summary>
        private int GetTargetColorIndex(BodySegment target)
        {
            if (target == null) return -1;

            // Get colorIndex from BodySegment's ColorIndex property
            return target.ColorIndex;
        }

        /// <summary>
        /// Rotate barrel object towards target
        /// </summary>
        private void RotateBarrelTowardsTarget(BodySegment target)
        {
            if (barrelAnimation.gameObject == null || target == null) return;

            Vector3 direction = (target.transform.position - barrelAnimation.gameObject.transform.position).normalized;
            // Default direction is Vector3.up, so need to subtract 90 degrees to align sprite correctly
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            barrelAnimation.gameObject.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        /// <summary>
        /// Spawn bullet towards target
        /// </summary>
        private void SpawnBulletTowardsTarget(BodySegment target)
        {
            spawnPoint = barrelAnimation.gameObject.transform;
            if (bulletPrefab == null || spawnPoint == null || target == null) return;
            SoundManager.Instance.PlaySFX(SoundManager.Instance.shootClip);
            // Spawn bullet at spawn point
            GameObject bullet = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
            // Calculate direction to target
            Vector3 direction = (target.transform.position - spawnPoint.position).normalized;

            // Use CannonArrow instead of Bullet
            CannonArrow arrowScript = bullet.GetComponent<CannonArrow>();
            if (arrowScript != null)
            {
                arrowScript.SetArrowSprite(cannonColorIndex);
                arrowScript.colorIndex = cannonColorIndex;
                arrowScript.currentTarget = target;
            }
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.velocity = direction * 20.0f; // Adjust speed as needed
            }
        }

        /// <summary>
        /// Update ammo UI display
        /// </summary>
        private void UpdateAmmoUI()
        {
            if (txtQuantity != null)
            {
                txtQuantity.text = $"{cannonAmmoCount}/{maxAmmoCount}";
            }

            // Hide UI if no ammo left
            if (cannonAmmoCount <= 0 && canvasObject != null)
            {
                canvasObject.SetActive(false);
            }
        }

        /// <summary>
        /// Play the box opening animation.
        /// </summary>
        public void PlayBoxOpenAnimation()
        {
            if (boxAnimation != null && !string.IsNullOrEmpty(boxOpenAnimationName))
            {
                boxAnimation.gameObject.SetActive(true);
                boxAnimation.AnimationState.SetAnimation(0, boxOpenAnimationName, false);
            }
        }

        /// <summary>
        /// Play the barrel idle animation.
        /// </summary>
        public void PlayBarrelIdleAnimation()
        {
            if (barrelAnimation != null && !string.IsNullOrEmpty(barrelIdleAnimationName))
            {
                barrelAnimation.AnimationState.SetAnimation(0, barrelIdleAnimationName, true);
                SetState(CannonSlotState.READY);
            }
        }


        /// <summary>
        /// Play the barrel fire animation.
        /// </summary>
        public void PlayBarrelFireAnimation()
        {
            if (barrelAnimation != null && !string.IsNullOrEmpty(barrelFireAnimationName))
            {
                barrelAnimation.AnimationState.SetAnimation(0, barrelFireAnimationName, false);
            }
        }

        /// <summary>
        /// Start the complete process: play box animation, then spawn cannon
        /// </summary>
        public void StartCannonActivation(int colorIndex = 0, int ammoCount = 1)
        {
            // Store the color index and ammo count
            cannonColorIndex = colorIndex;
            cannonAmmoCount = ammoCount;
            maxAmmoCount = ammoCount; // Set max ammo count
            Debug.Log($"CannonSlot {name}: Received colorIndex {colorIndex}, ammoCount {ammoCount}");

            // Set skins based on colorIndex
            SetSpineSkinsByColorIndex(colorIndex);

            // Set state to BOOKED first
            SetState(CannonSlotState.BOOKED);

            // Play box opening animation
            PlayBoxOpenAnimation();

            // Show UI
            ShowAmmoUI();

            // Start coroutine to spawn cannon after animation
            StartCoroutine(SpawnCannonAfterAnimation());
        }

        private void ShowAmmoUI()
        {
            if (canvasObject != null)
                canvasObject.SetActive(true);

            if (txtQuantity != null)
            {
                txtQuantity.gameObject.SetActive(true);
                txtQuantity.text = $"{cannonAmmoCount}/{maxAmmoCount}";
            }

            if (txtAD != null)
                txtAD.SetActive(false);
        }

        /// <summary>
        /// Spawn a new Cannon at the slot's spawn point.
        /// </summary>
        public void SpawnCannon()
        {
            barrelAnimation.gameObject.SetActive(true);
            // Play barrel idle animation after spawning
            PlayBarrelIdleAnimation();
        }

        /// <summary>
        /// Coroutine to spawn cannon after box opening animation completes
        /// </summary>
        private System.Collections.IEnumerator SpawnCannonAfterAnimation()
        {
            // Wait for box opening animation to complete (adjust time as needed)
            yield return new WaitForSeconds(0.5f);

            // Spawn the cannon
            SpawnCannon();
        }

        /// <summary>
        /// Get the current spawned Cannon instance.
        /// </summary>
        public GameObject GetCurrentCannon()
        {
            return currentCannon;
        }

        /// <summary>
        /// Get the color index of the cannon in this slot
        /// </summary>
        public int GetCannonColorIndex()
        {
            return cannonColorIndex;
        }

        /// <summary>
        /// Set the color index of the cannon in this slot
        /// </summary>
        public void SetCannonColorIndex(int colorIndex)
        {
            cannonColorIndex = colorIndex;
        }

        /// <summary>
        /// Get the ammo count of the cannon in this slot
        /// </summary>
        public int GetCannonAmmoCount()
        {
            return cannonAmmoCount;
        }

        /// <summary>
        /// Check if cannon can fire (has ammo and is ready)
        /// </summary>
        public bool CanFire()
        {
            return state == CannonSlotState.READY && cannonAmmoCount > 0;
        }

        /// <summary>
        /// Set the ammo count of the cannon in this slot
        /// </summary>
        public void SetCannonAmmoCount(int ammoCount)
        {
            cannonAmmoCount = ammoCount;
            UpdateAmmoUI();
        }

        /// <summary>
        /// Set the max ammo count of the cannon in this slot
        /// </summary>
        public void SetMaxAmmoCount(int maxAmmo)
        {
            maxAmmoCount = maxAmmo;
            UpdateAmmoUI();
        }

        /// <summary>
        /// Get the max ammo count of the cannon in this slot
        /// </summary>
        public int GetMaxAmmoCount()
        {
            return maxAmmoCount;
        }

        /// <summary>
        /// Get ammo info as string (current/max)
        /// </summary>
        public string GetAmmoInfo()
        {
            return $"{cannonAmmoCount}/{maxAmmoCount}";
        }

        /// <summary>
        /// Reset ammo count to initial value and show UI
        /// </summary>
        public void ResetAmmoCount()
        {
            cannonAmmoCount = 1; // Reset to initial value
            maxAmmoCount = 1; // Reset max ammo count
            if (canvasObject != null)
                canvasObject.SetActive(true);
            UpdateAmmoUI();
        }

        /// <summary>
        /// Set Spine skins based on colorIndex
        /// </summary>
        public void SetSpineSkinsByColorIndex(int colorIndex)
        {
            string skinName = GetSkinNameByColorIndex(colorIndex);
            Debug.Log($"CannonSlot {name}: Setting skin '{skinName}' for colorIndex {colorIndex}");

            // Set skin for box animation
            if (boxAnimation != null && boxAnimation.Skeleton != null)
            {
                try
                {
                    boxAnimation.Skeleton.SetSkin(skinName);
                    boxAnimation.Skeleton.SetToSetupPose();
                    Debug.Log($"Box animation skin set to: {skinName}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to set box animation skin '{skinName}': {e.Message}");
                }
            }

            // Set skin for barrel animation
            if (barrelAnimation != null && barrelAnimation.Skeleton != null)
            {
                try
                {
                    barrelAnimation.Skeleton.SetSkin(skinName);
                    barrelAnimation.Skeleton.SetToSetupPose();
                    Debug.Log($"Barrel animation skin set to: {skinName}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to set barrel animation skin '{skinName}': {e.Message}");
                }
            }
        }

        /// <summary>
        /// Get skin name based on colorIndex
        /// </summary>
        private string GetSkinNameByColorIndex(int colorIndex)
        {
            switch (colorIndex)
            {
                case 1: return "1";
                case 2: return "2";
                case 3: return "3";
                case 4: return "4";
                case 5: return "5";
                case 6: return "6";
                case 7: return "7";
                case 8: return "8";
                case 9: return "9";
                case 0: return "0";
                default: return "1";
            }
        }

        [ContextMenu("Test StartCannonActivation")]
        public void TestStartCannonActivation()
        {
            StartCannonActivation(1, 3); // Test with colorIndex = 1, ammoCount = 3
        }

        [ContextMenu("Test StartCannonActivation with ColorIndex 2")]
        public void TestStartCannonActivationColor2()
        {
            StartCannonActivation(2, 5); // Test with colorIndex = 2, ammoCount = 5
        }

        [ContextMenu("Test StartCannonActivation with High Ammo")]
        public void TestStartCannonActivationHighAmmo()
        {
            StartCannonActivation(3, 10); // Test with colorIndex = 3, ammoCount = 10
        }

        [ContextMenu("Test Set Skin Red")]
        public void TestSetSkinRed()
        {
            SetSpineSkinsByColorIndex(1);
        }

        [ContextMenu("Test Set Skin Blue")]
        public void TestSetSkinBlue()
        {
            SetSpineSkinsByColorIndex(2);
        }

        [ContextMenu("Test Set Skin Green")]
        public void TestSetSkinGreen()
        {
            SetSpineSkinsByColorIndex(3);
        }

        [ContextMenu("Test Auto Fire")]
        public void TestAutoFire()
        {
            if (state == CannonSlotState.READY)
            {
                Debug.Log($"Testing auto fire for {name} with colorIndex {cannonColorIndex}");
                StartAutoFire();
            }
            else
            {
                Debug.LogWarning($"Cannot test auto fire: state is {state}, need to be READY");
            }
        }

        [ContextMenu("Stop Auto Fire")]
        public void TestStopAutoFire()
        {
            Debug.Log($"Stopping auto fire for {name}");
            StopAutoFire();
        }

        [ContextMenu("Reset Barrel Rotation")]
        public void TestResetBarrelRotation()
        {
            ResetBarrelRotation();
        }

        [ContextMenu("Test Lock Slot")]
        public void TestLockSlot()
        {
            SetState(CannonSlotState.LOCK);
        }

        [ContextMenu("Test Unlock Slot")]
        public void TestUnlockSlot()
        {
            SetState(CannonSlotState.EMPTY);
        }

        [ContextMenu("Test Empty Slot")]
        public void TestEmptySlot()
        {
            SetState(CannonSlotState.EMPTY);
        }

        [ContextMenu("Test Setup Lock State")]
        public void TestSetupLockState()
        {
            // Test with slot index 2 in total 4 slots
            SetupLockState(2, 4);
        }

        [ContextMenu("Test Show Unlock Panel")]
        public void TestShowUnlockPanel()
        {
            ShowUnlockPanel();
        }

        [ContextMenu("Find UI References")]
        public void TestFindUIReferences()
        {
            Debug.Log($"=== Finding UI References for {name} ===");
            FindUIReferences();
            
            // Log results
            Debug.Log($"slotLock: {(slotLock != null ? "Found" : "Not Found")}");
            Debug.Log($"canvasObject: {(canvasObject != null ? "Found" : "Not Found")}");
            Debug.Log($"txtQuantity: {(txtQuantity != null ? "Found" : "Not Found")}");
            Debug.Log($"txtAD: {(txtAD != null ? "Found" : "Not Found")}");
            Debug.Log($"unlockButton: {(unlockButton != null ? "Found" : "Not Found")}");
        }

        /// <summary>
        /// Get current auto fire status
        /// </summary>
        public bool IsAutoFiring()
        {
            return isAutoFiring;
        }

        /// <summary>
        /// Get time since last fire
        /// </summary>
        public float GetTimeSinceLastFire()
        {
            return Time.time - lastFireTime;
        }

        /// <summary>
        /// Reset barrel rotation to initial direction (Vector3.Up)
        /// </summary>
        public void ResetBarrelRotation()
        {
            if (barrelAnimation != null && barrelAnimation.gameObject != null)
            {
                barrelAnimation.gameObject.transform.rotation = Quaternion.identity;
                Debug.Log($"Barrel rotation reset to identity for {name}");
            }
        }

        /// <summary>
        /// Test barrel rotation towards a specific target
        /// </summary>
        public void TestBarrelRotation(BodySegment testTarget)
        {
            if (testTarget != null)
            {
                Debug.Log($"Testing barrel rotation towards {testTarget.name}");
                RotateBarrelTowardsTarget(testTarget);
            }
            else
            {
                Debug.LogWarning("Test target is null");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var segment = other.GetComponent<BodySegment>();
            if (segment != null && segment.Type == BodySegment.SegmentType.BODY)
            {
                // Debug.Log($"Body segment {segment.name} entered range of {name}");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var segment = other.GetComponent<BodySegment>();
            if (segment != null)
            {
                // Debug.Log($"Body segment {segment.name} exited range of {name}");
            }
        }
    }
}