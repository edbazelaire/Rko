using Enums;
using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;


public class SynchronizedSlider : MObject
{
    #region Members

    public event Action<float> ValueChangedEvent;

    protected TMP_Text m_Title;
    private Slider m_Slider;
    private TMP_InputField m_InputField;

    protected EPlayerPref m_Option;
    protected float m_MinValue;
    protected float m_MaxValue;
    float m_BaseValue;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_Title = Finder.FindComponent<TMP_Text>(gameObject, "Title");
        m_Slider = Finder.FindComponent<Slider>(gameObject);
        m_InputField = Finder.FindComponent<TMP_InputField>(gameObject);
    }

    public void Initialize(string title, EPlayerPref option, float baseValue, float minValue, float maxValue)
    {
        m_Option    = option;
        m_BaseValue = PlayerPrefs.GetFloat(option.ToString(), baseValue);
        m_MinValue  = minValue;
        m_MaxValue  = maxValue;

        base.Initialize();

        m_Title.text = title;
    }

    protected override void SetUpUI()
    {
        base.SetUpUI();

        m_Slider.value = m_BaseValue;
        m_Slider.minValue = m_MinValue;
        m_Slider.maxValue = m_MaxValue;
        m_InputField.text = m_BaseValue.ToString("F2");
    }

    #endregion


    #region GUI Manipulators

    #endregion


    #region Listeners

    protected override void RegisterListeners()
    {
        base.RegisterListeners();

        m_Slider.onValueChanged.AddListener(OnSliderValueChanged);
        m_InputField.onEndEdit.AddListener(OnInputFieldValueChanged);
    }

    protected override void UnRegisterListeners()
    {
        base.UnRegisterListeners();

        m_Slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        m_InputField.onEndEdit.RemoveListener(OnInputFieldValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (float.TryParse(m_InputField.text, out float inputValue) && !Mathf.Approximately(inputValue, value))
        {
            m_InputField.text = value.ToString("F2");
        }

        PlayerPrefs.SetFloat(m_Option.ToString(), value);
        ValueChangedEvent?.Invoke(value);
    }

    private void OnInputFieldValueChanged(string text)
    {
        if (float.TryParse(text, out float value))
        {
            if (!Mathf.Approximately(m_Slider.value, value))
            {
                m_Slider.value = value;
            }
            ValueChangedEvent?.Invoke(value);
        }
        else
        {
            // If the input is not a valid float, reset it to the slider's value
            m_InputField.text = m_Slider.value.ToString("F2");
        }

        PlayerPrefs.SetFloat(m_Option.ToString(), value);
    }

    #endregion
}
