using Game.Spells;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;


public class Sensor : MObject
{
    #region Members

    [SerializeField] protected Color    m_ActivationColor;
    protected Color                     m_BaseColor;

    protected Controller        m_Controller;
    protected SpriteRenderer    m_SpriteRenderer;
    protected Collider2D        m_Collider;

    protected List<Spell> m_Spells = new List<Spell>();

    [HideInInspector] public bool IsTriggered = false;

    #endregion


    #region Init & End

    protected override void Start()
    {
        base.Start();

        Initialize();
    }

    protected override void FindComponents()
    {
        base.FindComponents();

        m_Controller        = Finder.FindComponent<Controller>(transform.parent.parent.gameObject);
        m_SpriteRenderer    = Finder.FindComponent<SpriteRenderer>(gameObject);
        m_Collider          = Finder.FindComponent<Collider2D>(gameObject);

        m_BaseColor = m_SpriteRenderer.color;
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void SetUpUI()
    {
        base.SetUpUI();
    }

    #endregion


    #region GUI Manipulators

    protected void UpdateColor()
    {
        if (!m_SpriteRenderer.gameObject.activeSelf)
            return;

        var color = IsTriggered ? m_ActivationColor : m_BaseColor;
        color.a = 0.4f;
        m_SpriteRenderer.color = color;
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
