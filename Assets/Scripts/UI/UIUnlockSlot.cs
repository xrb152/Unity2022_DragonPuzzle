using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RescuePrincess.Managers;

namespace RescuePrincess.UI
{
    public class UIUnlockSlot : UIPanel
    {
        [Header("UI Components")]
        [SerializeField] private Button btnUnlockWithCoins;
        [SerializeField] private Button btnUnlockWithAds;
        [SerializeField] private Button btnClose;

        [Header("Text Components")]
        [SerializeField] private Text txtTitle;
        [SerializeField] private Text txtDescription;
        [SerializeField] private Text txtCoinCost;
        [SerializeField] private Text txtAdsText;

        [Header("Icon Components")]
        [SerializeField] private Image imgCoinIcon;
        [SerializeField] private Image imgAdsIcon;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.3f;
        [SerializeField] private float hideDuration = 0.2f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;
        [SerializeField] private RectTransform rootTransform;

        [Header("Unlock Settings")]
        [SerializeField] private int coinCost = 100;
        [SerializeField] private string unlockSlotKey = "UnlockedSlot_Level_";

        [Header("Notification")]
        [SerializeField] private UINotify notifyPanel; // Fallback nếu không dùng UIManager

        private int targetSlotIndex = -1;
        private System.Action<int> onUnlockSuccess;
        private System.Action onUnlockFailed;

        protected override void Awake()
        {
            base.Awake();
            InitializeUI();
            SetupNotification();

            // Auto-assign rootTransform nếu chưa được assign
            if (rootTransform == null)
            {
                rootTransform = transform as RectTransform;
            }
        }

        private void Start()
        {
            // Ẩn UI ban đầu và reset scale
            Hide();
            if (rootTransform != null)
            {
                rootTransform.localScale = Vector3.zero;
            }
        }

        private void InitializeUI()
        {
            // Setup button listeners
            if (btnUnlockWithCoins != null)
                btnUnlockWithCoins.onClick.AddListener(OnUnlockWithCoinsClicked);
            else
                Debug.LogWarning("btnUnlockWithCoins chưa được assign!");

            if (btnUnlockWithAds != null)
                btnUnlockWithAds.onClick.AddListener(OnUnlockWithAdsClicked);
            else
                Debug.LogWarning("btnUnlockWithAds chưa được assign!");

            if (btnClose != null)
                btnClose.onClick.AddListener(OnCloseClicked);
            else
                Debug.LogWarning("btnClose chưa được assign!");
        }

        /// <summary>
        /// Setup notification system
        /// </summary>
        private void SetupNotification()
        {
            // Ưu tiên sử dụng UINotify từ UIManager thay vì FindObjectOfType
            if (GameManager.Instance?.UIManager?.notifyUI != null)
            {
                notifyPanel = GameManager.Instance.UIManager.notifyUI;
                Debug.Log("Đã sử dụng UINotify từ UIManager");
            }
            else if (notifyPanel == null)
            {
                // Fallback: Tự động tìm UINotify nếu chưa được assign
                notifyPanel = FindObjectOfType<UINotify>();
                if (notifyPanel != null)
                {
                    Debug.Log("Đã tự động tìm thấy UINotify trong scene (fallback)");
                }
                else
                {
                    Debug.LogWarning("Không tìm thấy UINotify! Vui lòng tạo và assign UINotify component hoặc setup trong UIManager.");
                }
            }
        }

        /// <summary>
        /// Lấy UINotify từ UIManager một cách an toàn
        /// </summary>
        /// <returns>UINotify instance hoặc null</returns>
        private UINotify GetNotifyPanel()
        {
            // Ưu tiên sử dụng từ UIManager
            if (GameManager.Instance?.UIManager?.notifyUI != null)
            {
                return GameManager.Instance.UIManager.notifyUI;
            }

            // Fallback về local reference
            return notifyPanel;
        }

        /// <summary>
        /// Hiển thị UI Unlock Slot
        /// </summary>
        /// <param name="slotIndex">Index của slot cần unlock</param>
        /// <param name="onSuccess">Callback khi unlock thành công</param>
        /// <param name="onFailed">Callback khi unlock thất bại</param>
        public void ShowUnlockSlot(int slotIndex, System.Action<int> onSuccess = null, System.Action onFailed = null)
        {
            targetSlotIndex = slotIndex;
            onUnlockSuccess = onSuccess;
            onUnlockFailed = onFailed;

            // Cập nhật UI text
            UpdateUIText();

            // Kiểm tra xem slot đã được unlock chưa cho level hiện tại
            if (IsSlotUnlocked(slotIndex))
            {
                Debug.LogWarning($"Slot {slotIndex} đã được unlock rồi cho level hiện tại!");
                onUnlockFailed?.Invoke();
                return;
            }

            // Hiển thị UI với animation
            ShowWithAnimation();
        }

