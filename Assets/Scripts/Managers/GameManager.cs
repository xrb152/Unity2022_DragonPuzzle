using UnityEngine;
using RescuePrincess.Gameplay;
using RescuePrincess.UI;

namespace RescuePrincess.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Components")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private LevelManager levelManager;

        [Header("Game State")]
        [SerializeField] private GameState currentGameState = GameState.MainMenu;
        [SerializeField] private int currentLevelIndex = 1;
        
        [Header("Coin System")]
        [SerializeField] private int coinValue = 0;
        
        // PlayerPrefs keys
        private const string LEVEL_INDEX_KEY = "CurrentLevelIndex";
        private const string UNLOCKED_LEVEL_KEY = "UnlockedLevelIndex";
        private const string COIN_VALUE_KEY = "CoinValue";
        
        public GameState CurrentGameState 
        { 
            get => currentGameState; 
            private set 
            {
                if (currentGameState != value)
                {
                    GameState previousState = currentGameState;
                    currentGameState = value;
                    OnGameStateChanged(previousState, currentGameState);
                }
            }
        }

        public BoardManager BoardManager => boardManager;
        public UIManager UIManager => uiManager;
        public LevelManager LevelManager => levelManager;
        public int CurrentLevelIndex => currentLevelIndex;
        public int CoinValue => coinValue;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
            Application.targetFrameRate = 60;
        }

        private void InitializeGame()
        {
            // Tự động tìm các component nếu chưa được assign
            if (boardManager == null)
                boardManager = FindObjectOfType<BoardManager>();
            
            if (uiManager == null)
                uiManager = FindObjectOfType<UIManager>();
                
            if (levelManager == null)
                levelManager = FindObjectOfType<LevelManager>();

            // Load level index từ PlayerPrefs
            LoadLevelIndex();
            
            // Load coin value từ PlayerPrefs
            LoadCoinValue();
            
            // Khởi tạo game state
            CurrentGameState = GameState.MainMenu;
        }

        private void Start()
        {
            // Đảm bảo các manager đã được khởi tạo
            if (boardManager != null)
            {
                Debug.Log("BoardManager đã được kết nối với GameManager");
            }
            else
            {
                Debug.LogError("BoardManager không được tìm thấy!");
            }

            if (uiManager != null)
            {
                Debug.Log("UIManager đã được kết nối với GameManager");
                // Khởi tạo UI: hiển thị Home UI và ẩn Game UI
                InitializeUI();
            }
            else
            {
                Debug.LogError("UIManager không được tìm thấy!");
            }
            
            if (levelManager != null)
            {
                Debug.Log("LevelManager đã được kết nối với GameManager");
            }
            else
            {
                Debug.LogError("LevelManager không được tìm thấy!");
            }
        }

        /// <summary>
        /// Khởi tạo UI khi ứng dụng bắt đầu
        /// </summary>
        private void InitializeUI()
        {
            if (uiManager != null)
            {
                // Hiển thị UI Home và ẩn UI Game khi khởi động
                uiManager.ShowHomeUI();
                
                // Cập nhật level display cho tất cả UI
                uiManager.UpdateLevelDisplay();
                
                Debug.Log("Đã khởi tạo UI: hiển thị Home UI, ẩn Game UI");
            }
        }

        #region Save/Load System
        /// <summary>
        /// Lưu level index hiện tại vào PlayerPrefs
        /// </summary>
        private void SaveLevelIndex()
        {
            PlayerPrefs.SetInt(LEVEL_INDEX_KEY, currentLevelIndex);
            PlayerPrefs.Save();
            //Debug.Log($"Đã lưu level index: {currentLevelIndex}");
        }
        
        /// <summary>
        /// Load level index từ PlayerPrefs
        /// </summary>
        private void LoadLevelIndex()
        {
            currentLevelIndex = PlayerPrefs.GetInt(LEVEL_INDEX_KEY, 1);
            //Debug.Log($"Đã load level index: {currentLevelIndex}");
        }
        
        /// <summary>
        /// Lưu unlocked level index (level cao nhất đã mở khóa)
        /// </summary>
        /// <param name="levelIndex">Level index đã mở khóa</param>
        public void SaveUnlockedLevel(int levelIndex)
        {
            int currentUnlocked = PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);
            if (levelIndex > currentUnlocked)
            {
                PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, levelIndex);
                PlayerPrefs.Save();
                //Debug.Log($"Đã mở khóa level: {levelIndex}");
            }
        }
        
        /// <summary>
        /// Lấy unlocked level index
        /// </summary>
        /// <returns>Level index cao nhất đã mở khóa</returns>
        public int GetUnlockedLevel()
        {
            return PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);
        }
        
        /// <summary>
        /// Reset tất cả dữ liệu đã lưu
        /// </summary>
        public void ResetAllData()
        {
            PlayerPrefs.DeleteKey(LEVEL_INDEX_KEY);
            PlayerPrefs.DeleteKey(UNLOCKED_LEVEL_KEY);
            PlayerPrefs.DeleteKey(COIN_VALUE_KEY);
            PlayerPrefs.Save();
            currentLevelIndex = 1;
            coinValue = 0;
            //Debug.Log("Đã reset tất cả dữ liệu game");
        }
        
        /// <summary>
        /// Load coin value từ PlayerPrefs
        /// </summary>
        private void LoadCoinValue()
        {
            coinValue = PlayerPrefs.GetInt(COIN_VALUE_KEY, 0);
            //Debug.Log($"Đã load coin value: {coinValue}");
        }
        
        /// <summary>
        /// Lưu coin value vào PlayerPrefs
        /// </summary>
        private void SaveCoinValue()
        {
            PlayerPrefs.SetInt(COIN_VALUE_KEY, coinValue);
            PlayerPrefs.Save();
           // Debug.Log($"Đã lưu coin value: {coinValue}");
        }
        
        /// <summary>
        /// Thêm coin
        /// </summary>
        /// <param name="amount">Số lượng coin cần thêm</param>
        public void AddCoin(int amount)
        {
            coinValue += amount;
            SaveCoinValue();
            
            // Thông báo cho UI cập nhật
            if (uiManager != null)
            {
                uiManager.UpdateCoinDisplay(coinValue);
            }
            
            //Debug.Log($"Đã thêm {amount} coin. Tổng: {coinValue}");
        }
        
        /// <summary>
        /// Giảm coin
        /// </summary>
        /// <param name="amount">Số lượng coin cần giảm</param>
        /// <returns>True nếu đủ coin để giảm, false nếu không đủ</returns>
        public bool SpendCoin(int amount)
        {
            if (coinValue >= amount)
            {
                coinValue -= amount;
                SaveCoinValue();
                
                // Thông báo cho UI cập nhật
                if (uiManager != null)
                {
                    uiManager.UpdateCoinDisplay(coinValue);
                }
                
                //Debug.Log($"Đã tiêu {amount} coin. Còn lại: {coinValue}");
                return true;
            }
            
            //Debug.LogWarning($"Không đủ coin để tiêu! Cần: {amount}, Có: {coinValue}");
            return false;
        }
        
        /// <summary>
        /// Set coin value
        /// </summary>
        /// <param name="value">Giá trị coin mới</param>
        public void SetCoinValue(int value)
        {
            coinValue = Mathf.Max(0, value); // Đảm bảo không âm
            SaveCoinValue();
            
            // Thông báo cho UI cập nhật
            if (uiManager != null)
            {
                uiManager.UpdateCoinDisplay(coinValue);
            }
            
            //Debug.Log($"Đã set coin value: {coinValue}");
        }
        #endregion

        #region Level Management
        /// <summary>
        /// Load level theo index
        /// </summary>
        /// <param name="levelIndex">Index của level cần load</param>
        /// <returns>True nếu load thành công, false nếu thất bại</returns>
        public bool LoadLevel(int levelIndex)
        {
            //Debug.Log($"GameManager đang load level {levelIndex}");
            
            if (boardManager == null)
            {
                //Debug.LogError("BoardManager không tồn tại!");
                return false;
            }
            
            // Xóa level hiện tại nếu có
            boardManager.ClearCurrentLevel();
            
            // Load level mới
            bool success = boardManager.LoadLevel(levelIndex);
            if (success)
            {
                currentLevelIndex = levelIndex;
                // Lưu level index mới
                SaveLevelIndex();
                //Debug.Log($"GameManager đã load thành công level {levelIndex}");
                
                // Cập nhật UI level
                if (uiManager != null)
                {
                    uiManager.UpdateLevelDisplay();
                }
            }
            else
            {
                //Debug.LogError($"GameManager không thể load level {levelIndex}!");
            }
            
            return success;
        }
        
        /// <summary>
        /// Load level tiếp theo với cơ chế tăng level index
        /// </summary>
        /// <returns>True nếu load thành công, false nếu không có level tiếp theo</returns>
        public bool LoadNextLevel()
        {
            if (levelManager == null)
            {
               // Debug.LogError("LevelManager không tồn tại!");
                return false;
            }
            
            int nextLevelIndex = levelManager.GetNextLevelIndex();
            if (nextLevelIndex == -1)
            {
                //Debug.LogWarning("Không có level tiếp theo!");
                return false;
            }
            
            // Tăng level index và mở khóa level mới
            currentLevelIndex = nextLevelIndex;
            SaveUnlockedLevel(currentLevelIndex);
            
            // Load level mới
            bool success = LoadLevel(currentLevelIndex);
            if (success)
            {
               // Debug.Log($"Đã chuyển sang level tiếp theo: {currentLevelIndex}");
            }
            
            return success;
        }
        
        /// <summary>
        /// Load level trước đó
        /// </summary>
        /// <returns>True nếu load thành công, false nếu không có level trước đó</returns>
        public bool LoadPreviousLevel()
        {
            if (levelManager == null)
            {
                //Debug.LogError("LevelManager không tồn tại!");
                return false;
            }
            
            int previousLevelIndex = levelManager.GetPreviousLevelIndex();
            if (previousLevelIndex == -1)
            {
                //Debug.LogWarning("Không có level trước đó!");
                return false;
            }
            
            return LoadLevel(previousLevelIndex);
        }
        
        /// <summary>
        /// Reload level hiện tại
        /// </summary>
        /// <returns>True nếu reload thành công</returns>
        public bool ReloadCurrentLevel()
        {
            return LoadLevel(currentLevelIndex);
        }
        #endregion

        #region Game State Management
        /// <summary>
        /// Load game: set state thành Playing, hiện lại UIGame, Init UIGame
        /// </summary>
        public void LoadGame()
        {
            //Debug.Log("GameManager đang load game...");
            
            // Set state thành Playing
            CurrentGameState = GameState.Playing;
            
            // Load level hiện tại nếu chưa có level nào
            if (boardManager != null && boardManager.GetCurrentLevelObject() == null)
            {
                LoadLevel(currentLevelIndex);
            }
            
            // Kích hoạt BoardManager
            if (boardManager != null)
            {
                boardManager.SetGameActive(true);
            }
            
            // Hiện lại UIGame và Init UIGame
            if (uiManager != null)
            {
                uiManager.ShowGameUI();
                
                // Init UIGame
                UIGame gameUI = uiManager.GetGameUI();
                if (gameUI != null)
                {
                    // Khởi tạo UI game với level hiện tại
                    gameUI.InitGameUI(currentLevelIndex);
                }
            }
            
           // Debug.Log("Game đã được load thành công!");
        }

        public void StartGame()
        {
            // Sử dụng LoadGame thay vì logic cũ
            LoadGame();
        }

        public void PauseGame()
        {
            if (CurrentGameState == GameState.Playing)
            {
                CurrentGameState = GameState.Paused;
                Time.timeScale = 0f;
                
                // Tạm dừng BoardManager
                if (boardManager != null)
                {
                    boardManager.SetGameActive(false);
                }
                
               // Debug.Log("Game đã tạm dừng!");
            }
        }

        public void ResumeGame()
        {
            if (CurrentGameState == GameState.Paused)
            {
                CurrentGameState = GameState.Playing;
                Time.timeScale = 1f;
                
                // Tiếp tục BoardManager
                if (boardManager != null)
                {
                    boardManager.SetGameActive(true);
                }
                
               // Debug.Log("Game đã tiếp tục!");
            }
        }

        public void GameOver()
        {
            CurrentGameState = GameState.GameOver;
            
            // Tạm dừng BoardManager
            if (boardManager != null)
            {
                boardManager.SetGameActive(false);
            }
            
            // Hiển thị Lose UI sau 2 giây
            StartCoroutine(ShowLoseUIAfterDelay(2f));
            
            Debug.Log("Game Over!");
        }

        /// <summary>
        /// Hiển thị Lose UI sau delay
        /// </summary>
        /// <param name="delay">Thời gian delay trước khi hiển thị</param>
        private System.Collections.IEnumerator ShowLoseUIAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Hiển thị Lose UI
            if (uiManager != null)
            {
                uiManager.ShowLoseUI();
            }
        }

        public void Victory()
        {
            CurrentGameState = GameState.Victory;
            
            // Tạm dừng BoardManager
            if (boardManager != null)
            {
                boardManager.SetGameActive(false);
            }
            
            // Hiển thị Win UI
            if (uiManager != null)
            {
                uiManager.ShowWinUI();
            }
            
            Debug.Log("Victory! Dragon defeated!");
        }

        public void ReturnToMainMenu()
        {
            CurrentGameState = GameState.MainMenu;
            Time.timeScale = 1f;
            
            // Clean level và tạm dừng BoardManager
            if (boardManager != null)
            {
                boardManager.ClearCurrentLevel();
                boardManager.SetGameActive(false);
            }
            
            // Chuyển về Home UI khi quay về main menu
            if (uiManager != null)
            {
                uiManager.ShowHomeUI();
            }
            
            Debug.Log("Đã quay về Main Menu và clean toàn bộ level!");
        }

        public void RestartGame()
        {
            CurrentGameState = GameState.Playing;
            Time.timeScale = 1f;
            
            // Reload level hiện tại
            ReloadCurrentLevel();
            
            // Kích hoạt BoardManager
            if (boardManager != null)
            {
                boardManager.SetGameActive(true);
            }
            
            // Đảm bảo Game UI được hiển thị khi restart
            if (uiManager != null)
            {
                uiManager.ShowGameUI();
            }
            
            //Debug.Log("Game đã restart!");
        }
        #endregion

        #region Event Handlers
        private void OnGameStateChanged(GameState previousState, GameState newState)
        {
            //Debug.Log($"Game State thay đổi từ {previousState} sang {newState}");
            
            // Thông báo cho UI Manager
            if (uiManager != null)
            {
                // Có thể thêm logic xử lý cho UIManager ở đây nếu cần
            }

            // Thông báo cho Board Manager nếu cần
            if (boardManager != null)
            {
                // Có thể thêm logic xử lý cho BoardManager ở đây
            }
            
            // Thông báo cho Level Manager nếu cần
            if (levelManager != null)
            {
                // Có thể thêm logic xử lý cho LevelManager ở đây
            }
        }
        #endregion

        #region Public Methods
        public bool IsGamePlaying()
        {
            return CurrentGameState == GameState.Playing;
        }

        public bool IsGamePaused()
        {
            return CurrentGameState == GameState.Paused;
        }
        
        /// <summary>
        /// Kiểm tra xem level có được mở khóa không
        /// </summary>
        /// <param name="levelIndex">Level index cần kiểm tra</param>
        /// <returns>True nếu level đã được mở khóa</returns>
        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex <= GetUnlockedLevel();
        }
        
        /// <summary>
        /// Lấy tổng số level có sẵn
        /// </summary>
        /// <returns>Tổng số level</returns>
        public int GetTotalLevels()
        {
            if (levelManager != null)
            {
                return levelManager.GetTotalLevels();
            }
            return 0;
        }
        
        /// <summary>
        /// Kiểm tra xem có level tiếp theo không
        /// </summary>
        /// <returns>True nếu có level tiếp theo</returns>
        public bool HasNextLevel()
        {
            if (levelManager != null)
            {
                int nextLevel = levelManager.GetNextLevelIndex();
                return nextLevel != -1;
            }
            return false;
        }
        
        /// <summary>
        /// Kiểm tra xem có level trước đó không
        /// </summary>
        /// <returns>True nếu có level trước đó</returns>
        public bool HasPreviousLevel()
        {
            if (levelManager != null)
            {
                int previousLevel = levelManager.GetPreviousLevelIndex();
                return previousLevel != -1;
            }
            return false;
        }

        public void QuitGame()
        {
            Debug.Log("Thoát game!");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        #endregion
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }
} 