using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public class UIToggleButton : MonoBehaviour
{
    [Header("Toggle Button Components")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private Image imgActive;
    [SerializeField] private Image imgUnActive;
    
    [Header("Fill Components")]
    [SerializeField] private Image unActiveBorder;
    [SerializeField] private Image activeBorder;
    [SerializeField] private Image imgCircle;
    [SerializeField] private Transform unActivePos;
    [SerializeField] private Transform activePos;
    
    [Header("Animation Settings")]
    [SerializeField] private float toggleAnimationDuration = 0.3f;
    [SerializeField] private Ease toggleEase = Ease.OutBack;
    [SerializeField] private float iconFadeDuration = 0.2f;
    
    [Header("Toggle State")]
    [SerializeField] private bool isOn = true;
    
    // Events
    public System.Action<bool> OnToggleChanged;
    
    private void Awake()
    {
        if (toggleButton == null)
            toggleButton = GetComponent<Button>();
            
        SetupButton();
        UpdateVisualState(false); // Update without animation on start
    }
    
    private void SetupButton()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnToggleButtonClicked);
        }
    }
    
    public void OnToggleButtonClicked()
    {
        SetToggleState(!isOn, true);
    }
    
    public void SetToggleState(bool newState, bool animate = true)
    {
        if (isOn == newState) return;
        
        isOn = newState;
        UpdateVisualState(animate);
        OnToggleChanged?.Invoke(isOn);
    }
    
    public bool GetToggleState()
    {
        return isOn;
    }
    
    private void UpdateVisualState(bool animate)
    {
        if (animate)
        {
            AnimateToggleState();
        }
        else
        {
            SetToggleStateImmediate();
        }
    }
    
    private void AnimateToggleState()
    {
        // Animate circle position
        Vector3 targetPos = isOn ? activePos.position : unActivePos.position;
        imgCircle.transform.DOMove(targetPos, toggleAnimationDuration)
            .SetEase(toggleEase)
            .SetUpdate(true); // Không bị ảnh hưởng bởi Time.timeScale
        
        // Set icon states
        if (imgActive != null)
            imgActive.gameObject.SetActive(isOn);
            
        if (imgUnActive != null)
            imgUnActive.gameObject.SetActive(!isOn);
        
        // Set border states
        if (activeBorder != null)
            activeBorder.gameObject.SetActive(isOn);
            
        if (unActiveBorder != null)
            unActiveBorder.gameObject.SetActive(!isOn);
    }
    
    private void SetToggleStateImmediate()
    {
        // Set circle position immediately
        Vector3 targetPos = isOn ? activePos.position : unActivePos.position;
        imgCircle.transform.position = targetPos;
        
        // Set icon states immediately
        if (imgActive != null)
            imgActive.gameObject.SetActive(isOn);
            
        if (imgUnActive != null)
            imgUnActive.gameObject.SetActive(!isOn);
        
        // Set border states immediately
        if (activeBorder != null)
            activeBorder.gameObject.SetActive(isOn);
            
        if (unActiveBorder != null)
            unActiveBorder.gameObject.SetActive(!isOn);
    }
    
    
    public void SetInteractable(bool interactable)
    {
        if (toggleButton != null)
        {
            toggleButton.interactable = interactable;
        }
    }
    
    private void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(OnToggleButtonClicked);
        }
    }
} 