using UnityEngine;
using RescuePrincess.Managers;

namespace RescuePrincess.UI
{
    public class UIUnlockSlotMigration : MonoBehaviour
    {
        [Header("Migration Settings")]
        [SerializeField] private bool autoMigrateOnStart = false;
        [SerializeField] private bool deleteOldDataAfterMigration = true;
        [SerializeField] private bool showMigrationLogs = true;
        
        [Header("Migration Info")]
        [SerializeField] private bool hasOldData = false;
        [SerializeField] private int migratedSlotsCount = 0;
        
        private const string OLD_KEY_PREFIX = "UnlockedSlot_";
        private const string NEW_KEY_PREFIX = "UnlockedSlot_Level_";
        
        private void Start()
        {
            if (autoMigrateOnStart)
            {
                CheckAndMigrateOldData();
            }
        }
        
        /// <summary>
        /// Kiểm tra và migrate dữ liệu cũ nếu cần
        /// </summary>
        [ContextMenu("Check And Migrate Old Data")]
        public void CheckAndMigrateOldData()
        {
            if (HasOldUnlockData())
            {
                if (showMigrationLogs)
                    Debug.Log("Phát hiện dữ liệu unlock slot cũ, bắt đầu migration...");
                
                MigrateOldUnlockData();
            }
            else
            {
                if (showMigrationLogs)
                    Debug.Log("Không có dữ liệu unlock slot cũ cần migration");
            }
        }
        
        /// <summary>
        /// Kiểm tra xem có dữ liệu cũ không
        /// </summary>
        /// <returns>True nếu có dữ liệu cũ</returns>
        public bool HasOldUnlockData()
        {
            hasOldData = false;
            
            for (int i = 0; i < 10; i++) // Giả sử có tối đa 10 slots
            {
                string oldKey = OLD_KEY_PREFIX + i;
                if (PlayerPrefs.HasKey(oldKey))
                {
                    hasOldData = true;
                    break;
                }
            }
            
            return hasOldData;
        }
        
        /// <summary>
        /// Migrate dữ liệu từ hệ thống cũ sang hệ thống mới
        /// </summary>
        public void MigrateOldUnlockData()
        {
            if (!HasOldUnlockData())
            {
                if (showMigrationLogs)
                    Debug.Log("Không có dữ liệu cũ để migration");
                return;
            }
            
            migratedSlotsCount = 0;
            int currentLevel = GetCurrentLevel();
            
            if (showMigrationLogs)
                Debug.Log($"Bắt đầu migration dữ liệu unlock slot cho level {currentLevel}");
            
            // Migrate từng slot
            for (int i = 0; i < 10; i++)
            {
                string oldKey = OLD_KEY_PREFIX + i;
                if (PlayerPrefs.HasKey(oldKey))
                {
                    int value = PlayerPrefs.GetInt(oldKey, 0);
                    if (value == 1)
                    {
                        // Migrate sang hệ thống mới cho level hiện tại
                        string newKey = NEW_KEY_PREFIX + currentLevel + "_Slot_" + i;
                        PlayerPrefs.SetInt(newKey, 1);
                        migratedSlotsCount++;
                        
                        if (showMigrationLogs)
                            Debug.Log($"✓ Migrated slot {i} từ hệ thống cũ sang level {currentLevel}");
                    }
                    
                    // Xóa key cũ nếu được yêu cầu
                    if (deleteOldDataAfterMigration)
                    {
                        PlayerPrefs.DeleteKey(oldKey);
                        if (showMigrationLogs)
                            Debug.Log($"  - Đã xóa key cũ: {oldKey}");
                    }
                }
            }
            
            PlayerPrefs.Save();
            
            if (showMigrationLogs)
            {
                Debug.Log($"Migration hoàn thành! Đã migrate {migratedSlotsCount} slots cho level {currentLevel}");
                
                if (deleteOldDataAfterMigration)
                {
                    Debug.Log("Đã xóa tất cả dữ liệu cũ");
                }
                else
                {
                    Debug.Log("Dữ liệu cũ vẫn được giữ lại (deleteOldDataAfterMigration = false)");
                }
            }
        }
        
