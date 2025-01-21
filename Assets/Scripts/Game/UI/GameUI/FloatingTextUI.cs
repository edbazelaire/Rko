using Enums;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;


public class FloatingTextUI : MObject
{
    #region Members

    public TMP_Text Text;
    public float    Duration = 1f;
    public Vector3  FloatOffset = new Vector3(0, 1, 0);

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();
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

    public void SetText(int damage, EHitType hitType)
    {
        Text.text = damage.ToString();

        switch (hitType)
        {
            case EHitType.Damage:
                Text.color = Color.red;
                break;

            case EHitType.Heal:
            case EHitType.LifeSteal:
                Text.color = Color.green;
                break;

            case EHitType.Shield:
                Text.color = Color.blue;
                break;

            default:
                ErrorHandler.Warning("Unhandled case : " + hitType);
                Text.color = Color.white;
                break;
        }

        StartCoroutine(FloatUp());
    }

    private IEnumerator FloatUp()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + FloatOffset;
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / Duration);
            yield return null;
        }

        Destroy(gameObject);
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
