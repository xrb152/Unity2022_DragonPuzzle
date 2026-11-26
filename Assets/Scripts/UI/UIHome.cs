using UnityEngine;
using UnityEngine.UI;
using RescuePrincess.Managers;
 
//电子邮件puhalskijsemen@gmail.com
//源码网站 开vpn全局模式打开 http://web3incubators.com/
//电报https://t.me/gamecode999
//网页客服 http://web3incubators.com/kefu.html

namespace RescuePrincess.UI
{
    public class UIHome : UIPanel
    {
        [Header("Home UI Elements")]
        [SerializeField] private Button buttonPlay;
        [SerializeField] private Button buttonSetting;
        [SerializeField] private Button buttonRemoveAds;
        
        [Header("Home UI Components")]
        [SerializeField] private GameObject logo;
        [SerializeField] private GameObject dragon;
        [SerializeField] private GameObject background;
        [SerializeField] private GameObject ground;
        [SerializeField] private GameObject princess; // SkeletonGraphic (RR_princess)
        
        [Header("Level Display")]
        [SerializeField] private Text txtLevel; // Text hiển thị level hiện tại
        

        
        private bool isInitialized = false;

        protected override void Awake()
        {
            base.Awake();
            InitializeUI();
        }

        private void Start()
        {
            // Đăng ký với GameManager nếu có
            if (GameManager.Instance != null)
            {
                Debug.Log("UIHome đã được đăng ký với GameManager");
                // Cập nhật level hiển thị khi khởi tạo
                UpdateLevelDisplay();
            }
        }

        private void InitializeUI()
        {
            if (isInitialized) return;
            
            // Tự động tìm các component nếu chưa được assign
            if (buttonPlay == null)
                buttonPlay = transform.Find("ButtonPlay")?.GetComponent<Button>();
                
            if (buttonSetting == null)
                buttonSetting = transform.Find("LeftBar/ForSaveArea/ButtonSetting")?.GetComponent<Button>();
                
            if (buttonRemoveAds == null)
                buttonRemoveAds = transform.Find("LeftBar/ButtonRemoveAds")?.GetComponent<Button>();

            // Tự động tìm các UI components
            if (logo == null)
                logo = transform.Find("Logo")?.gameObject;
                
            if (dragon == null)
                dragon = transform.Find("Dragon")?.gameObject;
                
            if (background == null)
                background = transform.Find("Background")?.gameObject;
                
            if (ground == null)
                ground = transform.Find("Ground")?.gameObject;
                
            if (princess == null)
                princess = transform.Find("SkeletonGraphic (RR_princess)")?.gameObject;

            // Tự động tìm txtLevel
            if (txtLevel == null)
                txtLevel = transform.Find("txtLevel")?.GetComponent<Text>();

            // Tự động tạo txtLevel nếu chưa có
            if (txtLevel == null)
            {
                CreateLevelText();
            }

            // Setup button listeners
            SetupButtonListeners();
            
            isInitialized = true;
        }

        /// <summary>
        /// Tự động tạo txtLevel nếu chưa có
        /// </summary>
        private void CreateLevelText()
        {
            // Tạo GameObject cho txtLevel
            GameObject txtLevelGO = new GameObject("txtLevel");
            txtLevelGO.transform.SetParent(transform);
            
            // Thêm RectTransform
            RectTransform rectTransform = txtLevelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, 100); // Vị trí trên logo
            rectTransform.sizeDelta = new Vector2(200, 50);
            
            // Thêm CanvasRenderer
            txtLevelGO.AddComponent<CanvasRenderer>();
            
            // Thêm Text component
            txtLevel = txtLevelGO.AddComponent<Text>();
            txtLevel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtLevel.fontSize = 36;
            txtLevel.fontStyle = FontStyle.Bold;
            txtLevel.alignment = TextAnchor.MiddleCenter;
            txtLevel.color = Color.white;
            txtLevel.text = "第 1 关";
            
            // Thêm Shadow effect
            Shadow shadow = txtLevelGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2, 2);
            
