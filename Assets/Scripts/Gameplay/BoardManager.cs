using UnityEngine;
using RescuePrincess.Managers;

namespace RescuePrincess.Gameplay
{
    public class BoardManager : MonoBehaviour
    {
        [Header("Game Configuration")]
        [Tooltip("General game configuration")]
        public GameConfig gameConfig;

        [Header("Layer Settings")]
        public LayerMask boxSelectorLayerMask;

        [Header("Path Settings")]
        public BoxCollider2D pathCollider;
        public ColorConfig colorConfig;

        [Header("Level Management")]
        [SerializeField] private LevelObject levelObject;
        [SerializeField] private int currentLevelIndex = 1;

        [Header("Game State")]
        [SerializeField] private bool isGameActive = false;

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            // Don't auto-initialize level - will be controlled by GameManager
            Debug.Log("BoardManager is ready, waiting for GameManager to load level");
        }

        #region Level Management
        /// <summary>
        /// Load level by index
        /// </summary>
        /// <param name="levelIndex">Index of the level to load</param>
        /// <returns>True if load successful, false if failed</returns>
        public bool LoadLevel(int levelIndex)
        {
            Debug.Log($"BoardManager is loading level {levelIndex}");

            // Check LevelManager
            if (GameManager.Instance?.LevelManager == null)
            {
                Debug.LogError("LevelManager does not exist!");
                return false;
            }

            // Get level object from LevelManager
            LevelObject newLevelObject = GameManager.Instance.LevelManager.GetLevelObject(levelIndex);
            if (newLevelObject == null)
            {
                Debug.LogError($"Level object with index {levelIndex} not found!");
                return false;
            }

            // Instantiate level object
            LevelObject instantiatedLevel = GameManager.Instance.LevelManager.InstantiateLevel(levelIndex, transform);
            if (instantiatedLevel == null)
            {
                Debug.LogError($"Cannot instantiate level {levelIndex}!");
                return false;
            }


            // Update level object and path collider
            levelObject = instantiatedLevel;

            // Initialize level
            levelObject.Init();
            pathCollider = levelObject.pathCollider;
            currentLevelIndex = levelIndex;

            Debug.Log($"Successfully loaded level {levelIndex}");
            return true;
        }

        /// <summary>
        /// Load level from existing prefab (for cases where level already exists in scene)
        /// </summary>
        /// <param name="levelObjectPrefab">LevelObject prefab to load</param>
        /// <returns>True if load successful, false if failed</returns>
        public bool LoadLevelFromPrefab(LevelObject levelObjectPrefab)
        {
            if (levelObjectPrefab == null)
            {
                Debug.LogError("LevelObject prefab not provided!");
                return false;
            }

            Debug.Log($"BoardManager is loading level from prefab: {levelObjectPrefab.name}");

            // Instantiate level object
            GameObject instantiatedLevel = Instantiate(levelObjectPrefab.gameObject, transform);
            LevelObject newLevelObject = instantiatedLevel.GetComponent<LevelObject>();

            if (newLevelObject == null)
            {
                Debug.LogError("Instantiated level does not have LevelObject component!");
                Destroy(instantiatedLevel);
                return false;
            }

            // Update level object and path collider
            levelObject = newLevelObject;
            pathCollider = levelObject.pathCollider;
            currentLevelIndex = levelObject.levelIndex;

            // Initialize level
            levelObject.Init();

            Debug.Log($"Successfully loaded level from prefab: {levelObjectPrefab.name}");
            return true;
        }

        /// <summary>
        /// Get current level index
        /// </summary>
        public int GetCurrentLevelIndex()
        {
            return currentLevelIndex;
        }

        /// <summary>
        /// Get current level object
        /// </summary>
        public LevelObject GetCurrentLevelObject()
        {
            return levelObject;
        }

        /// <summary>
        /// Clear current level
        /// </summary>
        public void ClearCurrentLevel()
        {
            if (levelObject != null)
            {
                Debug.Log($"Clearing current level: {levelObject.name}");
                DestroyImmediate(levelObject.gameObject);
                levelObject = null;
                pathCollider = null;
            }
        }
        #endregion

        private void Update()
        {
            // Only process input when game is running and not in GameOver state
            if (!IsGameActive() || GameManager.Instance?.CurrentGameState == GameState.GameOver) return;

            if (Input.GetMouseButtonDown(0))
            {
                Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                //Debug.Log($"Mouse clicked at world position: {worldPos}");

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity, boxSelectorLayerMask);
                //Debug.Log($"Found {hits.Length} colliders hit by raycast (filtered by boxSelectorLayerMask)");

                foreach (RaycastHit2D hit in hits)
                {
                    Debug.Log($"Checking collider: {hit.collider.name} on layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

                    ColorBox colorBox = hit.collider.GetComponentInParent<ColorBox>();
                    if (colorBox != null)
                    {
                        // Debug.Log($"Found ColorBox: {colorBox.name}, calling OnTap()");
                        colorBox.OnTap();
                        return;
                    }
                }

                Debug.LogWarning("No ColorBox component found in any collider hit by raycast");
            }
        }

        #region Game State Management
        public void SetGameActive(bool active)
        {
            isGameActive = active;
            Debug.Log($"BoardManager game active: {active}");
        }

        public bool IsGameActive()
        {
            // Fallback to local state
            return isGameActive;
        }

        // TODO: Implement when GameManager is ready
        // public void OnGameStateChanged(GameState previousState, GameState newState)
        // {
        //     Debug.Log($"BoardManager received state change from {previousState} to {newState}");
        //     
        //     switch (newState)
        //     {
        //         case GameState.Playing:
        //             SetGameActive(true);
        //             break;
        //             
        //         case GameState.Paused:
        //         case GameState.GameOver:
        //         case GameState.Victory:
        //         case GameState.MainMenu:
        //             SetGameActive(false);
        //             break;
        //     }
        // }
        #endregion

        private void OnDrawGizmos()
        {
            if (pathCollider == null) return;
            Gizmos.color = Color.red;
            Vector2 center = pathCollider.transform.TransformPoint(pathCollider.offset);
            Vector2 size = Vector2.Scale(pathCollider.size, pathCollider.transform.lossyScale);
            Vector3[] corners = new Vector3[5];
            corners[0] = center + new Vector2(-size.x / 2, -size.y / 2); // Bottom Left
            corners[1] = center + new Vector2(-size.x / 2, size.y / 2);  // Top Left
            corners[2] = center + new Vector2(size.x / 2, size.y / 2);   // Top Right
            corners[3] = center + new Vector2(size.x / 2, -size.y / 2);  // Bottom Right
            corners[4] = corners[0];
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}