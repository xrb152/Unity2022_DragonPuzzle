using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Spine.Unity;
using RescuePrincess.Managers;
using System.Collections;

public class UIWin : UIPanel
{
    [Header("Win Panel Components")]
    [SerializeField] private GameObject bg;
    [SerializeField] private SkeletonGraphic winBannerSpineAnimation;
    [SerializeField] private SkeletonGraphic winPrincessSpineAnimation;

    [Header("Buttons")]
    [SerializeField] private Button buttonNext;
    [SerializeField] private Text buttonNextText;
    [SerializeField] private Button btnReward;
    [SerializeField] private Text txtMsg;
    [SerializeField] private Image rewardImage;
    [SerializeField] private Text adTxt;

    [Header("Reward System")]
    [SerializeField] private GameObject rewardMultiBar;
    [SerializeField] private Image imgCoin;
    [SerializeField] private Text txtCoin;

    [Header("Multiplier System")]
    [SerializeField] private Image imgGacchaArrow;
    [SerializeField] private Transform[] targets = new Transform[6]; // Target 1-6
    [SerializeField] private Text[] txtMultipliers = new Text[6]; // Text multipliers
    [SerializeField] private float arrowMoveDuration = 0.3f; // Giảm thời gian để chuyển động mượt hơn
    [SerializeField] private bool useSmoothPath = true; // Sử dụng path mượt mà thay vì di chuyển trực tiếp
    [SerializeField] private float arrowYOffset = 20f; // Offset Y cho arrow (cao hơn targets)

    private readonly int[] multiplierValues = { 2, 3, 5, 3, 2 }; // x2, x3, x5, x3, x2 (cho các khoảng)
    private readonly string[] multiplierMessages = {
        "太棒了！获得 ×2 金币！",
        "太棒了！获得 ×3 金币！",
        "太棒了！获得 ×5 金币！",
        "太棒了！获得 ×3 金币！",
        "太棒了！获得 ×2 金币！"
    };

    [Header("Effects")]
    [SerializeField] private GameObject trumpets;
    [SerializeField] private SkeletonGraphic spineAnimTrumpetLeft;
    [SerializeField] private SkeletonGraphic spineAnimTrumpetRight;

    [Header("Animation Settings")]
    [SerializeField] private float showAnimationDuration = 0.5f;
    [SerializeField] private float hideAnimationDuration = 0.3f;

    [Header("State Management")]
    [SerializeField] private bool isShowing = false;      // Đang hiển thị
    [SerializeField] private bool isAnimating = false;    // Đang animate

    // Arrow animation tracking
    private Sequence arrowSequence;
    private bool isArrowAnimating = false;
    private bool isProcessingNextLevel = false;

    // Button animation tracking
    private bool areButtonsShown = false;
    private bool isRewardBarShown = false;
    private bool isWinAnimationsPlaying = false;

    private void Start()
    {
        SetupButtons();
        //SetupInitialState();
    }

    private void SetupButtons()
    {
        if (buttonNext != null)
            buttonNext.onClick.AddListener(OnNextButtonClicked);

        if (btnReward != null)
            btnReward.onClick.AddListener(OnRewardButtonClicked);
    }

    private void SetupInitialState()
    {

        // Ẩn reward bar mặc định
        if (rewardMultiBar != null)
            rewardMultiBar.SetActive(false);

        // Ẩn 2 nút ban đầu
        if (buttonNext != null)
            buttonNext.gameObject.SetActive(false);

        if (btnReward != null)
            btnReward.gameObject.SetActive(false);

        // Disable tất cả spine animation gameobjects
        DisableAllSpineAnimations();

        // Khởi tạo multiplier system
        InitializeMultiplierSystem();
    }

    /// <summary>
    /// Khởi tạo hệ thống multiplier
    /// </summary>
    private void InitializeMultiplierSystem()
    {
        // Set giá trị cho các text multipliers theo thứ tự point 1-6
        if (txtMultipliers.Length >= 6)
        {
            // Forward direction multipliers: point1->point2: x2, point2->point3: x3, point3->point4: x5, point4->point5: x3, point5->point6: x2
            if (txtMultipliers[0] != null) txtMultipliers[0].text = "×2"; // point1
            if (txtMultipliers[1] != null) txtMultipliers[1].text = "×3"; // point2
            if (txtMultipliers[2] != null) txtMultipliers[2].text = "×5"; // point3
            if (txtMultipliers[3] != null) txtMultipliers[3].text = "×3"; // point4
            if (txtMultipliers[4] != null) txtMultipliers[4].text = "×2"; // point5
            if (txtMultipliers[5] != null) txtMultipliers[5].text = "×2"; // point6
        }

        // Đặt arrow về vị trí đầu tiên với Y offset
        if (imgGacchaArrow != null && targets.Length > 0 && targets[0] != null)
        {
            imgGacchaArrow.transform.position = GetArrowPosition(targets[0].position);
        }

        Debug.Log("UIWin: Đã khởi tạo multiplier system");
    }

