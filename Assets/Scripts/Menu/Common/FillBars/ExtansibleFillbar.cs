using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;

public class ExtansibleFillbar : MObject
{
    #region Members

    [SerializeField]
    float m_AnimationDuration = 1.0f;

    // ===================================================================================
    // Data
    protected Coroutine         m_Animation;
    protected int               m_MaxValue      = 1;
    protected int               m_CurrentValue  = 0;

    // ===================================================================================
    // GameObject & Components
    protected GameObject        m_Container;
    protected Image             m_Bar;
    protected RectTransform     m_ContainerRect;
    protected RectTransform     m_BarRect;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_Container = Finder.Find(gameObject, "Container");
        m_Bar = Finder.FindComponent<Image>(gameObject, "Bar");
        m_ContainerRect = Finder.FindComponent<RectTransform>(m_Container);
        m_BarRect = Finder.FindComponent<RectTransform>(m_Bar.gameObject);
    }

    public virtual void Initialize(int currentValue, int maxValue, Color? color = null, bool withAnimation = false)
    {
        base.Initialize();

        if (color.HasValue)
            SetColor(color.Value);

        // delay the refresh by one to avoid conflicts with bar
        UpdateValue(currentValue, maxValue > 0 ? maxValue : currentValue, withAnimation);
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
