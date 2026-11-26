using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace RescuePrincess.UI
{
    /// <summary>
    /// Script UILoading đơn giản để test độc lập
    /// </summary>
    public class UILoadingSimple : MonoBehaviour
    {
        [Header("Loading Settings")]
        [SerializeField] private Image progressBar;
        [SerializeField] private float loadingDuration = 3f;
        [SerializeField] private string targetSceneName = "Game";
        
        [Header("Debug")]
        [SerializeField] private bool autoStart = true;
        
        private void Start()
        {
            if (autoStart)
            {
                StartLoading();
            }
        }

        /// <summary>
        /// Bắt đầu quá trình loading
        /// </summary>
        public void StartLoading()
        {
            Debug.Log("Bắt đầu loading...");
            
            // Đảm bảo progress bar bắt đầu từ 0
            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
                
                // Animate progress bar từ 0 đến 1
                DOTween.To(() => 0f, x => {
                    progressBar.fillAmount = x;
                }, 1f, loadingDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => {
                    Debug.Log($"Hoàn thành loading, chuyển sang scene: {targetSceneName}");
                    SceneManager.LoadScene(targetSceneName);
                });
            }
            else
            {
                // Fallback nếu không có progress bar
                DOVirtual.DelayedCall(loadingDuration, () => {
                    Debug.Log($"Hoàn thành loading, chuyển sang scene: {targetSceneName}");
                    SceneManager.LoadScene(targetSceneName);
                });
            }
        }

        /// <summary>
        /// Test method để cập nhật progress thủ công
        /// </summary>
        [ContextMenu("Test Update Progress")]
        public void TestUpdateProgress()
        {
            if (progressBar != null)
            {
                progressBar.fillAmount = 0.5f;
            }
        }

        /// <summary>
        /// Test method để chuyển scene ngay lập tức
        /// </summary>
        [ContextMenu("Test Load Scene")]
        public void TestLoadScene()
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
} 