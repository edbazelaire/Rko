using Game;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBarUI : MonoBehaviour
{
    const string c_Fill = "Fill";
    const string c_Text = "Text";

    [SerializeField]
    protected Color m_FullColor;

    protected Image         m_Fill;
    protected TMP_Text      m_Text;

    // effects
    protected GameObject    m_FullEffect;

    // data
    protected Color         m_BaseColor;
    protected int           m_MaxValue;
    protected int           m_CurrentValue;

    protected bool m_IsFull => m_CurrentValue >= m_MaxValue;

    protected virtual void FindComponents()
    {
        m_Fill = Finder.FindComponent<Image>(gameObject, c_Fill);
        m_Text = Finder.FindComponent<TMP_Text>(gameObject, c_Text, throwError: false);

        // Effects
        m_FullEffect = Finder.Find(gameObject, "FullEffect", false);

        // Colors
        m_BaseColor = m_Fill.color;
    }

    public virtual void Initialize(int currentValue, int maxValue)
    {
        FindComponents();

        SetMaxValue(maxValue);
        SetValue(currentValue);
        UpdateChanges();
    }

    public void OnValueChanged(int _, int newValue)
    {
        SetValue(newValue);
        UpdateChanges();
    }

    public void OnMaxValueChanged(int _, int newValue)
    {
        SetMaxValue(newValue);
        UpdateChanges();
    }

    void SetMaxValue(int value)
    {
        m_MaxValue = value;
    }

    void SetValue(int value)
    {
        m_CurrentValue = value;
    }

    void UpdateChanges()
    {
        if (GameManager.IsGameOver)
            return;

        if (m_Fill.IsDestroyed())
            return;

        // update fill amount of the energy bar
        m_Fill.fillAmount = (float)m_CurrentValue / m_MaxValue;

        // activate "energy effect" when energy is full
        if (m_FullEffect != null)
            m_FullEffect.SetActive(m_IsFull);

        if (m_FullColor != default)
            m_Fill.color = m_IsFull ? m_FullColor : m_BaseColor;

        if (m_Text != null && m_Text.isActiveAndEnabled)
            m_Text.text = GetText();
    }

    protected virtual string GetText()
    {
        return $"{m_CurrentValue} / {m_MaxValue}";
    }

}