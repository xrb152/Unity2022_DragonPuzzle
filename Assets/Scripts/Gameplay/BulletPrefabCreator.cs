using UnityEngine;

namespace RescuePrincess.Gameplay
{
    /// <summary>
    /// Script to create bullet prefab with necessary components
    /// </summary>
    public class BulletPrefabCreator : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float bulletSpeed = 10f;
        [SerializeField] private float bulletLifetime = 5f;
        [SerializeField] private float bulletSize = 0.1f;
        [SerializeField] private Color bulletColor = Color.red;
        
        [ContextMenu("Create Bullet Prefab")]
        public void CreateBulletPrefab()
        {
            // Create GameObject for bullet
            GameObject bullet = new GameObject("Bullet");
            
            // Add SpriteRenderer
            SpriteRenderer spriteRenderer = bullet.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateCircleSprite();
            spriteRenderer.color = bulletColor;
            spriteRenderer.sortingOrder = 10; // Ensure bullet displays above other objects
            
            // Add CircleCollider2D
            CircleCollider2D collider = bullet.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = bulletSize;
            
            // Add Rigidbody2D
            Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // Not affected by gravity
            rb.drag = 0f; // No drag
            
            // Add Bullet script
            Bullet bulletScript = bullet.AddComponent<Bullet>();
            
            // Set size
            bullet.transform.localScale = Vector3.one * bulletSize;
            
            Debug.Log("Bullet prefab created successfully!");
            Debug.Log("You can now drag this GameObject to create a prefab in your project.");
        }
        
        /// <summary>
        /// Create simple circular sprite
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance <= radius)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
} 