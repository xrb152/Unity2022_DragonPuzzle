using UnityEngine;
using RescuePrincess.Managers;
using RescuePrincess.Utils;
using Lofelt.NiceVibrations;

namespace RescuePrincess.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CannonArrow : MonoBehaviour
    {
        [Header("Config References")]
        [SerializeField] private ColorConfig colorConfig;
        [Header("Arrow Renderer")]
        [SerializeField] private SpriteRenderer arrowRenderer;

        public int colorIndex; // Store colorIndex of the bullet

        [Header("Collision Settings")]
        [SerializeField] private bool hasHitTarget = false; // Flag to ensure only 1 target is hit
        public BodySegment currentTarget;

        private void Reset()
        {
            if (arrowRenderer == null)
                arrowRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (arrowRenderer == null)
                arrowRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Set arrow sprite by colorIndex
        /// </summary>
        public void SetArrowSprite(int colorIndex)
        {
            if (colorConfig == null)
            {
                Debug.LogWarning($"CannonArrow: ColorConfig is not assigned on {gameObject.name}");
                return;
            }
            var colorData = colorConfig.GetColorByIndex(colorIndex);
            if (colorData != null && colorData.arrowSprite != null)
            {
                arrowRenderer.sprite = colorData.arrowSprite;
            }
            else
            {
                Debug.LogWarning($"CannonArrow: No arrow sprite found for colorIndex {colorIndex} on {gameObject.name}");
            }
        }

        /// <summary>
        /// Reset hit target state (used when reusing arrow)
        /// </summary>
        public void ResetHitState()
        {
            hasHitTarget = false;
        }

        /// <summary>
        /// Check if arrow has hit target
        /// </summary>
        /// <returns>True if target has been hit</returns>
        public bool HasHitTarget()
        {
            return hasHitTarget;
        }

        /// <summary>
        /// Set color index for arrow
        /// </summary>
        /// <param name="index">Color index</param>
        public void SetColorIndex(int index)
        {
            colorIndex = index;
            SetArrowSprite(index);
            ResetHitState(); // Reset state when setting new color
        }

        /// <summary>
        /// Debug arrow information
        /// </summary>
        public void DebugArrowInfo()
        {
            Debug.Log($"=== Arrow Debug Info ===");
            Debug.Log($"Arrow: {gameObject.name}");
            Debug.Log($"Color Index: {colorIndex}");
            Debug.Log($"Has Hit Target: {hasHitTarget}");
            Debug.Log($"Collider Enabled: {GetComponent<Collider2D>()?.enabled}");
            Debug.Log($"Position: {transform.position}");
            Debug.Log("========================");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Check if arrow has already hit target
            if (hasHitTarget)
            {
                return; // If already hit, skip
            }

            var bodySegment = other.GetComponent<BodySegment>();
            if (bodySegment != null)
            {
                if (currentTarget != bodySegment)
                    return;

                if (bodySegment.ColorIndex == colorIndex)
                {
                    // Mark that target has been hit
                    hasHitTarget = true;

                    // Disable collider immediately to avoid additional collisions
                    var arrowCollider = GetComponent<Collider2D>();
                    if (arrowCollider != null)
                    {
                        arrowCollider.enabled = false;
                    }

                    // Spawn DragonPartExplosion at bodySegment position
                    if (colorConfig != null)
                    {
                        var colorData = colorConfig.GetColorByIndex(colorIndex);
                        if (colorData != null && colorData.DragonPartExplosion != null)
                        {
                            var explosion = Instantiate(colorData.DragonPartExplosion, bodySegment.transform.position, Quaternion.identity);
                            // Play all particle systems in the spawned effect
                            foreach (var ps in explosion.GetComponentsInChildren<ParticleSystem>())
                            {
                                ps.Play();
                            }
                            // Destroy explosion after 2 seconds to avoid memory leaks
                            Destroy(explosion, 2f);
                        }
                    }
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.bodyExplodeClip);
                    // Disable body segment
                    bodySegment.DisableSegment();
                    // Check vibration setting before using
                    TriggerVibrationIfEnabled(HapticPatterns.PresetType.LightImpact);
                    // Destroy arrow
                    Destroy(gameObject);

                    //Debug.Log($"Arrow hit body segment with color index {colorIndex} and destroyed");
                }
            }
        }

        /// <summary>
        /// Trigger vibration only when vibration setting is enabled
        /// </summary>
        /// <param name="presetType">Vibration preset type</param>
        private void TriggerVibrationIfEnabled(HapticPatterns.PresetType presetType)
        {
            VibrationHelper.TriggerVibrationIfEnabled(presetType);
        }
    }
}