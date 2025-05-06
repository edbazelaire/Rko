using Tools;
using UnityEngine;

public class MObject : MonoBehaviour
{
    #region Members

    protected bool m_Initialized = false;

    #endregion


    #region Init & End

    protected virtual void Start()
    {
        // register commands of the current class
        if (Debugger.Instance != null && ! Debugger.Instance.IsRegistered(this))
            Debugger.Instance.RegisterClass(this);
    }

    protected virtual void OnDestroy()
    {
        if (Debugger.Instance != null && !Debugger.Instance.IsRegistered(this))
            Debugger.Instance.UnregisterClass(this);

        if (m_Initialized)
            UnRegisterListeners();
    }

    public virtual void Initialize()
    {
        FindComponents();
        SetUpUI();
        RegisterListeners();

        m_Initialized = true;
    }

    /// <summary>
    /// Place in this method all the requested class components and game objects
    /// </summary>
    protected virtual void FindComponents() { }
    protected virtual void SetUpUI() { }
    protected virtual void RegisterListeners() 
    {
        if (m_Initialized)
            UnRegisterListeners();
    }

    protected virtual void UnRegisterListeners() { }

    #endregion


    protected virtual void OnEnable()
    {
        
    }

    protected virtual void OnDisable()
    {
       
    }
}
