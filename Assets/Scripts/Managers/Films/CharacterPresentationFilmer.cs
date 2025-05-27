using System.Collections;
using System.Collections.Generic;
using TMPro;
using Tools;
using Tools.Animations;
using UnityEngine;


public class CharacterPresentationFilmer : MObject
{
    #region Members

    const float CHARACTER_DISPLAY_DURATION          = 1.5f;
    const float SPECIAL_EFFECT_ANIMATION_DURATION   = 0.7f;
    const float SPELLS_ANIMATION_DURATION           = 0.7f;

    TMP_Text m_Title;
    GameObject m_TemplateCharacterItem;
    GameObject m_SpellsContainer;
    GameObject m_SpecialEffectContainer;
    List<GameObject> m_SpellContainers;

    #endregion


    #region Init & End

    private void Awake()
    {
        Initialize();
    }

    protected override void FindComponents()
    {
        base.FindComponents();

        m_Title                     = Finder.FindComponent<TMP_Text>(gameObject, "Title");
        m_TemplateCharacterItem     = Finder.Find(gameObject, "TemplateCharacterItem");
        m_SpellsContainer           = Finder.Find(gameObject, "SpellsContainer");
        m_SpecialEffectContainer    = Finder.Find(gameObject, "SpecialEffectContainer");
        m_SpellContainers = Finder.Finds(gameObject, "SpellContainer");
    }

    public override void Initialize()
    {
        base.Initialize();

        StartCoroutine(StartAnimation());
    }

    protected override void SetUpUI()
    {
        base.SetUpUI();

        m_TemplateCharacterItem.SetActive(false);
        m_SpecialEffectContainer.SetActive(false);

        foreach (var spellContainer in m_SpellContainers)
        {
            spellContainer.SetActive(false);
        }
    }

    #endregion


    #region GUI Manipulators

    #endregion


    #region Animation

    IEnumerator StartAnimation()
    {
        yield return DisplayCharacter();
        yield return DisplaySpecialEffect();
        yield return DisplaySpells();
    }

    IEnumerator DisplayCharacter()
    {
        m_TemplateCharacterItem.SetActive(true);

        var rotation = m_TemplateCharacterItem.AddComponent<RotateAnimation>();
        rotation.Initialize(duration: CHARACTER_DISPLAY_DURATION, rotation: new Vector3(0, 720, 0));

        var zoomIn = m_TemplateCharacterItem.AddComponent<Fade>();
        zoomIn.Initialize(duration: CHARACTER_DISPLAY_DURATION, startScale: 0.5f);

        yield return new WaitForSeconds(CHARACTER_DISPLAY_DURATION + 0.5f);
    }

    IEnumerator DisplaySpecialEffect()
    {
        m_SpecialEffectContainer.SetActive(true);

        var fadeIn = m_SpecialEffectContainer.AddComponent<Fade>();
        fadeIn.Initialize(duration: SPECIAL_EFFECT_ANIMATION_DURATION, startOpacity: 0f, startScale: 0.7f);

        yield return new WaitForSeconds(SPECIAL_EFFECT_ANIMATION_DURATION + 0.5f);
    }

    IEnumerator DisplaySpells()
    {
        foreach (var spellContainer in m_SpellContainers)
        {
            spellContainer.SetActive(true);

            var fadeIn = spellContainer.AddComponent<Fade>();
            fadeIn.Initialize(duration: SPELLS_ANIMATION_DURATION, startOpacity: 0f, startScale: 0.7f);

            yield return new WaitForSeconds(SPELLS_ANIMATION_DURATION + 0.5f);
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
