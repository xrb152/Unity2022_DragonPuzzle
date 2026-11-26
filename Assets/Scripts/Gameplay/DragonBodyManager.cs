using System.Collections.Generic;
using UnityEngine;

public class DragonBodyManager : MonoBehaviour
{
    [Header("Dragon Body Configuration")]
    [SerializeField] private GameObject headPrefab;
    [SerializeField] private GameObject bodyPrefab;
    [SerializeField] private GameObject tailPrefab;
    
    [Header("Dragon Settings")]
    [SerializeField] private int initialBodySegments = 3;
    [SerializeField] private int maxBodySegments = 10;
    

    
    // Lists để quản lý segments
    private List<BodySegment> dragonSegments = new List<BodySegment>();
    private List<GameObject> segmentObjects = new List<GameObject>();
    
    // Events
    public System.Action<BodySegment> OnSegmentAdded;
    public System.Action<BodySegment> OnSegmentRemoved;
    public System.Action<BodySegment> OnSegmentStateChanged;
    
    private void Start()
    {
        InitializeDragon();
    }
    
    private void InitializeDragon()
    {
        // Tạo head segment
        CreateHeadSegment();
        
        // Tạo body segments
        for (int i = 0; i < initialBodySegments; i++)
        {
            CreateBodySegment();
        }
        
        // Tạo tail segment
        CreateTailSegment();
    }
    
    private void CreateHeadSegment()
    {
        if (headPrefab == null)
        {
            Debug.LogError("Head prefab chưa được assign!");
            return;
        }
        
        GameObject headObject = Instantiate(headPrefab, transform.position, transform.rotation, transform);
        BodySegment headSegment = headObject.GetComponent<BodySegment>();
        
        if (headSegment == null)
        {
            headSegment = headObject.AddComponent<BodySegment>();
        }
        
        // Setup head segment
        SetupSegment(headSegment, BodySegment.SegmentType.HEAD);
        
        // Set sort order cho head (cao nhất)
        headSegment.SetAutoSortOrder(0, dragonSegments.Count + 1);
        
        // Thêm vào lists
        dragonSegments.Add(headSegment);
        segmentObjects.Add(headObject);
        
        // Subscribe events
        SubscribeToSegmentEvents(headSegment);
        
        OnSegmentAdded?.Invoke(headSegment);
        
        Debug.Log("Head segment được tạo thành công");
    }
    
    private void CreateBodySegment()
    {
        if (bodyPrefab == null)
        {
            Debug.LogError("Body prefab chưa được assign!");
            return;
        }
        
        Vector3 spawnPosition = GetLastSegmentPosition();
        GameObject bodyObject = Instantiate(bodyPrefab, spawnPosition, transform.rotation, transform);
        BodySegment bodySegment = bodyObject.GetComponent<BodySegment>();
        
        if (bodySegment == null)
        {
            bodySegment = bodyObject.AddComponent<BodySegment>();
        }
        
        // Setup body segment
        SetupSegment(bodySegment, BodySegment.SegmentType.BODY);
        
        // Set sort order cho body (giảm dần)
        bodySegment.SetAutoSortOrder(dragonSegments.Count, dragonSegments.Count + 1);
        
        // Thêm vào lists
        dragonSegments.Add(bodySegment);
        segmentObjects.Add(bodyObject);
        
        // Subscribe events
        SubscribeToSegmentEvents(bodySegment);
        
        OnSegmentAdded?.Invoke(bodySegment);
        
        Debug.Log("Body segment được tạo thành công");
    }
    
    private void CreateTailSegment()
    {
        if (tailPrefab == null)
        {
            Debug.LogError("Tail prefab chưa được assign!");
            return;
        }
        
        Vector3 spawnPosition = GetLastSegmentPosition();
        GameObject tailObject = Instantiate(tailPrefab, spawnPosition, transform.rotation, transform);
        BodySegment tailSegment = tailObject.GetComponent<BodySegment>();
        
        if (tailSegment == null)
        {
            tailSegment = tailObject.AddComponent<BodySegment>();
        }
        
        // Setup tail segment
        SetupSegment(tailSegment, BodySegment.SegmentType.TAIL);
        
        // Set sort order cho tail (thấp nhất)
        tailSegment.SetAutoSortOrder(dragonSegments.Count, dragonSegments.Count + 1);
        
        // Thêm vào lists
        dragonSegments.Add(tailSegment);
        segmentObjects.Add(tailObject);
        
        // Subscribe events
        SubscribeToSegmentEvents(tailSegment);
        
        OnSegmentAdded?.Invoke(tailSegment);
        
        Debug.Log("Tail segment được tạo thành công");
    }
    
