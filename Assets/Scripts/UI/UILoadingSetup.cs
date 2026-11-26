using UnityEngine;
using UnityEngine.UI;

namespace RescuePrincess.UI
{
    /// <summary>
    /// Script hướng dẫn thiết lập UI Loading trong Unity Editor
    /// </summary>
    public class UILoadingSetup : MonoBehaviour
    {
        [Header("Hướng dẫn thiết lập")]
        [TextArea(5, 10)]
        [SerializeField] private string setupInstructions = 
            "HƯỚNG DẪN THIẾT LẬP UI LOADING:\n\n" +
            "1. Tạo Canvas mới trong scene Loading\n" +
            "2. Thêm UILoading script vào Canvas\n" +
            "3. Tạo UI Image làm progress bar:\n" +
            "   - Tạo Image con trong Canvas\n" +
            "   - Đặt Image Type = Filled\n" +
            "   - Đặt Fill Method = Horizontal\n" +
            "   - Đặt Fill Origin = Left\n" +
            "   - Đặt Fill Amount = 0\n" +
            "4. Kéo Image progress bar vào field 'Progress Bar' của UILoading script\n" +
            "5. Đặt 'Loading Duration' = 3 (giây)\n" +
            "6. Đặt 'Target Scene Name' = 'Game'\n\n" +
            "Lưu ý: Script sẽ tự động chuyển scene sau 3 giây với progress bar từ 0 đến 1";

        private void Start()
        {
            // Script này chỉ dùng để hiển thị hướng dẫn
            Debug.Log("UILoadingSetup: Vui lòng làm theo hướng dẫn trong Inspector để thiết lập UI Loading");
        }
    }
} 