            Debug.Log("Đã tự động tạo txtLevel trong UIHome");
        }

        private void SetupButtonListeners()
        {
            if (buttonPlay != null)
            {
                buttonPlay.onClick.AddListener(OnPlayButtonClicked);
                //Debug.Log("Play button listener đã được setup");
            }
           

            if (buttonSetting != null)
            {
                buttonSetting.onClick.AddListener(OnSettingButtonClicked);
                //Debug.Log("Setting button listener đã được setup");
            }
          

            if (buttonRemoveAds != null)
            {
                buttonRemoveAds.onClick.AddListener(OnRemoveAdsButtonClicked);
               // Debug.Log("RemoveAds button listener đã được setup");
            }
          
        }

        public override void Show()
        {
            base.Show();
            Debug.Log("UIHome đã được hiển thị");
            
            // Cập nhật level display khi hiển thị UIHome
            UpdateLevelDisplay();
        }

        public override void Hide()
        {
            base.Hide();
            Debug.Log("UIHome đã được ẩn");
        }

        #region Button Event Handlers
        private void OnPlayButtonClicked()
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
            Debug.Log("Play button được click!");
            
            // Kiểm tra GameManager
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager.Instance không tồn tại!");
                return;
            }
            
            // Ẩn UIHome
            Hide();
            Debug.Log("UIHome đã được ẩn");
            
            // Hiện UIGame thông qua UIManager
            if (GameManager.Instance.UIManager != null)
            {
                GameManager.Instance.UIManager.ShowGameUI();
                Debug.Log("UIGame đã được hiển thị");
            }
            else
            {
                Debug.LogError("UIManager không tồn tại!");
            }
            
            // Load level hiện tại
            if (GameManager.Instance.BoardManager != null)
            {
                int currentLevel = GameManager.Instance.CurrentLevelIndex;
                bool loadSuccess = GameManager.Instance.LoadLevel(currentLevel);
                
                if (loadSuccess)
                {
                    Debug.Log($"Đã load thành công level {currentLevel}");
                }
                else
                {
                    Debug.LogError($"Không thể load level {currentLevel}!");
                }
            }
            else
            {
                Debug.LogError("BoardManager không tồn tại!");
            }
            
            // Load game với function mới
            GameManager.Instance.LoadGame();
        }

        private void OnSettingButtonClicked()
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
            Debug.Log("Setting button được click!");           
            // Hiển thị setting panel với type Home
            if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            {
                GameManager.Instance.UIManager.ShowSettingHome();
            }
            else
            {
                Debug.LogError("GameManager hoặc UIManager không tồn tại!");
            }
        }

        private void OnRemoveAdsButtonClicked()
        {
             SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
            Debug.Log("RemoveAds button được click!");
            
            // TODO: Xử lý remove ads
            // if (AdsManager.Instance != null)
            // {
            //     AdsManager.Instance.RemoveAds();
            // }
        }
        #endregion



        #region Public Methods
        public void SetPlayButtonInteractable(bool interactable)
        {
            if (buttonPlay != null)
            {
                buttonPlay.interactable = interactable;
            }
        }

        public void SetSettingButtonInteractable(bool interactable)
        {
            if (buttonSetting != null)
            {
                buttonSetting.interactable = interactable;
            }
        }

        public void SetRemoveAdsButtonInteractable(bool interactable)
        {
            if (buttonRemoveAds != null)
            {
                buttonRemoveAds.interactable = interactable;
            }
        }

        /// <summary>
        /// Cập nhật hiển thị level hiện tại
        /// </summary>
        public void UpdateLevelDisplay()
        {
            if (GameManager.Instance != null)
            {
                int currentLevel = GameManager.Instance.CurrentLevelIndex;
                SetLevel(currentLevel);
            }
        }

        /// <summary>
        /// Set level hiển thị trên txtLevel
        /// </summary>
        /// <param name="level">Level cần hiển thị</param>
        public void SetLevel(int level)
        {
            if (txtLevel != null)
            {
                txtLevel.text = $"第 {level} 关";
                Debug.Log($"UIHome đã cập nhật level hiển thị: Level {level}");
            }
            else
            {
                Debug.LogWarning("txtLevel không được tìm thấy trong UIHome!");
            }
        }

        public void UpdateUI()
        {
            // Cập nhật level display
            UpdateLevelDisplay();
            
            // TODO: Cập nhật các UI elements khác nếu cần
            // Ví dụ: cập nhật text, icon, etc.
        }
        #endregion

        #region Game State Response
        public void OnGameStateChanged(GameState previousState, GameState newState)
        {
            Debug.Log($"UIHome nhận được thay đổi state từ {previousState} sang {newState}");
            
            switch (newState)
            {
                case GameState.Playing:
                    // Ẩn home panel khi bắt đầu game
                    Hide();
                    break;
                    
                case GameState.MainMenu:
                    // Hiển thị home panel khi về main menu
                    Show();
                    break;
            }
        }
        #endregion

        /// <summary>
        /// Test method để kiểm tra cơ chế cập nhật level
        /// </summary>
        [ContextMenu("Test Update Level Display")]
        public void TestUpdateLevelDisplay()
        {
            Debug.Log("UIHome: Testing level display update...");
            UpdateLevelDisplay();
        }

        /// <summary>
        /// Test method để set level cụ thể
        /// </summary>
        /// <param name="level">Level cần test</param>
        [ContextMenu("Test Set Level")]
        public void TestSetLevel(int level = 5)
        {
            Debug.Log($"UIHome: Testing set level to {level}");
            SetLevel(level);
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (buttonPlay != null)
                buttonPlay.onClick.RemoveAllListeners();
            
            if (buttonSetting != null)
                buttonSetting.onClick.RemoveAllListeners();
            
            if (buttonRemoveAds != null)
                buttonRemoveAds.onClick.RemoveAllListeners();
        }
    }
} 