        /// <summary>
        /// Migrate dữ liệu cho tất cả levels (nếu cần)
        /// </summary>
        /// <param name="maxLevel">Level tối đa để migration</param>
        [ContextMenu("Migrate For All Levels")]
        public void MigrateForAllLevels(int maxLevel = 10)
        {
            if (!HasOldUnlockData())
            {
                if (showMigrationLogs)
                    Debug.Log("Không có dữ liệu cũ để migration");
                return;
            }
            
            int totalMigrated = 0;
            
            if (showMigrationLogs)
                Debug.Log($"Bắt đầu migration dữ liệu unlock slot cho tất cả levels (1-{maxLevel})");
            
            // Migrate cho từng level
            for (int level = 1; level <= maxLevel; level++)
            {
                int levelMigratedCount = 0;
                
                for (int slot = 0; slot < 10; slot++)
                {
                    string oldKey = OLD_KEY_PREFIX + slot;
                    if (PlayerPrefs.HasKey(oldKey))
                    {
                        int value = PlayerPrefs.GetInt(oldKey, 0);
                        if (value == 1)
                        {
                            // Migrate sang hệ thống mới cho level cụ thể
                            string newKey = NEW_KEY_PREFIX + level + "_Slot_" + slot;
                            PlayerPrefs.SetInt(newKey, 1);
                            levelMigratedCount++;
                            totalMigrated++;
                        }
                    }
                }
                
                if (levelMigratedCount > 0 && showMigrationLogs)
                {
                    Debug.Log($"Level {level}: Migrated {levelMigratedCount} slots");
                }
            }
            
            // Xóa dữ liệu cũ nếu được yêu cầu
            if (deleteOldDataAfterMigration)
            {
                for (int i = 0; i < 10; i++)
                {
                    string oldKey = OLD_KEY_PREFIX + i;
                    if (PlayerPrefs.HasKey(oldKey))
                    {
                        PlayerPrefs.DeleteKey(oldKey);
                    }
                }
            }
            
            PlayerPrefs.Save();
            
            if (showMigrationLogs)
            {
                Debug.Log($"Migration cho tất cả levels hoàn thành! Tổng cộng {totalMigrated} slots đã được migrate");
                
                if (deleteOldDataAfterMigration)
                {
                    Debug.Log("Đã xóa tất cả dữ liệu cũ");
                }
            }
        }
        
        /// <summary>
        /// Rollback migration (khôi phục dữ liệu cũ)
        /// </summary>
        [ContextMenu("Rollback Migration")]
        public void RollbackMigration()
        {
            if (showMigrationLogs)
                Debug.Log("Bắt đầu rollback migration...");
            
            int rollbackCount = 0;
            int currentLevel = GetCurrentLevel();
            
            // Khôi phục dữ liệu cũ từ level hiện tại
            for (int i = 0; i < 10; i++)
            {
                string newKey = NEW_KEY_PREFIX + currentLevel + "_Slot_" + i;
                if (PlayerPrefs.HasKey(newKey))
                {
                    int value = PlayerPrefs.GetInt(newKey, 0);
                    if (value == 1)
                    {
                        string oldKey = OLD_KEY_PREFIX + i;
                        PlayerPrefs.SetInt(oldKey, 1);
                        rollbackCount++;
                        
                        if (showMigrationLogs)
                            Debug.Log($"✓ Rollback slot {i} từ level {currentLevel} về hệ thống cũ");
                    }
                    
                    // Xóa key mới
                    PlayerPrefs.DeleteKey(newKey);
                }
            }
            
            PlayerPrefs.Save();
            
            if (showMigrationLogs)
            {
                Debug.Log($"Rollback hoàn thành! Đã rollback {rollbackCount} slots");
            }
        }
        
        /// <summary>
        /// Xóa tất cả dữ liệu unlock slot (cả cũ và mới)
        /// </summary>
        [ContextMenu("Clear All Unlock Data")]
        public void ClearAllUnlockData()
        {
            if (showMigrationLogs)
                Debug.Log("Bắt đầu xóa tất cả dữ liệu unlock slot...");
            
            int deletedCount = 0;
            
            // Xóa dữ liệu cũ
            for (int i = 0; i < 10; i++)
            {
                string oldKey = OLD_KEY_PREFIX + i;
                if (PlayerPrefs.HasKey(oldKey))
                {
                    PlayerPrefs.DeleteKey(oldKey);
                    deletedCount++;
                }
            }
            
            // Xóa dữ liệu mới (cho tất cả levels)
            for (int level = 1; level <= 100; level++)
            {
                for (int slot = 0; slot < 10; slot++)
                {
                    string newKey = NEW_KEY_PREFIX + level + "_Slot_" + slot;
                    if (PlayerPrefs.HasKey(newKey))
                    {
                        PlayerPrefs.DeleteKey(newKey);
                        deletedCount++;
                    }
                }
            }
            
            PlayerPrefs.Save();
            
            if (showMigrationLogs)
            {
                Debug.Log($"Đã xóa tất cả dữ liệu unlock slot! Tổng cộng {deletedCount} keys đã được xóa");
            }
        }
        
