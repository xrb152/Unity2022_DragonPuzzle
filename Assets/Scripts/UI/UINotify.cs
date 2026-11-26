using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class UINotify : UIPanel
{
    [Header("Notify Settings")]
    [SerializeField] private Text notifyText;
    [SerializeField] private Button closeButton;
    [SerializeField] private float autoHideDelay = 3f;
    [SerializeField] private bool autoHide = true;
    
    [Header("Animation Settings")]
    [SerializeField] private float showDuration = 0.3f;
    [SerializeField] private float hideDuration = 0.2f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;
    
    private Vector3 originalScale;
    private Coroutine autoHideCoroutine;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (notifyText == null)
            notifyText = GetComponentInChildren<Text>();
            
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
            
        originalScale = transform.localScale;
    }
    
    /// <summary>
    /// Show notify with animation
    /// </summary>
    public override void Show()
    {
        ShowWithAnimation();
    }
    
    /// <summary>
    /// Hide notify with animation
    /// </summary>
    public override void Hide()
    {
        HideWithAnimation();
    }
    
    /// <summary>
    /// Show notify with text content and animation
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="customAutoHide">Override auto hide setting</param>
    /// <param name="customDelay">Override auto hide delay</param>
    public virtual void ShowNotify(string message, bool? customAutoHide = null, float? customDelay = null)
    {
        if (notifyText != null)
            notifyText.text = message;
            
        ShowWithAnimation();
        
        // Handle auto hide
        bool shouldAutoHide = customAutoHide ?? autoHide;
        float delay = customDelay ?? autoHideDelay;
        
        if (shouldAutoHide && delay > 0)
        {
            if (autoHideCoroutine != null)
                StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = StartCoroutine(AutoHideCoroutine(delay));
        }
    }
    
    /// <summary>
    /// Show notify with animation effect
    /// </summary>
    private void ShowWithAnimation()
    {
        // Stop any existing auto hide
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        
        // Reset state
        canvasGroup.alpha = 0;
        transform.localScale = Vector3.zero;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        
        // Animate in
        canvasGroup.DOFade(1f, showDuration);
        transform.DOScale(originalScale, showDuration).SetEase(showEase);
    }
    
    /// <summary>
    /// Hide notify with animation effect
    /// </summary>
    private void HideWithAnimation()
    {
        // Stop auto hide coroutine
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        
        // Animate out
        canvasGroup.DOFade(0f, hideDuration);
        transform.DOScale(Vector3.zero, hideDuration).SetEase(hideEase)
            .OnComplete(() => {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            });
    }
    
    /// <summary>
    /// Auto hide coroutine
    /// </summary>
    private IEnumerator AutoHideCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideWithAnimation();
    }
    
    /// <summary>
    /// Set notify text directly
    /// </summary>
    public void SetText(string text)
    {
        if (notifyText != null)
            notifyText.text = text;
    }
    
    /// <summary>
    /// Set auto hide settings
    /// </summary>
    public void SetAutoHide(bool enabled, float delay = 3f)
    {
        autoHide = enabled;
        autoHideDelay = delay;
    }
    
    /// <summary>
    /// Set animation settings
    /// </summary>
    public void SetAnimationSettings(float showDur, float hideDur, Ease showEaseType, Ease hideEaseType)
    {
        showDuration = showDur;
        hideDuration = hideDur;
        showEase = showEaseType;
        hideEase = hideEaseType;
    }
    
    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
    }
} 