    /// <summary>
    /// Disable tất cả spine animation gameobjects
    /// </summary>
    private void DisableAllSpineAnimations()
    {
        if (winBannerSpineAnimation != null)
            winBannerSpineAnimation.gameObject.SetActive(false);

        if (winPrincessSpineAnimation != null)
            winPrincessSpineAnimation.gameObject.SetActive(false);

        if (spineAnimTrumpetLeft != null)
            spineAnimTrumpetLeft.gameObject.SetActive(false);

        if (spineAnimTrumpetRight != null)
            spineAnimTrumpetRight.gameObject.SetActive(false);

        Debug.Log("UIWin: Đã disable tất cả spine animation gameobjects");
    }

    public override void Show()
    {
       
        // Kiểm tra nếu đang hiển thị hoặc đang animate
        if (isShowing || isAnimating)
        {
            Debug.LogWarning("UIWin: Đang hiển thị hoặc đang animate, bỏ qua lệnh Show");
            return;
        }

        // Cập nhật state
        isShowing = true;
        isAnimating = true;
   

        Debug.Log("UIWin: Bắt đầu hiển thị");

        base.Show();

        // Reset trạng thái buttons về interactable = true
        EnableAllButtons();

        // Reset trạng thái processing để tránh bị block
        isProcessingNextLevel = false;

        // Reset arrow animation state
        isArrowAnimating = false;
        if (arrowSequence != null && arrowSequence.IsActive())
        {
            arrowSequence.Kill();
            arrowSequence = null;
        }

        // Reset button state
        areButtonsShown = false;
        isRewardBarShown = false;
        isWinAnimationsPlaying = false;

        // Play win sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.winClip);
            Debug.Log("UIWin: Đã play win sound");
        }
        SetupInitialState();
        // Animation hiển thị panel
        canvasGroup.DOFade(1f, showAnimationDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                PlayWinAnimations();
            });
    }

    public override void Hide()
    {
        // Dừng tất cả animations trước khi ẩn
        StopAllAnimations();

        // Reset state
        isShowing = false;
        isAnimating = false;
        areButtonsShown = false;
        isRewardBarShown = false;
        isWinAnimationsPlaying = false;

        Debug.Log("UIWin: Ẩn panel");

        // Animation ẩn panel
        canvasGroup.DOFade(0f, hideAnimationDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                base.Hide();
            });
    }

    /// <summary>
    /// Force show UIWin (bỏ qua cooldown và state check)
    /// </summary>
    public void ForceShow()
    {
        Debug.Log("UIWin: Force show được gọi");

        // Reset state
        isShowing = false;
        isAnimating = false;
        areButtonsShown = false;
        isRewardBarShown = false;
        isWinAnimationsPlaying = false;
        // Dừng animation cũ nếu có
        if (arrowSequence != null && arrowSequence.IsActive())
        {
            arrowSequence.Kill();
            arrowSequence = null;
        }

        Show();
    }

    /// <summary>
    /// Kiểm tra xem UIWin có đang hiển thị không
    /// </summary>
    /// <returns>True nếu đang hiển thị</returns>
    public bool IsShowing()
    {
        return isShowing;
    }

    /// <summary>
    /// Kiểm tra xem UIWin có đang animate không
    /// </summary>
    /// <returns>True nếu đang animate</returns>
    public bool IsAnimating()
    {
        return isAnimating;
    }

    /// <summary>
    /// Dừng tất cả spine animations và disable gameobjects
    /// </summary>
    private void StopAllAnimations()
    {
        if (winBannerSpineAnimation != null)
        {
            winBannerSpineAnimation.AnimationState.ClearTracks();
            winBannerSpineAnimation.gameObject.SetActive(false);
        }

        if (winPrincessSpineAnimation != null)
        {
            winPrincessSpineAnimation.AnimationState.ClearTracks();
            winPrincessSpineAnimation.gameObject.SetActive(false);
        }

        if (spineAnimTrumpetLeft != null)
        {
            spineAnimTrumpetLeft.AnimationState.ClearTracks();
            spineAnimTrumpetLeft.gameObject.SetActive(false);
        }

        if (spineAnimTrumpetRight != null)
        {
            spineAnimTrumpetRight.AnimationState.ClearTracks();
            spineAnimTrumpetRight.gameObject.SetActive(false);
        }

        // Dừng arrow animation nếu đang chạy
        StopArrowAnimation();

        Debug.Log("UIWin: Đã dừng tất cả animations và disable gameobjects");
    }

    /// <summary>
    /// Dừng arrow animation và giữ tại vị trí hiện tại
    /// </summary>
    private void StopArrowAnimation()
    {
        // Dừng arrow sequence nếu đang chạy
        if (arrowSequence != null && arrowSequence.IsActive())
        {
            arrowSequence.Kill();
            arrowSequence = null;
        }

        // Kill tất cả DOTween animations của arrow
        if (imgGacchaArrow != null)
        {
            imgGacchaArrow.transform.DOKill();

            // Giữ arrow tại vị trí hiện tại, không reset về vị trí gốc
            Debug.Log($"UIWin: Arrow dừng tại vị trí: {imgGacchaArrow.transform.position}");
        }

        isArrowAnimating = false;
        Debug.Log("UIWin: Đã dừng arrow animation và giữ tại vị trí hiện tại");
    }

    private void PlayWinAnimations()
    {
        // Kiểm tra xem win animations đã được chạy chưa để tránh chạy 2 lần
        if (isWinAnimationsPlaying)
        {
            Debug.Log("UIWin: Win animations đang chạy, bỏ qua");
            return;
        }

        isWinAnimationsPlaying = true;
        Debug.Log("UIWin: Bắt đầu play win animations");

        // Chạy animation banner với delay nhỏ
        if (winBannerSpineAnimation != null)
        {
            // Active gameobject trước khi play animation
            winBannerSpineAnimation.gameObject.SetActive(true);

            // Kiểm tra animation có tồn tại không
            if (winBannerSpineAnimation.Skeleton.Data.FindAnimation("animation") != null)
            {
                winBannerSpineAnimation.AnimationState.SetAnimation(0, "animation", false);
                Debug.Log("UIWin: Playing win animation cho banner");
            }
            else if (winBannerSpineAnimation.Skeleton.Data.FindAnimation("victory") != null)
            {
                winBannerSpineAnimation.AnimationState.SetAnimation(0, "victory", false);
                Debug.Log("UIWin: Playing victory animation cho banner");
            }
            else
            {
                Debug.LogWarning("UIWin: Không tìm thấy animation 'win' hoặc 'victory' cho banner");
            }
        }

        // Chạy animation princess với delay
        DOVirtual.DelayedCall(0.2f, () =>
        {
            if (winPrincessSpineAnimation != null)
            {
                // Active gameobject trước khi play animation
                winPrincessSpineAnimation.gameObject.SetActive(true);

                if (winPrincessSpineAnimation.Skeleton.Data.FindAnimation("smile") != null)
                {
                    winPrincessSpineAnimation.AnimationState.SetAnimation(0, "smile", true);
                    Debug.Log("UIWin: Playing win animation cho princess");
                }
                else if (winPrincessSpineAnimation.Skeleton.Data.FindAnimation("victory") != null)
                {
                    winPrincessSpineAnimation.AnimationState.SetAnimation(0, "victory", false);
                    Debug.Log("UIWin: Playing victory animation cho princess");
                }
                else if (winPrincessSpineAnimation.Skeleton.Data.FindAnimation("idle") != null)
                {
                    winPrincessSpineAnimation.AnimationState.SetAnimation(0, "idle", false);
                    Debug.Log("UIWin: Playing idle animation cho princess");
                }
                else
                {
                    Debug.LogWarning("UIWin: Không tìm thấy animation phù hợp cho princess");
                }
            }
        });

        // Chạy animation trumpets với delay
        DOVirtual.DelayedCall(0.1f, () =>
        {
            if (spineAnimTrumpetLeft != null)
            {
                // Active gameobject trước khi play animation
                spineAnimTrumpetLeft.gameObject.SetActive(true);

                if (spineAnimTrumpetLeft.Skeleton.Data.FindAnimation("fast_left1") != null)
                {
                    spineAnimTrumpetLeft.AnimationState.SetAnimation(0, "fast_left1", false);
                    Debug.Log("UIWin: Playing trumpet animation cho trumpet left");
                }
                else if (spineAnimTrumpetLeft.Skeleton.Data.FindAnimation("play") != null)
                {
                    spineAnimTrumpetLeft.AnimationState.SetAnimation(0, "play", false);
                    Debug.Log("UIWin: Playing play animation cho trumpet left");
                }
                else
                {
                    Debug.LogWarning("UIWin: Không tìm thấy animation phù hợp cho trumpet left");
                }
            }

            if (spineAnimTrumpetRight != null)
            {
                // Active gameobject trước khi play animation
                spineAnimTrumpetRight.gameObject.SetActive(true);

                if (spineAnimTrumpetRight.Skeleton.Data.FindAnimation("fast_right1") != null)
                {
                    spineAnimTrumpetRight.AnimationState.SetAnimation(0, "fast_right1", false);
                    Debug.Log("UIWin: Playing trumpet animation cho trumpet right");
                }
                else if (spineAnimTrumpetRight.Skeleton.Data.FindAnimation("play") != null)
                {
                    spineAnimTrumpetRight.AnimationState.SetAnimation(0, "play", false);
                    Debug.Log("UIWin: Playing play animation cho trumpet right");
                }
                else
                {
                    Debug.LogWarning("UIWin: Không tìm thấy animation phù hợp cho trumpet right");
                }
            }
        });

        // Hiển thị reward bar với animation sau khi animations đã chạy
        DOVirtual.DelayedCall(0.5f, () =>
        {
            ShowRewardBar();
        });

        // Set isAnimating = false sau khi tất cả animations đã được setup
        DOVirtual.DelayedCall(0.6f, () =>
        {
            isAnimating = false;
            Debug.Log("UIWin: Win animations đã hoàn thành, isAnimating = false");
        });
    }

    private void ShowRewardBar()
    {
        // Kiểm tra xem reward bar đã được hiển thị chưa để tránh animation 2 lần
        if (isRewardBarShown)
        {
            Debug.Log("UIWin: Reward bar đã được hiển thị, bỏ qua animation");
            return;
        }

        isRewardBarShown = true;

        if (rewardMultiBar != null)
        {
            rewardMultiBar.SetActive(true);
            rewardMultiBar.transform.localScale = Vector3.zero;
            rewardMultiBar.transform.DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    // Bắt đầu arrow animation sau khi reward bar hiển thị
                    StartArrowAnimation();

                    // Hiển thị 2 nút sau khi reward bar hiển thị xong
                    ShowButtons();
                });
        }
    }

    /// <summary>
    /// Hiển thị 2 nút buttonNext và btnReward
    /// </summary>
    private void ShowButtons()
    {
        // Kiểm tra xem buttons đã được hiển thị chưa để tránh animation 2 lần
        if (areButtonsShown)
        {
            Debug.Log("UIWin: Buttons đã được hiển thị, bỏ qua animation");
            return;
        }

        areButtonsShown = true;

        // Hiển thị nút Next
        if (buttonNext != null)
        {
            buttonNext.gameObject.SetActive(true);
            buttonNext.transform.localScale = Vector3.zero;
            buttonNext.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.1f); // Delay nhỏ so với reward bar
        }

        // Hiển thị nút Reward
        if (btnReward != null)
        {
            btnReward.gameObject.SetActive(true);
            btnReward.transform.localScale = Vector3.zero;
            btnReward.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.2f); // Delay thêm so với nút Next
        }
        StartCoroutine(ShowAds());
        Debug.Log("UIWin: Đã hiển thị 2 nút buttonNext và btnReward");
    }

    IEnumerator ShowAds()
    {
        yield return new WaitForSeconds(0.5f);
        AdsControl.Instance.ShowInterstital();
    }

    /// <summary>
    /// Bắt đầu animation cho arrow di chuyển qua các targets
    /// </summary>
    private void StartArrowAnimation()
    {
        if (imgGacchaArrow == null || targets.Length == 0)
        {
            Debug.LogWarning("UIWin: Arrow hoặc targets không được setup!");
            return;
        }

        Debug.Log("UIWin: Bắt đầu arrow animation");

        if (useSmoothPath)
        {
            StartSmoothPathAnimation();
        }
        else
        {
            StartDirectPathAnimation();
        }
    }

    /// <summary>
    /// Animation di chuyển trực tiếp giữa các targets
    /// </summary>
    private void StartDirectPathAnimation()
    {
        // Dừng animation cũ nếu đang chạy
        if (arrowSequence != null && arrowSequence.IsActive())
        {
            arrowSequence.Kill();
        }

        // Tạo sequence cho arrow di chuyển liền mạch
        arrowSequence = DOTween.Sequence();
        isArrowAnimating = true;

        // Di chuyển từ point 1 đến point 6 (forward) - liền mạch
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                int targetIndex = i; // Capture index để sử dụng trong callback

                // Tạo position với Y offset
                Vector3 targetPosition = GetArrowPosition(targets[i].position);

                // Di chuyển arrow đến target với ease mượt mà
                arrowSequence.Append(imgGacchaArrow.transform.DOMove(targetPosition, arrowMoveDuration)
                    .SetEase(Ease.InOutQuad));

                // Cập nhật message khi đến target (không dừng)
                arrowSequence.AppendCallback(() =>
                {
                    UpdateMultiplierMessage(targetIndex, true); // true = forward direction
                });
            }
        }

        // Di chuyển từ point 6 về point 1 (backward) - liền mạch
        for (int i = targets.Length - 1; i >= 0; i--)
        {
            if (targets[i] != null)
            {
                int targetIndex = i; // Capture index để sử dụng trong callback

                // Tạo position với Y offset
                Vector3 targetPosition = GetArrowPosition(targets[i].position);

                // Di chuyển arrow đến target với ease mượt mà
                arrowSequence.Append(imgGacchaArrow.transform.DOMove(targetPosition, arrowMoveDuration)
                    .SetEase(Ease.InOutQuad));

                // Cập nhật message khi đến target (không dừng)
                arrowSequence.AppendCallback(() =>
                {
                    UpdateMultiplierMessage(targetIndex, false); // false = backward direction
                });
            }
        }

        // Lặp lại animation
        arrowSequence.SetLoops(-1, LoopType.Restart);

        Debug.Log("UIWin: Direct path animation đã được setup");
    }

    /// <summary>
    /// Animation di chuyển theo path mượt mà
    /// </summary>
    private void StartSmoothPathAnimation()
    {
        // Dừng animation cũ nếu đang chạy
        if (arrowSequence != null && arrowSequence.IsActive())
        {
            arrowSequence.Kill();
        }

        // Tạo path từ tất cả targets với Y offset
        Vector3[] path = new Vector3[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                path[i] = GetArrowPosition(targets[i].position);
            }
        }

        // Tạo path ngược lại với Y offset
        Vector3[] reversePath = new Vector3[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            reversePath[i] = path[targets.Length - 1 - i];
        }

        // Tạo sequence cho arrow di chuyển theo path
        arrowSequence = DOTween.Sequence();
        isArrowAnimating = true;

        // Di chuyển theo path forward
        arrowSequence.Append(imgGacchaArrow.transform.DOPath(path, arrowMoveDuration * targets.Length, PathType.Linear, PathMode.Full3D)
            .SetEase(Ease.Linear)
            .OnWaypointChange(waypointIndex =>
            {
                if (waypointIndex < targets.Length)
                {
                    UpdateMultiplierMessage(waypointIndex, true);
                }
            }));

        // Di chuyển theo path backward
        arrowSequence.Append(imgGacchaArrow.transform.DOPath(reversePath, arrowMoveDuration * targets.Length, PathType.Linear, PathMode.Full3D)
            .SetEase(Ease.Linear)
            .OnWaypointChange(waypointIndex =>
            {
                if (waypointIndex < targets.Length)
                {
                    UpdateMultiplierMessage(targets.Length - 1 - waypointIndex, false);
                }
            }));

        // Lặp lại animation
        arrowSequence.SetLoops(-1, LoopType.Restart);

        Debug.Log("UIWin: Smooth path animation đã được setup");
    }

    /// <summary>
    /// Cập nhật message dựa trên target index và hướng di chuyển
    /// </summary>
    /// <param name="targetIndex">Index của target (0-5)</param>
    /// <param name="isForward">True nếu di chuyển từ point 1->6, false nếu từ 6->1</param>
    private void UpdateMultiplierMessage(int targetIndex, bool isForward)
    {
        int multiplierIndex = GetMultiplierIndex(targetIndex, isForward);

        if (multiplierIndex >= 0 && multiplierIndex < multiplierMessages.Length)
        {
            SetRewardMessage(multiplierMessages[multiplierIndex]);
            //Debug.Log($"UIWin: Cập nhật message cho target {targetIndex + 1} ({GetDirectionText(isForward)}): {multiplierMessages[multiplierIndex]}");
        }
    }

    /// <summary>
    /// Lấy index của multiplier dựa trên target index và hướng di chuyển
    /// </summary>
    /// <param name="targetIndex">Index của target (0-5)</param>
    /// <param name="isForward">True nếu di chuyển từ point 1->6, false nếu từ 6->1</param>
    /// <returns>Index của multiplier (0-4)</returns>
    private int GetMultiplierIndex(int targetIndex, bool isForward)
    {
        if (isForward)
        {
            // Forward: point1->point2: x2, point2->point3: x3, point3->point4: x5, point4->point5: x3, point5->point6: x2
            switch (targetIndex)
            {
                case 0: return 0; // point1: x2
                case 1: return 1; // point2: x3
                case 2: return 2; // point3: x5
                case 3: return 3; // point4: x3
                case 4: return 4; // point5: x2
                case 5: return 4; // point6: x2 (giống point5)
                default: return 0;
            }
        }
        else
        {
            // Backward: point6->point5: x2, point5->point4: x3, point4->point3: x5, point3->point2: x3, point2->point1: x2
            switch (targetIndex)
            {
                case 5: return 4; // point6: x2
                case 4: return 3; // point5: x3
                case 3: return 2; // point4: x5
                case 2: return 1; // point3: x3
                case 1: return 0; // point2: x2
                case 0: return 0; // point1: x2 (giống point2)
                default: return 0;
            }
        }
    }

    /// <summary>
    /// Lấy text mô tả hướng di chuyển
    /// </summary>
    /// <param name="isForward">True nếu di chuyển từ point 1->6, false nếu từ 6->1</param>
    /// <returns>Text mô tả hướng</returns>
    private string GetDirectionText(bool isForward)
    {
        return isForward ? "forward" : "backward";
    }

    /// <summary>
    /// Lấy position cho arrow với Y offset
    /// </summary>
    /// <param name="targetPosition">Position gốc của target</param>
    /// <returns>Position mới với Y offset</returns>
    private Vector3 GetArrowPosition(Vector3 targetPosition)
    {
        return new Vector3(targetPosition.x, targetPosition.y + arrowYOffset, targetPosition.z);
    }

    /// <summary>
    /// Lấy current multiplier dựa trên vị trí arrow
    /// </summary>
    /// <returns>Giá trị multiplier hiện tại</returns>
    private int GetCurrentMultiplier()
    {
        if (imgGacchaArrow == null || targets.Length == 0)
            return 2; // Mặc định x2

        // Tìm target gần nhất với arrow
        float minDistance = float.MaxValue;
        int closestTargetIndex = 0;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                float distance = Vector3.Distance(imgGacchaArrow.transform.position, GetArrowPosition(targets[i].position));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTargetIndex = i;
                }
            }
        }

        // Lấy multiplier dựa trên target index (forward direction)
        int multiplierIndex = GetMultiplierIndex(closestTargetIndex, true);
        if (multiplierIndex >= 0 && multiplierIndex < multiplierValues.Length)
        {
            return multiplierValues[multiplierIndex];
        }

        return 2; // Mặc định x2
    }

    public void SetCoinReward(int coinAmount)
    {
        if (txtCoin != null)
        {
            txtCoin.text = coinAmount.ToString();

            // Animation cho coin
            txtCoin.transform.DOScale(1.2f, 0.2f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    txtCoin.transform.DOScale(1f, 0.1f);
                });
        }
    }

    public void SetRewardMessage(string message)
    {
        if (txtMsg != null)
        {
            txtMsg.text = message;
        }
    }

    public void SetAdText(string adText)
    {
        if (adTxt != null)
        {
            adTxt.text = adText;
        }
    }

    public void SetNextButtonText(string text)
    {
        if (buttonNextText != null)
        {
            buttonNextText.text = text;
        }
    }

    private void OnNextButtonClicked()
    {
        if (isProcessingNextLevel)
        {
            Debug.Log("UIWin: Đang xử lý next level, bỏ qua click!");
            return;
        }

        SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);

        // Disable cả 2 nút ngay lập tức để tránh spam click
        DisableAllButtons();

        isProcessingNextLevel = true;

        // Lấy normalCoinValue từ GameConfig
        int normalCoinValue = 100; // Default value
        if (GameManager.Instance?.BoardManager?.gameConfig != null)
        {
            normalCoinValue = GameManager.Instance.BoardManager.gameConfig.normalCoinValue;
        }

        Debug.Log($"UIWin: Next button - Adding {normalCoinValue} coins (normal coin value)");

        // Hiệu ứng nhận coin trước khi chuyển level
        if (GameManager.Instance?.UIManager != null)
        {
            GameManager.Instance.UIManager.ReceiveCoins(normalCoinValue);

            // Chờ animation coin hoàn thành rồi chuyển level
            StartCoroutine(WaitForCoinAnimationAndNextLevel());
        }
        else
        {
            // Fallback: chuyển level ngay nếu không có UIManager
            ProcessNextLevel();
        }
    }

    /// <summary>
    /// Coroutine chờ animation coin hoàn thành rồi chuyển level
    /// </summary>
    private IEnumerator WaitForCoinAnimationAndNextLevel()
    {
        // Chờ animation coin hoàn thành (khoảng 3-4 giây)
        yield return new WaitForSeconds(3.5f);

        // Chuyển level
        ProcessNextLevel();
    }

    /// <summary>
    /// Disable tất cả buttons để tránh spam click
    /// </summary>
    private void DisableAllButtons()
    {
        if (buttonNext != null)
            buttonNext.interactable = false;

        if (btnReward != null)
            btnReward.interactable = false;

        Debug.Log("UIWin: Đã disable tất cả buttons");
    }

    /// <summary>
    /// Enable tất cả buttons (nếu cần)
    /// </summary>
    private void EnableAllButtons()
    {
        if (buttonNext != null)
            buttonNext.interactable = true;

        if (btnReward != null)
            btnReward.interactable = true;

        Debug.Log("UIWin: Đã enable tất cả buttons");
    }

    /// <summary>
    /// Xử lý chuyển level
    /// </summary>
    private void ProcessNextLevel()
    {
        // Gọi GameManager để chuyển level tiếp theo
        if (GameManager.Instance != null)
        {
            // Kiểm tra xem có level tiếp theo không
            if (GameManager.Instance.HasNextLevel())
            {
                bool success = GameManager.Instance.LoadNextLevel();
                if (success)
                {
                    // Load game với level mới
                    GameManager.Instance.LoadGame();
                }
            }
            else
            {
                Debug.Log("Đã hoàn thành tất cả level!");
                // Có thể chuyển về main menu hoặc hiển thị thông báo
                GameManager.Instance.ReturnToMainMenu();
            }
        }

        Hide();
        isProcessingNextLevel = false;

        // Reset button states (nếu cần enable lại trong tương lai)
        // EnableAllButtons(); // Uncomment nếu cần enable buttons
    }

    private void OnRewardButtonClicked()
    {
        WatchAds();
    }

    /// <summary>
    /// Coroutine chờ animation coin hoàn thành rồi chuyển level (cho reward button)
    /// </summary>
    private IEnumerator WaitForRewardCoinAnimationAndNextLevel()
    {
        // Chờ animation coin hoàn thành (khoảng 2-3 giây)
        yield return new WaitForSeconds(3f);

        // Chuyển level giống như nút next
        ProcessNextLevel();
    }

    /// <summary>
    /// Kiểm tra xem arrow có đang animate không
    /// </summary>
    /// <returns>True nếu arrow đang animate</returns>
    public bool IsArrowAnimating()
    {
        return isArrowAnimating && (arrowSequence != null && arrowSequence.IsActive());
    }

    /// <summary>
    /// Force dừng arrow animation (public method để test)
    /// </summary>
    [ContextMenu("Force Stop Arrow Animation")]
    public void ForceStopArrowAnimation()
    {
        Debug.Log("UIWin: Force stop arrow animation");
        StopArrowAnimation();
    }

    /// <summary>
    /// Reset arrow về vị trí gốc (nếu cần)
    /// </summary>
    [ContextMenu("Reset Arrow to Start Position")]
    public void ResetArrowToStartPosition()
    {
        if (imgGacchaArrow != null && targets.Length > 0 && targets[0] != null)
        {
            imgGacchaArrow.transform.position = GetArrowPosition(targets[0].position);
            Debug.Log($"UIWin: Arrow đã reset về vị trí gốc: {imgGacchaArrow.transform.position}");
        }
    }

    /// <summary>
    /// Test hiển thị buttons (public method để test)
    /// </summary>
    [ContextMenu("Test Show Buttons")]
    public void TestShowButtons()
    {
        Debug.Log("UIWin: Test show buttons");
        ShowButtons();
    }

    /// <summary>
    /// Debug thông tin animations có sẵn
    /// </summary>
    [ContextMenu("Debug Available Animations")]
    private void DebugAvailableAnimations()
    {
        Debug.Log("=== UIWin Available Animations ===");

        if (winBannerSpineAnimation != null && winBannerSpineAnimation.Skeleton != null)
        {
            Debug.Log("Banner animations:");
            foreach (var anim in winBannerSpineAnimation.Skeleton.Data.Animations)
            {
                Debug.Log($"  - {anim.Name}");
            }
        }

        if (winPrincessSpineAnimation != null && winPrincessSpineAnimation.Skeleton != null)
        {
            Debug.Log("Princess animations:");
            foreach (var anim in winPrincessSpineAnimation.Skeleton.Data.Animations)
            {
                Debug.Log($"  - {anim.Name}");
            }
        }

        if (spineAnimTrumpetLeft != null && spineAnimTrumpetLeft.Skeleton != null)
        {
            Debug.Log("Trumpet Left animations:");
            foreach (var anim in spineAnimTrumpetLeft.Skeleton.Data.Animations)
            {
                Debug.Log($"  - {anim.Name}");
            }
        }

        if (spineAnimTrumpetRight != null && spineAnimTrumpetRight.Skeleton != null)
        {
            Debug.Log("Trumpet Right animations:");
            foreach (var anim in spineAnimTrumpetRight.Skeleton.Data.Animations)
            {
                Debug.Log($"  - {anim.Name}");
            }
        }

        Debug.Log("================================");
    }

    /// <summary>
    /// Test arrow animation
    /// </summary>
    [ContextMenu("Test Arrow Animation")]
    private void TestArrowAnimation()
    {
        if (rewardMultiBar != null)
        {
            rewardMultiBar.SetActive(true);
            StartArrowAnimation();
        }
        else
        {
            Debug.LogWarning("UIWin: rewardMultiBar không tồn tại!");
        }
    }

    /// <summary>
    /// Test smooth path animation
    /// </summary>
    [ContextMenu("Test Smooth Path Animation")]
    private void TestSmoothPathAnimation()
    {
        if (rewardMultiBar != null)
        {
            rewardMultiBar.SetActive(true);
            StartSmoothPathAnimation();
        }
        else
        {
            Debug.LogWarning("UIWin: rewardMultiBar không tồn tại!");
        }
    }

    /// <summary>
    /// Test direct path animation
    /// </summary>
    [ContextMenu("Test Direct Path Animation")]
    private void TestDirectPathAnimation()
    {
        if (rewardMultiBar != null)
        {
            rewardMultiBar.SetActive(true);
            StartDirectPathAnimation();
        }
        else
        {
            Debug.LogWarning("UIWin: rewardMultiBar không tồn tại!");
        }
    }

    /// <summary>
    /// Debug multiplier system
    /// </summary>
    [ContextMenu("Debug Multiplier System")]
    private void DebugMultiplierSystem()
    {
        Debug.Log("=== UIWin Multiplier System ===");
        Debug.Log($"Arrow: {(imgGacchaArrow != null ? "OK" : "NULL")}");
        Debug.Log($"Targets count: {targets.Length}");
        Debug.Log($"Multipliers count: {txtMultipliers.Length}");

        Debug.Log("Forward direction (point1->point6):");
        Debug.Log("  point1->point2: x2 - Excellent! Get x2 coins!");
        Debug.Log("  point2->point3: x3 - Excellent! Get x3 coins!");
        Debug.Log("  point3->point4: x5 - Excellent! Get x5 coins!");
        Debug.Log("  point4->point5: x3 - Excellent! Get x3 coins!");
        Debug.Log("  point5->point6: x2 - Excellent! Get x2 coins!");

        Debug.Log("Backward direction (point6->point1):");
        Debug.Log("  point6->point5: x2 - Excellent! Get x2 coins!");
        Debug.Log("  point5->point4: x3 - Excellent! Get x3 coins!");
        Debug.Log("  point4->point3: x5 - Excellent! Get x5 coins!");
        Debug.Log("  point3->point2: x3 - Excellent! Get x3 coins!");
        Debug.Log("  point2->point1: x2 - Excellent! Get x2 coins!");

        Debug.Log("===============================");
    }

    private void OnDestroy()
    {
        // Cleanup button listeners
        if (buttonNext != null)
            buttonNext.onClick.RemoveListener(OnNextButtonClicked);

        if (btnReward != null)
            btnReward.onClick.RemoveListener(OnRewardButtonClicked);
    }

    public void WatchAds()
    {
        AdsControl.Instance.ShowRewardedAd(result =>
        {
            if (result != AdsControl.AdResult.COMPLETED)
            {
                ShowAdResultTip(result);
                return;
            }

            GetRewardCoins();
        });
    }

    /// <summary>
    /// 广告没看完 / 播放失败时给玩家提示，避免点了没反应
    /// </summary>
    private void ShowAdResultTip(AdsControl.AdResult result)
    {
        string message = result == AdsControl.AdResult.SKIPPED
            ? "需要完整观看广告才能领取奖励"
            : "广告暂时无法播放，请稍后再试";

        UINotify notify = (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            ? GameManager.Instance.UIManager.notifyUI
            : null;
        if (notify != null)
        {
            notify.ShowNotify(message, true, 2.5f);
        }
        else
        {
            Debug.Log($"[广告] {message}");
        }
    }

    // 兼容旧调用名
    public void ShowRWUnityAds()
    {
        WatchAds();
    }

    private void GetRewardCoins()
    {
        if (isProcessingNextLevel)
        {
            Debug.Log("UIWin: Đang xử lý next level, bỏ qua click!");
            return;
        }

        SoundManager.Instance.PlaySFX(SoundManager.Instance.btnClip);
        // Xử lý reward button
        if (GameManager.Instance != null)
        {
            Debug.Log("UIWin: Reward button clicked - Dừng arrow animation và xử lý reward");

            // Disable cả 2 nút ngay lập tức để tránh spam click
            DisableAllButtons();

            // Dừng arrow animation hoàn toàn
            StopArrowAnimation();

            // Đảm bảo arrow đã dừng
            if (isArrowAnimating)
            {
                Debug.LogWarning("UIWin: Arrow vẫn đang animate, force stop!");
                StopArrowAnimation();
            }

            // Tính toán coin reward dựa trên vị trí arrow
            int normalCoinValue = 100; // Default value
            if (GameManager.Instance?.BoardManager?.gameConfig != null)
            {
                normalCoinValue = GameManager.Instance.BoardManager.gameConfig.normalCoinValue;
            }
            int currentMultiplier = GetCurrentMultiplier();
            int totalRewardCoins = normalCoinValue * currentMultiplier;

            Debug.Log($"UIWin: Reward button - Adding {totalRewardCoins} coins (normal: {normalCoinValue} x multiplier: {currentMultiplier})");

            // Hiệu ứng nhận coin
            if (GameManager.Instance.UIManager != null)
            {
                GameManager.Instance.UIManager.ReceiveCoins(totalRewardCoins);

                // Chờ animation coin hoàn thành rồi chuyển level
                StartCoroutine(WaitForRewardCoinAnimationAndNextLevel());
            }
        }
    }

}