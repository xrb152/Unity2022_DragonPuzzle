using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Spine.Unity;
using RescuePrincess.Managers;
using RescuePrincess.Gameplay;
using RescuePrincess.UI;

public class ChainController : MonoBehaviour
{
    [Header("Dragon Parts")]
    public GameObject headPrefab;
    public GameObject bodyPrefab;
    public GameObject tailPrefab;
    [Tooltip("Size will be calculated from levelData.colorIndices if available")]
    [HideInInspector] public int size = 0;

    [Header("Level Configuration")]
    [Tooltip("Level data containing color indices for dragon body segments")]
    public LevelData levelData;

    [Header("Color Configuration")]
    [Tooltip("Color config for dragon body sprites")]
    public ColorConfig colorConfig;

    [Header("HP Management")]
    [SerializeField] private int maxHP = 10;
    [SerializeField] private int currentHP = 10;

    [Header("Path Settings")]
    public List<Transform> waypoints = new List<Transform>();
    public float segmentDistance = 1.0f;
    public float moveSpeed = 5f;
    public float rotationSpeed = 360f; // degrees/second

    [Header("Reward Retreat")]
    [Tooltip("看广告奖励让怪物后退的身位数（1 身位 = 一个身体节距）")]
    public int retreatSteps = 5;

    //[HideInInspector]
    [HideInInspector] public List<Transform> bodySegments = new List<Transform>(); // Head (index 0) + Body + Tail (last index)
    private List<BodySegment> bodySegmentComponents = new List<BodySegment>(); // Cache BodySegment components
    private List<Collider2D> bodySegmentColliders = new List<Collider2D>(); // Cache Collider2D components
    private List<SpriteRenderer> bodySegmentSpriteRenderers = new List<SpriteRenderer>(); // Cache SpriteRenderer components
    private List<SkeletonAnimation> bodySegmentSpineAnimations = new List<SkeletonAnimation>(); // Cache SkeletonAnimation components

    private List<Marker> markerList = new List<Marker>();
    private int currentWaypointIndex = 0;
    private bool isMovingBackward = false; // Flag to pause markerList updates
    private float backwardStartTime = 0f;  // 后退动画开始时间，用于超时兜底

    // Dragon state
    public enum DragonState
    {
        PAUSE,  // Pause movement
        WALK,   // Moving
        RESIZE, // Changing size
        DEAD    // Dragon is dead
    }

    [Header("Dragon State")]
    [SerializeField] private DragonState currentDragonState = DragonState.WALK;

    // Events
    public System.Action<DragonState> OnDragonStateChanged;
    public System.Action<int, int> OnHPChanged; // currentHP, maxHP
    public System.Action OnDragonDefeated;

    // Properties to access head and tail
    public Transform Head => bodySegments.Count > 0 ? bodySegments[0] : null;
    public Transform Tail => bodySegments.Count > 0 ? bodySegments[bodySegments.Count - 1] : null;

    // Properties to access dragon state
    public DragonState CurrentDragonState => currentDragonState;
    
    // Properties to access HP
    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public float HPPercentage => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public bool IsDefeated => currentHP <= 0;

    // Marker class to store position, rotation and waypoint index
    public class Marker
    {
        public Vector3 position;
        public Quaternion rotation;
        public int waypointIndex;

        public Marker(Vector3 pos, Quaternion rot, int wpIndex)
        {
            position = pos;
            rotation = rot;
            waypointIndex = wpIndex;
        }
    }

    void Start()
    {
        //Debug.Log("ChainController Start");
        // Calculate size from levelData if available
        CalculateSizeFromLevelData();
        
        // Initialize HP system
        InitializeHP();
        
        CreateBodyParts();
    }

    /// <summary>
    /// Calculate size from levelData.colorIndices
    /// </summary>
    private void CalculateSizeFromLevelData()
    {
        if (levelData != null)
        {
            int newSize = levelData.GetSegmentCount();
            if (newSize > 0)
            {
                size = newSize;
                //Debug.Log($"Calculated dragon size from LevelData: {size} segments");
            }
            else
            {
                Debug.LogWarning("LevelData has no segments, using default size");
            }
        }
        else
        {
            //Debug.Log("No LevelData assigned, using default size");
        }
    }

    /// <summary>
    /// Initialize HP system
    /// </summary>
    private void InitializeHP()
    {
        // Set max HP based on number of body segments
        maxHP = size;
        currentHP = maxHP;
        
        // Update UI
        UpdateHPUI();
        
        // Trigger event
        OnHPChanged?.Invoke(currentHP, maxHP);
        
        Debug.Log($"HP system initialized with max HP: {maxHP}");
    }

    // Create dragon body parts
    public void CreateBodyParts()
    {
        // Ensure size is calculated from levelData
        CalculateSizeFromLevelData();
        
        markerList.Clear();

        // Clear old body if exists
        foreach (var seg in bodySegments)
            if (seg != null) Destroy(seg.gameObject);
        bodySegments.Clear();
        bodySegmentComponents.Clear(); // Clear cached components
        bodySegmentColliders.Clear(); // Clear cached colliders
        bodySegmentSpriteRenderers.Clear(); // Clear cached sprite renderers
        bodySegmentSpineAnimations.Clear(); // Clear cached spine animations

        // Calculate starting position from first waypoint
        Vector3 startPosition = waypoints != null && waypoints.Count > 0 ? waypoints[0].position : transform.position;

        // Spawn head at first position
        GameObject headObj = Instantiate(headPrefab, startPosition, Quaternion.identity, transform);

        // Setup head segment
        BodySegment headSegment = headObj.GetComponent<BodySegment>();
        if (headSegment == null)
        {
            headSegment = headObj.AddComponent<BodySegment>();
        }
        headSegment.SetSegmentType(BodySegment.SegmentType.HEAD);

        bodySegments.Add(headObj.transform);
        bodySegmentComponents.Add(headSegment); // Cache the component
        markerList.Add(new Marker(headObj.transform.position, headObj.transform.rotation, currentWaypointIndex));

        // Set sort order for head (highest)
        headSegment.SetAutoSortOrder(0, size + 2); // +2 because there's head and tail

        // Cache all components of head
        CacheSegmentComponents(headObj);

        // Spawn body segments according to size, each segment separated by segmentDistance in up direction
        for (int i = 0; i < size; i++)
        {
            Vector3 bodyPosition = startPosition + Vector3.up * segmentDistance * (i + 1);
            GameObject bodyObj = Instantiate(bodyPrefab, bodyPosition, Quaternion.identity, transform);
            bodyObj.name = $"BodySegment_{i}";
            bodySegments.Add(bodyObj.transform);

            // Add component to handle tap
            BodySegmentTapHandler tapHandler = bodyObj.GetComponent<BodySegmentTapHandler>();
            if (tapHandler == null)
            {
                tapHandler = bodyObj.AddComponent<BodySegmentTapHandler>();
            }

            // Setup segment type and color index
            BodySegment bodySegment = bodyObj.GetComponent<BodySegment>();
            if (bodySegment == null)
            {
                bodySegment = bodyObj.AddComponent<BodySegment>();
            }
            bodySegment.SetSegmentType(BodySegment.SegmentType.BODY);

            // Subscribe tap event
            bodySegment.OnSegmentTapped += OnBodySegmentTapped;

            // Cache the component
            bodySegmentComponents.Add(bodySegment);

            // Cache all components of body segment
            CacheSegmentComponents(bodyObj);

            // Set sort order for body segment (decreasing)
            bodySegment.SetAutoSortOrder(i + 1, size + 2); // +1 because head already occupies index 0

            // Apply dragon body sprite and color to body segment
            ApplyDragonBodySpriteToSegment(bodyObj, i);
        }

        // Spawn tail at final position
        Vector3 tailPosition = startPosition + Vector3.up * segmentDistance * (size + 1);
        GameObject tailObj = Instantiate(tailPrefab, tailPosition, Quaternion.identity, transform);

        // Setup tail segment
        BodySegment tailSegment = tailObj.GetComponent<BodySegment>();
        if (tailSegment == null)
        {
            tailSegment = tailObj.AddComponent<BodySegment>();
        }
        tailSegment.SetSegmentType(BodySegment.SegmentType.TAIL);

        bodySegments.Add(tailObj.transform);
        bodySegmentComponents.Add(tailSegment); // Cache the component

        // Cache all components of tail
        CacheSegmentComponents(tailObj);

        // Set sort order for tail (lowest)
        tailSegment.SetAutoSortOrder(size + 1, size + 2); // Tail at the end

        // Update HP when dragon is recreated
        SetMaxHP(size);
        
        // Check if size = 0 then dragon dies
        if (size <= 0)
        {
            Debug.Log("Dragon size = 0, setting to DEAD state");
            SetDragonState(DragonState.DEAD);
        }

        Debug.Log($"Created dragon with {bodySegments.Count} segments: Head + {size} Body + Tail");
    }

