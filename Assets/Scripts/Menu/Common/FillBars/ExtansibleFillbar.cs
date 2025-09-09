using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A UI component that displays a horizontal fill bar.
/// Supports both:
/// - Single mode (classic bar with one value)
/// - Split mode (Direct + Tick stacked inside the same container)
/// </summary>
public class ExtansibleFillbar : MObject
{
    #region Members

    [SerializeField]
    float m_AnimationDuration = 1.0f;

    // ===================================================================================
    // Data
    protected Coroutine m_Animation;
    protected int m_MaxValue = 1;
    protected int m_CurrentValue = 0;

    // ===================================================================================
    // GameObject & Components
    protected GameObject m_Container;
    protected Image m_Bar;            // Main bar (used for Direct or single mode)
    protected RectTransform m_ContainerRect;
    protected RectTransform m_BarRect;

    // --- New for Split Mode ---
    protected Image m_BarTick;        // Secondary bar for Tick values
    protected RectTransform m_BarTickRect;

    #endregion

    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_Container = Finder.Find(gameObject, "Container");
        m_ContainerRect = Finder.FindComponent<RectTransform>(m_Container);
        m_Bar = Finder.FindComponent<Image>(gameObject, "Bar");
        m_BarRect = Finder.FindComponent<RectTransform>(m_Bar.gameObject);

        m_BarTick = Finder.FindComponent<Image>(gameObject, "BarTick");
        m_BarTickRect = Finder.FindComponent<RectTransform>(m_BarTick.gameObject);
    }

    public virtual void Initialize(int currentValue, int maxValue, Color? color = null, bool withAnimation = false)
    {
        base.Initialize();

        if (color.HasValue)
            SetColor(color.Value);

        m_BarTick.gameObject.SetActive(false);

        // delay the refresh by one to avoid conflicts with bar
        UpdateValue(currentValue, maxValue > 0 ? maxValue : currentValue, withAnimation);
    }

    /// <summary>
    /// Initialize in split mode (Direct + Tick values).
    /// </summary>
    public virtual void InitializeSplit(int firstValue, int secondValue, int maxValue, Color colorDirect, Color colorTick, bool withAnimation = false)
    {
        base.Initialize();

        m_MaxValue = Mathf.Max(1, maxValue);

        // Ensure both bars exist
        if (m_Bar == null || m_BarTick == null)
        {
            ErrorHandler.Warning("ExtansibleFillbar.InitializeSplit called but 'BarTick' is missing in prefab.");
            return;
        }

        // Set colors
        m_Bar.color = colorDirect;
        m_BarTick.color = colorTick;

        if (withAnimation)
        {
            if (m_Animation != null)
                StopCoroutine(m_Animation);
            m_Animation = StartCoroutine(UpdateSplitAnimationCoroutine(firstValue, secondValue));
        }
        else
        {
            UpdateSplitBarSize(firstValue, secondValue);
        }
    }

    protected override void SetUpUI()
    {
        base.SetUpUI();
    }

    #endregion

    #region GUI Manipulators

    public virtual void SetColor(Color color)
    {
        m_Bar.color = color;
    }

    public virtual void UpdateValue(int value, int? maxValue = null, bool withAnimation = false)
    {
        m_CurrentValue = value;
        if (maxValue.HasValue)
        {
            m_MaxValue = maxValue.Value;
        }

        if (withAnimation)
        {
            if (m_Animation != null)
            {
                StopCoroutine(m_Animation);
            }
            m_Animation = StartCoroutine(UpdateValueAnimationCoroutine(value));
        }
        else
        {
            UpdateBarSize();
        }
    }

    void UpdateBarSize()
    {
        float fillPercentage = Mathf.Clamp01((float)m_CurrentValue / m_MaxValue);
        m_BarRect.sizeDelta = new Vector2(m_ContainerRect.rect.width * fillPercentage, m_BarRect.sizeDelta.y);
    }

    void UpdateSplitBarSize(int directValue, int tickValue)
    {
        float directFill = Mathf.Clamp01((float)directValue / m_MaxValue);
        float tickFill = Mathf.Clamp01((float)(directValue + tickValue) / m_MaxValue);

        // Direct is the base bar
        if (directValue <= 0)
        {
            m_Bar.gameObject.SetActive(false);
        }
        else
        {
            m_Bar.gameObject.SetActive(true);
            m_BarRect.sizeDelta = new Vector2(m_ContainerRect.rect.width * directFill, m_BarRect.sizeDelta.y);
        }

        // Tick overlays on top (wider than direct)
        if (tickValue <= 0)
        {
            m_BarTick.gameObject.SetActive(false);
        }
        else
        {
            m_BarTick.gameObject.SetActive(true);
            m_BarTickRect.sizeDelta = new Vector2(m_ContainerRect.rect.width * tickFill, m_BarTickRect.sizeDelta.y);
        }
    }

    #endregion

    #region Animation

    IEnumerator UpdateValueAnimationCoroutine(int targetValue)
    {
        float startValue = m_CurrentValue;
        float elapsedTime = 0f;

        while (elapsedTime < m_AnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / m_AnimationDuration);
            int interpolatedValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, t));

            m_CurrentValue = interpolatedValue;
            UpdateBarSize();
            yield return null;
        }

        m_CurrentValue = targetValue;
        UpdateBarSize();
        m_Animation = null;
    }

    IEnumerator UpdateSplitAnimationCoroutine(int targetDirect, int targetTick)
    {
        int startDirect = 0;
        int startTick = 0;
        float elapsedTime = 0f;

        while (elapsedTime < m_AnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / m_AnimationDuration);

            int currentDirect = Mathf.RoundToInt(Mathf.Lerp(startDirect, targetDirect, t));
            int currentTick = Mathf.RoundToInt(Mathf.Lerp(startTick, targetTick, t));

            UpdateSplitBarSize(currentDirect, currentTick);
            yield return null;
        }

        UpdateSplitBarSize(targetDirect, targetTick);
        m_Animation = null;
    }

    #endregion

    #region Listeners

    protected override void RegisterListeners()
    {
        base.RegisterListeners();
    }

    protected override void UnRegisterListeners()
    {
        base.UnRegisterListeners();
    }

    #endregion
}
