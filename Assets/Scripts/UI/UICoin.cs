using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using RescuePrincess.Managers;

namespace RescuePrincess.UI
{
    public class UICoin : UIPanel
    {
        [Header("Coin Bar Components")]
        [SerializeField] private Transform coinBar;
        [SerializeField] private Text txtCoinValue;
        [SerializeField] private Image imgCoinIcon;
        
        [Header("Coin Container")]
        [SerializeField] private Transform coinContainer;
        [SerializeField] private Transform startPos;
         [SerializeField] private Transform targetPos;
        [SerializeField] private List<GameObject> coinObjects = new List<GameObject>();
        
        [Header("Animation Settings")]
        [SerializeField] private float spreadRadius = 75f; // Bán kính toả ra (50-100)
        [SerializeField] private float spreadDuration = 0.5f; // Thời gian toả ra
        [SerializeField] private float moveToBarDuration = 1f; // Thời gian di chuyển về bar
        [SerializeField] private float delayBetweenCoins = 0.1f; // Delay giữa các coin khi di chuyển về bar
        [SerializeField] private Ease spreadEase = Ease.OutQuad;
        [SerializeField] private Ease moveToBarEase = Ease.InQuad;
        
        [Header("Random Spread Settings")]
        [SerializeField] private float minSpreadRadiusPercent = 0.3f; // Bán kính tối thiểu (30% của spreadRadius)
        [SerializeField] private float maxSpreadRadiusPercent = 1f; // Bán kính tối đa (100% của spreadRadius)
        
        [Header("Magnetic Effect")]
        [SerializeField] private float magneticForce = 2f; // Lực hút nam châm
        [SerializeField] private float magneticRadius = 100f; // Bán kính ảnh hưởng
        
        private int currentCoinValue = 0;
        private bool isAnimating = false;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Tự động tìm các component nếu chưa được assign
            if (coinBar == null)
                coinBar = transform.Find("CoinBar");
                
            if (txtCoinValue == null)
                txtCoinValue = transform.Find("CoinBar/txtQuantity")?.GetComponent<Text>();
                
            if (imgCoinIcon == null)
                imgCoinIcon = transform.Find("CoinBar/imgCoin")?.GetComponent<Image>();
                
            if (coinContainer == null)
                coinContainer = transform.Find("CoinContainer");
                
            if (startPos == null)
                startPos = transform.Find("StartPos");
            
            // Tự động tìm các coin objects
            if (coinContainer != null && coinObjects.Count == 0)
            {
                for (int i = 0; i < coinContainer.childCount; i++)
                {
                    GameObject coinObj = coinContainer.GetChild(i).gameObject;
                    coinObjects.Add(coinObj);
                    coinObj.SetActive(false); // Ẩn tất cả coin ban đầu
                }
            }
        }
        
        private void Start()
        {
            // Load coin value từ GameManager
            if (GameManager.Instance != null)
            {
                UpdateCoinDisplay(GameManager.Instance.CoinValue);
            }
            
            // Ẩn UICoin ban đầu - chỉ hiện khi cần add coin
            Hide();
        }
        
        /// <summary>
        /// Cập nhật hiển thị coin value (không tự động hiển thị panel)
        /// </summary>
        /// <param name="value">Giá trị coin mới</param>
        public void UpdateCoinDisplay(int value)
        {
            currentCoinValue = value;
            
            if (txtCoinValue != null)
            {
                // Animation cho text
                int oldValue = int.Parse(txtCoinValue.text);
                DOTween.To(() => oldValue, x => txtCoinValue.text = x.ToString(), value, 0.5f)
                    .SetEase(Ease.OutQuad);
            }
        }
        
        /// <summary>
        /// Hiệu ứng nhận coin với animation (sử dụng tối đa coin objects)
        /// </summary>
        /// <param name="amount">Số lượng coin nhận được (không ảnh hưởng đến số coin objects hiển thị)</param>
        public void ReceiveCoins(int amount)
        {
            if (isAnimating)
            {
                Debug.LogWarning("Đang có animation coin đang chạy, bỏ qua!");
                return;
            }
            
            // Đảm bảo UICoin hiển thị để user quan sát được
            Show();
            
            StartCoroutine(AnimateReceiveCoins(amount));
        }
        