    // Helper method to cache all components of a segment
    private void CacheSegmentComponents(GameObject segmentObj)
    {
        // Cache Collider2D
        Collider2D collider = segmentObj.GetComponent<Collider2D>();
        bodySegmentColliders.Add(collider);

        // Cache SpriteRenderer
        SpriteRenderer spriteRenderer = segmentObj.GetComponent<SpriteRenderer>();
        bodySegmentSpriteRenderers.Add(spriteRenderer);

        // Cache SkeletonAnimation
        SkeletonAnimation spineAnimation = segmentObj.GetComponent<SkeletonAnimation>();
        bodySegmentSpineAnimations.Add(spineAnimation);
    }

    void FixedUpdate()
    {
        // 兜底：后退动画如果被打断（目标销毁 / DOTween 被 Kill），标记也要复位，
        // 否则龙会永远停在"正在后退"状态不再前进
        if (isMovingBackward && Time.time - backwardStartTime > 3f)
        {
            isMovingBackward = false;
            Debug.LogWarning("[退后] 后退动画超时，已自动复位");
        }

        ManageBody();

        // Check and handle body segments with DISABLE state
        CheckAndHandleDisableSegments();

        // Check HP = 0 and switch to DEAD state if needed
        CheckAndHandleDeadState();

        // Only move when dragon is in WALK state (don't move when DEAD)
        // 后退动画期间暂停前进，否则 DOTween 和 MoveTowards 会互相打架
        if (currentDragonState == DragonState.WALK && !isMovingBackward)
        {
            DragonMovement();
        }

        // Check B key to move dragon backward (only when WALKING)
        if (Input.GetKeyDown(KeyCode.B) && currentDragonState == DragonState.WALK)
        {
            MoveDragonBackward();
        }
    }

    void Update()
    {
        // Input handling for Pause and Resume
        HandlePauseResumeInput();

        // Test input for DISABLE segments
        HandleTestInput();
    }