        /// <summary>
        /// Cập nhật text UI
        /// </summary>
        private void UpdateUIText()
        {
            if (txtTitle != null)
                txtTitle.text = $"解锁槽位 {targetSlotIndex + 1}";

            if (txtDescription != null)
                txtDescription.text = "选择解锁方式：";

            if (txtCoinCost != null)
                txtCoinCost.text = $"{coinCost}";

            if (txtAdsText != null)
                txtAdsText.text = "看广告";
        }

        /// <summary>
        /// Hiển thị UI với animation
        /// </summary>
        private void ShowWithAnimation()
        {
            // Hiển thị UICoin để user thấy số coin hiện tại
            ShowUICoin();

            // Reset scale về 0
            rootTransform.localScale = Vector3.zero;

            // Hiển thị UI
            Show();

            // Animation scale up với DOTween
            rootTransform.DOScale(Vector3.one, showDuration)
                .SetEase(showEase)
                .SetUpdate(true) // Không bị ảnh hưởng bởi Time.timeScale
                .OnComplete(() =>
                {
                    Debug.Log("UI Unlock Slot đã hiển thị với animation");
                });
        }

        /// <summary>
        /// Ẩn UI với animation
        /// </summary>
        private void HideWithAnimation()
        {
            // Animation scale down với DOTween
            rootTransform.DOScale(Vector3.zero, hideDuration)
                .SetEase(hideEase)
                .SetUpdate(true) // Không bị ảnh hưởng bởi Time.timeScale
                .OnComplete(() =>
                {
                    Hide();
                    // Ẩn UICoin khi ẩn UI Unlock Slot
                    HideUICoin();
                    Debug.Log("UI Unlock Slot đã ẩn với animation");
                });
        }

        /// <summary>
        /// Xử lý khi click button unlock bằng coins
        /// </summary>
        private void OnUnlockWithCoinsClicked()
        {
            Debug.Log($"Thử unlock slot {targetSlotIndex} bằng {coinCost} coins");

            // Kiểm tra GameManager
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager không tồn tại!");
                onUnlockFailed?.Invoke();
                return;
            }

            // Kiểm tra đủ coin không
            if (GameManager.Instance.CoinValue < coinCost)
            {
                Debug.LogWarning($"Not enough coins! Need: {coinCost}, Have: {GameManager.Instance.CoinValue}");
                ShowNotEnoughCoinsMessage();
                onUnlockFailed?.Invoke();
                return;
            }

            // Tiêu coin và unlock slot
            bool spendSuccess = GameManager.Instance.SpendCoin(coinCost);
            if (spendSuccess)
            {
                UnlockSlot(targetSlotIndex);
                ShowUnlockSuccessMessage();
                onUnlockSuccess?.Invoke(targetSlotIndex);
                HideWithAnimation();
            }
            else
            {
                Debug.LogError("Không thể tiêu coin!");
                onUnlockFailed?.Invoke();
            }
        }

        /// <summary>
        /// Xử lý khi click button unlock bằng ads
        /// </summary>
        private void OnUnlockWithAdsClicked()
        {
            WatchAds();
        }

