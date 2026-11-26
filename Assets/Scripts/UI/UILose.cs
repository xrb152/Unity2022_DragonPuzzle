using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using RescuePrincess.Managers;
using System.Collections;

namespace RescuePrincess.UI
{
    public class UILose : UIPanel
    {
        [Header("Lose Panel Components")]
        [SerializeField] private GameObject background;
        [SerializeField] private GameObject spineAnimationPrincess;
        [SerializeField] private Button buttonReplay;
        [SerializeField] private Button buttonHome;
        [SerializeField] private GameObject ribbon;

        [Header("State Management")]
        [SerializeField] private bool isShowing = false;
        [SerializeField] private bool isAnimating = false;
        [SerializeField] private float showCooldown = 1.0f; // Thời gian cooldown giữa các lần show

        private float lastShowTime = 0f;
        private Sequence animationSequence;

        protected override void Awake()
        {
            base.Awake();

            // Setup button listeners
            if (buttonReplay != null)
                buttonReplay.onClick.AddListener(OnReplayButtonClicked);

            if (buttonHome != null)
                buttonHome.onClick.AddListener(OnHomeButtonClicked);
        }

        public override void Show()
        {
            // Kiểm tra cooldown để tránh show nhiều lần liên tiếp
            if (Time.time - lastShowTime < showCooldown)
            {
                Debug.LogWarning($"UILose: Đang trong cooldown, bỏ qua lệnh Show. Thời gian còn lại: {showCooldown - (Time.time - lastShowTime):F2}s");
                return;
            }

            // Kiểm tra nếu đang hiển thị hoặc đang animate
            if (isShowing || isAnimating)
            {
                Debug.LogWarning("UILose: Đang hiển thị hoặc đang animate, bỏ qua lệnh Show");
                return;
            }

            // Cập nhật state
            isShowing = true;
            isAnimating = true;
            lastShowTime = Time.time;

            Debug.Log("UILose: Bắt đầu hiển thị");

            // Play lose sound
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.loseClip);
                Debug.Log("UILose: Đã play lose sound");
            }

            base.Show();

            // Animation cho các thành phần con
            AnimateChildElements();
        }

        IEnumerator ShowAds()
        {
            yield return new WaitForSeconds(0.5f);
            AdsControl.Instance.ShowInterstital();
        }

        public override void Hide()
        {
            // Dừng animation nếu đang chạy
            if (animationSequence != null && animationSequence.IsActive())
            {
                animationSequence.Kill();
                animationSequence = null;
            }

            // Reset state
            isShowing = false;
            isAnimating = false;

            Debug.Log("UILose: Ẩn panel");

            base.Hide();
        }

        private void AnimateChildElements()
        {
            // Dừng animation cũ nếu có
            if (animationSequence != null && animationSequence.IsActive())
            {
                animationSequence.Kill();
            }

            // Tạo sequence mới
            animationSequence = DOTween.Sequence();

            // Animation cho ribbon (DoScale)
            if (ribbon != null)
            {
                ribbon.transform.localScale = Vector3.zero;
                animationSequence.Insert(0.2f, ribbon.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
            }

            // Animation cho princess (hiện)
            if (spineAnimationPrincess != null)
            {
                spineAnimationPrincess.transform.localScale = Vector3.zero;
                animationSequence.Insert(0.4f, spineAnimationPrincess.transform.DOScale(Vector3.one * 0.75f, 0.4f).SetEase(Ease.OutBack));
            }

            // Animation cho buttonReplay (hiện sau cùng)
            if (buttonReplay != null)
            {
                buttonReplay.transform.localScale = Vector3.zero;
                animationSequence.Insert(0.6f, buttonReplay.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
            }

            // Animation cho buttonHome (hiện sau cùng)
            if (buttonHome != null)
            {
                buttonHome.transform.localScale = Vector3.zero;
                animationSequence.Insert(0.7f, buttonHome.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
            }

            // Kết thúc animation
            animationSequence.OnComplete(() =>
            {
                isAnimating = false;
                Debug.Log("UILose: Animation hoàn thành");
                StartCoroutine(ShowAds());
            });
        }

        private void OnReplayButtonClicked()
        {
            Debug.Log("UILose: Replay button clicked");
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
            // Gọi GameManager để restart level
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }

            Hide();
        }

        private void OnHomeButtonClicked()
        {
            Debug.Log("UILose: Home button clicked");
            SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
            // Gọi GameManager để về main menu
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }

            Hide();
        }

        /// <summary>
        /// Force show UILose (bỏ qua cooldown và state check)
        /// </summary>
        public void ForceShow()
        {
            Debug.Log("UILose: Force show được gọi");

            // Reset state
            isShowing = false;
            isAnimating = false;
            lastShowTime = 0f;

            // Dừng animation cũ nếu có
            if (animationSequence != null && animationSequence.IsActive())
            {
                animationSequence.Kill();
                animationSequence = null;
            }

            Show();
        }

        /// <summary>
        /// Kiểm tra xem UILose có đang hiển thị không
        /// </summary>
        /// <returns>True nếu đang hiển thị</returns>
        public bool IsShowing()
        {
            return isShowing;
        }

        /// <summary>
        /// Kiểm tra xem UILose có đang animate không
        /// </summary>
        /// <returns>True nếu đang animate</returns>
        public bool IsAnimating()
        {
            return isAnimating;
        }

        /// <summary>
        /// Lấy thời gian còn lại của cooldown
        /// </summary>
        /// <returns>Thời gian còn lại (0 nếu hết cooldown)</returns>
        public float GetRemainingCooldown()
        {
            float remaining = showCooldown - (Time.time - lastShowTime);
            return Mathf.Max(0, remaining);
        }

        /// <summary>
        /// Set cooldown time
        /// </summary>
        /// <param name="cooldown">Thời gian cooldown mới</param>
        public void SetShowCooldown(float cooldown)
        {
            showCooldown = Mathf.Max(0, cooldown);
        }

        private void OnDestroy()
        {
            // Cleanup button listeners
            if (buttonReplay != null)
                buttonReplay.onClick.RemoveListener(OnReplayButtonClicked);

            if (buttonHome != null)
                buttonHome.onClick.RemoveListener(OnHomeButtonClicked);

            // Cleanup animation
            if (animationSequence != null && animationSequence.IsActive())
            {
                animationSequence.Kill();
            }
        }

        /// <summary>
        /// Debug thông tin UILose
        /// </summary>
        [ContextMenu("Debug UILose Info")]
        public void DebugInfo()
        {
            Debug.Log($"=== UILose Debug Info ===");
            Debug.Log($"Is Showing: {isShowing}");
            Debug.Log($"Is Animating: {isAnimating}");
            Debug.Log($"Show Cooldown: {showCooldown}s");
            Debug.Log($"Remaining Cooldown: {GetRemainingCooldown():F2}s");
            Debug.Log($"Last Show Time: {lastShowTime:F2}");
            Debug.Log($"Current Time: {Time.time:F2}");
            Debug.Log($"Canvas Group Alpha: {canvasGroup.alpha}");
            Debug.Log($"Canvas Group Blocks Raycasts: {canvasGroup.blocksRaycasts}");
            Debug.Log($"Canvas Group Interactable: {canvasGroup.interactable}");
            Debug.Log("========================");
        }
    }
}