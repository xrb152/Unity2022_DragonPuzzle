using UnityEngine;

namespace RescuePrincess.Managers
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "RescuePrincess/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("ColorBox Movement Settings")]
        [Tooltip("Tốc độ di chuyển của ColorBox")]
        public float colorBoxMoveSpeed = 5f;
        
        [Tooltip("Hệ số tốc độ xoay của ColorBox (càng lớn càng nhanh)")]
        public float colorBoxRotationSpeedMultiplier = 10f;
        
        [Header("Game Settings")]
        [Tooltip("Thời gian di chuyển theo path")]
        public float pathDuration = 2f;
        
        [Header("Cannon Settings")]
        [Tooltip("Thời gian delay giữa các lần bắn của CannonSlot")]
        public float cannonFireDelay = 1.0f;
        
        [Header("Animation Settings")]
        [Tooltip("Ease type cho animation di chuyển")]
        public DG.Tweening.Ease moveEase = DG.Tweening.Ease.Linear;
        
        [Tooltip("Ease type cho animation xoay")]
        public DG.Tweening.Ease rotationEase = DG.Tweening.Ease.Linear;
        
        [Header("Effects")]
        [Tooltip("Hiệu ứng va chạm khi Box bị chặn")]
        public GameObject boxCollisionFx;
        
        [Header("Reward Settings")]
        [Tooltip("Số coin cơ bản nhận được khi hoàn thành level")]
        public int normalCoinValue = 100;
    }
} 