        /// <summary>
        /// Kiểm tra trạng thái migration
        /// </summary>
        [ContextMenu("Check Migration Status")]
        public void CheckMigrationStatus()
        {
            Debug.Log("=== Migration Status Check ===");
            
            // Kiểm tra dữ liệu cũ
            bool hasOld = HasOldUnlockData();
            Debug.Log($"Có dữ liệu cũ: {hasOld}");
            
            // Kiểm tra dữ liệu mới cho level hiện tại
            int currentLevel = GetCurrentLevel();
            int newDataCount = 0;
            
            for (int i = 0; i < 10; i++)
            {
                string newKey = NEW_KEY_PREFIX + currentLevel + "_Slot_" + i;
                if (PlayerPrefs.HasKey(newKey))
                {
                    newDataCount++;
                }
            }
            
            Debug.Log($"Level {currentLevel} có {newDataCount} slots đã unlock trong hệ thống mới");
            
            // Kiểm tra tổng số dữ liệu mới
            int totalNewData = 0;
            for (int level = 1; level <= 10; level++)
            {
                for (int slot = 0; slot < 10; slot++)
                {
                    string newKey = NEW_KEY_PREFIX + level + "_Slot_" + slot;
                    if (PlayerPrefs.HasKey(newKey))
                    {
                        totalNewData++;
                    }
                }
            }
            
            Debug.Log($"Tổng cộng có {totalNewData} slots đã unlock trong hệ thống mới (levels 1-10)");
            
            // Đề xuất hành động
            if (hasOld && newDataCount == 0)
            {
                Debug.LogWarning("Có dữ liệu cũ nhưng chưa có dữ liệu mới. Cần migration!");
            }
            else if (hasOld && newDataCount > 0)
            {
                Debug.LogWarning("Có cả dữ liệu cũ và mới. Có thể cần cleanup!");
            }
            else if (!hasOld && newDataCount > 0)
            {
                Debug.Log("Chỉ có dữ liệu mới. Migration đã hoàn thành!");
            }
            else
            {
                Debug.Log("Không có dữ liệu unlock slot nào!");
            }
        }
        
        /// <summary>
        /// Lấy level hiện tại từ GameManager
        /// </summary>
        /// <returns>Index của level hiện tại</returns>
        private int GetCurrentLevel()
        {
            if (GameManager.Instance != null)
            {
                return GameManager.Instance.CurrentLevelIndex;
            }
            return 1; // Default to level 1
        }
        
        /// <summary>
        /// Test migration với dữ liệu giả
        /// </summary>
        [ContextMenu("Test Migration With Fake Data")]
        public void TestMigrationWithFakeData()
        {
            if (showMigrationLogs)
                Debug.Log("Tạo dữ liệu giả để test migration...");
            
            // Tạo dữ liệu giả
            PlayerPrefs.SetInt(OLD_KEY_PREFIX + "0", 1);
            PlayerPrefs.SetInt(OLD_KEY_PREFIX + "2", 1);
            PlayerPrefs.SetInt(OLD_KEY_PREFIX + "5", 1);
            PlayerPrefs.Save();
            
            if (showMigrationLogs)
                Debug.Log("Đã tạo dữ liệu giả cho slots 0, 2, 5");
            
            // Thực hiện migration
            MigrateOldUnlockData();
        }
        
        /// <summary>
        /// Log thông tin debug
        /// </summary>
        private void LogDebugInfo()
        {
            Debug.Log("=== UIUnlockSlotMigration Debug Info ===");
            Debug.Log($"Auto Migrate On Start: {autoMigrateOnStart}");
            Debug.Log($"Delete Old Data After Migration: {deleteOldDataAfterMigration}");
            Debug.Log($"Show Migration Logs: {showMigrationLogs}");
            Debug.Log($"Has Old Data: {hasOldData}");
            Debug.Log($"Migrated Slots Count: {migratedSlotsCount}");
            
            if (GameManager.Instance != null)
            {
                Debug.Log($"Current Level: {GameManager.Instance.CurrentLevelIndex}");
            }
            else
            {
                Debug.LogWarning("GameManager không tồn tại!");
            }
        }
        
        private void OnValidate()
        {
            // Cập nhật thông tin khi thay đổi trong Inspector
            hasOldData = HasOldUnlockData();
        }
    }
} 