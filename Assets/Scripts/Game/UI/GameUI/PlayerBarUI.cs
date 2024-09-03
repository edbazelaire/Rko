using Game;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBarUI : MonoBehaviour
{
    const string c_Fill = "Fill";
    const string c_Text = "Text";

    protected Image         m_Fill;
    protected TMP_Text      m_Text;
    protected int           m_MaxValue;
    protected int           m_CurrentValue;

    protected virtual void FindComponents()
    {
        m_Fill = Finder.FindComponent<Image>(gameObject, c_Fill);
        m_Text = Finder.FindComponent<TMP_Text>(gameObject, c_Text, throwError: false);
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

        m_Fill.fillAmount = (float)m_CurrentValue / m_MaxValue;
        if (m_Text != null)
            m_Text.text = GetText();
    }

    protected virtual string GetText()
    {
        return $"{m_CurrentValue} / {m_MaxValue}";
    }

}