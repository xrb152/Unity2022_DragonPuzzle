using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RescuePrincess.Gameplay;
using RescuePrincess.Managers;

public class LevelManager : MonoBehaviour
{
    [Header("Level Objects")]
    [SerializeField] private List<LevelObject> levelObjectList = new List<LevelObject>();

    [Header("Settings")]
    [SerializeField] private bool loadFromResources = true;
    [SerializeField] private string resourcesPath = "Levels";

    [Header("Test Mode")]
    [SerializeField] private bool testMode = false;
    [SerializeField] private int testLevelIndex = 1;
    [SerializeField] private bool showTestModeWarning = true;

    private void Awake()
    {
        if (loadFromResources)
        {
            LoadLevelObjectsFromResources();
        }

        // Hiển thị cảnh báo nếu đang ở chế độ test
        if (testMode && showTestModeWarning)
        {
            Debug.LogWarning($"=== TEST MODE ENABLED ===\nChỉ chơi level test: {testLevelIndex}\nTắt test mode trong LevelManager để chơi bình thường!");
        }
    }

    /// <summary>
    /// Load all level objects from Resources folder
    /// </summary>
    public void LoadLevelObjectsFromResources()
    {
        GameObject[] loadedLevelPrefabs = Resources.LoadAll<GameObject>(resourcesPath);
        levelObjectList.Clear();

        foreach (GameObject prefab in loadedLevelPrefabs)
        {
            LevelObject levelObject = prefab.GetComponent<LevelObject>();
            if (levelObject != null)
            {
                levelObjectList.Add(levelObject);
            }
        }

        // Sort by level index
        levelObjectList = levelObjectList.OrderBy(level => level.levelIndex).ToList();

        Debug.Log($"Loaded {levelObjectList.Count} level objects from Resources/{resourcesPath}");
    }

    /// <summary>
    /// Get level object by current level index from GameManager
    /// </summary>
    /// <returns>LevelObject if found, null otherwise</returns>
    public LevelObject GetCurrentLevelObject()
    {
        int currentLevelIndex = GameManager.Instance.CurrentLevelIndex;
        
        // Nếu đang ở chế độ test, chỉ trả về level test
        if (testMode)
        {
            if (testLevelIndex >= 0 && testLevelIndex < levelObjectList.Count)
                return levelObjectList[testLevelIndex];
            else
                return levelObjectList[0];
        }

        // Sử dụng chỉ số của mảng thay vì level.levelIndex
        if (currentLevelIndex >= 0 && currentLevelIndex < levelObjectList.Count)
        {
            return levelObjectList[currentLevelIndex - 1];
        }

        return levelObjectList[0];
    }

    /// <summary>
    /// Get level object by level index (for backward compatibility)
    /// </summary>
    /// <param name="levelIndex">Level index to find</param>
    /// <returns>LevelObject if found, null otherwise</returns>
    public LevelObject GetLevelObject(int levelIndex)
    {
        // Nếu đang ở chế độ test, chỉ trả về level test
        if (testMode)
        {
            if (levelIndex >= 0 && levelIndex < levelObjectList.Count)
                return levelObjectList[testLevelIndex];
            else
                return levelObjectList[0];
        }

        // Sử dụng chỉ số của mảng thay vì level.levelIndex
        if (levelIndex >= 0 && levelIndex < levelObjectList.Count)
        {
            return levelObjectList[levelIndex - 1];
        }

        return levelObjectList[0];
    }

    /// <summary>
    /// Get level object by index in list
    /// </summary>
    /// <param name="index">Index in the level list</param>
    /// <returns>LevelObject if index is valid, null otherwise</returns>
    public LevelObject GetLevelObjectByIndex(int index)
    {
        if (index >= 0 && index < levelObjectList.Count)
        {
            // Nếu đang ở chế độ test, chỉ trả về level test
            if (testMode)
            {
                if (testLevelIndex >= 0 && testLevelIndex < levelObjectList.Count)
                {
                    return levelObjectList[testLevelIndex];
                }
                return null;
            }

            return levelObjectList[index];
        }
        return null;
    }

