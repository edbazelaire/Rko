using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Tools;

public class FocusManager : MonoBehaviour
{
    private Material        m_MaskMaterial;

    private void Start()
    {
        m_MaskMaterial = Finder.FindComponent<Image>(gameObject).material;

        Activate(false);
    }

    public void Activate(bool activate = true)
    {
        gameObject.SetActive(activate);
    }

    public void Focus(GameObject target)
    {
        Activate(true);
        StartCoroutine(AnimateFocus(target));
    }

    private IEnumerator AnimateFocus(GameObject target, float size = 0.3f)
    {
        RectTransform targetRect = target.GetComponent<RectTransform>();
        Canvas canvas = target.GetComponentInParent<Canvas>();
        Vector2 normalizedPos;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Direct screen position for Overlay mode
            Vector3 screenPos = targetRect.position;
            normalizedPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
        }
        else
        {
            // Convert for Screen Space - Camera / World Space
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, targetRect.position);
            normalizedPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
        }

        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * 2f; // Smooth animation speed
            float cutoutSize = Mathf.Lerp(0f, size, progress);

            // Adjust position to counteract shift caused by the cutout growth
            Vector2 adjustedPos = normalizedPos - new Vector2(cutoutSize * 0.25f, cutoutSize * 0.5f);

            m_MaskMaterial.SetVector("_CutoutPos", adjustedPos);
            m_MaskMaterial.SetFloat("_CutoutSize", cutoutSize);

            yield return null;
        }
    }

    public void RemoveFocus()
    {
        Activate(false);
    }
}
