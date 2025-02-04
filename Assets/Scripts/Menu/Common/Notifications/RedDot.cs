using Menu.Common.Notifications;
using TMPro;
using Tools;
using UnityEngine;


public class RedDot : MObject
{
    #region Members

    float m_Size;
    int m_Counter;
    ENotificationPosition m_NotificationPosition;

    TMP_Text m_CounterText;

    #endregion


    #region Static 

    public static RedDot AddRedDot(GameObject target, int counter = 0, float size = 1, ENotificationPosition notificationPosition = ENotificationPosition.BottomRight)
    {
        var redDot = Instantiate(AssetLoader.LoadComponentPrefab<RedDot>("RedDot"), target.transform);
        redDot.Initialize(counter, size, notificationPosition);

        return redDot;
    }

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_CounterText = Finder.FindComponent<TMP_Text>(gameObject);
    }

    public void Initialize(int counter = 0, float size = 1, ENotificationPosition notificationPosition = ENotificationPosition.BottomRight)
    {
        m_Counter = counter;
        m_Size = size;
        m_NotificationPosition = notificationPosition;

        base.Initialize();
    }

    protected override void SetUpUI()
    {
        base.SetUpUI();

        UpdateSize(m_Size);
        UpdateCounter(m_Counter);
        UpdatePosition(m_NotificationPosition);
    }

    #endregion


    #region GUI Manipulators

    public void UpdateSize(float size)
    {
        m_Size = size;

        transform.localScale = new Vector3(size, size, 1f);
    }

    public void UpdateCounter(int counter)
    {
        m_Counter = counter;

        if (counter <= 0)
        {
            m_CounterText.gameObject.SetActive(false);
            return;
        }

        m_CounterText.gameObject.SetActive(true);
        m_CounterText.text = m_Counter.ToString();
    }

    public void UpdatePosition(ENotificationPosition notificationPosition)
    {
        RectTransform targetRect = transform.parent.GetComponent<RectTransform>();
        RectTransform redDotRect = GetComponent<RectTransform>();

        if (targetRect == null)
        {
            ErrorHandler.Error("unable to find RectTransform in RedDot parent");
            return;
        }

        if (redDotRect == null)
        {
            ErrorHandler.Error("unable to find RectTransform in RedDot object");
            return;
        }

        Vector2 offset = new Vector2(10f, 20f);               // Adjust based on visual preference
        redDotRect.anchorMin = redDotRect.anchorMax = new Vector2(0.5f, 0.5f);

        switch (notificationPosition)
        {
            case ENotificationPosition.TopLeft:
                redDotRect.anchoredPosition = new Vector2(-targetRect.rect.width / 2 + offset.x, targetRect.rect.height / 2 - offset.y);
                break;
            case ENotificationPosition.TopRight:
                redDotRect.anchoredPosition = new Vector2(targetRect.rect.width / 2 - offset.x, targetRect.rect.height / 2 - offset.y);
                break;
            case ENotificationPosition.BottomLeft:
                redDotRect.anchoredPosition = new Vector2(targetRect.rect.width / 2 + offset.x, -targetRect.rect.height / 2 + offset.y);
                break;
            case ENotificationPosition.BottomRight:
                redDotRect.anchoredPosition = new Vector2(targetRect.rect.width / 2 - offset.x, -targetRect.rect.height / 2 + offset.y);
                break;
            case ENotificationPosition.None:
                redDotRect.anchoredPosition = Vector2.zero; // Centered (optional behavior)
                break;
        }
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