    /// <summary>
    /// Get total number of levels
    /// </summary>
    public int GetTotalLevels()
    {
        // Nếu đang ở chế độ test, chỉ có 1 level
        if (testMode)
        {
            return 1;
        }

        return levelObjectList.Count;
    }

    /// <summary>
    /// Get all level objects
    /// </summary>
    public List<LevelObject> GetAllLevelObjects()
    {
        // Nếu đang ở chế độ test, chỉ trả về level test
        if (testMode)
        {
            if (testLevelIndex >= 0 && testLevelIndex < levelObjectList.Count)
            {
                return new List<LevelObject> { levelObjectList[testLevelIndex] };
            }
            return new List<LevelObject>();
        }

        return new List<LevelObject>(levelObjectList);
    }

    /// <summary>
    /// Check if current level exists
    /// </summary>
    /// <returns>True if current level exists</returns>
    public bool HasCurrentLevel()
    {
        int currentLevelIndex = GameManager.Instance.CurrentLevelIndex;
        
        // Nếu đang ở chế độ test, chỉ level test mới tồn tại
        if (testMode)
        {
            return testLevelIndex >= 0 && testLevelIndex < levelObjectList.Count;
        }

        // Kiểm tra xem currentLevelIndex có hợp lệ không (từ 1 đến tổng số level)
        return currentLevelIndex >= 1 && currentLevelIndex <= levelObjectList.Count;
    }

    /// <summary>
    /// Check if a level exists (for backward compatibility)
    /// </summary>
    /// <param name="levelIndex">Level index to check</param>
    /// <returns>True if level exists</returns>
    public bool HasLevel(int levelIndex)
    {
        // Nếu đang ở chế độ test, chỉ level test mới tồn tại
        if (testMode)
        {
            return levelIndex == testLevelIndex && levelObjectList.Any(level => level.levelIndex == testLevelIndex);
        }

        return levelObjectList.Any(level => level.levelIndex == levelIndex);
    }

    /// <summary>
    /// Get the next level index based on current level from GameManager
    /// </summary>
    /// <returns>Next level index, or -1 if no next level</returns>
    public int GetNextLevelIndex()
    {
        int currentLevelIndex = GameManager.Instance.CurrentLevelIndex;
        
        // Nếu đang ở chế độ test, không có level tiếp theo
        if (testMode)
        {
            return -1;
        }

        // Kiểm tra xem có level tiếp theo không
        if (currentLevelIndex >= 1 && currentLevelIndex < levelObjectList.Count)
        {
            return currentLevelIndex + 1;
        }

        return -1; // No next level
    }

    /// <summary>
    /// Get the next level index (for backward compatibility)
    /// </summary>
    /// <param name="currentLevel">Current level index</param>
    /// <returns>Next level index, or -1 if no next level</returns>
    public int GetNextLevelIndex(int currentLevel)
    {
        // Nếu đang ở chế độ test, không có level tiếp theo
        if (testMode)
        {
            return -1;
        }

        var currentLevelObject = GetLevelObject(currentLevel);
        if (currentLevelObject == null)
            return -1;

        int currentIndex = levelObjectList.IndexOf(currentLevelObject);
        if (currentIndex >= 0 && currentIndex < levelObjectList.Count - 1)
        {
            return levelObjectList[currentIndex + 1].levelIndex;
        }

        return -1; // No next level
    }

    /// <summary>
    /// Get the previous level index based on current level from GameManager
    /// </summary>
    /// <returns>Previous level index, or -1 if no previous level</returns>
    public int GetPreviousLevelIndex()
    {
        int currentLevelIndex = GameManager.Instance.CurrentLevelIndex;
        
        // Nếu đang ở chế độ test, không có level trước đó
        if (testMode)
        {
            return -1;
        }

        // Kiểm tra xem có level trước đó không
        if (currentLevelIndex > 1 && currentLevelIndex <= levelObjectList.Count)
        {
            return currentLevelIndex - 1;
        }

        return -1; // No previous level
    }

