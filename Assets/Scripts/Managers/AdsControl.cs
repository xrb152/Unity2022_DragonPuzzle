using System;
using System.Collections;
using UnityEngine;

/*
 * 广告控制中心
 *
 * 统一的广告入口：上层 UI（UIWin / UIUnlockSlot / UILose）只调用这里的方法，
 * 内部再转给抖音小游戏广告（AdManager -> TikTokPlatform），
 * 这样以后换广告平台（TopOn / 微信）只要改这一层。
 *
 * 激励视频结果：
 *   COMPLETED 完整看完，可以发奖励
 *   SKIPPED   中途关闭，不发奖励
 *   FAILED    加载/展示失败，不发奖励
 */
public class AdsControl : MonoBehaviour
{
    private static AdsControl instance;

    #region 旧广告配置（已停用，保留字段避免场景配置丢失）
    public string Android_AppID, IOS_AppID;
    public string Android_Banner_Key, IOS_Banner_Key;
    public string Android_Interestital_Key, IOS_Interestital_Key;
    public string Android_RW_Key, IOS_RW_Key;

    public string androidUnityGameId;
    public string iOSUnityGameId;
    public string androidUnityAdUnitId;
    public string iOSUnityAdUnitId;

    [HideInInspector]
    public string adUnityUnitId;

    public string androidUnityRWAdUnitId;
    public string iOSUnityRWAdUnitId;

    [HideInInspector]
    public string adUnityRWUnitId = null;

    public bool testMode;
    #endregion

    [Header("抖音广告")]
    [Tooltip("编辑器里不真正拉广告，直接模拟“看完”，方便测发奖励流程。真机打包无效。")]
    [SerializeField] private bool simulateRewardInEditor = true;

    [Tooltip("插屏广告每调用多少次展示一次（1 = 每次都展示）")]
    [SerializeField] private int interstitialInterval = 3;

    public enum ADS_TYPE
    {
        ADMOB,
        UNITY,
        MEDIATION,
        DOUYIN
    }

    public ADS_TYPE currentAdsType = ADS_TYPE.DOUYIN;

    /// <summary>
    /// 激励视频播放结果，替代原 Unity Ads 的 UnityAdsShowCompletionState。
    /// </summary>
    public enum AdResult
    {
        COMPLETED,
        SKIPPED,
        FAILED
    }

    public static AdsControl Instance { get { return instance; } }

    /// <summary>激励视频是否正在加载/播放，避免连点重复拉广告。</summary>
    private bool isRewardPlaying;

    private Coroutine rewardWatchdog;

    private void Awake()
    {
        if (FindObjectsOfType(typeof(AdsControl)).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        adUnityUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? iOSUnityAdUnitId
            : androidUnityAdUnitId;

        adUnityRWUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? iOSUnityRWAdUnitId
            : androidUnityRWAdUnitId;

        if (AdManager.Instance == null)
        {
            Debug.LogWarning("[广告] 没找到 AdManager（应在 Loading 场景里，且已配置广告位），广告将不可用。");
            return;
        }

        AdManager manager = AdManager.Instance;

        if (string.IsNullOrEmpty(manager.AppId))
        {
            Debug.LogWarning("[广告] AdManager 的 AppID 为空，激励视频会退回代码里的默认广告位；正式发布前请在 Loading 场景填写自己的 AppID。");
        }

        if (manager.videoAdIdList == null || manager.videoAdIdList.Count == 0)
        {
            Debug.LogWarning("[广告] 激励视频广告位列表为空，正在使用默认广告位；正式发布前请填写自己的激励视频广告位 ID。");
        }

        Debug.Log("[广告] 抖音广告通道已就绪。");
    }

    #region 插屏广告

    public void LoadInterstitialAd()
    {
        // 抖音插屏由 TikTokPlatform 创建后自动 Load 并常驻，这里不需要额外预加载。
    }

    public void ShowInterstitialAd()
    {
        if (IsRemoveAds())
            return;

        // 编辑器里 TT 接口是空实现，调了也没有画面，直接跳过避免误导。
        if (Application.isEditor)
        {
            Debug.Log("[广告] 编辑器不展示插屏广告（模拟跳过）。");
            return;
        }

        AdManager manager = AdManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[广告] 没有 AdManager，插屏广告无法展示。");
            return;
        }

        manager.ShowInterstitialAd();
    }

    public void DestroyInterstitialAd()
    {
        // 抖音插屏实例由 TikTokPlatform 内部持有，关闭后自动重建，这里无需手动销毁。
    }

    /// <summary>
    /// 按节奏控制插屏展示：默认每 3 次调用展示一次（可通过 Inspector 调整）。
    /// </summary>
    public void ShowInterstital()
    {
        if (IsRemoveAds())
            return;

        int interval = interstitialInterval <= 0 ? 3 : interstitialInterval;
        int numberShow = PlayerPrefs.GetInt("ShowAds");

        if (numberShow < interval - 1)
        {
            numberShow++;
            PlayerPrefs.SetInt("ShowAds", numberShow);
            return;
        }

        PlayerPrefs.SetInt("ShowAds", 0);
        ShowInterstitialAd();
    }

