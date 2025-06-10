using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/StateEffects/DamageEffect")]
    public class DamageEffect : StateEffect
    {
        [Header("Damage")]
        [SerializeField] protected int      m_Damage        = 0;
        [SerializeField] protected int      m_EndDamage     = 0;
        [SerializeField] protected int      m_Heal          = 0;
        [SerializeField] protected int      m_EndHeal       = 0;

        /// <summary>
        /// Apply damages / Heal on end
        /// </summary>
        protected override void OnStart()
        {
            base.OnStart();
        }

        /// <summary>
        /// Apply damages / Heal on end
        /// </summary>
        public override void End()
        {
            base.End();
        }

    }
}