        public void WatchAds()
        {
            AdsControl.Instance.ShowRewardedAd(result =>
            {
                if (result != AdsControl.AdResult.COMPLETED)
                {
                    if (result == AdsControl.AdResult.SKIPPED)
                        ShowAdsSkippedMessage();
                    else
                        ShowAdsNotAvailableMessage();
                    return;
                }

                UnlockSlot(targetSlotIndex);
                ShowUnlockSuccessMessage();
                onUnlockSuccess?.Invoke(targetSlotIndex);
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unlockClip);
                HideWithAnimation();
            });
        }

        // 兼容旧调用名
        public void ShowRWUnityAds()
        {
            WatchAds();
        }

        /// <summary>
        /// Xử lý khi click button close
        /// </summary>
        private void OnCloseClicked()
        {
            Debug.Log("User đóng UI Unlock Slot");
            onUnlockFailed?.Invoke();
            HideWithAnimation();
        }

        /// <summary>
        /// Unlock slot và lưu vào PlayerPrefs cho level hiện tại
        /// </summary>
        /// <param name="slotIndex">Index của slot cần unlock</param>
        private void UnlockSlot(int slotIndex)
        {
            int currentLevel = GetCurrentLevel();
            string key = unlockSlotKey + currentLevel + "_Slot_" + slotIndex;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();

            Debug.Log($"Unlocked slot {slotIndex} for level {currentLevel} and saved to PlayerPrefs");
        }

        /// <summary>
        /// Check if slot is unlocked for current level
        /// </summary>
        /// <param name="slotIndex">Index of slot to check</param>
        /// <returns>True if unlocked, false if not</returns>
        public bool IsSlotUnlocked(int slotIndex)
        {
            int currentLevel = GetCurrentLevel();
            string key = unlockSlotKey + currentLevel + "_Slot_" + slotIndex;
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        /// <summary>
        /// Check if slot is unlocked for specific level
        /// </summary>
        /// <param name="levelIndex">Index of level to check</param>
        /// <param name="slotIndex">Index of slot to check</param>
        /// <returns>True if unlocked, false if not</returns>
        public bool IsSlotUnlockedForLevel(int levelIndex, int slotIndex)
        {
            string key = unlockSlotKey + levelIndex + "_Slot_" + slotIndex;
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        /// <summary>
        /// Reset all slots to locked state for current level
        /// </summary>
        public void ResetAllSlots()
        {
            int currentLevel = GetCurrentLevel();
            // Delete all unlock slot keys for current level
            for (int i = 0; i < 10; i++) // Assume maximum 10 slots
            {
                string key = unlockSlotKey + currentLevel + "_Slot_" + i;
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();

            Debug.Log($"Reset all slots to locked state for level {currentLevel}");
        }

        /// <summary>
        /// Reset all slots to locked state for specific level
        /// </summary>
        /// <param name="levelIndex">Index of level to reset</param>
        public void ResetAllSlotsForLevel(int levelIndex)
        {
            // Delete all unlock slot keys for specific level
            for (int i = 0; i < 10; i++) // Assume maximum 10 slots
            {
                string key = unlockSlotKey + levelIndex + "_Slot_" + i;
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();

            Debug.Log($"Reset all slots to locked state for level {levelIndex}");
        }

        /// <summary>
        /// Reset all slots to locked state for all levels
        /// </summary>
        public void ResetAllSlotsForAllLevels()
        {
            // Delete all unlock slot keys for all levels (assume maximum 100 levels)
            for (int level = 1; level <= 100; level++)
            {
                for (int slot = 0; slot < 10; slot++)
                {
                    string key = unlockSlotKey + level + "_Slot_" + slot;
                    PlayerPrefs.DeleteKey(key);
                }
            }
            PlayerPrefs.Save();

            Debug.Log("Reset all slots to locked state for all levels");
        }

        /// <summary>
        /// Get list of unlocked slots for current level
        /// </summary>
        /// <returns>Array containing indices of unlocked slots</returns>
        public int[] GetUnlockedSlotsForCurrentLevel()
        {
            int currentLevel = GetCurrentLevel();
            return GetUnlockedSlotsForLevel(currentLevel);
        }

        /// <summary>
        /// Get list of unlocked slots for specific level
        /// </summary>
        /// <param name="levelIndex">Index of level to check</param>
        /// <returns>Array containing indices of unlocked slots</returns>
        public int[] GetUnlockedSlotsForLevel(int levelIndex)
        {
            System.Collections.Generic.List<int> unlockedSlots = new System.Collections.Generic.List<int>();

            for (int i = 0; i < 10; i++) // Assume maximum 10 slots
            {
                if (IsSlotUnlockedForLevel(levelIndex, i))
                {
                    unlockedSlots.Add(i);
                }
            }

            return unlockedSlots.ToArray();
        }

        /// <summary>
        /// Get number of unlocked slots for current level
        /// </summary>
        /// <returns>Number of unlocked slots</returns>
        public int GetUnlockedSlotsCountForCurrentLevel()
        {
            return GetUnlockedSlotsForCurrentLevel().Length;
        }

        /// <summary>
        /// Get number of unlocked slots for specific level
        /// </summary>
        /// <param name="levelIndex">Index of level to check</param>
        /// <returns>Number of unlocked slots</returns>
        public int GetUnlockedSlotsCountForLevel(int levelIndex)
        {
            return GetUnlockedSlotsForLevel(levelIndex).Length;
        }

        /// <summary>
        /// Get current level from GameManager
        /// </summary>
        /// <returns>Index of current level</returns>
        private int GetCurrentLevel()
        {
            if (GameManager.Instance != null)
            {
                return GameManager.Instance.CurrentLevelIndex;
            }
            return 1; // Default to level 1
        }

        /// <summary>
        /// Show not enough coins message
        /// </summary>
        private void ShowNotEnoughCoinsMessage()
        {
            UINotify notify = GetNotifyPanel();
            if (notify != null)
            {
                string message = "金币不足！";
                notify.ShowNotify(message, true, 3f);
            }
        }

        /// <summary>
        /// Show ads not available message
        /// </summary>
        private void ShowAdsNotAvailableMessage()
        {
            UINotify notify = GetNotifyPanel();
            if (notify != null)
            {
                string message = "广告暂不可用，请稍后再试。";
                notify.ShowNotify(message, true, 2.5f);
                Debug.Log($"Showing notification: {message}");
            }
            else
            {
                Debug.LogWarning("Reward ads not implemented yet! (UINotify not set up)");
            }
        }

        /// <summary>
        /// 激励视频没看完就关了
        /// </summary>
        private void ShowAdsSkippedMessage()
        {
            UINotify notify = GetNotifyPanel();
            if (notify != null)
            {
                string message = "需要完整观看广告才能解锁槽位。";
                notify.ShowNotify(message, true, 2.5f);
                Debug.Log($"Showing notification: {message}");
            }
            else
            {
                Debug.Log("奖励视频未看完，不发放奖励。");
            }
        }

        /// <summary>
        /// Show unlock success message
        /// </summary>
        private void ShowUnlockSuccessMessage()
        {
            UINotify notify = GetNotifyPanel();
            if (notify != null)
            {
                string message = $"已成功解锁槽位 {targetSlotIndex + 1}！";
                notify.ShowNotify(message, true, 2f);
                Debug.Log($"Showing notification: {message}");
                SoundManager.Instance.PlaySFX(SoundManager.Instance.unlockClip);
            }
            else
            {
                Debug.Log($"Successfully unlocked slot {targetSlotIndex}! (UINotify not set up)");
            }
        }

        /// <summary>
        /// Set coin cost for unlock
        /// </summary>
        /// <param name="cost">Number of coins needed to unlock</param>
        public void SetCoinCost(int cost)
        {
            coinCost = Mathf.Max(1, cost);
            UpdateUIText();
        }

        /// <summary>
        /// Get current coin cost
        /// </summary>
        /// <returns>Number of coins needed to unlock</returns>
        public int GetCoinCost()
        {
            return coinCost;
        }

        /// <summary>
        /// Force close UI (use when need to close quickly)
        /// </summary>
        public void ForceClose()
        {
            rootTransform.DOKill(); // Stop all animations
            Hide();
            // Hide UICoin when force close
            HideUICoin();
            onUnlockFailed?.Invoke();
        }

        /// <summary>
        /// Show UICoin so user can see current coin amount
        /// </summary>
        private void ShowUICoin()
        {
            if (GameManager.Instance?.UIManager != null)
            {
                // Show UICoin fixed so user can see current coin amount
                GameManager.Instance.UIManager.ShowCoinUI();
                Debug.Log("Showed UICoin so user can see current coin amount");
            }
        }

        /// <summary>
        /// Hide UICoin
        /// </summary>
        private void HideUICoin()
        {
            if (GameManager.Instance?.UIManager != null)
            {
                GameManager.Instance.UIManager.HideCoinUI();
                Debug.Log("Hidden UICoin");
            }
        }

        /// <summary>
        /// Kiểm tra và fix assignment của rootTransform
        /// </summary>
        [ContextMenu("Check RootTransform Assignment")]
        public void CheckRootTransformAssignment()
        {
            Debug.Log("=== Check RootTransform Assignment ===");

            if (rootTransform == null)
            {
                Debug.LogError("❌ RootTransform is NULL! Auto-assigning...");
                rootTransform = transform as RectTransform;
                if (rootTransform != null)
                {
                    Debug.Log("✅ Auto-assigned RootTransform successfully!");
                }
                else
                {
                    Debug.LogError("❌ Failed to auto-assign RootTransform!");
                }
            }
            else
            {
                Debug.Log($"✅ RootTransform: {rootTransform.name}");
            }

            Debug.Log("=== End Check ===");
        }

        /// <summary>
        /// Test animation show/hide
        /// </summary>
        [ContextMenu("Test Animation")]
        public void TestAnimation()
        {
            Debug.Log("=== Test Animation ===");

            if (rootTransform == null)
            {
                Debug.LogError("❌ RootTransform is null! Cannot test animation.");
                return;
            }

            // Test show animation
            rootTransform.localScale = Vector3.zero;
            rootTransform.DOScale(Vector3.one, showDuration)
                .SetEase(showEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    Debug.Log("✅ Show animation completed!");

                    // Test hide animation after 1 second
                    StartCoroutine(TestHideAnimationAfterDelay(1f));
                });
        }

        private System.Collections.IEnumerator TestHideAnimationAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            rootTransform.DOScale(Vector3.zero, hideDuration)
                .SetEase(hideEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    Debug.Log("✅ Hide animation completed!");
                });
        }

        /// <summary>
        /// Reset animation state
        /// </summary>
        [ContextMenu("Reset Animation State")]
        public void ResetAnimationState()
        {
            if (rootTransform != null)
            {
                rootTransform.DOKill();
                rootTransform.localScale = Vector3.zero;
                Debug.Log("✅ Animation state reset!");
            }
        }
    }
}