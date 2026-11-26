using UnityEngine;
using RescuePrincess.Managers;

namespace RescuePrincess.Gameplay
{
    public class Bullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 5f;
        
        private Vector3 direction;
        private Rigidbody2D rb;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            
            // Destroy bullet after lifetime
            Destroy(gameObject, lifetime);
        }
        
        /// <summary>
        /// Set the direction for the bullet to move
        /// </summary>
        /// <param name="newDirection">Normalized direction vector</param>
        public void SetDirection(Vector3 newDirection)
        {
            direction = newDirection.normalized;
            
            // Set velocity
            if (rb != null)
            {
                rb.velocity = direction * speed;
            }
            
            // Rotate bullet to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        /// <summary>
        /// Set the speed of the bullet
        /// </summary>
        /// <param name="newSpeed">New speed value</param>
        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
            if (rb != null && direction != Vector3.zero)
            {
                rb.velocity = direction * speed;
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Handle collision with body segments
            BodySegment segment = other.GetComponent<BodySegment>();
            if (segment != null && segment.Type == BodySegment.SegmentType.BODY)
            {
                // Check game state - don't cause damage during GameOver
                if (RescuePrincess.Managers.GameManager.Instance?.CurrentGameState == RescuePrincess.Managers.GameState.GameOver)
                {
                    Debug.Log($"Bullet hit body segment during GameOver - only destroying bullet: {segment.name}");
                    // Only destroy bullet, don't cause damage
                    Destroy(gameObject);
                    return;
                }
                
                // TODO: Handle bullet hit on body segment
                Debug.Log($"Bullet hit body segment: {segment.name}");
                
                // Destroy bullet on hit
                Destroy(gameObject);
            }
        }
    }
} 