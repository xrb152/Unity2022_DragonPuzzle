using Spine.Unity;
using UnityEngine;

public class SegmentPrefabCreator : MonoBehaviour
{
    [Header("Prefab Creation")]
    [SerializeField] private string prefabName = "DragonSegment";
    [SerializeField] private Vector3 segmentSize = new Vector3(1f, 1f, 1f);
    
    [Header("Components")]
    [SerializeField] private bool addSpriteRenderer = true;
    [SerializeField] private bool addSkeletonAnimation = false;
    [SerializeField] private bool addCollider = true;
    [SerializeField] private bool addRigidbody = true;
    
    [Header("Spine Settings")]
    [SerializeField] private SkeletonDataAsset spineDataAsset;
    [SerializeField] private string defaultAnimation = "idle";
    
    [Header("Sprite Settings")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Color defaultColor = Color.white;
    
    // Method để tạo head prefab
    public GameObject CreateHeadPrefab()
    {
        GameObject headPrefab = CreateBasePrefab("DragonHead");
        
        // Setup cho head
        BodySegment headSegment = headPrefab.GetComponent<BodySegment>();
        if (headSegment != null)
        {
            headSegment.SetSegmentType(BodySegment.SegmentType.HEAD);
        }
        
        // Thêm SkeletonAnimation cho head
        if (addSkeletonAnimation)
        {
            SkeletonAnimation spineAnim = headPrefab.GetComponent<SkeletonAnimation>();
            if (spineAnim == null)
            {
                spineAnim = headPrefab.AddComponent<SkeletonAnimation>();
            }
            
            if (spineDataAsset != null)
            {
                spineAnim.skeletonDataAsset = spineDataAsset;
                spineAnim.AnimationState.SetAnimation(0, defaultAnimation, true);
            }
        }
        
        // Disable SpriteRenderer cho head
        SpriteRenderer spriteRenderer = headPrefab.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        return headPrefab;
    }
    
    // Method để tạo body prefab
    public GameObject CreateBodyPrefab()
    {
        GameObject bodyPrefab = CreateBasePrefab("DragonBody");
        
        // Setup cho body
        BodySegment bodySegment = bodyPrefab.GetComponent<BodySegment>();
        if (bodySegment != null)
        {
            bodySegment.SetSegmentType(BodySegment.SegmentType.BODY);
        }
        
        // Disable SkeletonAnimation cho body
        SkeletonAnimation spineAnim = bodyPrefab.GetComponent<SkeletonAnimation>();
        if (spineAnim != null)
        {
            spineAnim.enabled = false;
        }
        
        return bodyPrefab;
    }
    
    // Method để tạo tail prefab
    public GameObject CreateTailPrefab()
    {
        GameObject tailPrefab = CreateBasePrefab("DragonTail");
        
        // Setup cho tail
        BodySegment tailSegment = tailPrefab.GetComponent<BodySegment>();
        if (tailSegment != null)
        {
            tailSegment.SetSegmentType(BodySegment.SegmentType.TAIL);
        }
        
        // Disable SkeletonAnimation cho tail
        SkeletonAnimation spineAnim = tailPrefab.GetComponent<SkeletonAnimation>();
        if (spineAnim != null)
        {
            spineAnim.enabled = false;
        }
        
        return tailPrefab;
    }
    
    // Method để tạo base prefab với các components cơ bản
    private GameObject CreateBasePrefab(string name)
    {
        GameObject prefab = new GameObject(name);
        
        // Thêm BodySegment component
        BodySegment segment = prefab.AddComponent<BodySegment>();
        
        // Thêm BodySegmentTapHandler
        BodySegmentTapHandler tapHandler = prefab.AddComponent<BodySegmentTapHandler>();
        
        // Thêm SpriteRenderer nếu cần
        if (addSpriteRenderer)
        {
            SpriteRenderer spriteRenderer = prefab.AddComponent<SpriteRenderer>();
            if (defaultSprite != null)
            {
                spriteRenderer.sprite = defaultSprite;
            }
            spriteRenderer.color = defaultColor;
        }
        
        // Thêm SkeletonAnimation nếu cần
        if (addSkeletonAnimation)
        {
            SkeletonAnimation spineAnim = prefab.AddComponent<SkeletonAnimation>();
            if (spineDataAsset != null)
            {
                spineAnim.skeletonDataAsset = spineDataAsset;
            }
        }
        
        // Thêm Collider2D nếu cần
        if (addCollider)
        {
            BoxCollider2D collider = prefab.AddComponent<BoxCollider2D>();
            collider.size = segmentSize;
        }
        
        // Thêm Rigidbody2D nếu cần
        if (addRigidbody)
        {
            Rigidbody2D rb = prefab.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.drag = 0.5f;
        }
        
        // Set transform
        prefab.transform.localScale = segmentSize;
        
        return prefab;
    }
    
    // Method để tạo tất cả prefabs
    public void CreateAllPrefabs()
    {
        GameObject headPrefab = CreateHeadPrefab();
        GameObject bodyPrefab = CreateBodyPrefab();
        GameObject tailPrefab = CreateTailPrefab();
        
        Debug.Log("Đã tạo tất cả prefabs:");
        Debug.Log($"- Head: {headPrefab.name}");
        Debug.Log($"- Body: {bodyPrefab.name}");
        Debug.Log($"- Tail: {tailPrefab.name}");
    }
    
    // Method để setup prefab cho DragonBodyManager
    public void SetupDragonBodyManager(DragonBodyManager manager)
    {
        if (manager == null) return;
        
        // Tạo prefabs và assign cho manager
        GameObject headPrefab = CreateHeadPrefab();
        GameObject bodyPrefab = CreateBodyPrefab();
        GameObject tailPrefab = CreateTailPrefab();
        
        // Sử dụng reflection để set private fields
        var headField = typeof(DragonBodyManager).GetField("headPrefab", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var bodyField = typeof(DragonBodyManager).GetField("bodyPrefab", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tailField = typeof(DragonBodyManager).GetField("tailPrefab", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (headField != null) headField.SetValue(manager, headPrefab);
        if (bodyField != null) bodyField.SetValue(manager, bodyPrefab);
        if (tailField != null) tailField.SetValue(manager, tailPrefab);
        
        Debug.Log("Đã setup DragonBodyManager với các prefabs");
    }
    
    // Method để tạo prefab từ existing GameObject
    public GameObject CreatePrefabFromGameObject(GameObject sourceObject, string prefabName)
    {
        if (sourceObject == null) return null;
        
        GameObject prefab = Instantiate(sourceObject);
        prefab.name = prefabName;
        
        // Đảm bảo có BodySegment component
        if (prefab.GetComponent<BodySegment>() == null)
        {
            prefab.AddComponent<BodySegment>();
        }
        
        // Đảm bảo có BodySegmentTapHandler component
        if (prefab.GetComponent<BodySegmentTapHandler>() == null)
        {
            prefab.AddComponent<BodySegmentTapHandler>();
        }
        
        return prefab;
    }
    
    // Method để validate prefab
    public bool ValidatePrefab(GameObject prefab, BodySegment.SegmentType expectedType)
    {
        if (prefab == null) return false;
        
        BodySegment segment = prefab.GetComponent<BodySegment>();
        if (segment == null)
        {
            Debug.LogError($"Prefab {prefab.name} thiếu BodySegment component!");
            return false;
        }
        
        if (segment.Type != expectedType)
        {
            Debug.LogWarning($"Prefab {prefab.name} có type {segment.Type} nhưng mong đợi {expectedType}");
        }
        
        // Kiểm tra components dựa trên type
        switch (expectedType)
        {
            case BodySegment.SegmentType.HEAD:
                if (prefab.GetComponent<SkeletonAnimation>() == null)
                {
                    Debug.LogWarning($"Head prefab {prefab.name} nên có SkeletonAnimation component!");
                }
                break;
            case BodySegment.SegmentType.BODY:
            case BodySegment.SegmentType.TAIL:
                if (prefab.GetComponent<SpriteRenderer>() == null)
                {
                    Debug.LogWarning($"Body/Tail prefab {prefab.name} nên có SpriteRenderer component!");
                }
                break;
        }
        
        return true;
    }
} 