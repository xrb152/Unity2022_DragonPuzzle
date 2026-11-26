using UnityEngine;
using UnityEngine.UI;
using RescuePrincess.Managers;

namespace RescuePrincess.UI
{
    public class UIGame : UIPanel
    {
        [Header("Game UI Elements")]
        [SerializeField] private Button buttonSetting;
        [SerializeField] private Button buttonSkip;
        [SerializeField] private Button buttonSpeed;
        
        [Header("Game Info UI")]
        [SerializeField] private Text txtLevel;
        [SerializeField] private Text txtHP;
        [SerializeField] private Image imgHPFill;
        
        [Header("Game Areas")]
        [SerializeField] private GameObject mapArea;
        [SerializeField] private GameObject dragonHPArea;
        [SerializeField] private GameObject forSaveArea;

        [Header("Reward Button")]
        [Tooltip("关卡内「击退怪物」按钮（场景里的 jiliBtn），首页/结算界面需要隐藏")]
        [SerializeField] private GameObject retreatButton;
        [Tooltip("看完一次激励广告让怪物后退几个身位")]
        [SerializeField] private int retreatSteps = 5;

        protected override void Awake()
        {
            base.Awake();
            InitializeUI();
            ResolveRetreatButton();

            // jiliBtn 直接挂在 Canvas 下，不受 UIGameplay 的 CanvasGroup 控制，
            // 所以这里默认关掉，等进入关卡时由 Show() 打开
            if (retreatButton != null)
                retreatButton.SetActive(false);
        }

