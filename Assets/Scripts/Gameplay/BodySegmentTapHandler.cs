using UnityEngine;

public class BodySegmentTapHandler : MonoBehaviour
{
    private BodySegment bodySegment;
    private Camera mainCamera;
    private Collider2D segmentCollider;

    void Awake()
    {
        mainCamera = Camera.main;
        segmentCollider = GetComponent<Collider2D>();
        bodySegment = GetComponent<BodySegment>();
        
        // Ensure there's a Collider2D to detect tap
        if (segmentCollider == null)
        {
            segmentCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Ensure there's a BodySegment component
        if (bodySegment == null)
        {
            bodySegment = gameObject.AddComponent<BodySegment>();
        }
    }

    void Update()
    {
        // Only process tap if segment can be interacted with
        if (bodySegment != null && !bodySegment.CanInteract())
            return;
            
        // Check tap on mobile
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                CheckTap(touch.position);
            }
        }
        
        // Check mouse click on PC
        if (Input.GetMouseButtonDown(0))
        {
            CheckTap(Input.mousePosition);
        }
    }

    void CheckTap(Vector3 screenPosition)
    {
        if (mainCamera == null || segmentCollider == null) return;

        // Convert screen position to world position
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = transform.position.z;

        // Check if tap is within the segment's collider
        if (segmentCollider.OverlapPoint(worldPosition))
        {
            OnSegmentTapped();
        }
    }

    void OnSegmentTapped()
    {
        if (bodySegment != null)
        {
            // Call BodySegment's OnTap method
            bodySegment.OnTap();
            
            // Toggle segment state (optional)
            // bodySegment.ToggleState();
            
            Debug.Log($"Segment tapped: {bodySegment.GetSegmentInfo()}");
        }
    }

    // Method to set up segment from outside
    public void SetupSegment(BodySegment.SegmentType type, int colorIndex)
    {
        if (bodySegment != null)
        {
            // Can add setup logic here if needed
            //bodySegment.SetColorIndex(colorIndex);
        }
    }
    
    // Method to get segment information
    public BodySegment GetBodySegment()
    {
        return bodySegment;
    }
} 