        /// <summary>
        /// Coroutine để animate việc nhận coin (luôn sử dụng tối đa coin objects)
        /// </summary>
        private IEnumerator AnimateReceiveCoins(int amount)
        {
            isAnimating = true;
            coinContainer.gameObject.SetActive(true);
            
            // Hiển thị UICoin để user quan sát animation
            Show();
            
            // Luôn sử dụng tối đa coin objects trong container
            int coinsToShow = coinObjects.Count;
            
            // Set tất cả coin về startPos và kích hoạt
            for (int i = 0; i < coinsToShow; i++)
            {
                if (i < coinObjects.Count)
                {
                    GameObject coin = coinObjects[i];
                    coin.SetActive(true);
                    coin.transform.position = startPos.position;
                }
            }
            
            // Animation xuất hiện coin
            for (int i = 0; i < coinsToShow; i++)
            {
                if (i < coinObjects.Count)
                {
                    GameObject coin = coinObjects[i];
                    
                    // Chỉ delay, không có scale animation
                    //yield return new WaitForSeconds(0.05f);
                }
            }
            
            //yield return new WaitForSeconds(0.1f);
            
            // Animation toả ra xung quanh (đồng thời)
            for (int i = 0; i < coinsToShow; i++)
            {
                if (i < coinObjects.Count)
                {
                    GameObject coin = coinObjects[i];
                    
                    // Tính toán vị trí toả ra ngẫu nhiên trong vòng tròn
                    float randomAngle = Random.Range(0f, 360f);
                    float randomRadius = Random.Range(spreadRadius * minSpreadRadiusPercent, spreadRadius * maxSpreadRadiusPercent);
                    Vector3 spreadPosition = startPos.position + 
                        Quaternion.Euler(0, 0, randomAngle) * Vector3.right * randomRadius;
                    
                    // Animation toả ra (không có delay)
                    coin.transform.DOMove(spreadPosition, spreadDuration)
                        .SetEase(spreadEase);
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Animation di chuyển về coin bar với hiệu ứng nam châm (lần lượt)
            for (int i = 0; i < coinsToShow; i++)
            {
                if (i < coinObjects.Count)
                {
                    GameObject coin = coinObjects[i];
                    
                    // Animation di chuyển về bar (có delay lần lượt)
                    coin.transform.DOMove(targetPos.position, moveToBarDuration)
                        .SetEase(moveToBarEase)
                        .OnComplete(() => {
                            // Ẩn coin sau khi đến bar
                            coin.SetActive(false);
                            SoundManager.Instance.PlaySFX(SoundManager.Instance.coinClip);
                        });
                    
                    // Delay lần lượt giữa các coin
                    yield return new WaitForSeconds(delayBetweenCoins);
                }
            }
            
            // Cập nhật coin value sau khi animation hoàn thành
            yield return new WaitForSeconds(moveToBarDuration);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(amount);
            }
            
            // Ẩn UICoin sau khi animation hoàn thành
            yield return new WaitForSeconds(1.0f); // Delay để user thấy kết quả cuối
            Hide();
            coinContainer.gameObject.SetActive(false);
            isAnimating = false;
        }
        
        /// <summary>
        /// Test function để test animation
        /// </summary>
        [ContextMenu("Test Receive 5 Coins")]
        public void TestReceiveCoins()
        {
            ReceiveCoins(5);
        }
        
        /// <summary>
        /// Reset tất cả coin về trạng thái ban đầu
        /// </summary>
        public void ResetCoins()
        {
            foreach (GameObject coin in coinObjects)
            {
                if (coin != null)
                {
                    coin.SetActive(false);
                }
            }
            isAnimating = false;
        }
        
        /// <summary>
        /// Force hiển thị UICoin để user quan sát
        /// </summary>
        public void ForceShow()
        {
            Show();
            Debug.Log("UICoin đã được force hiển thị");
        }
        
        /// <summary>
        /// Hiển thị UICoin tạm thời (tự động ẩn sau delay)
        /// </summary>
        /// <param name="delay">Thời gian delay trước khi ẩn (mặc định 2 giây)</param>
        public void ShowTemporarily(float delay = 2f)
        {
            Show();
            StartCoroutine(HideAfterDelay(delay));
        }
        
        /// <summary>
        /// Coroutine để ẩn UICoin sau delay
        /// </summary>
        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Hide();
        }
        
        /// <summary>
        /// Kiểm tra xem UICoin có đang hiển thị không
        /// </summary>
        /// <returns>True nếu đang hiển thị</returns>
        public bool IsVisible()
        {
            return canvasGroup.alpha > 0 && canvasGroup.blocksRaycasts;
        }
        
        /// <summary>
        /// Debug thông tin UICoin
        /// </summary>
        [ContextMenu("Debug UICoin Info")]
        public void DebugInfo()
        {
            Debug.Log($"=== UICoin Debug Info ===");
            Debug.Log($"Current Coin Value: {currentCoinValue}");
            Debug.Log($"Is Animating: {isAnimating}");
            Debug.Log($"Is Visible: {IsVisible()}");
            Debug.Log($"Canvas Group Alpha: {canvasGroup.alpha}");
            Debug.Log($"Coin Objects Count: {coinObjects.Count}");
            Debug.Log($"Coin Container: {(coinContainer != null ? coinContainer.name : "null")}");
            Debug.Log($"Start Pos: {(startPos != null ? startPos.name : "null")}");
            Debug.Log($"Coin Bar: {(coinBar != null ? coinBar.name : "null")}");
            Debug.Log($"Spread Radius: {spreadRadius}");
            Debug.Log($"Min Spread Radius: {spreadRadius * minSpreadRadiusPercent}");
            Debug.Log($"Max Spread Radius: {spreadRadius * maxSpreadRadiusPercent}");
            Debug.Log("========================");
        }
    }
} 