        /// <summary>
        /// 自动向上层 Canvas 查找 jiliBtn，并绑定激励广告点击
        /// </summary>
        private void ResolveRetreatButton()
        {
            if (transform.parent == null)
                return;

            if (retreatButton == null)
            {
                Transform found = transform.parent.Find("jiliBtn");
                if (found != null)
                {
                    retreatButton = found.gameObject;
                }
                else
                {
                    Debug.LogWarning("[UIGame] 未找到 jiliBtn，击退怪物按钮不会随关卡显隐");
                    return;
                }
            }

            Button btn = retreatButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnRetreatClicked);
                btn.onClick.AddListener(OnRetreatClicked);
            }
        }

        /// <summary>
        /// 点击「击退怪物」：弹广告确认框，看完才退怪
        /// </summary>
        private void OnRetreatClicked()
        {
            if (AdManager.Instance == null)
            {
                Debug.LogError("[击退怪物] AdManager 未初始化，无法播放广告");
                return;
            }

            AdConfirm.Show(OnRetreatRewarded);
        }

        private void OnRetreatRewarded()
        {
            ChainController chain = FindObjectOfType<ChainController>();
            if (chain == null)
            {
                Debug.LogWarning("[击退怪物] 场景里没有 ChainController，无法后退");
                return;
            }

            int moved = chain.PushDragonBack(retreatSteps);
            if (moved <= 0)
            {
                Debug.LogWarning("[击退怪物] 怪物身后还没有轨迹，本次没有后退");
            }
            else if (moved < retreatSteps)
            {
                Debug.Log($"[击退怪物] 广告奖励生效，轨迹不足，怪物后退了 {moved} 节（请求 {retreatSteps} 节）");
            }
            else
            {
                Debug.Log($"[击退怪物] 广告奖励生效，怪物后退 {moved} 个身位");
            }
        }

        public override void Show()
        {
            base.Show();
            if (retreatButton != null)
                retreatButton.SetActive(true);
        }

        public override void Hide()
        {
            base.Hide();
            if (retreatButton != null)
                retreatButton.SetActive(false);

            // 关卡结束时若广告确认框还开着，一并关掉，避免残留到结算界面
            AdConfirm adConfirm = FindObjectOfType<AdConfirm>();
            if (adConfirm != null && adConfirm.gameObject.activeSelf)
                adConfirm.gameObject.SetActive(false);
        }

        private void InitializeUI()
        {
            // Tự động tìm các component nếu chưa được assign
            if (buttonSetting == null)
                buttonSetting = transform.Find("btnSetting")?.GetComponent<Button>();
                
            if (buttonSkip == null)
                buttonSkip = transform.Find("ButtonSkip")?.GetComponent<Button>();
                
            if (buttonSpeed == null)
                buttonSpeed = transform.Find("ButtonSpeed")?.GetComponent<Button>();

            // Tự động tìm các UI components
            if (txtLevel == null)
                txtLevel = transform.Find("ForSaveArea/txtLevel")?.GetComponent<Text>();
                
            if (txtHP == null)
                txtHP = transform.Find("ForSaveArea/DragonHP/txtHP")?.GetComponent<Text>();
                
            if (imgHPFill == null)
                imgHPFill = transform.Find("ForSaveArea/DragonHP/ImgFill")?.GetComponent<Image>();

            if (mapArea == null)
                mapArea = transform.Find("MapArea")?.gameObject;
                
            if (dragonHPArea == null)
                dragonHPArea = transform.Find("ForSaveArea/DragonHP")?.gameObject;
                
            if (forSaveArea == null)
                forSaveArea = transform.Find("ForSaveArea")?.gameObject;

            // Setup button listeners
            SetupButtonListeners();
        }

        private void SetupButtonListeners()
        {
            if (buttonSetting != null)
            {
                buttonSetting.onClick.AddListener(OnSettingButtonClicked);
            }

            if (buttonSkip != null)
            {
                buttonSkip.onClick.AddListener(OnSkipButtonClicked);
            }

            if (buttonSpeed != null)
            {
                buttonSpeed.onClick.AddListener(OnSpeedButtonClicked);
            }
        }

        #region Button Event Handlers
        private void OnSettingButtonClicked()
        {
            Debug.Log("Setting button được click!");
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
            // Hiển thị setting panel với type Game
            if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            {
                GameManager.Instance.UIManager.ShowSettingGame();
            }
            else
            {
                Debug.LogError("GameManager hoặc UIManager không tồn tại!");
            }
        }

        private void OnSkipButtonClicked()
        {
             SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
            Debug.Log("Skip button được click!");
        }

        private void OnSpeedButtonClicked()
        {
             SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
            Debug.Log("Speed button được click!");
        }
        #endregion

        #region UI Update Methods
        /// <summary>
        /// Khởi tạo UI game với level mới
        /// </summary>
        /// <param name="level">Level hiện tại</param>
        public void InitGameUI(int level)
        {
            SetLevel(level);
            
            // Reset HP về 100%
            SetHP(100, 100);
            
            // Có thể thêm các init khác ở đây
            Debug.Log($"UIGame đã được init với level {level}");
        }
        
        public void SetLevel(int level)
        {
            if (txtLevel != null)
            {
                // Kiểm tra nếu đang ở chế độ test mode
                if (GameManager.Instance?.LevelManager != null && GameManager.Instance.LevelManager.IsTestModeEnabled())
                {
                    // Lấy level index từ level object test
                    int testLevelIndex = GameManager.Instance.LevelManager.GetTestLevelIndex();
                    txtLevel.text = $"第 {testLevelIndex} 关";
                }
                else
                {
                    // Chế độ bình thường
                    txtLevel.text = $"第 {level} 关";
                }
            }
        }

        public void SetHP(int current, int max)
        {
            if (txtHP != null)
            {
                // Tính phần trăm HP
                float percentage = max > 0 ? (float)current / max * 100f : 0f;
                txtHP.text = $"{Mathf.RoundToInt(percentage)}%";
            }
            
            if (imgHPFill != null)
            {
                float fillAmount = (float)current / max;
                imgHPFill.fillAmount = fillAmount;
            }
        }

        public void UpdateHP(int newHP, int maxHP)
        {
            SetHP(newHP, maxHP);
        }
        
        /// <summary>
        /// Cập nhật level display (dùng khi test mode thay đổi)
        /// </summary>
        public void UpdateLevelDisplay()
        {
            if (GameManager.Instance?.LevelManager != null)
            {
                int currentLevel = GameManager.Instance.CurrentLevelIndex;
                SetLevel(currentLevel);
            }
        }
        
        /// <summary>
        /// Force cập nhật level display với level index cụ thể
        /// </summary>
        /// <param name="levelIndex">Level index để hiển thị</param>
        public void ForceSetLevel(int levelIndex)
        {
            if (txtLevel != null)
            {
                txtLevel.text = $"第 {levelIndex + 1} 关";
            }
        }
        #endregion

        private void OnDestroy()
        {
            // Clean up button listeners
            if (buttonSetting != null)
                buttonSetting.onClick.RemoveAllListeners();
            
            if (buttonSkip != null)
                buttonSkip.onClick.RemoveAllListeners();
            
            if (buttonSpeed != null)
                buttonSpeed.onClick.RemoveAllListeners();
        }
    }
} 