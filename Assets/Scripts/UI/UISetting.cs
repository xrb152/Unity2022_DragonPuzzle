using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RescuePrincess.Managers;
using RescuePrincess.Utils;
using Lofelt.NiceVibrations;

public class UISetting : UIPanel
{
    public enum SettingType
    {
        Setting_Home,
        Setting_Game
    }

    [Header("Setting Type")]
    [SerializeField] private SettingType currentType = SettingType.Setting_Home;

    [Header("Setting UI Elements")]
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnHome;
    [SerializeField] private Button btnRestart;

    [Header("Toggle Buttons")]
    [SerializeField] private UIToggleButton musicToggle;
    [SerializeField] private UIToggleButton soundToggle;
    [SerializeField] private UIToggleButton vibrationToggle;

    [Header("Animation Settings")]
    [SerializeField] private float showAnimationDuration = 0.3f;
    [SerializeField] private float hideAnimationDuration = 0.2f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;
    [SerializeField] private RectTransform rootTransform;

    // Setting states
    private bool isMusicOn = true;
    private bool isSoundOn = true;
    private bool isVibrationOn = true;

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
        LoadSettings();
        UpdateUIByType();
    }

    private void SetupButtons()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(OnCloseButtonClicked);

        if (btnHome != null)
            btnHome.onClick.AddListener(OnHomeButtonClicked);

        if (btnRestart != null)
            btnRestart.onClick.AddListener(OnRestartButtonClicked);

        // Setup toggle buttons
        if (musicToggle != null)
        {
            musicToggle.OnToggleChanged += OnMusicToggleChanged;
        }

        if (soundToggle != null)
        {
            soundToggle.OnToggleChanged += OnSoundToggleChanged;
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.OnToggleChanged += OnVibrationToggleChanged;
        }
    }

    public override void Show()
    {
        base.Show();
        UpdateUIByType();
        RefreshSettingsUI(); // Đảm bảo UI sync với SoundManager
        PlayShowAnimation();
    }

    public void Show(SettingType type)
    {
        currentType = type;
        Show();
    }

    public override void Hide()
    {
        // Resume game nếu đang ở Setting_Game
        if (currentType == SettingType.Setting_Game)
        {
            ResumeGame();
        }

        PlayHideAnimation();
    }

    private void PlayShowAnimation()
    {
        // Reset scale for animation
        rootTransform.localScale = Vector3.zero;

        // Animate scale with DOTween
        rootTransform.DOScale(Vector3.one, showAnimationDuration)
            .SetEase(showEase)
            .SetUpdate(true) // Không bị ảnh hưởng bởi Time.timeScale
            .OnComplete(() =>
            {
                // Animation complete - Pause game sau khi animation hoàn thành
                if (currentType == SettingType.Setting_Game)
                {
                    PauseGame();
                }
            });
    }

    private void PlayHideAnimation()
    {
        rootTransform.DOScale(Vector3.zero, hideAnimationDuration)
            .SetEase(hideEase)
            .SetUpdate(true) // Không bị ảnh hưởng bởi Time.timeScale
            .OnComplete(() =>
            {
                base.Hide();
            });
    }

    private void OnCloseButtonClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
        }
        Hide();
    }

    private void OnHomeButtonClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
        }
        Debug.Log("Home button clicked - Return to main menu");

        // Hide settings panel first
        Hide();

        // Return to main menu (đã bao gồm clean level)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
        else
        {
            Debug.LogError("GameManager không tồn tại!");
        }
    }

    private void OnRestartButtonClicked()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
        }
        Debug.Log("Restart button clicked - Restart current level");

        // Hide settings panel first
        Hide();

        // Restart current level
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("GameManager không tồn tại!");
        }
    }

    private void UpdateUIByType()
    {
        switch (currentType)
        {
            case SettingType.Setting_Home:
                // Setting_Home: Ẩn 2 nút Home và Restart
                if (btnHome != null)
                    btnHome.gameObject.SetActive(false);

                if (btnRestart != null)
                    btnRestart.gameObject.SetActive(false);
                break;

            case SettingType.Setting_Game:
                // Setting_Game: Hiển thị 2 nút Home và Restart
                if (btnHome != null)
                    btnHome.gameObject.SetActive(true);

                if (btnRestart != null)
                    btnRestart.gameObject.SetActive(true);
                break;
        }
    }

    public void SetSettingType(SettingType type)
    {
        currentType = type;
        UpdateUIByType();
    }

    /// <summary>
    /// Refresh UI to match current settings from SoundManager
    /// </summary>
    public void RefreshSettingsUI()
    {
        LoadSettings();
    }

    /// <summary>
    /// Get current sound state from SoundManager
    /// </summary>
    public bool GetCurrentSoundState()
    {
        if (SoundManager.Instance != null)
        {
            return SoundManager.Instance.IsSoundOn();
        }
        return isSoundOn; // Fallback to local state
    }

    /// <summary>
    /// Get current music state from SoundManager
    /// </summary>
    public bool GetCurrentMusicState()
    {
        if (SoundManager.Instance != null)
        {
            return SoundManager.Instance.IsMusicOn();
        }
        return isMusicOn; // Fallback to local state
    }

    /// <summary>
    /// Get current vibration state from SoundManager
    /// </summary>
    public bool GetCurrentVibrationState()
    {
        if (SoundManager.Instance != null)
        {
            return SoundManager.Instance.IsVibrationOn();
        }
        return isVibrationOn; // Fallback to local state
    }

    /// <summary>
    /// Force sync settings with SoundManager
    /// </summary>
    public void ForceSyncWithSoundManager()
    {
        if (SoundManager.Instance != null)
        {
            // Sync local state with SoundManager
            isMusicOn = SoundManager.Instance.IsMusicOn();
            isSoundOn = SoundManager.Instance.IsSoundOn();
            isVibrationOn = SoundManager.Instance.IsVibrationOn();
            
            // Update UI
            RefreshSettingsUI();
            
            Debug.Log("Settings synced with SoundManager");
        }
    }

    /// <summary>
    /// Debug method để kiểm tra trạng thái các toggle button
    /// </summary>
    [ContextMenu("Debug Toggle Buttons")]
    public void DebugToggleButtons()
    {
        Debug.Log("=== Debug Toggle Buttons ===");
        
        if (musicToggle != null)
        {
            Debug.Log($"Music Toggle: {musicToggle.name} - Message: {musicToggle.GetToggleState()}");
        }
        else
        {
            Debug.LogError("Music Toggle is null!");
        }
        
        if (soundToggle != null)
        {
            Debug.Log($"Sound Toggle: {soundToggle.name} - Message: {soundToggle.GetToggleState()}");
        }
        else
        {
            Debug.LogError("Sound Toggle is null!");
        }
        
        if (vibrationToggle != null)
        {
            Debug.Log($"Vibration Toggle: {vibrationToggle.name} - Message: {vibrationToggle.GetToggleState()}");
        }
        else
        {
            Debug.LogError("Vibration Toggle is null!");
        }
        
        Debug.Log("=== End Debug ===");
    }

    /// <summary>
    /// Kiểm tra và fix assignment của toggle buttons
    /// </summary>
    [ContextMenu("Check Toggle Button Assignment")]
    public void CheckToggleButtonAssignment()
    {
        Debug.Log("=== Check Toggle Button Assignment ===");
        
        // Kiểm tra xem có toggle nào bị null không
        if (musicToggle == null)
        {
            Debug.LogError("❌ Music Toggle is NULL!");
        }
        else
        {
            Debug.Log($"✅ Music Toggle: {musicToggle.name}");
        }
        
        if (soundToggle == null)
        {
            Debug.LogError("❌ Sound Toggle is NULL!");
        }
        else
        {
            Debug.Log($"✅ Sound Toggle: {soundToggle.name}");
        }
        
        if (vibrationToggle == null)
        {
            Debug.LogError("❌ Vibration Toggle is NULL!");
        }
        else
        {
            Debug.Log($"✅ Vibration Toggle: {vibrationToggle.name}");
        }
        
        // Kiểm tra xem có toggle nào bị duplicate assignment không
        if (musicToggle != null && soundToggle != null && musicToggle == soundToggle)
        {
            Debug.LogError("❌ Music Toggle và Sound Toggle đang trỏ đến cùng một object!");
        }
        
        if (soundToggle != null && vibrationToggle != null && soundToggle == vibrationToggle)
        {
            Debug.LogError("❌ Sound Toggle và Vibration Toggle đang trỏ đến cùng một object!");
        }
        
        if (musicToggle != null && vibrationToggle != null && musicToggle == vibrationToggle)
        {
            Debug.LogError("❌ Music Toggle và Vibration Toggle đang trỏ đến cùng một object!");
        }
        
        Debug.Log("=== End Check ===");
    }

    /// <summary>
    /// Swap assignment giữa Sound Toggle và Vibration Toggle nếu bị nhầm
    /// </summary>
    [ContextMenu("Swap Sound and Vibration Toggle")]
    public void SwapSoundAndVibrationToggle()
    {
        Debug.Log("=== Swap Sound and Vibration Toggle ===");
        
        if (soundToggle != null && vibrationToggle != null)
        {
            // Swap references
            var temp = soundToggle;
            soundToggle = vibrationToggle;
            vibrationToggle = temp;
            
            // Re-setup event listeners
            if (soundToggle != null)
            {
                soundToggle.OnToggleChanged -= OnSoundToggleChanged;
                soundToggle.OnToggleChanged += OnSoundToggleChanged;
            }
            
            if (vibrationToggle != null)
            {
                vibrationToggle.OnToggleChanged -= OnVibrationToggleChanged;
                vibrationToggle.OnToggleChanged += OnVibrationToggleChanged;
            }
            
            Debug.Log("✅ Swapped Sound and Vibration Toggle successfully!");
        }
        else
        {
            Debug.LogError("❌ Cannot swap: One or both toggles are null!");
        }
        
        Debug.Log("=== End Swap ===");
    }

    private void PauseGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
            Debug.Log("Game đã được pause khi mở Setting UI");
        }
    }

    private void ResumeGame()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
            Debug.Log("Game đã được resume khi đóng Setting UI");
        }
    }

    private void OnMusicToggleChanged(bool isOn)
    {
        isMusicOn = isOn;
        
        // Sử dụng SoundManager để quản lý music
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMusicState(isOn);
        }
        
        Debug.Log($"Music: {(isMusicOn ? "ON" : "OFF")}");
    }

    private void OnSoundToggleChanged(bool isOn)
    {
        isSoundOn = isOn;
        
        // Sử dụng SoundManager để quản lý sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSoundState(isOn);
            
            // Test sound khi bật để user biết sound đã hoạt động
            if (isOn)
            {
                // Delay một chút để đảm bảo sound đã được unmute
                StartCoroutine(TestSoundAfterDelay(0.1f));
            }
        }
        
        Debug.Log($"Sound: {(isSoundOn ? "ON" : "OFF")}");
    }

    private System.Collections.IEnumerator TestSoundAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        if (SoundManager.Instance != null && isSoundOn)
        {
            // Phát test sound với volume thấp hơn
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip, 0.5f);
        }
    }

    private void OnVibrationToggleChanged(bool isOn)
    {
        isVibrationOn = isOn;
        
        // Sử dụng SoundManager để quản lý vibration
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetVibrationState(isOn);
            
            // Test vibration khi bật
            if (isOn)
            {
                VibrationHelper.TriggerVibrationIfEnabled(HapticPatterns.PresetType.MediumImpact);
            }
        }
        
        Debug.Log($"Vibration: {(isVibrationOn ? "ON" : "OFF")}");
    }

    private void LoadSettings()
    {
        // Load settings từ SoundManager thay vì PlayerPrefs trực tiếp
        if (SoundManager.Instance != null)
        {
            isMusicOn = SoundManager.Instance.IsMusicOn();
            isSoundOn = SoundManager.Instance.IsSoundOn();
            isVibrationOn = SoundManager.Instance.IsVibrationOn();
        }
        else
        {
            // Fallback nếu SoundManager chưa sẵn sàng
            isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
            isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
            isVibrationOn = PlayerPrefs.GetInt("VibrationOn", 1) == 1;
        }

        // Update toggle button states
        if (musicToggle != null)
            musicToggle.SetToggleState(isMusicOn, false);

        if (soundToggle != null)
            soundToggle.SetToggleState(isSoundOn, false);

        if (vibrationToggle != null)
            vibrationToggle.SetToggleState(isVibrationOn, false);
    }

    private void SaveSettings()
    {
        // Settings đã được lưu trong SoundManager, không cần lưu lại ở đây
        // Chỉ cần đảm bảo PlayerPrefs được save
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (btnClose != null)
            btnClose.onClick.RemoveListener(OnCloseButtonClicked);

        if (btnHome != null)
            btnHome.onClick.RemoveListener(OnHomeButtonClicked);

        if (btnRestart != null)
            btnRestart.onClick.RemoveListener(OnRestartButtonClicked);

        // Clean up toggle button events
        if (musicToggle != null)
            musicToggle.OnToggleChanged -= OnMusicToggleChanged;

        if (soundToggle != null)
            soundToggle.OnToggleChanged -= OnSoundToggleChanged;

        if (vibrationToggle != null)
            vibrationToggle.OnToggleChanged -= OnVibrationToggleChanged;
    }
}