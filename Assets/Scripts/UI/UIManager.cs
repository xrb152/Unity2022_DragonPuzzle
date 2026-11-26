using UnityEngine;
using RescuePrincess.Managers;
using RescuePrincess.UI;

namespace RescuePrincess.UI
{
    public class UIManager : MonoBehaviour
    {
            [Header("UI Panels")]
    public UIGame gameUI;
    public UIHome homeUI;
    public UIWin winUI;
    public UILose loseUI;
    public UICoin coinUI; // TODO: Uncomment when UICoin is created
    public UISetting settingUI;
    public UIUnlockSlot unlockSlotUI;
    public UINotify notifyUI;
    public UILoading loadingUI;

        private void Awake()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Tự động tìm UIGame component nếu chưa được assign
            if (gameUI == null)
            {
                gameUI = FindObjectOfType<UIGame>();
                if (gameUI == null)
                {
                    Debug.LogWarning("Không tìm thấy UIGame component trong scene!");
                }
            }
            
            // Tự động tìm UIHome component nếu chưa được assign
            if (homeUI == null)
            {
                homeUI = FindObjectOfType<UIHome>();
                if (homeUI == null)
                {
                    Debug.LogWarning("Không tìm thấy UIHome component trong scene!");
                }
            }
            
            // Tự động tìm UIWin component nếu chưa được assign
            if (winUI == null)
            {
                winUI = FindObjectOfType<UIWin>();
                if (winUI == null)
                {
                    Debug.LogWarning("Không tìm thấy UIWin component trong scene!");
                }
            }
            
            // Tự động tìm UILose component nếu chưa được assign
            if (loseUI == null)
            {
                loseUI = FindObjectOfType<UILose>();
                if (loseUI == null)
                {
                    Debug.LogWarning("Không tìm thấy UILose component trong scene!");
                }
            }
            
            // Tự động tìm UISetting component nếu chưa được assign
            if (settingUI == null)
            {
                settingUI = FindObjectOfType<UISetting>();
                if (settingUI == null)
                {
                    Debug.LogWarning("Không tìm thấy UISetting component trong scene!");
                }
            }
            
            // TODO: Uncomment when UICoin is created
            // // Tự động tìm UICoin component nếu chưa được assign
            // if (coinUI == null)
            // {
            //     coinUI = FindObjectOfType<UICoin>();
            //     if (coinUI == null)
            //     {
            //         Debug.LogWarning("Không tìm thấy UICoin component trong scene!");
            //     }
            // }
            
            // Tự động tìm UIUnlockSlot component nếu chưa được assign
            if (unlockSlotUI == null)
            {
                unlockSlotUI = FindObjectOfType<UIUnlockSlot>();
                if (unlockSlotUI == null)
                {
                    Debug.LogWarning("Không tìm thấy UIUnlockSlot component trong scene!");
                }
            }
            
            // Tự động tìm UILoading component nếu chưa được assign
            if (loadingUI == null)
            {
                loadingUI = FindObjectOfType<UILoading>();
                if (loadingUI == null)
                {
                    Debug.LogWarning("Không tìm thấy UILoading component trong scene!");
                }
            }
        }

        #region Panel Management
        /// <summary>
        /// Hiển thị UI Home và ẩn UI Game
        /// </summary>
        public void ShowHomeUI()
        {
            if (homeUI != null)
            {
                homeUI.Show();
                
                // Cập nhật level display cho UIHome
                homeUI.UpdateLevelDisplay();
                
                Debug.Log("UIHome đã được hiển thị");
            }
            else
            {
                Debug.LogWarning("UIHome không tồn tại!");
            }
            
            if (gameUI != null)
            {
                gameUI.Hide();
                Debug.Log("UIGame đã được ẩn");
            }
        }

        /// <summary>
        /// Hiển thị UI Game và ẩn UI Home
        /// </summary>
        public void ShowGameUI()
        {
            if (gameUI != null)
            {
                gameUI.Show();
                Debug.Log("UIGame đã được hiển thị");
            }
            else
            {
                Debug.LogWarning("UIGame không tồn tại!");
            }
            
            if (homeUI != null)
            {
                homeUI.Hide();
                Debug.Log("UIHome đã được ẩn");
            }
        }