    #endregion

    #region 激励视频广告

    public void LoadRewardAd()
    {
        // 抖音激励视频在 TikTokPlatform 构造时创建，播放结束/出错后会自动重建，无需手动预加载。
    }

    public bool IsRewardedVideoAvailable()
    {
        if (isRewardPlaying)
            return false;

        return Application.isEditor || AdManager.Instance != null;
    }

    /// <summary>
    /// 播放激励视频。回调只会触发一次；只有 COMPLETED 才应发放奖励。
    /// </summary>
    public void ShowRewardedAd(Action<AdResult> onComplete)
    {
        if (isRewardPlaying)
        {
            Debug.LogWarning("[广告] 激励视频正在播放中，忽略这次点击。");
            onComplete?.Invoke(AdResult.FAILED);
            return;
        }

        isRewardPlaying = true;

        bool finished = false;
        void Finish(AdResult result)
        {
            if (finished)
                return;

            finished = true;
            isRewardPlaying = false;

            if (rewardWatchdog != null)
            {
                StopCoroutine(rewardWatchdog);
                rewardWatchdog = null;
            }

            Debug.Log($"[广告] 激励视频结束：{result}");
            onComplete?.Invoke(result);
        }

        // 编辑器里直接模拟“看完”，方便验证奖励发放
        if (Application.isEditor && simulateRewardInEditor)
        {
            StartCoroutine(SimulateRewardInEditorCoroutine(Finish));
            return;
        }

        AdManager manager = AdManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[广告] 没有 AdManager，无法播放激励视频。请确认 Loading 场景里有 AdManager，并填写了 AppID 与激励视频广告位。");
            Finish(AdResult.FAILED);
            return;
        }

        try
        {
            manager.ShowRewardedVideoAd(
                () => Finish(AdResult.COMPLETED),
                error => Finish(ParseResultFromError(error)));
        }
        catch (Exception e)
        {
            Debug.LogError($"[广告] 调用激励视频异常：{e.Message}");
            Finish(AdResult.FAILED);
            return;
        }

        // 兜底：SDK 万一不回调，避免“播放中”标记一直挂着导致再也点不动
        rewardWatchdog = StartCoroutine(RewardWatchdogCoroutine());
    }

    private static AdResult ParseResultFromError(string error)
    {
        if (string.IsNullOrEmpty(error))
            return AdResult.FAILED;

        // TikTokPlatform 在用户提前关闭广告时回传“广告未看完”
        if (error.Contains("未看完") || error.Contains("取消") || error.ToLower().Contains("cancel"))
            return AdResult.SKIPPED;

        return AdResult.FAILED;
    }

    private IEnumerator SimulateRewardInEditorCoroutine(Action<AdResult> finish)
    {
        Debug.Log("[广告] 编辑器模拟：0.5 秒后视为完整看完激励视频。");
        yield return new WaitForSecondsRealtime(0.5f);
        finish?.Invoke(AdResult.COMPLETED);
    }

    private IEnumerator RewardWatchdogCoroutine()
    {
        yield return new WaitForSecondsRealtime(120f);

        if (isRewardPlaying)
        {
            Debug.LogWarning("[广告] 激励视频 120 秒无回调，解除“播放中”状态。");
            isRewardPlaying = false;
        }

        rewardWatchdog = null;
    }

    // 兼容旧调用名，逻辑统一走 ShowRewardedAd
    public void PlayUnityVideoAd(Action<string, AdResult> onComplete)
    {
        ShowRewardedAd(result => onComplete?.Invoke(adUnityRWUnitId, result));
    }

    public void LoadUnityAd()
    {
        LoadRewardAd();
    }

    public void LoadUnityRWAd()
    {
        LoadRewardAd();
    }

    public void ShowUnityAd()
    {
        ShowInterstitialAd();
    }

    public void ShowUnityRWAd()
    {
        ShowRewardedAd(null);
    }

    public void InitializeUnityAds()
    {
        // 已无 Unity Ads，保留空方法兼容旧调用
    }

    #endregion

    #region Banner 广告

    public void CreateBannerView()
    {
        // TODO: 如需 banner，接入抖音 banner 广告
    }

    public void LoadBannerAd()
    {
        // TODO: 如需 banner，接入抖音 banner 广告
    }

    public void ShowBannerAd()
    {
        // TODO: 如需 banner，接入抖音 banner 广告
    }

    public void HideBannerAd()
    {
        // TODO: 如需 banner，接入抖音 banner 广告
    }

    public void DestroyBannerAd()
    {
        // TODO: 如需 banner，接入抖音 banner 广告
    }

    #endregion

    #region 去广告

    public void RemoveAds()
    {
        PlayerPrefs.SetInt("removeAds", 1);
        HideBannerAd();
        DestroyBannerAd();
    }

    public bool IsRemoveAds()
    {
        if (!PlayerPrefs.HasKey("removeAds"))
            return false;

        return PlayerPrefs.GetInt("removeAds") == 1;
    }

    #endregion
}