    /// <summary>
    /// Get the previous level index (for backward compatibility)
    /// </summary>
    /// <param name="currentLevel">Current level index</param>
    /// <returns>Previous level index, or -1 if no previous level</returns>
    public int GetPreviousLevelIndex(int currentLevel)
    {
        // Nếu đang ở chế độ test, không có level trước đó
        if (testMode)
        {
            return -1;
        }

        var currentLevelObject = GetLevelObject(currentLevel);
        if (currentLevelObject == null)
            return -1;

        int currentIndex = levelObjectList.IndexOf(currentLevelObject);
        if (currentIndex > 0)
        {
            return levelObjectList[currentIndex - 1].levelIndex;
        }

        return -1; // No previous level
    }

    /// <summary>
    /// Add level object manually (for runtime)
    /// </summary>
    public void AddLevelObject(LevelObject levelObject)
    {
        if (levelObject != null && !levelObjectList.Contains(levelObject))
        {
            levelObjectList.Add(levelObject);
            levelObjectList = levelObjectList.OrderBy(level => level.levelIndex).ToList();
        }
    }

    /// <summary>
    /// Remove level object
    /// </summary>
    public void RemoveLevelObject(LevelObject levelObject)
    {
        if (levelObject != null)
        {
            levelObjectList.Remove(levelObject);
        }
    }

    /// <summary>
    /// Clear all level objects
    /// </summary>
    public void ClearLevelObjects()
    {
        levelObjectList.Clear();
    }

    /// <summary>
    /// Instantiate current level object from GameManager
    /// </summary>
    /// <param name="parent">Parent transform for the instantiated level</param>
    /// <returns>Instantiated LevelObject, or null if not found</returns>
    public LevelObject InstantiateCurrentLevel(Transform parent = null)
    {
        int currentLevelIndex = GameManager.Instance.CurrentLevelIndex;
        
        // Nếu đang ở chế độ test, chỉ instantiate level test
        if (testMode)
        {
            currentLevelIndex = testLevelIndex;
        }

        LevelObject levelObject = GetLevelObject(currentLevelIndex);
        if (levelObject == null)
        {
            Debug.LogWarning($"Level object with index {currentLevelIndex} not found!");
            return null;
        }

        GameObject instantiatedLevel = Instantiate(levelObject.gameObject, parent);
        LevelObject instantiatedLevelObject = instantiatedLevel.GetComponent<LevelObject>();

        // if (instantiatedLevelObject != null)
        // {
        //     instantiatedLevelObject.Init();
        // }

        return instantiatedLevelObject;
    }

    /// <summary>
    /// Instantiate a level object by index (for backward compatibility)
    /// </summary>
    /// <param name="levelIndex">Level index to instantiate</param>
    /// <param name="parent">Parent transform for the instantiated level</param>
    /// <returns>Instantiated LevelObject, or null if not found</returns>
    public LevelObject InstantiateLevel(int levelIndex, Transform parent = null)
    {
        // Nếu đang ở chế độ test, chỉ instantiate level test
        if (testMode)
        {
            levelIndex = testLevelIndex;
        }

        LevelObject levelObject = GetLevelObject(levelIndex);
        if (levelObject == null)
        {
            Debug.LogWarning($"Level object with index {levelIndex} not found!");
            return null;
        }

        GameObject instantiatedLevel = Instantiate(levelObject.gameObject, parent);
        LevelObject instantiatedLevelObject = instantiatedLevel.GetComponent<LevelObject>();

        // if (instantiatedLevelObject != null)
        // {
        //     instantiatedLevelObject.Init();
        // }

        return instantiatedLevelObject;
    }

