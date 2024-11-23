using Data;
using Enums;
using Tools;

namespace Game.Spells.SpecialEffects
{
    public class _OnHitSoaring : SpecialEffect
    {
        #region Members

        Controller m_Controller;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Controller = Finder.FindComponent<Spell>(gameObject).Controller;
        }

        public override void Initialize(int level)
        {
            base.Initialize(level);

            CheckPowerOrbs();
        }

        #endregion


        #region Check Power orbs

        void CheckPowerOrbs()
        {
            int nStacks = 0;

            var allSpanws = GameManager.Instance.Spawns.Values;
            foreach (var spawnController in allSpanws)
            {
                if (spawnController.Character != ESpawn.AzurePowerOrb.ToString())
                    continue;

                spawnController.Life.Kill(true);
                nStacks++;  
            }

            if (nStacks == 0)
                return;

            m_Controller.StateHandler.AddStateEffect(new SStateEffectData(EStateEffect.AzurePowerOrb, nStacks), m_Controller, m_Level);
        }

        #endregion
    }
}