    private void SetupSegment(BodySegment segment, BodySegment.SegmentType type)
    {
        // Setup segment type
        segment.SetSegmentType(type);
    }
    
    private void SubscribeToSegmentEvents(BodySegment segment)
    {
        if (segment != null)
        {
            segment.OnSegmentStateChanged += OnSegmentStateChangedHandler;
            segment.OnSegmentTapped += OnSegmentTappedHandler;
        }
    }
    
    private void UnsubscribeFromSegmentEvents(BodySegment segment)
    {
        if (segment != null)
        {
            segment.OnSegmentStateChanged -= OnSegmentStateChangedHandler;
            segment.OnSegmentTapped -= OnSegmentTappedHandler;
        }
    }
    
    private void OnSegmentStateChangedHandler(BodySegment segment)
    {
        OnSegmentStateChanged?.Invoke(segment);
        Debug.Log($"Segment state changed: {segment.GetSegmentInfo()}");
    }
    
    private void OnSegmentTappedHandler(BodySegment segment)
    {
        Debug.Log($"Segment tapped: {segment.GetSegmentInfo()}");
        
        // Có thể thêm logic xử lý tap ở đây
        // Ví dụ: Toggle state, thay đổi màu, v.v.
    }
    
    private Vector3 GetLastSegmentPosition()
    {
        if (segmentObjects.Count > 0)
        {
            return segmentObjects[segmentObjects.Count - 1].transform.position;
        }
        return transform.position;
    }
    

    
    // Public methods để thêm/xóa segments
    public void AddBodySegment()
    {
        if (dragonSegments.Count < maxBodySegments)
        {
            CreateBodySegment();
        }
        else
        {
            Debug.LogWarning("Đã đạt giới hạn số lượng body segments!");
        }
    }
    
    public void RemoveBodySegment(int index)
    {
        if (index >= 0 && index < dragonSegments.Count)
        {
            BodySegment segment = dragonSegments[index];
            GameObject segmentObject = segmentObjects[index];
            
            // Unsubscribe events
            UnsubscribeFromSegmentEvents(segment);
            
            // Remove from lists
            dragonSegments.RemoveAt(index);
            segmentObjects.RemoveAt(index);
            
            // Destroy object
            if (segmentObject != null)
            {
                Destroy(segmentObject);
            }
            
            OnSegmentRemoved?.Invoke(segment);
            
            Debug.Log($"Segment {index} đã được xóa");
        }
    }
    
    public void RemoveLastBodySegment()
    {
        if (dragonSegments.Count > 1) // Giữ lại ít nhất head
        {
            RemoveBodySegment(dragonSegments.Count - 1);
        }
    }
    
    // Method để lấy thông tin dragon
    public List<BodySegment> GetAllSegments()
    {
        return new List<BodySegment>(dragonSegments);
    }
    
    public BodySegment GetHeadSegment()
    {
        if (dragonSegments.Count > 0)
        {
            return dragonSegments[0];
        }
        return null;
    }
    
    public BodySegment GetTailSegment()
    {
        if (dragonSegments.Count > 0)
        {
            return dragonSegments[dragonSegments.Count - 1];
        }
        return null;
    }
    
    public int GetSegmentCount()
    {
        return dragonSegments.Count;
    }
    
    // Method để reset dragon
    public void ResetDragon()
    {
        // Destroy tất cả segments hiện tại
        foreach (GameObject obj in segmentObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        // Clear lists
        dragonSegments.Clear();
        segmentObjects.Clear();
        
        // Tạo lại dragon
        InitializeDragon();
        
        Debug.Log("Dragon đã được reset");
    }
    
    // Method để set state cho tất cả segments
    public void SetAllSegmentsState(BodySegment.SegmentState state)
    {
        foreach (BodySegment segment in dragonSegments)
        {
            if (segment != null)
            {
                segment.SetState(state);
            }
        }
    }
    

    
    // Method để refresh sort order cho tất cả segments
    public void RefreshAllSegmentsSortOrder()
    {
        int totalSegments = dragonSegments.Count;
        
        for (int i = 0; i < dragonSegments.Count; i++)
        {
            if (dragonSegments[i] != null)
            {
                dragonSegments[i].SetAutoSortOrder(i, totalSegments);
            }
        }
        
        Debug.Log($"Đã refresh sort order cho {totalSegments} segments");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe tất cả events khi destroy
        foreach (BodySegment segment in dragonSegments)
        {
            UnsubscribeFromSegmentEvents(segment);
        }
    }
} 