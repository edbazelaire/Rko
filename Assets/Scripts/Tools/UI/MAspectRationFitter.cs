using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MAspectRatioFitter : MonoBehaviour
{
    [Tooltip("Desired aspect ratio (width / height)")]
    public float AspectRatio = 1.0f;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRectTransform = transform.parent.GetComponent<RectTransform>();
        UpdateAspectRatio();
    }

    void UpdateAspectRatio()
    {
        if (rectTransform == null || parentRectTransform == null)
            return;

        float parentWidth = parentRectTransform.rect.width;
        float parentHeight = parentRectTransform.rect.height;

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // Check if there is more room in width or height
        if (parentWidth / AspectRatio < parentHeight)
        {
            // Adjust height based on width and aspect ratio
            width = Mathf.Min(parentWidth, parentHeight * AspectRatio);
            height = width / AspectRatio;
        }
        else
        {
            // Adjust width based on height and aspect ratio
            height = Mathf.Min(parentHeight, parentWidth / AspectRatio);
            width = height * AspectRatio;
        }

        // Ensure the new size does not exceed the container's bounds
        width = Mathf.Min(width, parentWidth);
        height = Mathf.Min(height, parentHeight);

        // Apply the new size
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    void Update()
    {
        // Optionally update every frame to handle dynamic changes
        UpdateAspectRatio();
    }

    void OnValidate()
    {
        // Update in editor when the aspect ratio is changed
        if (Application.isPlaying)
            UpdateAspectRatio();
    }
}