    // Method to handle Pause and Resume input
    private void HandlePauseResumeInput()
    {
        // P key to Pause
        if (Input.GetKeyDown(KeyCode.P))
        {
            PauseDragon();
        }

        // R key to Resume (Walk)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResumeDragon();
        }
    }

    // Method to handle test input
    private void HandleTestInput()
    {
        // T key to test disable random 3 body segments
        // if (Input.GetKeyDown(KeyCode.T))
        // {
        //     TestDisableRandomBodySegments();
        // }

        // // I key to debug size info
        // if (Input.GetKeyDown(KeyCode.I))
        // {
        //     DebugSizeInfo();
        // }

        // // H key to debug HP info
        // if (Input.GetKeyDown(KeyCode.H))
        // {
        //     DebugHPInfo();
        // }

        // // X key to test set dragon to DEAD state
        // if (Input.GetKeyDown(KeyCode.X))
        // {
        //     TestSetDragonDead();
        // }
    }

    // Manage marker list and refresh dragon when broken
    void ManageBody()
    {
        // Refresh dragon - remove null segments and update
        for (int i = 0; i < bodySegments.Count; i++)
        {
            if (bodySegments[i] == null)
            {
                bodySegments.RemoveAt(i);
                bodySegmentComponents.RemoveAt(i); // Remove cached component too
                bodySegmentColliders.RemoveAt(i); // Remove cached collider
                bodySegmentSpriteRenderers.RemoveAt(i); // Remove cached sprite renderer
                bodySegmentSpineAnimations.RemoveAt(i); // Remove cached spine animation
                i = i - 1;

                // If segments remain and head is lost, update head
                if (bodySegments.Count > 0 && bodySegments[0] != null)
                {
                    // Remove old markers corresponding to lost segment
                    int markersToRemove = (int)segmentDistance;
                    if (markerList.Count >= markersToRemove)
                    {
                        markerList.RemoveRange(0, markersToRemove);
                    }
                }
            }
        }

        // If no segments remain, destroy dragon
        if (bodySegments.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // Update marker from head (only when not moving backward and dragon is WALKING)
        if (!isMovingBackward && currentDragonState == DragonState.WALK && bodySegments.Count > 0 && bodySegments[0] != null)
        {
            markerList.Insert(0, new Marker(bodySegments[0].transform.position, bodySegments[0].transform.rotation, currentWaypointIndex));
        }
    }

    void DragonMovement()
    {
        if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count || bodySegments.Count == 0) return;

        // Move head (index 0)
        Transform head = bodySegments[0];
        if (head != null)
        {
            Vector3 target = waypoints[currentWaypointIndex].position;
            Vector3 direction = target - head.position;

            // Move head
            head.position = Vector3.MoveTowards(head.position, target, moveSpeed * Time.deltaTime);

            // Rotate head towards movement direction (only when still away from waypoint)
            if (direction.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
                Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
                head.rotation = Quaternion.RotateTowards(
                    head.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            // If head gets close to waypoint, move to next waypoint
            if (Vector3.Distance(head.position, target) < 0.05f)
            {
                currentWaypointIndex++;
                
                // Check if reached final waypoint
                if (currentWaypointIndex >= waypoints.Count)
                {
                    OnDragonReachedFinalWaypoint();
                }
            }
        }

        // Move body segments and tail (from index 1 onwards)
        for (int i = 1; i < bodySegments.Count; i++)
        {
            if (bodySegments[i] != null)
            {
                int index = i * (int)segmentDistance - 1;
                if (index < markerList.Count)
                {
                    bodySegments[i].transform.position = markerList[index].position;
                    bodySegments[i].transform.rotation = markerList[index].rotation;
                }
            }
        }
    }

    // Helper methods to access head and tail
    public Transform GetHead()
    {
        return Head;
    }

    public Transform GetTail()
    {
        return Tail;
    }

    public int GetBodySegmentCount()
    {
        return bodySegments.Count > 2 ? bodySegments.Count - 2 : 0; // Subtract head and tail
    }

    // Method to recreate entire dragon
    public void RecreateDragon()
    {
        CreateBodyParts();
    }

    // Method to change dragon size
    public void SetDragonSize(int newSize)
    {
        if (newSize != size)
        {
            // Switch to RESIZE state before changing
            SetDragonState(DragonState.RESIZE);

            size = newSize;
            RecreateDragon();

            // After resize is complete, switch back to WALK state
            SetDragonState(DragonState.WALK);
        }
    }

    /// <summary>
    /// Update size from levelData and recreate dragon
    /// </summary>
    public void UpdateSizeFromLevelData()
    {
        if (levelData != null)
        {
            int newSize = levelData.GetSegmentCount();
            if (newSize > 0 && newSize != size)
            {
                Debug.Log($"Updating dragon size from {size} to {newSize} based on LevelData");
                SetDragonSize(newSize);
            }
        }
    }

    /// <summary>
    /// Get current dragon size (calculated from levelData if available)
    /// </summary>
    /// <returns>Number of body segments</returns>
    public int GetCurrentSize()
    {
        if (levelData != null)
        {
            int levelDataSize = levelData.GetSegmentCount();
            if (levelDataSize > 0)
            {
                return levelDataSize;
            }
        }
        return size;
    }

    /// <summary>
    /// Check if size matches levelData
    /// </summary>
    /// <returns>True if size matches levelData</returns>
    public bool IsSizeValid()
    {
        if (levelData != null)
        {
            int levelDataSize = levelData.GetSegmentCount();
            return levelDataSize > 0 && levelDataSize == size;
        }
        return true; // If no levelData, consider as valid
    }

    /// <summary>
    /// Debug information about size and levelData
    /// </summary>
    public void DebugSizeInfo()
    {
        Debug.Log("=== Dragon Size Debug Info ===");
        Debug.Log($"Current size: {size}");
        Debug.Log($"LevelData assigned: {levelData != null}");
        
        if (levelData != null)
        {
            Debug.Log($"LevelData name: {levelData.levelName}");
            Debug.Log($"LevelData segment count: {levelData.GetSegmentCount()}");
            Debug.Log($"LevelData color indices: {levelData.GetColorIndicesString()}");
        }
        
        Debug.Log($"GetCurrentSize(): {GetCurrentSize()}");
        Debug.Log($"IsSizeValid(): {IsSizeValid()}");
        Debug.Log($"Body segments count: {bodySegments.Count}");
        Debug.Log("=============================");
    }

    // Method to handle tap on body segment
    public void OnBodySegmentTapped(BodySegment segment)
    {
        Debug.Log($"Body segment tapped: {segment.GetSegmentInfo()}");

        // Toggle segment state
        segment.ToggleState();

        // If segment just switched to DISABLE state, trigger immediate check
        if (segment.CurrentState == BodySegment.SegmentState.DISABLE)
        {
            Debug.Log($"Segment {segment.name} is now DISABLE, triggering immediate check");
            // Trigger immediate check instead of waiting for FixedUpdate
            StartCoroutine(CheckDisableSegmentsNextFrame());
        }
    }

    // Method to set dragon state
    public void SetDragonState(DragonState newState)
    {
        if (currentDragonState != newState)
        {
            DragonState oldState = currentDragonState;
            currentDragonState = newState;

            // Handle logic when state changes
            OnDragonStateChanged?.Invoke(currentDragonState);

            //Debug.Log($"Dragon state changed from {oldState} to {currentDragonState}");

            // Handle specific logic for each state
            HandleDragonStateChange(oldState, currentDragonState);
        }
    }

    // Method to handle logic when dragon state changes
    private void HandleDragonStateChange(DragonState oldState, DragonState newState)
    {
        switch (newState)
        {
            case DragonState.PAUSE:
                HandlePauseState();
                break;
            case DragonState.WALK:
                HandleWalkState();
                break;
            case DragonState.RESIZE:
                HandleResizeState();
                break;
            case DragonState.DEAD:
                HandleDeadState();
                break;
        }
    }

    // Handle PAUSE state
    private void HandlePauseState()
    {
        Debug.Log("Dragon has paused movement");

        // Stop head animation if available
        if (Head != null)
        {
            BodySegment headSegment = Head.GetComponent<BodySegment>();
            if (headSegment != null)
            {
                // Can set FREEZING state for head to stop animation
                headSegment.SetState(BodySegment.SegmentState.FREEZING);
            }
        }

        // Can add other pause effect logic here

        // Can add other logic such as:
        // - Show pause UI
        // - Stop background music
        // - Show pause indicator
    }

    // Handle WALK state
    private void HandleWalkState()
    {
        //Debug.Log("Dragon is moving");

        // Restore head animation if available
        if (Head != null)
        {
            BodySegment headSegment = Head.GetComponent<BodySegment>();
            if (headSegment != null)
            {
                // Restore RUNNING state for head
                headSegment.SetState(BodySegment.SegmentState.RUNNING);
            }
        }

        // Can add other restore effect logic here

        // Can add other logic such as:
        // - Hide pause UI
        // - Restore background music
        // - Hide pause indicator
    }

    // Handle RESIZE state
    private void HandleResizeState()
    {
        //Debug.Log("Dragon is changing size");

        // Can add logic such as:
        // - Show resize UI
        // - Change animation
        // - Prepare for resize
    }

    // Handle DEAD state
    private void HandleDeadState()
    {
        Debug.Log("Dragon is dead - stop movement and play die animation");

        // Stop movement
        // DragonMovement() will not be called when state = DEAD

        // Play die animation for head
        if (Head != null)
        {
            BodySegment headSegment = Head.GetComponent<BodySegment>();
            if (headSegment != null)
            {
                // Set DISABLE state for head to trigger die animation
                headSegment.SetState(BodySegment.SegmentState.DISABLE);
            }

            // Play spine die animation if available
            SkeletonAnimation headSpine = headSegment.spineAnimation;
            if (headSpine != null)
            {
                // Find die animation in spine data
                if (headSpine.Skeleton.Data.FindAnimation("die") != null)
                {
                    headSpine.AnimationState.SetAnimation(0, "die", false);
                    Debug.Log("Playing die animation for dragon head");
                }
                else if (headSpine.Skeleton.Data.FindAnimation("death") != null)
                {
                    headSpine.AnimationState.SetAnimation(0, "death", false);
                    Debug.Log("Playing death animation for dragon head");
                }
                else
                {
                    Debug.LogWarning("No die/death animation found in spine data");
                }
            }
        }

        // Can add other logic such as:
        // - Show game over UI
        // - Play sound effect
        // - Particle effects
    }

    // Utility methods to set state
    public void PauseDragon()
    {
        SetDragonState(DragonState.PAUSE);
    }

    public void WalkDragon()
    {
        SetDragonState(DragonState.WALK);
    }

    public void ResizeDragon()
    {
        SetDragonState(DragonState.RESIZE);
    }

    // Method to toggle between PAUSE and WALK
    public void TogglePauseWalk()
    {
        if (currentDragonState == DragonState.PAUSE)
        {
            WalkDragon();
        }
        else if (currentDragonState == DragonState.WALK)
        {
            PauseDragon();
        }
    }

    // Method to Resume dragon (switch from PAUSE to WALK)
    public void ResumeDragon()
    {
        if (currentDragonState == DragonState.PAUSE)
        {
            WalkDragon();
            Debug.Log("Dragon has been Resumed - continue moving");
        }
        else
        {
            Debug.Log("Dragon is not in PAUSE state so cannot Resume");
        }
    }

    // Method to set sort order for all segments
    public void SetAllSegmentsSortOrder()
    {
        int totalSegments = bodySegments.Count;

        for (int i = 0; i < bodySegments.Count; i++)
        {
            if (bodySegments[i] != null)
            {
                BodySegment segment = bodySegments[i].GetComponent<BodySegment>();
                if (segment != null)
                {
                    segment.SetAutoSortOrder(i, totalSegments);
                }
            }
        }
    }



    // Function to move dragon backward to past position
    private void MoveDragonBackward()
    {
        PushDragonBack(retreatSteps);
    }

    /// <summary>
    /// 让整条龙沿走过的路径后退 steps 个身位（1 身位 = segmentDistance 个轨迹点）
    /// 只要身后还有历史轨迹就一定会退，轨迹不足时按实际距离退（不足 steps 节）
    /// </summary>
    /// <returns>实际后退的身位数（可能小于 steps），0 表示完全无路可退（刚出生 / 正在后退 / 已死亡）</returns>
    public int PushDragonBack(int steps = -1)
    {
        if (steps <= 0)
            steps = Mathf.Max(1, retreatSteps);

        if (isMovingBackward || bodySegments.Count == 0)
            return 0;

        if (currentDragonState == DragonState.DEAD)
            return 0;

        // 至少要有两个采样点（一个给自己，一个给身后）才谈得上后退
        if (markerList.Count < 2)
        {
            Debug.LogWarning($"[退后] 怪物身后还没有轨迹（markers={markerList.Count}），本次无法后退");
            return 0;
        }

        int stepMarkers = Mathf.Max(1, (int)segmentDistance);
        int lastIndex = markerList.Count - 1;

        // 想退的点数，按实际存在的轨迹截断：能退多少退多少，绝不因为"够不够 5 节"就放弃
        int wantedIndex = steps * stepMarkers;
        int backwardIndex = Mathf.Clamp(wantedIndex, 1, lastIndex);
        bool shortage = backwardIndex < wantedIndex;

        int moveSteps = Mathf.Max(1, Mathf.RoundToInt(backwardIndex / (float)stepMarkers));
        float duration = Mathf.Clamp(0.35f + 0.05f * Mathf.Min(moveSteps, 12), 0.35f, 1.0f);

        // Enable flag to pause markerList updates
        isMovingBackward = true;
        backwardStartTime = Time.time;

        // Create main sequence for entire dragon
        Sequence dragonSequence = DOTween.Sequence();

        // 采样步长：沿轨迹取中间点，让每一节"顺着自己的轨迹"滑回去。
        // 之前是直接朝终点直线插值，遇到弯道会互相穿插、挤成一堆
        int sampleStep = Mathf.Max(1, stepMarkers / 4);

        // Move head using DOTween
        Transform head = bodySegments[0];
        if (head != null)
            MoveAlongTrail(dragonSequence, head, 0, backwardIndex, duration, sampleStep);

        // Move body segments and tail using DOTween
        int segmentCount = bodySegments.Count;
        int skipped = 0;
        for (int segmentIndex = 1; segmentIndex < segmentCount; segmentIndex++)
        {
            Transform seg = bodySegments[segmentIndex];
            if (seg == null)
                continue;

            // 该节当前对应的轨迹点（和 DragonMovement 的跟随规则保持一致）
            int ownIndex = segmentIndex * stepMarkers - 1;

            // 还没走上轨迹（还停在出生点的初始排列里，通常在画面外）：
            // 让它留在原地，不参与这次后退，否则会被硬拉到轨迹末端和别的节叠在一起
            if (ownIndex > lastIndex)
            {
                skipped++;
                continue;
            }

            MoveAlongTrail(dragonSequence, seg, ownIndex, backwardIndex + ownIndex, duration, sampleStep);
        }

        // Complete entire animation
        dragonSequence.OnComplete(() =>
        {
            // 丢掉已经走过的轨迹，让队首回到当前位置
            int markersToRemove = Mathf.Clamp(backwardIndex, 0, markerList.Count);
            if (markersToRemove > 0)
                markerList.RemoveRange(0, markersToRemove);

            isMovingBackward = false;
            UpdateWaypointIndexAfterBackward();

            if (shortage)
                Debug.Log($"[退后] 轨迹不足，本次只后退了约 {moveSteps} 节（请求 {steps} 节，可用轨迹 {backwardIndex} 点）");
            else
                Debug.Log($"[退后] 怪物后退了 {moveSteps} 个身位");

            if (skipped > 0)
                Debug.Log($"[退后] 有 {skipped} 节还在轨迹之外（出生点上方），本次未参与后退");
        });

        return moveSteps;
    }

    /// <summary>
    /// 把某一节从轨迹点 fromIndex 顺着轨迹滑到 toIndex（toIndex 超出轨迹末端时按轨迹方向直线延伸），
    /// 全程保持与相邻节相等的间距，不会因为直线插值而互相重叠
    /// </summary>
    private void MoveAlongTrail(Sequence seq, Transform seg, int fromIndex, int toIndex, float duration, int sampleStep)
    {
        Quaternion targetRot;
        Vector3 targetPos = GetTrailPoint(toIndex, out targetRot);
        Vector3[] path = BuildTrailPath(fromIndex, toIndex, seg.position, sampleStep);

        if (path != null)
            seq.Join(seg.DOPath(path, duration, PathType.Linear, PathMode.TopDown2D).SetEase(Ease.OutQuad));
        else
            seq.Join(seg.DOMove(targetPos, duration).SetEase(Ease.OutQuad));

        seq.Join(seg.DORotate(targetRot.eulerAngles, duration).SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// 生成从当前位置沿轨迹到 toIndex 的点集（至少 2 个点，否则返回 null 让调用方退回直线移动）
    /// </summary>
    private Vector3[] BuildTrailPath(int fromIndex, int toIndex, Vector3 currentPos, int sampleStep)
    {
        List<Vector3> pts = new List<Vector3>(32);
        AddUniquePoint(pts, currentPos);

        for (int idx = fromIndex + sampleStep; idx < toIndex && idx < markerList.Count; idx += sampleStep)
            AddUniquePoint(pts, markerList[idx].position);

        Quaternion rot;
        AddUniquePoint(pts, GetTrailPoint(toIndex, out rot));

        return pts.Count >= 2 ? pts.ToArray() : null;
    }

    private static void AddUniquePoint(List<Vector3> pts, Vector3 p)
    {
        if (pts.Count > 0 && (pts[pts.Count - 1] - p).sqrMagnitude < 0.000001f)
            return;
        pts.Add(p);
    }

    /// <summary>
    /// 取轨迹上第 index 个点（0 = 队首 / 最新，越大越"旧"）。
    /// index 超出已记录的轨迹时，沿轨迹起点方向做直线延伸 —— 这样即使整条龙比走过的轨迹还长，
    /// 尾部也只是"退回关卡起点外"，节与节之间的间距依然相等，绝不会叠在一起
    /// </summary>
    private Vector3 GetTrailPoint(int index, out Quaternion rot)
    {
        int last = markerList.Count - 1;
        index = Mathf.Max(0, index);

        if (index <= last)
        {
            rot = markerList[index].rotation;
            return markerList[index].position;
        }

        rot = markerList[last].rotation;

        int span = Mathf.Min(10, last);
        Vector3 dir;
        float unitPerMarker;

        if (span > 0)
        {
            Vector3 delta = markerList[last].position - markerList[last - span].position;
            unitPerMarker = delta.magnitude / span;
            dir = delta.sqrMagnitude > 1e-10f ? delta.normalized : Vector3.down;
        }
        else
        {
            // 轨迹还没成型：用头部朝向的反方向兜底
            dir = -(markerList[last].rotation * Vector3.up);
            unitPerMarker = 0f;
        }

        if (unitPerMarker <= 1e-5f)
            unitPerMarker = Mathf.Max(0.001f, moveSpeed * Time.fixedDeltaTime);

        return markerList[last].position + dir * ((index - last) * unitPerMarker);
    }

    // Update waypoint index after moving backward
    private void UpdateWaypointIndexAfterBackward()
    {
        if (waypoints == null || waypoints.Count == 0 || bodySegments.Count == 0) return;

        Transform head = bodySegments[0];
        if (head == null) return;

        // Get waypoint index from first marker (current head position)
        if (markerList.Count > 0)
        {
            currentWaypointIndex = markerList[0].waypointIndex;

            // Ensure not exceeding number of waypoints
            if (currentWaypointIndex >= waypoints.Count)
            {
                currentWaypointIndex = waypoints.Count - 1;
            }

            // Ensure not negative
            if (currentWaypointIndex < 0)
            {
                currentWaypointIndex = 0;
            }

            //Debug.Log($"Updated waypoint index to {currentWaypointIndex} (from marker data)");
        }
        else
        {
            // If no markers remain, find nearest waypoint to head
            FindNearestWaypointToHead();
        }
    }

    // Method to find nearest waypoint to head when no markers remain
    private void FindNearestWaypointToHead()
    {
        if (waypoints == null || waypoints.Count == 0 || bodySegments.Count == 0) return;

        Transform head = bodySegments[0];
        if (head == null) return;

        float minDistance = float.MaxValue;
        int nearestWaypointIndex = 0;

        // Find nearest waypoint to head
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] != null)
            {
                float distance = Vector3.Distance(head.position, waypoints[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestWaypointIndex = i;
                }
            }
        }

        currentWaypointIndex = nearestWaypointIndex;
        Debug.Log($"No markers available. Found nearest waypoint at index {currentWaypointIndex} (distance: {minDistance:F2})");
    }

    private void OnDestroy()
    {
        // Unsubscribe all events when destroying
        foreach (var bodySegment in bodySegmentComponents)
        {
            if (bodySegment != null)
            {
                bodySegment.OnSegmentTapped -= OnBodySegmentTapped;
            }
        }

        // No need to unsubscribe because HP is managed directly in ChainController
    }

    // Method to check HP = 0 and switch to DEAD state
    private void CheckAndHandleDeadState()
    {
        // Only check when dragon is not already in DEAD state
        if (currentDragonState == DragonState.DEAD) return;

        // Check HP = 0 or size = 0
        if (IsDefeated || size <= 0)
        {
            Debug.Log("Dragon HP = 0 or size = 0, setting to DEAD state");
            SetDragonState(DragonState.DEAD);
            
            // Call GameManager to handle victory
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Victory();
            }
        }
    }

    // Method to check and handle body segments with DISABLE state
    private void CheckAndHandleDisableSegments()
    {
        // 后退动画期间不要剔除身体段，否则 bodySegments 会被中途移除，
        // 与正在播放的 DOTween 目标发生冲突
        if (isMovingBackward) return;

        // Only process when dragon is in WALK or RESIZE state (don't process when DEAD)
        if (currentDragonState != DragonState.WALK && currentDragonState != DragonState.RESIZE) return;

        // If in WALK state and found DISABLE segment, switch to RESIZE
        if (currentDragonState == DragonState.WALK)
        {
            int disableSegmentIndex = FindClosestDisableSegmentToHead();
            if (disableSegmentIndex != -1)
            {
                //Debug.Log($"Found DISABLE segment at index {disableSegmentIndex}, switching to RESIZE state");
                SetDragonState(DragonState.RESIZE);
                return; // Return to wait for next frame processing
            }
        }

        // If in RESIZE state, continue processing DISABLE segments
        if (currentDragonState == DragonState.RESIZE)
        {
            int disableSegmentIndex = FindClosestDisableSegmentToHead();
            if (disableSegmentIndex != -1)
            {
                // Process DISABLE segment
                ProcessDisableSegment(disableSegmentIndex);
            }
            else
            {
                // No more DISABLE segments, switch back to WALK
                SetDragonState(DragonState.WALK);
                //Debug.Log("No more DISABLE segments, switching back to WALK state");
            }
        }
    }

    // Method to find body segment with DISABLE state closest to head
    private int FindClosestDisableSegmentToHead()
    {
        for (int i = 0; i < bodySegmentComponents.Count; i++)
        {
            if (bodySegmentComponents[i] != null && bodySegmentComponents[i].CurrentState == BodySegment.SegmentState.DISABLE)
            {
                return i;
            }
        }
        return -1; // No segment found with DISABLE state
    }

    // Method to process segment with DISABLE state
    private void ProcessDisableSegment(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= bodySegments.Count) return;

        Transform disableSegment = bodySegments[segmentIndex];
        if (disableSegment == null) return;

       // Debug.Log($"=== Processing DISABLE segment at index {segmentIndex} ===");

        // 1. Disable this node (disable collider and sprite renderer)
        DisableSegmentComponents(segmentIndex);

        // 2. Reduce HP when body segment is disabled
        TakeDamage(1);

        // 3. Adjacent body segments up to head move backward by segmentDistance
        MoveSegmentsBackwardFromHead(segmentIndex);

        // 4. Remove this DISABLE state node from list
        RemoveSegmentFromList(segmentIndex);

        // 5. Check if there are more body segments with DISABLE state
        CheckRemainingDisableSegments();

        //Debug.Log($"=== Finished processing DISABLE segment at index {segmentIndex} ===");
    }

    // Method to disable segment components
    private void DisableSegmentComponents(int segmentIndex)
    {
        // Disable Collider2D
        if (segmentIndex < bodySegmentColliders.Count && bodySegmentColliders[segmentIndex] != null)
        {
            bodySegmentColliders[segmentIndex].enabled = false;
        }

        // Disable SpriteRenderer
        if (segmentIndex < bodySegmentSpriteRenderers.Count && bodySegmentSpriteRenderers[segmentIndex] != null)
        {
            bodySegmentSpriteRenderers[segmentIndex].enabled = false;
        }

        // Disable SkeletonAnimation if available (for head)
        if (segmentIndex < bodySegmentSpineAnimations.Count && bodySegmentSpineAnimations[segmentIndex] != null)
        {
            bodySegmentSpineAnimations[segmentIndex].enabled = false;
        }

        //Debug.Log($"Disabled components for segment at index {segmentIndex}");
    }

    // Method to move segments from head to DISABLE segment backward by one segment
    private void MoveSegmentsBackwardFromHead(int disableSegmentIndex)
    {
        // Calculate number of markers to remove (corresponding to segmentDistance)
        int markersToRemove = (int)segmentDistance;

        // Ensure not removing more than available markers
        if (markersToRemove > markerList.Count)
        {
            markersToRemove = markerList.Count;
        }

        // Save waypoint index before removing markers
        int oldWaypointIndex = currentWaypointIndex;

        // Remove markers from beginning with corresponding length
        if (markersToRemove > 0)
        {
            markerList.RemoveRange(0, markersToRemove);
            //Debug.Log($"Removed {markersToRemove} markers from markerList");
        }

        // Move segments from head to DISABLE segment backward
        for (int i = 0; i < disableSegmentIndex; i++)
        {
            if (bodySegments[i] != null)
            {
                // Calculate new position for segment
                int markerIndex = i * (int)segmentDistance - 1;
                if (markerIndex >= 0 && markerIndex < markerList.Count)
                {
                    // Move segment to new marker position
                    bodySegments[i].position = markerList[markerIndex].position;
                    bodySegments[i].rotation = markerList[markerIndex].rotation;
                }
                else if (markerIndex < 0 && markerList.Count > 0)
                {
                    // If not enough markers, use first marker
                    bodySegments[i].position = markerList[0].position;
                    bodySegments[i].rotation = markerList[0].rotation;
                }
            }
        }

        // Update waypoint index after moving backward
        //Debug.Log($"Before backward: waypoint index = {oldWaypointIndex}, markers count = {markerList.Count}");
        UpdateWaypointIndexAfterBackward();
        //Debug.Log($"After backward: waypoint index = {currentWaypointIndex}, markers count = {markerList.Count}");

       // Debug.Log($"Moved {disableSegmentIndex} segments backward by {segmentDistance} units. Waypoint index: {oldWaypointIndex} -> {currentWaypointIndex}");
    }

    // Method to remove segment from list
    private void RemoveSegmentFromList(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= bodySegments.Count) return;

        Transform segmentToRemove = bodySegments[segmentIndex];
        BodySegment bodySegmentComponent = bodySegmentComponents[segmentIndex];

        // Unsubscribe events before removing
        if (bodySegmentComponent != null)
        {
            bodySegmentComponent.OnSegmentTapped -= OnBodySegmentTapped;
        }

        // Remove from lists
        bodySegments.RemoveAt(segmentIndex);
        bodySegmentComponents.RemoveAt(segmentIndex);
        bodySegmentColliders.RemoveAt(segmentIndex);
        bodySegmentSpriteRenderers.RemoveAt(segmentIndex);
        bodySegmentSpineAnimations.RemoveAt(segmentIndex);

        // Destroy GameObject
        if (segmentToRemove != null)
        {
            Destroy(segmentToRemove.gameObject);
        }

        //Debug.Log($"Removed segment at index {segmentIndex} from bodySegments list");
    }

    // Method to check if there are more body segments with DISABLE state
    private void CheckRemainingDisableSegments()
    {
        // Check if there are any segments with DISABLE state
        bool hasDisableSegments = false;
        for (int i = 0; i < bodySegmentComponents.Count; i++)
        {
            if (bodySegmentComponents[i] != null && bodySegmentComponents[i].CurrentState == BodySegment.SegmentState.DISABLE)
            {
                hasDisableSegments = true;
                break;
            }
        }

        if (hasDisableSegments)
        {
            // Still has DISABLE segments, continue processing
            Debug.Log("Still has DISABLE segments, continuing processing...");
            // No need to do anything more, CheckAndHandleDisableSegments will be called in next FixedUpdate
        }
        else
        {
            // No more DISABLE segments, but don't automatically switch back to WALK
            // Logic to switch back to WALK is already handled in CheckAndHandleDisableSegments
            //Debug.Log("No more DISABLE segments found in CheckRemainingDisableSegments");
        }
    }

    // Coroutine to check DISABLE segments in next frame
    private IEnumerator CheckDisableSegmentsNextFrame()
    {
        yield return null; // Wait for next frame
        CheckAndHandleDisableSegments();
    }

    // Method to test disable random 3 body segments
    private void TestDisableRandomBodySegments()
    {
        if (bodySegments.Count < 4) // Need at least head + 2 body + tail
        {
            Debug.LogWarning("Not enough segments to test (need at least 4 segments)");
            return;
        }

        // Only disable body segments (not head or tail)
        int bodySegmentCount = bodySegments.Count - 2; // Subtract head and tail
        if (bodySegmentCount < 3)
        {
            Debug.LogWarning("Not enough body segments to disable 3 segments");
            return;
        }

        // Create list of body segment indices (skip head and tail)
        List<int> bodyIndices = new List<int>();
        for (int i = 1; i < bodySegmentComponents.Count - 1; i++) // Skip head (0) and tail (end)
        {
            if (bodySegmentComponents[i] != null && bodySegmentComponents[i].CurrentState != BodySegment.SegmentState.DISABLE)
            {
                bodyIndices.Add(i);
            }
        }

        // Check if there are enough segments to disable
        if (bodyIndices.Count < 3)
        {
            Debug.LogWarning($"Only {bodyIndices.Count} body segments can be disabled");
            return;
        }

        // Randomly select 3 indices
        List<int> selectedIndices = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, bodyIndices.Count);
            selectedIndices.Add(bodyIndices[randomIndex]);
            bodyIndices.RemoveAt(randomIndex); // Avoid duplicates
        }

        // Disable 3 selected segments
        Debug.Log("=== Test Disable Random 3 Body Segments ===");
        foreach (int index in selectedIndices)
        {
            if (bodySegmentComponents[index] != null)
            {
                bodySegmentComponents[index].SetState(BodySegment.SegmentState.DISABLE);
                Debug.Log($"Disabled body segment at index {index}: {bodySegmentComponents[index].GetSegmentInfo()}");
            }
        }

        Debug.Log($"Disabled {selectedIndices.Count} body segments. ChainController will automatically process these segments.");

        // Debug: Print information of all current segments
        Debug.Log("=== Current segments state after disabling ===");
        for (int i = 0; i < bodySegmentComponents.Count; i++)
        {
            if (bodySegmentComponents[i] != null)
            {
                Debug.Log($"Segment {i}: {bodySegmentComponents[i].GetSegmentInfo()}");
            }
        }
    }

    /// <summary>
    /// Apply dragon body sprite directly based on segmentIndex and colorIndex
    /// </summary>
    /// <param name="spriteRenderer">SpriteRenderer to apply</param>
    /// <param name="segmentIndex">Index of segment</param>
    /// <param name="colorIndex">Index of color</param>
    private void ApplyDragonBodyToRenderer(SpriteRenderer spriteRenderer, int segmentIndex, int colorIndex)
    {
        if (spriteRenderer == null)
        {
            //Debug.LogWarning("SpriteRenderer is null!");
            return;
        }

        if (colorConfig == null)
        {
           // Debug.LogWarning("ColorConfig is null, cannot apply dragon body sprite!");
            return;
        }

        // Use ColorConfig to apply sprite and color
        colorConfig.ApplyDragonBodyToRenderer(spriteRenderer, colorIndex);
        
       // Debug.Log($"Applied dragon body sprite to segment {segmentIndex} with color index {colorIndex}");
    }

    /// <summary>
    /// Apply random dragon body sprite variation directly based on segmentIndex and colorIndex
    /// </summary>
    /// <param name="spriteRenderer">SpriteRenderer to apply</param>
    /// <param name="segmentIndex">Index of segment</param>
    /// <param name="colorIndex">Index of color</param>
    private void ApplyRandomDragonBodyToRenderer(SpriteRenderer spriteRenderer, int segmentIndex, int colorIndex)
    {
        if (spriteRenderer == null)
        {
           // Debug.LogWarning("SpriteRenderer is null!");
            return;
        }

        if (colorConfig == null)
        {
            Debug.LogWarning("ColorConfig is null, cannot apply random dragon body sprite!");
            return;
        }

        // Use ColorConfig to apply random sprite and color
        colorConfig.ApplyRandomDragonBodyToRenderer(spriteRenderer, colorIndex);
        
        Debug.Log($"Applied random dragon body sprite to segment {segmentIndex} with color index {colorIndex}");
    }

    /// <summary>
    /// Apply dragon body sprite and color to body segment
    /// </summary>
    /// <param name="segmentObj">Body segment GameObject</param>
    /// <param name="segmentIndex">Index of segment in body (0-based)</param>
    private void ApplyDragonBodySpriteToSegment(GameObject segmentObj, int segmentIndex)
    {
        // Use cached SpriteRenderer instead of GetComponent
        if (segmentIndex + 1 < bodySegmentSpriteRenderers.Count) // +1 because head occupies index 0
        {
            SpriteRenderer spriteRenderer = bodySegmentSpriteRenderers[segmentIndex + 1];
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"Body segment {segmentIndex} doesn't have SpriteRenderer component!");
                return;
            }

            // Calculate color index
            int colorIndex = 1; // Default color index

            // Prioritize using LevelData if available
            if (levelData != null)
            {
                colorIndex = levelData.GetColorIndex(segmentIndex);
                ApplyDragonBodyToRenderer(spriteRenderer, segmentIndex, colorIndex);
                //Debug.Log($"Applied dragon body sprite to segment {segmentIndex} using LevelData with color index {colorIndex}");
            }
            else if (colorConfig != null)
            {
                // Fallback: use ColorConfig directly
                colorIndex = (segmentIndex % 10) + 1; // Cycle through colors 1-10
                ApplyDragonBodyToRenderer(spriteRenderer, segmentIndex, colorIndex);
                //Debug.Log($"Applied dragon body sprite to segment {segmentIndex} using ColorConfig with color index {colorIndex}");
            }
            else
            {
                //Debug.LogWarning($"No LevelData or ColorConfig to apply dragon body sprite for segment {segmentIndex}");
            }

            // Set color index for BodySegment component
            BodySegment bodySegment = bodySegmentComponents[segmentIndex + 1]; // +1 because head occupies index 0
            if (bodySegment != null)
            {
                bodySegment.SetColorIndex(colorIndex);
                //Debug.Log($"Set color index {colorIndex} for body segment {segmentIndex}");
            }
        }
        else
        {
            //Debug.LogError($"Segment index {segmentIndex} out of range for cached SpriteRenderers!");
        }
    }

    /// <summary>
    /// Apply random dragon body sprite variation to body segment
    /// </summary>
    /// <param name="segmentObj">Body segment GameObject</param>
    /// <param name="segmentIndex">Index of segment in body (0-based)</param>
    private void ApplyRandomDragonBodySpriteToSegment(GameObject segmentObj, int segmentIndex)
    {
        // Use cached SpriteRenderer instead of GetComponent
        if (segmentIndex + 1 < bodySegmentSpriteRenderers.Count) // +1 because head occupies index 0
        {
            SpriteRenderer spriteRenderer = bodySegmentSpriteRenderers[segmentIndex + 1];
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"Body segment {segmentIndex} doesn't have SpriteRenderer component!");
                return;
            }

            // Calculate color index
            int colorIndex = 1; // Default color index

            // Prioritize using LevelData if available
            if (levelData != null)
            {
                colorIndex = levelData.GetColorIndex(segmentIndex);
                ApplyRandomDragonBodyToRenderer(spriteRenderer, segmentIndex, colorIndex);
                Debug.Log($"Applied random dragon body sprite to segment {segmentIndex} using LevelData with color index {colorIndex}");
            }
            else if (colorConfig != null)
            {
                // Fallback: use ColorConfig directly
                colorIndex = (segmentIndex % 10) + 1; // Cycle through colors 1-10
                ApplyRandomDragonBodyToRenderer(spriteRenderer, segmentIndex, colorIndex);
                Debug.Log($"Applied random dragon body sprite to segment {segmentIndex} using ColorConfig with color index {colorIndex}");
            }
            else
            {
                Debug.LogWarning($"No LevelData or ColorConfig to apply random dragon body sprite for segment {segmentIndex}");
            }

            // Set color index for BodySegment component
            BodySegment bodySegment = bodySegmentComponents[segmentIndex + 1]; // +1 because head occupies index 0
            if (bodySegment != null)
            {
                bodySegment.SetColorIndex(colorIndex);
                Debug.Log($"Set color index {colorIndex} for body segment {segmentIndex}");
            }
        }
        else
        {
            Debug.LogError($"Segment index {segmentIndex} out of range for cached SpriteRenderers!");
        }
    }

    /// <summary>
    /// Refresh dragon body sprites for all body segments
    /// </summary>
    public void RefreshDragonBodySprites()
    {
        for (int i = 0; i < bodySegments.Count; i++)
        {
            if (bodySegments[i] != null && i > 0 && i < bodySegments.Count - 1) // Skip head and tail
            {
                ApplyDragonBodySpriteToSegment(bodySegments[i].gameObject, i - 1); // -1 because head occupies index 0
            }
        }
        Debug.Log("Refreshed dragon body sprites for all body segments");
    }

    /// <summary>
    /// Refresh dragon body sprites with random variations
    /// </summary>
    public void RefreshDragonBodySpritesWithRandomVariations()
    {
        for (int i = 0; i < bodySegments.Count; i++)
        {
            if (bodySegments[i] != null && i > 0 && i < bodySegments.Count - 1) // Skip head and tail
            {
                ApplyRandomDragonBodySpriteToSegment(bodySegments[i].gameObject, i - 1); // -1 because head occupies index 0
            }
        }
        Debug.Log("Refreshed dragon body sprites with random variations for all body segments");
    }

    /// <summary>
    /// Set LevelData for dragon
    /// </summary>
    /// <param name="newLevelData">New LevelData</param>
    public void SetLevelData(LevelData newLevelData)
    {
        levelData = newLevelData;
        if (levelData != null)
        {
            // Update size from level data if needed
            int newSize = levelData.GetSegmentCount();
            if (newSize > 0 && newSize != size)
            {
                size = newSize;
                Debug.Log($"Updated dragon size to {size} based on LevelData");
                
                // If dragon has been created, update immediately
                if (bodySegments.Count > 0)
                {
                    UpdateSizeFromLevelData();
                }
            }
        }
    }

    /// <summary>
    /// Set ColorConfig for dragon
    /// </summary>
    /// <param name="newColorConfig">New ColorConfig</param>
    public void SetColorConfig(ColorConfig newColorConfig)
    {
        colorConfig = newColorConfig;
    }

    /// <summary>
    /// Get SpriteRenderer of body segment by index
    /// </summary>
    /// <param name="bodySegmentIndex">Index of body segment (0-based, excluding head)</param>
    /// <returns>SpriteRenderer of body segment, or null if not found</returns>
    public SpriteRenderer GetBodySegmentSpriteRenderer(int bodySegmentIndex)
    {
        int spriteRendererIndex = bodySegmentIndex + 1; // +1 because head occupies index 0
        if (spriteRendererIndex >= 0 && spriteRendererIndex < bodySegmentSpriteRenderers.Count)
        {
            return bodySegmentSpriteRenderers[spriteRendererIndex];
        }
        Debug.LogWarning($"Body segment index {bodySegmentIndex} out of range!");
        return null;
    }

    /// <summary>
    /// Get all SpriteRenderers of body segments
    /// </summary>
    /// <returns>List of SpriteRenderers of body segments (excluding head and tail)</returns>
    public List<SpriteRenderer> GetAllBodySegmentSpriteRenderers()
    {
        List<SpriteRenderer> bodySpriteRenderers = new List<SpriteRenderer>();

        // Skip head (index 0) and tail (last index)
        for (int i = 1; i < bodySegmentSpriteRenderers.Count - 1; i++)
        {
            if (bodySegmentSpriteRenderers[i] != null)
            {
                bodySpriteRenderers.Add(bodySegmentSpriteRenderers[i]);
            }
        }

        return bodySpriteRenderers;
    }

    /// <summary>
    /// Apply dragon body sprite to body segment by index directly
    /// </summary>
    /// <param name="bodySegmentIndex">Index of body segment (0-based, excluding head)</param>
    public void ApplyDragonBodySpriteToBodySegment(int bodySegmentIndex)
    {
        SpriteRenderer spriteRenderer = GetBodySegmentSpriteRenderer(bodySegmentIndex);
        if (spriteRenderer == null) return;

        // Calculate color index
        int colorIndex = 1; // Default color index

        // Prioritize using LevelData if available
        if (levelData != null)
        {
            colorIndex = levelData.GetColorIndex(bodySegmentIndex);
            ApplyDragonBodyToRenderer(spriteRenderer, bodySegmentIndex, colorIndex);
            Debug.Log($"Applied dragon body sprite to body segment {bodySegmentIndex} using LevelData with color index {colorIndex}");
        }
        else if (colorConfig != null)
        {
            // Fallback: use ColorConfig directly
            colorIndex = (bodySegmentIndex % 10) + 1; // Cycle through colors 1-10
            ApplyDragonBodyToRenderer(spriteRenderer, bodySegmentIndex, colorIndex);
            Debug.Log($"Applied dragon body sprite to body segment {bodySegmentIndex} using ColorConfig with color index {colorIndex}");
        }
        else
        {
            Debug.LogWarning($"No LevelData or ColorConfig to apply dragon body sprite for body segment {bodySegmentIndex}");
        }

        // Set color index for BodySegment component
        if (bodySegmentIndex + 1 < bodySegmentComponents.Count) // +1 because head occupies index 0
        {
            BodySegment bodySegment = bodySegmentComponents[bodySegmentIndex + 1];
            if (bodySegment != null)
            {
                bodySegment.SetColorIndex(colorIndex);
                Debug.Log($"Set color index {colorIndex} for body segment {bodySegmentIndex}");
            }
        }
    }

    /// <summary>
    /// Apply random dragon body sprite variation to body segment by index directly
    /// </summary>
    /// <param name="bodySegmentIndex">Index of body segment (0-based, excluding head)</param>
    public void ApplyRandomDragonBodySpriteToBodySegment(int bodySegmentIndex)
    {
        SpriteRenderer spriteRenderer = GetBodySegmentSpriteRenderer(bodySegmentIndex);
        if (spriteRenderer == null) return;

        // Calculate color index
        int colorIndex = 1; // Default color index

        // Prioritize using LevelData if available
        if (levelData != null)
        {
            colorIndex = levelData.GetColorIndex(bodySegmentIndex);
            ApplyRandomDragonBodyToRenderer(spriteRenderer, bodySegmentIndex, colorIndex);
            Debug.Log($"Applied random dragon body sprite to body segment {bodySegmentIndex} using LevelData with color index {colorIndex}");
        }
        else if (colorConfig != null)
        {
            // Fallback: use ColorConfig directly
            colorIndex = (bodySegmentIndex % 10) + 1; // Cycle through colors 1-10
            ApplyRandomDragonBodyToRenderer(spriteRenderer, bodySegmentIndex, colorIndex);
            Debug.Log($"Applied random dragon body sprite to body segment {bodySegmentIndex} using ColorConfig with color index {colorIndex}");
        }
        else
        {
            Debug.LogWarning($"No LevelData or ColorConfig to apply random dragon body sprite for body segment {bodySegmentIndex}");
        }

        // Set color index for BodySegment component
        if (bodySegmentIndex + 1 < bodySegmentComponents.Count) // +1 because head occupies index 0
        {
            BodySegment bodySegment = bodySegmentComponents[bodySegmentIndex + 1];
            if (bodySegment != null)
            {
                bodySegment.SetColorIndex(colorIndex);
                Debug.Log($"Set color index {colorIndex} for body segment {bodySegmentIndex}");
            }
        }
    }

    /// <summary>
    /// Apply dragon body sprite to all body segments directly
    /// </summary>
    public void ApplyDragonBodySpritesToAllBodySegments()
    {
        List<SpriteRenderer> bodySpriteRenderers = GetAllBodySegmentSpriteRenderers();

        for (int i = 0; i < bodySpriteRenderers.Count; i++)
        {
            SpriteRenderer spriteRenderer = bodySpriteRenderers[i];
            if (spriteRenderer == null) continue;

            // Prioritize using LevelData if available
            if (levelData != null)
            {
                int colorIndex = levelData.GetColorIndex(i);
                ApplyDragonBodyToRenderer(spriteRenderer, i, colorIndex);
            }
            else if (colorConfig != null)
            {
                int colorIndex = (i % 10) + 1; // Cycle through colors 1-10
                ApplyDragonBodyToRenderer(spriteRenderer, i, colorIndex);
            }
        }

        Debug.Log($"Applied dragon body sprites to {bodySpriteRenderers.Count} body segments");
    }

    /// <summary>
    /// Apply random dragon body sprite variations to all body segments directly
    /// </summary>
    public void ApplyRandomDragonBodySpritesToAllBodySegments()
    {
        List<SpriteRenderer> bodySpriteRenderers = GetAllBodySegmentSpriteRenderers();

        for (int i = 0; i < bodySpriteRenderers.Count; i++)
        {
            SpriteRenderer spriteRenderer = bodySpriteRenderers[i];
            if (spriteRenderer == null) continue;

            // Prioritize using LevelData if available
            if (levelData != null)
            {
                int colorIndex = levelData.GetColorIndex(i);
                ApplyRandomDragonBodyToRenderer(spriteRenderer, i, colorIndex);
            }
            else if (colorConfig != null)
            {
                int colorIndex = (i % 10) + 1; // Cycle through colors 1-10
                ApplyRandomDragonBodyToRenderer(spriteRenderer, i, colorIndex);
            }
        }

        Debug.Log($"Applied random dragon body sprites to {bodySpriteRenderers.Count} body segments");
    }

    /// <summary>
    /// Update HP UI
    /// </summary>
    private void UpdateHPUI()
    {
        // Find UIManager and update HP
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateHP(currentHP, maxHP);
        }
    }

    /// <summary>
    /// Reduce HP when body segment is disabled
    /// </summary>
    /// <param name="damage">Amount of HP lost (default is 1)</param>
    public void TakeDamage(int damage = 1)
    {
        if (damage <= 0)
        {
            Debug.LogWarning("ChainController: Damage must be greater than 0");
            return;
        }
        
        if (IsDefeated)
        {
            Debug.Log("ChainController: Dragon is already defeated");
            return;
        }
        
        int oldHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - damage);
        
        UpdateHPUI();
        OnHPChanged?.Invoke(currentHP, maxHP);
        
        //Debug.Log($"ChainController: Took {damage} damage. HP: {oldHP} -> {currentHP}");
        
        // Check if dragon has been defeated
        if (IsDefeated)
        {
            OnDragonDefeated?.Invoke();
            Debug.Log("ChainController: Dragon has been defeated!");
            
            // Switch to DEAD state and play die animation
            SetDragonState(DragonState.DEAD);
            
            // Call GameManager to handle victory
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Victory();
            }
        }
    }

    /// <summary>
    /// Restore HP
    /// </summary>
    /// <param name="healAmount">Amount of HP to restore</param>
    public void Heal(int healAmount)
    {
        if (healAmount <= 0)
        {
            Debug.LogWarning("ChainController: Heal amount must be greater than 0");
            return;
        }
        
        int oldHP = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + healAmount);
        
        UpdateHPUI();
        OnHPChanged?.Invoke(currentHP, maxHP);
        
        Debug.Log($"ChainController: Healed {healAmount} HP. HP: {oldHP} -> {currentHP}");
    }

    /// <summary>
    /// Reset HP to maximum
    /// </summary>
    public void ResetHP()
    {
        currentHP = maxHP;
        UpdateHPUI();
        OnHPChanged?.Invoke(currentHP, maxHP);
        
        Debug.Log($"ChainController: HP reset to {currentHP}/{maxHP}");
    }

    /// <summary>
    /// Set maximum HP
    /// </summary>
    /// <param name="newMaxHP">New maximum HP</param>
    public void SetMaxHP(int newMaxHP)
    {
        if (newMaxHP <= 0)
        {
            Debug.LogWarning("ChainController: MaxHP cannot be less than or equal to 0");
            return;
        }
        
        maxHP = newMaxHP;
        
        // Adjust currentHP if needed
        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }
        
        UpdateHPUI();
        OnHPChanged?.Invoke(currentHP, maxHP);
        
        Debug.Log($"ChainController: MaxHP set to {maxHP}, CurrentHP: {currentHP}");
    }

    /// <summary>
    /// Test set dragon to DEAD state
    /// </summary>
    public void TestSetDragonDead()
    {
        Debug.Log("=== Test Set Dragon Dead ===");
        Debug.Log($"Current state: {currentDragonState}");
        Debug.Log($"Current HP: {currentHP}/{maxHP}");
        Debug.Log($"Size: {size}");
        
        SetDragonState(DragonState.DEAD);
        
        Debug.Log($"New state: {currentDragonState}");
        Debug.Log("Dragon should now be dead and stop moving");
    }

    /// <summary>
    /// Debug HP information
    /// </summary>
    public void DebugHPInfo()
    {
        Debug.Log($"=== Dragon HP Debug Info ===");
        Debug.Log($"Current HP: {currentHP}");
        Debug.Log($"Max HP: {maxHP}");
        Debug.Log($"HP Percentage: {HPPercentage:P1}");
        Debug.Log($"Is Defeated: {IsDefeated}");
        Debug.Log("=============================");
    }

    /// <summary>
    /// Handle when dragon head reaches final waypoint
    /// </summary>
    private void OnDragonReachedFinalWaypoint()
    {
        Debug.Log("Dragon head has reached final waypoint - Game Over!");
        
        // Stop dragon movement
        //SetDragonState(DragonState.DEAD);
        
        // Play fire breath animation for head
        PlayFireBreathAnimation();
        
        // Call GameManager to handle Game Over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    /// <summary>
    /// Play fire breath animation for dragon head
    /// </summary>
    private void PlayFireBreathAnimation()
    {
        if (Head != null)
        {
            BodySegment headSegment = Head.GetComponent<BodySegment>();
            if (headSegment != null)
            {
                // Play spine fire breath animation if available
                SkeletonAnimation headSpine = headSegment.spineAnimation;
                if (headSpine != null)
                {
                    // Find fire breath animation in spine data
                    if (headSpine.Skeleton.Data.FindAnimation("idle3_fire") != null)
                    {
                        headSpine.AnimationState.SetAnimation(0, "idle3_fire", true);
                        Debug.Log("Playing fire_breath animation for dragon head");
                    }
                    else
                    {
                        Debug.LogWarning("No fire breath animation found in spine data");
                    }
                }
            }
        }
    }
}