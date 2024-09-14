using Enums;
using Game.Spells;
using TMPro;
using Tools;
using UnityEngine.UI;

public class Caption : MObject
{
    #region Members

    Image m_CaptionImage;
    TMP_Text m_Text;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_CaptionImage = Finder.FindComponent<Image>(gameObject, "CaptionImage");
        m_Text = Finder.FindComponent<TMP_Text>(gameObject, "Text");
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void SetUpUI()
    {
        base.SetUpUI();

        m_CaptionImage.gameObject.SetActive(false);
    }

    #endregion


    #region Caption Manipulation

    public void Activate(bool activate)
    {
        gameObject.SetActive(activate);
    }

    public void Write(string text, ECaptionType captionType = ECaptionType.Normal)
    {
        m_Text.gameObject.SetActive(true);

        m_Text.text = text;
        SetCaption(captionType);

        gameObject.SetActive(true);
    }

    public void SetCaption(ECaptionType captionType)
    {
        m_CaptionImage.gameObject.SetActive(true);
        m_CaptionImage.sprite = AssetLoader.LoadCaption(captionType);
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