    /// <summary>
    /// Load and instantiate current level from Resources
    /// </summary>
    /// <param name="parent">Parent transform for the instantiated level</param>
    /// <returns>Instantiated LevelObject, or null if not found</returns>
    public LevelObject LoadAndInstantiateCurrentLevel(Transform parent = null)
    {
        int currentLevelIndex = GameManager.Instance.CurrentLevelIndex;
        
        // Nếu đang ở chế độ test, chỉ load level test
        if (testMode)
        {
            currentLevelIndex = testLevelIndex;
        }

        string levelPath = $"{resourcesPath}/Level_{currentLevelIndex}";
        GameObject levelPrefab = Resources.Load<GameObject>(levelPath);

        if (levelPrefab == null)
        {
            Debug.LogWarning($"Level prefab not found at path: {levelPath}");
            return null;
        }

        GameObject instantiatedLevel = Instantiate(levelPrefab, parent);
        LevelObject levelObject = instantiatedLevel.GetComponent<LevelObject>();

        if (levelObject != null)
        {
            levelObject.levelIndex = currentLevelIndex;
            //levelObject.Init();
        }

        return levelObject;
    }

    /// <summary>
    /// Load and instantiate level from Resources (for backward compatibility)
    /// </summary>
    /// <param name="levelIndex">Level index to load</param>
    /// <param name="parent">Parent transform for the instantiated level</param>
    /// <returns>Instantiated LevelObject, or null if not found</returns>
    public LevelObject LoadAndInstantiateLevel(int levelIndex, Transform parent = null)
    {
        // Nếu đang ở chế độ test, chỉ load level test
        if (testMode)
        {
            levelIndex = testLevelIndex;
        }

        string levelPath = $"{resourcesPath}/Level_{levelIndex}";
        GameObject levelPrefab = Resources.Load<GameObject>(levelPath);

        if (levelPrefab == null)
        {
            Debug.LogWarning($"Level prefab not found at path: {levelPath}");
            return null;
        }

        GameObject instantiatedLevel = Instantiate(levelPrefab, parent);
        LevelObject levelObject = instantiatedLevel.GetComponent<LevelObject>();

        if (levelObject != null)
        {
            levelObject.levelIndex = levelIndex;
            //levelObject.Init();
        }

        return levelObject;
    }

    #region Test Mode Methods
    /// <summary>
    /// Enable test mode
    /// </summary>
    /// <param name="levelIndex">Level index to test</param>
    public void EnableTestMode(int levelIndex)
    {
        testMode = true;
        testLevelIndex = levelIndex;

        if (showTestModeWarning)
        {
            Debug.LogWarning($"=== TEST MODE ENABLED ===\nChỉ chơi level test: {testLevelIndex}");
        }
    }

    /// <summary>
    /// Disable test mode
    /// </summary>
    public void DisableTestMode()
    {
        testMode = false;
        Debug.Log("=== TEST MODE DISABLED ===\nChế độ chơi bình thường đã được kích hoạt!");
    }

    /// <summary>
    /// Check if test mode is enabled
    /// </summary>
    /// <returns>True if test mode is enabled</returns>
    public bool IsTestModeEnabled()
    {
        return testMode;
    }

    /// <summary>
    /// Get current test level index
    /// </summary>
    /// <returns>Test level index</returns>
    public int GetTestLevelIndex()
    {
        return testLevelIndex;
    }

    /// <summary>
    /// Set test level index
    /// </summary>
    /// <param name="levelIndex">New test level index</param>
    public void SetTestLevelIndex(int levelIndex)
    {
        testLevelIndex = levelIndex;

        if (testMode && showTestModeWarning)
        {
            Debug.LogWarning($"Test level đã được thay đổi thành: {testLevelIndex}");
        }
    }

    /// <summary>
    /// Toggle test mode warning
    /// </summary>
    /// <param name="show">Whether to show test mode warnings</param>
    public void SetTestModeWarning(bool show)
    {
        showTestModeWarning = show;
    }
    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// Editor method to refresh level objects
    /// </summary>
    [ContextMenu("Refresh Level Objects")]
    public void RefreshLevelObjects()
    {
        LoadLevelObjectsFromResources();
    }

    /// <summary>
    /// Editor method to enable test mode
    /// </summary>
    [ContextMenu("Enable Test Mode")]
    public void EditorEnableTestMode()
    {
        EnableTestMode(testLevelIndex);
    }

    /// <summary>
    /// Editor method to disable test mode
    /// </summary>
    [ContextMenu("Disable Test Mode")]
    public void EditorDisableTestMode()
    {
        DisableTestMode();
    }
#endif
}