        /// <summary>
        /// Ẩn tất cả UI panels
        /// </summary>
        public void HideAllUI()
        {
            if (homeUI != null)
                homeUI.Hide();
                
            if (gameUI != null)
                gameUI.Hide();
                
            if (winUI != null)
                winUI.Hide();
                
            if (loseUI != null)
                loseUI.Hide();
                
            Debug.Log("Tất cả UI panels đã được ẩn");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Lấy reference đến UIGame
        /// </summary>
        public UIGame GetGameUI()
        {
            return gameUI;
        }

        /// <summary>
        /// Lấy reference đến UIHome
        /// </summary>
        public UIHome GetHomeUI()
        {
            return homeUI;
        }

        /// <summary>
        /// Lấy reference đến UIWin
        /// </summary>
        public UIWin GetWinUI()
        {
            return winUI;
        }

        /// <summary>
        /// Lấy reference đến UILose
        /// </summary>
        public UILose GetLoseUI()
        {
            return loseUI;
        }

        /// <summary>
        /// Lấy reference đến UISetting
        /// </summary>
        public UISetting GetSettingUI()
        {
            return settingUI;
        }

        /// <summary>
        /// Hiển thị Setting UI với type Home
        /// </summary>
        public void ShowSettingHome()
        {
            if (settingUI != null)
            {
                settingUI.Show(UISetting.SettingType.Setting_Home);
                Debug.Log("Setting UI (Home) đã được hiển thị");
            }
            else
            {
                Debug.LogWarning("UISetting không tồn tại!");
            }
        }

        /// <summary>
        /// Hiển thị Setting UI với type Game
        /// </summary>
        public void ShowSettingGame()
        {
            if (settingUI != null)
            {
                settingUI.Show(UISetting.SettingType.Setting_Game);
                Debug.Log("Setting UI (Game) đã được hiển thị");
            }
            else
            {
                Debug.LogWarning("UISetting không tồn tại!");
            }
        }

        /// <summary>
        /// Set level hiển thị trên UI
        /// </summary>
        public void SetLevel(int level)
        {
            // Cập nhật level cho UIGame
            if (gameUI != null)
            {
                gameUI.SetLevel(level);
            }
            
            // Cập nhật level cho UIHome
            if (homeUI != null)
            {
                homeUI.SetLevel(level);
            }
        }

        /// <summary>
        /// Cập nhật level hiển thị cho tất cả UI panels
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
        /// Set HP hiển thị trên UI
        /// </summary>
        public void SetHP(int current, int max)
        {
            if (gameUI != null)
            {
                gameUI.SetHP(current, max);
            }
        }

        /// <summary>
        /// Update HP trên UI
        /// </summary>
        public void UpdateHP(int newHP, int maxHP)
        {
            if (gameUI != null)
            {
                gameUI.UpdateHP(newHP, maxHP);
            }
        }

        /// <summary>
        /// Hiển thị Win UI
        /// </summary>
        public void ShowWinUI()
        {
            if (winUI != null)
            {
                // Kiểm tra xem UIWin có đang hiển thị không
                if (winUI.IsShowing())
                {
                    Debug.LogWarning("UIWin đang hiển thị, bỏ qua lệnh ShowWinUI");
                    return;
                }
                
                winUI.Show();
                Debug.Log("UIWin đã được hiển thị");
            }
            else
            {
                Debug.LogWarning("UIWin không tồn tại!");
            }
            
            // Ẩn Game UI khi hiển thị Win UI
            if (gameUI != null)
            {
                gameUI.Hide();
            }
        }
        
        /// <summary>
        /// Force hiển thị Win UI (bỏ qua state check)
        /// </summary>
        public void ForceShowWinUI()
        {
            if (winUI != null)
            {
                winUI.ForceShow();
                Debug.Log("UIWin đã được force hiển thị");
            }
            else
            {
                Debug.LogWarning("UIWin không tồn tại!");
            }
            
            // Ẩn Game UI khi hiển thị Win UI
            if (gameUI != null)
            {
                gameUI.Hide();
            }
        }

        /// <summary>
        /// Hiển thị Lose UI
        /// </summary>
        public void ShowLoseUI()
        {
            if (loseUI != null)
            {
                // Kiểm tra xem UILose có đang hiển thị không
                if (loseUI.IsShowing())
                {
                    Debug.LogWarning("UILose đang hiển thị, bỏ qua lệnh ShowLoseUI");
                    return;
                }
                
                loseUI.Show();
                Debug.Log("UILose đã được hiển thị");
            }
            else
            {
                Debug.LogWarning("UILose không tồn tại!");
            }
            
            // Ẩn Game UI khi hiển thị Lose UI
            if (gameUI != null)
            {
                gameUI.Hide();
            }
        }
        
        /// <summary>
        /// Force hiển thị Lose UI (bỏ qua state check)
        /// </summary>
        public void ForceShowLoseUI()
        {
            if (loseUI != null)
            {
                loseUI.ForceShow();
                Debug.Log("UILose đã được force hiển thị");
            }
            else
            {
                Debug.LogWarning("UILose không tồn tại!");
            }
            
            // Ẩn Game UI khi hiển thị Lose UI
            if (gameUI != null)
            {
                gameUI.Hide();
            }
        }
        
        /// <summary>
        /// Cập nhật hiển thị coin
        /// </summary>
        /// <param name="coinValue">Giá trị coin mới</param>
        public void UpdateCoinDisplay(int coinValue)
        {
            if (coinUI != null)
            {
                coinUI.UpdateCoinDisplay(coinValue);
            }
        }
        
        /// <summary>
        /// Hiệu ứng nhận coin
        /// </summary>
        /// <param name="amount">Số lượng coin nhận được</param>
        public void ReceiveCoins(int amount)
        {
            if (coinUI != null)
            {
                coinUI.ReceiveCoins(amount);
            }
        }
        
        /// <summary>
        /// Force hiển thị UICoin
        /// </summary>
        public void ShowCoinUI()
        {
            if (coinUI != null)
            {
                coinUI.ForceShow();
            }
        }
        
        /// <summary>
        /// Hiển thị UICoin tạm thời (tự động ẩn sau delay)
        /// </summary>
        /// <param name="delay">Thời gian delay trước khi ẩn</param>
        public void ShowCoinUITemporarily(float delay = 2f)
        {
            if (coinUI != null)
            {
                coinUI.ShowTemporarily(delay);
            }
        }
        
        /// <summary>
        /// Ẩn UICoin
        /// </summary>
        public void HideCoinUI()
        {
            if (coinUI != null)
            {
                coinUI.Hide();
            }
        }
        #endregion

        #region Unlock Slot Management
        /// <summary>
        /// Hiển thị UI Unlock Slot
        /// </summary>
        /// <param name="slotIndex">Index của slot cần unlock</param>
        /// <param name="onSuccess">Callback khi unlock thành công</param>
        /// <param name="onFailed">Callback khi unlock thất bại</param>
        public void ShowUnlockSlotUI(int slotIndex, System.Action<int> onSuccess = null, System.Action onFailed = null)
        {
            if (unlockSlotUI != null)
            {
                unlockSlotUI.ShowUnlockSlot(slotIndex, onSuccess, onFailed);
            }
            else
            {
                Debug.LogWarning("UIUnlockSlot không tồn tại!");
                onFailed?.Invoke();
            }
        }
        
        /// <summary>
        /// Ẩn UI Unlock Slot
        /// </summary>
        public void HideUnlockSlotUI()
        {
            if (unlockSlotUI != null)
            {
                unlockSlotUI.ForceClose();
            }
        }
        
        /// <summary>
        /// Kiểm tra slot đã được unlock chưa cho level hiện tại
        /// </summary>
        /// <param name="slotIndex">Index của slot cần kiểm tra</param>
        /// <returns>True nếu đã unlock, false nếu chưa</returns>
        public bool IsSlotUnlocked(int slotIndex)
        {
            if (unlockSlotUI != null)
            {
                return unlockSlotUI.IsSlotUnlocked(slotIndex);
            }
            return false;
        }
        
        /// <summary>
        /// Kiểm tra slot đã được unlock chưa cho level cụ thể
        /// </summary>
        /// <param name="levelIndex">Index của level cần kiểm tra</param>
        /// <param name="slotIndex">Index của slot cần kiểm tra</param>
        /// <returns>True nếu đã unlock, false nếu chưa</returns>
        public bool IsSlotUnlockedForLevel(int levelIndex, int slotIndex)
        {
            if (unlockSlotUI != null)
            {
                return unlockSlotUI.IsSlotUnlockedForLevel(levelIndex, slotIndex);
            }
            return false;
        }
        
        /// <summary>
        /// Reset tất cả slot về trạng thái locked cho level hiện tại
        /// </summary>
        public void ResetAllSlots()
        {
            if (unlockSlotUI != null)
            {
                unlockSlotUI.ResetAllSlots();
            }
        }
        
        /// <summary>
        /// Reset tất cả slot về trạng thái locked cho level cụ thể
        /// </summary>
        /// <param name="levelIndex">Index của level cần reset</param>
        public void ResetAllSlotsForLevel(int levelIndex)
        {
            if (unlockSlotUI != null)
            {
                unlockSlotUI.ResetAllSlotsForLevel(levelIndex);
            }
        }
        
        /// <summary>
        /// Reset tất cả slot về trạng thái locked cho tất cả levels
        /// </summary>
        public void ResetAllSlotsForAllLevels()
        {
            if (unlockSlotUI != null)
            {
                unlockSlotUI.ResetAllSlotsForAllLevels();
            }
        }
        
        /// <summary>
        /// Lấy danh sách các slot đã unlock cho level hiện tại
        /// </summary>
        /// <returns>Array chứa index của các slot đã unlock</returns>
        public int[] GetUnlockedSlotsForCurrentLevel()
        {
            if (unlockSlotUI != null)
            {
                return unlockSlotUI.GetUnlockedSlotsForCurrentLevel();
            }
            return new int[0];
        }
        
        /// <summary>
        /// Lấy danh sách các slot đã unlock cho level cụ thể
        /// </summary>
        /// <param name="levelIndex">Index của level cần kiểm tra</param>
        /// <returns>Array chứa index của các slot đã unlock</returns>
        public int[] GetUnlockedSlotsForLevel(int levelIndex)
        {
            if (unlockSlotUI != null)
            {
                return unlockSlotUI.GetUnlockedSlotsForLevel(levelIndex);
            }
            return new int[0];
        }
        
        /// <summary>
        /// Lấy số lượng slot đã unlock cho level hiện tại
        /// </summary>
        /// <returns>Số lượng slot đã unlock</returns>
        public int GetUnlockedSlotsCountForCurrentLevel()
        {
            if (unlockSlotUI != null)
            {
                return unlockSlotUI.GetUnlockedSlotsCountForCurrentLevel();
            }
            return 0;
        }
        
        /// <summary>
        /// Lấy số lượng slot đã unlock cho level cụ thể
        /// </summary>
        /// <param name="levelIndex">Index của level cần kiểm tra</param>
        /// <returns>Số lượng slot đã unlock</returns>
        public int GetUnlockedSlotsCountForLevel(int levelIndex)
        {
            if (unlockSlotUI != null)
            {
                return unlockSlotUI.GetUnlockedSlotsCountForLevel(levelIndex);
            }
            return 0;
        }
        
        #region Loading UI Management
        /// <summary>
        /// Lấy UILoading component
        /// </summary>
        /// <returns>UILoading component nếu có</returns>
        public UILoading GetLoadingUI()
        {
            return loadingUI;
        }
        
        /// <summary>
        /// Bắt đầu quá trình loading
        /// </summary>
        public void StartLoading()
        {
            if (loadingUI != null)
            {
                loadingUI.StartLoading();
            }
        }
        
        /// <summary>
        /// Cập nhật progress bar
        /// </summary>
        /// <param name="progress">Giá trị progress từ 0 đến 1</param>
        public void UpdateLoadingProgress(float progress)
        {
            if (loadingUI != null)
            {
                loadingUI.UpdateProgress(progress);
            }
        }
        
        /// <summary>
        /// Bỏ qua loading và chuyển scene ngay lập tức
        /// </summary>
        public void SkipLoading()
        {
            if (loadingUI != null)
            {
                loadingUI.SkipLoading();
            }
        }
        #endregion
        #endregion
    }
} 