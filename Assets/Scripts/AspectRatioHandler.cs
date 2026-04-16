using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class AspectRatioHandler : MonoBehaviour
{
    [SerializeField] private float targetWidth = 9f;
    [SerializeField] private float targetHeight = 16f;
    [SerializeField] private Color barColor = Color.black;

    private Camera cachedCamera;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        ApplyAspectRatio();
    }

    private void OnEnable()
    {
        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
        }

        ApplyAspectRatio();
    }

    private void OnValidate()
    {
        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
        }

        ApplyAspectRatio();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            ApplyAspectRatio();
        }
    }

    private void ApplyAspectRatio()
    {
        if (cachedCamera == null || targetWidth <= 0f || targetHeight <= 0f || Screen.height <= 0)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        cachedCamera.clearFlags = CameraClearFlags.SolidColor;
        cachedCamera.backgroundColor = barColor;

        var targetAspect = targetWidth / targetHeight;
        var currentAspect = (float)Screen.width / Screen.height;
        var viewportRect = new Rect(0f, 0f, 1f, 1f);

        if (currentAspect > targetAspect)
        {
            var normalizedWidth = targetAspect / currentAspect;
            viewportRect.width = normalizedWidth;
            viewportRect.x = (1f - normalizedWidth) * 0.5f;
        }
        else if (currentAspect < targetAspect)
        {
            var normalizedHeight = currentAspect / targetAspect;
            viewportRect.height = normalizedHeight;
            viewportRect.y = (1f - normalizedHeight) * 0.5f;
        }

        cachedCamera.rect = viewportRect;
    }
}
