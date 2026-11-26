using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace RescuePrincess.UI
{
    public class UILoading : MonoBehaviour
    {
        [Header("Loading UI Components")]
        [SerializeField] private Image progressBar;
        [SerializeField] private float loadingDuration = 3f;
        [SerializeField] private string targetSceneName = "Game";
        
        private float currentProgress = 0f;
        private bool isLoading = false;

        private void Awake()
        {
            // Tự động tìm progress bar nếu chưa được assign
            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Image>();
                if (progressBar == null)
                {
                    Debug.LogWarning("Không tìm thấy Image component cho progress bar!");
                }
            }
            
            // Đảm bảo progress bar bắt đầu từ 0
            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
            }
        }

        private void Start()
        {
            StartLoading();
        }

        /// <summary>
        /// Bắt đầu quá trình loading
        /// </summary>
        public void StartLoading()
        {
            if (isLoading) return;
            
            isLoading = true;
            currentProgress = 0f;
            
            // Animate progress bar từ 0 đến 1 trong 3 giây
            if (progressBar != null)
            {
                DOTween.To(() => currentProgress, x => {
                    currentProgress = x;
                    progressBar.fillAmount = x;
                }, 1f, loadingDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => {
                    // Chuyển scene khi hoàn thành
                    LoadTargetScene();
                });
            }
            else
            {
                // Fallback nếu không có progress bar
                DOVirtual.DelayedCall(loadingDuration, () => {
                    LoadTargetScene();
                });
            }
        }

        /// <summary>
        /// Chuyển đến scene đích
        /// </summary>
        private void LoadTargetScene()
        {
            Debug.Log($"Chuyển sang scene: {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }

        /// <summary>
        /// Cập nhật progress bar thủ công (có thể gọi từ bên ngoài)
        /// </summary>
        /// <param name="progress">Giá trị progress từ 0 đến 1</param>
        public void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.fillAmount = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Dừng loading và chuyển scene ngay lập tức
        /// </summary>
        public void SkipLoading()
        {
            if (isLoading)
            {
                DOTween.Kill(this);
                LoadTargetScene();
            }
        }

        /// <summary>
        /// Thiết lập thời gian loading
        /// </summary>
        /// <param name="duration">Thời gian loading tính bằng giây</param>
        public void SetLoadingDuration(float duration)
        {
            loadingDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        /// Thiết lập tên scene đích
        /// </summary>
        /// <param name="sceneName">Tên scene cần chuyển đến</param>
        public void SetTargetScene(string sceneName)
        {
            targetSceneName = sceneName;
        }
    }
} 