using UnityEngine;
using Spine.Unity;

public class BodySegment : MonoBehaviour
{
    [Header("Segment Configuration")]
    [SerializeField] private SegmentType segmentType;
    
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] public SkeletonAnimation spineAnimation;
    
    [Header("State")]
    [SerializeField] private SegmentState currentState = SegmentState.RUNNING;
    
    [Header("Color Configuration")]
    [SerializeField] private int colorIndex = 1; // Color index for this segment
    
    // Enums
    public enum SegmentType
    {
        HEAD,
        BODY,
        TAIL
    }
    
    public enum SegmentState
    {
        FREEZING,
        RUNNING,
        DISABLE
    }
    
    // Properties
    public SegmentType Type => segmentType;
    public SegmentState CurrentState => currentState;
    public int ColorIndex => colorIndex; // Public property to get colorIndex
    public bool isTarget = false;
    
    // Events
    public System.Action<BodySegment> OnSegmentStateChanged;
    public System.Action<BodySegment> OnSegmentTapped;
    
    private void Awake()
    {
        InitializeComponents();
        SetupSegment();
    }
    
    private void InitializeComponents()
    {
        // Auto-find components if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (spineAnimation == null)
            spineAnimation = GetComponent<SkeletonAnimation>();
            isTarget = false;
    }
    
    private void SetupSegment()
    {
        switch (segmentType)
        {
            case SegmentType.HEAD:
                SetupHeadSegment();
                break;
            case SegmentType.BODY:
                SetupBodySegment();
                break;
            case SegmentType.TAIL:
                SetupTailSegment();
                break;
        }
    }
    
    private void SetupHeadSegment()
    {
        // Head uses Spine Animation
        if (spineAnimation != null)
        {
            spineAnimation.gameObject.SetActive(true);
            if (spriteRenderer != null)
                spriteRenderer.gameObject.SetActive(false);
                
            // Setup spine animation for head
            SetupSpineAnimation();
        }
        else
        {
            Debug.LogWarning("Head segment needs SkeletonAnimation component!");
        }
    }
    
    private void SetupBodySegment()
    {
        // Body uses SpriteRenderer
        if (spriteRenderer != null)
        {
            spriteRenderer.gameObject.SetActive(true);
            if (spineAnimation != null)
                spineAnimation.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Body segment needs SpriteRenderer component!");
        }
    }
    
    private void SetupTailSegment()
    {
        // Tail uses SpriteRenderer
        if (spriteRenderer != null)
        {
            spriteRenderer.gameObject.SetActive(true);
            if (spineAnimation != null)
                spineAnimation.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Tail segment needs SpriteRenderer component!");
        }
    }
    
    private void SetupSpineAnimation()
    {
        if (spineAnimation != null)
        {
            // Setup animation for head based on state
            UpdateSpineAnimation();
        }
    }
    

    
    public void SetSegmentType(SegmentType newType)
    {
        segmentType = newType;
        SetupSegment();
    }
    
    public void SetState(SegmentState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            UpdateSegmentByState();
            OnSegmentStateChanged?.Invoke(this);
        }
    }
    
    private void UpdateSegmentByState()
    {
        switch (currentState)
        {
            case SegmentState.FREEZING:
                ApplyFreezingState();
                break;
            case SegmentState.RUNNING:
                ApplyRunningState();
                break;
            case SegmentState.DISABLE:
                ApplyDisableState();
                break;
        }
    }
    
    private void ApplyFreezingState()
    {
        // Freezing effect
        if (segmentType == SegmentType.HEAD && spineAnimation != null)
        {
            // Stop animation and apply freezing effect
            spineAnimation.AnimationState.SetAnimation(0, "idle_smoke", true);
            //spineAnimation.skeleton.SetColor(new Color(0.7f, 0.9f, 1f, 0.8f)); // Ice color
        }
        else if (spriteRenderer != null)
        {
            // Apply freezing effect to sprite
            //spriteRenderer.color = new Color(0.7f, 0.9f, 1f, 0.8f);
        }
    }
    
    private void ApplyRunningState()
    {
        // Normal state
        if (segmentType == SegmentType.HEAD && spineAnimation != null)
        {
            // Run normal animation
            spineAnimation.AnimationState.SetAnimation(0, "idle", true);
        }
        else if (spriteRenderer != null)
        {
            // Restore sprite renderer to normal state
        }
    }
    
    private void ApplyDisableState()
    {
        // Disabled state
        if (segmentType == SegmentType.HEAD && spineAnimation != null)
        {
            // Stop animation
            spineAnimation.AnimationState.ClearTracks();
            //spineAnimation.skeleton.SetColor(new Color(0.5f, 0.5f, 0.5f, 0.5f)); // Gray color
        }
        else if (spriteRenderer != null)
        {
            // Apply disabled effect
            //spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }
    
    private void UpdateSpineAnimation()
    {
        if (spineAnimation != null)
        {
            // Update animation based on state
            switch (currentState)
            {
                case SegmentState.FREEZING:
                    spineAnimation.AnimationState.SetAnimation(0, "freeze", true);
                    break;
                case SegmentState.RUNNING:
                    spineAnimation.AnimationState.SetAnimation(0, "idle", true);
                    break;
                case SegmentState.DISABLE:
                    spineAnimation.AnimationState.ClearTracks();
                    break;
            }
        }
    }
    
    // Method to tap on segment
    public void OnTap()
    {
        OnSegmentTapped?.Invoke(this);
    }
    
    // Method to toggle state
    public void ToggleState()
    {
        switch (currentState)
        {
            case SegmentState.RUNNING:
                SetState(SegmentState.FREEZING);
                break;
            case SegmentState.FREEZING:
                SetState(SegmentState.DISABLE);
                break;
            case SegmentState.DISABLE:
                SetState(SegmentState.RUNNING);
                break;
        }
    }
    
    // Method to reset to initial state
    public void ResetToDefault()
    {
        SetState(SegmentState.RUNNING);
    }
    
    // Method to check if segment can be interacted with
    public bool CanInteract()
    {
        return currentState != SegmentState.DISABLE;
    }
    
    // Method to get segment information
    public string GetSegmentInfo()
    {
        return $"Segment Type: {segmentType}, State: {currentState}";
    }
    
    // Method to set sort order for segment by index
    public void SetSortOrder(int sortOrder)
    {
        switch (segmentType)
        {
            case SegmentType.HEAD:
                SetHeadSortOrder(sortOrder);
                break;
            case SegmentType.BODY:
            case SegmentType.TAIL:
                SetBodyTailSortOrder(sortOrder);
                break;
        }
    }
    
    // Method to set sort order for head (Spine Animation)
    private void SetHeadSortOrder(int sortOrder)
    {
        if (spineAnimation != null)
        {
            // Set sort order for Spine Animation
            spineAnimation.GetComponent<MeshRenderer>().sortingOrder = sortOrder;
        }
        else
        {
            Debug.LogWarning($"Head segment does not have SkeletonAnimation component to set sort order!");
        }
    }
    
    // Method to set sort order for body/tail (SpriteRenderer)
    private void SetBodyTailSortOrder(int sortOrder)
    {
        if (spriteRenderer != null)
        {
            // Set sort order for SpriteRenderer
            spriteRenderer.sortingOrder = sortOrder;
        }
        else
        {
            Debug.LogWarning($"Body/Tail segment does not have SpriteRenderer component to set sort order!");
        }
    }
    
    // Method to get current sort order
    public int GetSortOrder()
    {
        switch (segmentType)
        {
            case SegmentType.HEAD:
                if (spineAnimation != null)
                {
                    return spineAnimation.GetComponent<MeshRenderer>().sortingOrder;
                }
                break;
            case SegmentType.BODY:
            case SegmentType.TAIL:
                if (spriteRenderer != null)
                {
                    return spriteRenderer.sortingOrder;
                }
                break;
        }
        return 0; // Default sort order
    }
    
    // Method to set sort order with automatic logic (head highest, body decreasing, tail lowest)
    public void SetAutoSortOrder(int segmentIndex, int totalSegments)
    {
        int sortOrder;
        
        switch (segmentType)
        {
            case SegmentType.HEAD:
                // Head always has highest sort order
                sortOrder = totalSegments;
                break;
            case SegmentType.BODY:
                // Body segments have decreasing sort order from head
                sortOrder = totalSegments - segmentIndex;
                break;
            case SegmentType.TAIL:
                // Tail always has lowest sort order
                sortOrder = 0;
                break;
            default:
                sortOrder = 0;
                break;
        }
        
        SetSortOrder(sortOrder);
    }
    
    /// <summary>
    /// Set the color index for this segment
    /// </summary>
    /// <param name="newColorIndex">New color index value</param>
    public void SetColorIndex(int newColorIndex)
    {
        colorIndex = newColorIndex;
    }

    public void DisableSegment()
    {
        SetState(SegmentState.DISABLE);
    }
} 