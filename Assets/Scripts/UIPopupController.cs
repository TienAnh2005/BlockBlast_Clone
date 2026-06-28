using DG.Tweening;
using UnityEngine;

// Drives popup open/close transitions while staying safe during pause states.
[DisallowMultipleComponent]
public class UIPopupController : MonoBehaviour
{
    private const float HiddenScale = 0.92f;

    [Header("References")]
    [SerializeField] private RectTransform mainPopupPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CanvasGroup backgroundOverlay;

    [Header("Animation")]
    [SerializeField, Min(0.0f)] private float duration = 0.3f;
    [SerializeField] private Ease openEase = Ease.OutBack;
    [SerializeField] private Ease closeEase = Ease.InBack;

    private Sequence openSequence;
    private Sequence closeSequence;

    private void Awake()
    {
        EnsureConfigured();
    }

    private void Reset()
    {
        mainPopupPanel = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        backgroundOverlay = GetComponent<CanvasGroup>();
    }

    public void Configure(RectTransform popupPanel = null, CanvasGroup popupCanvasGroup = null, CanvasGroup overlayCanvasGroup = null)
    {
        if (popupPanel != null)
        {
            mainPopupPanel = popupPanel;
        }

        if (popupCanvasGroup != null)
        {
            canvasGroup = popupCanvasGroup;
        }

        if (overlayCanvasGroup != null)
        {
            backgroundOverlay = overlayCanvasGroup;
        }

        EnsureConfigured();
    }

    public void OpenPopup()
    {
        EnsureConfigured();
        if (ValidateReferences() == false)
        {
            return;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (mainPopupPanel != null)
        {
            mainPopupPanel.gameObject.SetActive(true);
        }

        KillActiveTweens();
        SetInteractable(false);
        SetVisualState(HiddenScale, 0.0f, 0.0f);

        openSequence = DOTween.Sequence().SetUpdate(true);

        if (backgroundOverlay != null && backgroundOverlay != canvasGroup)
        {
            openSequence.Join(backgroundOverlay.DOFade(1.0f, duration).SetUpdate(true));
        }

        openSequence.Join(mainPopupPanel.DOScale(Vector3.one, duration).SetEase(openEase).SetUpdate(true));
        openSequence.Join(canvasGroup.DOFade(1.0f, duration).SetUpdate(true));
        openSequence.OnComplete(() => SetInteractable(true));
    }

    public void ClosePopup()
    {
        EnsureConfigured();
        if (ValidateReferences() == false)
        {
            return;
        }

        KillActiveTweens();
        SetInteractable(false);

        closeSequence = DOTween.Sequence().SetUpdate(true);

        if (backgroundOverlay != null && backgroundOverlay != canvasGroup)
        {
            closeSequence.Join(backgroundOverlay.DOFade(0.0f, duration).SetUpdate(true));
        }

        closeSequence.Join(mainPopupPanel.DOScale(Vector3.one * HiddenScale, duration).SetEase(closeEase).SetUpdate(true));
        closeSequence.Join(canvasGroup.DOFade(0.0f, duration).SetUpdate(true));
        closeSequence.OnComplete(() => gameObject.SetActive(false));
    }

    public void HideImmediately()
    {
        EnsureConfigured();
        KillActiveTweens();

        if (ValidateReferences())
        {
            SetInteractable(false);
            SetVisualState(HiddenScale, 0.0f, 0.0f);
        }

        gameObject.SetActive(false);
    }

    private void EnsureConfigured()
    {
        if (backgroundOverlay == null)
        {
            backgroundOverlay = EnsureCanvasGroup(gameObject);
        }

        if (mainPopupPanel == null)
        {
            mainPopupPanel = ResolvePopupPanel();
        }

        if (canvasGroup == null && mainPopupPanel != null)
        {
            canvasGroup = EnsureCanvasGroup(mainPopupPanel.gameObject);
        }
    }

    private RectTransform ResolvePopupPanel()
    {
        var explicitContentRoot = transform.Find("PopupContent") as RectTransform;
        if (explicitContentRoot != null)
        {
            return explicitContentRoot;
        }

        var bestCandidate = FindLargestChildRect();
        if (bestCandidate != null)
        {
            return bestCandidate;
        }

        return transform as RectTransform;
    }

    private RectTransform FindLargestChildRect()
    {
        RectTransform largestChild = null;
        var largestArea = 0.0f;

        foreach (Transform child in transform)
        {
            if (child is not RectTransform childRect)
            {
                continue;
            }

            var childArea = Mathf.Abs(childRect.rect.width * childRect.rect.height);
            if (childArea <= largestArea)
            {
                continue;
            }

            largestArea = childArea;
            largestChild = childRect;
        }

        return largestChild;
    }

    private CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        var targetCanvasGroup = target.GetComponent<CanvasGroup>();
        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = target.AddComponent<CanvasGroup>();
        }

        return targetCanvasGroup;
    }

    private void SetVisualState(float popupScale, float popupAlpha, float overlayAlpha)
    {
        if (mainPopupPanel != null)
        {
            mainPopupPanel.localScale = new Vector3(popupScale, popupScale, 1.0f);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = popupAlpha;
        }

        if (backgroundOverlay != null && backgroundOverlay != canvasGroup)
        {
            backgroundOverlay.alpha = overlayAlpha;
        }
    }

    private void SetInteractable(bool value)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }

        if (backgroundOverlay != null && backgroundOverlay != canvasGroup)
        {
            backgroundOverlay.interactable = value;
            backgroundOverlay.blocksRaycasts = value;
        }
    }

    private bool ValidateReferences()
    {
        if (mainPopupPanel == null)
        {
            Debug.LogError("UIPopupController is missing Main Popup Panel.", this);
            return false;
        }

        if (canvasGroup == null)
        {
            Debug.LogError("UIPopupController is missing CanvasGroup for the popup panel.", this);
            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        KillActiveTweens();
    }

    private void KillActiveTweens()
    {
        if (openSequence != null)
        {
            openSequence.Kill();
            openSequence = null;
        }

        if (closeSequence != null)
        {
            closeSequence.Kill();
            closeSequence = null;
        }

        if (mainPopupPanel != null)
        {
            mainPopupPanel.DOKill();
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }

        if (backgroundOverlay != null)
        {
            backgroundOverlay.DOKill();
        }
    }
}
