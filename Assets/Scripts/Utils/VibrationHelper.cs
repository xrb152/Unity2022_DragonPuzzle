using UnityEngine;
using RescuePrincess.Managers;
using Lofelt.NiceVibrations;

namespace RescuePrincess.Utils
{
    /// <summary>
    /// Helper class để quản lý vibration một cách tập trung
    /// </summary>
    public static class VibrationHelper
    {
        /// <summary>
        /// Trigger vibration chỉ khi vibration setting được bật
        /// </summary>
        /// <param name="presetType">Loại vibration preset</param>
        /// <param name="fallbackEnabled">Có sử dụng fallback không (default: true)</param>
        public static void TriggerVibrationIfEnabled(HapticPatterns.PresetType presetType, bool fallbackEnabled = true)
        {
            bool shouldVibrate = false;

            // Kiểm tra từ SoundManager trước
            if (SoundManager.Instance != null)
            {
                shouldVibrate = SoundManager.Instance.IsVibrationOn();
            }
            else if (fallbackEnabled)
            {
                // Fallback: kiểm tra PlayerPrefs trực tiếp
                shouldVibrate = PlayerPrefs.GetInt("VibrationOn", 1) == 1;
            }

            if (shouldVibrate)
            {
                HapticPatterns.PlayPreset(presetType);
            }
        }

        /// <summary>
        /// Kiểm tra xem vibration có được bật không
        /// </summary>
        /// <returns>True nếu vibration được bật</returns>
        public static bool IsVibrationEnabled()
        {
            if (SoundManager.Instance != null)
            {
                return SoundManager.Instance.IsVibrationOn();
            }
            
            // Fallback
            return PlayerPrefs.GetInt("VibrationOn", 1) == 1;
        }

        /// <summary>
        /// Trigger vibration ngay lập tức (không kiểm tra setting)
        /// </summary>
        /// <param name="presetType">Loại vibration preset</param>
        public static void TriggerVibration(HapticPatterns.PresetType presetType)
        {
            HapticPatterns.PlayPreset(presetType);
        }
    }
} 