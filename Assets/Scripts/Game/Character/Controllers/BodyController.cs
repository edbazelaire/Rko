using Game.Spells;
using Tools;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Character.Controllers
{
    public class BodyController : Controller
    {
        #region Members

        Spell m_Spell;

        public override int Team => m_Spell.Caster.Team;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            // setup components
            m_Life = gameObject.AddComponent<Life>();
        }

        public void Initialize(Spell spell, int hp)
        {
            FindComponents();

            m_Spell = spell;
            m_Life.Initialize(hp, 0);

            // add ui for life component
            AddUI();

            RegisterListeners();
        }

        void End()
        {
            if (gameObject.IsDestroyed())
                return;

            UnRegisterListeners();
            Destroy(gameObject);
        }

        #endregion


        #region UI

        void AddUI()
        {
            var spawnUIPrefab = AssetLoader.Load<SpawnUI>("SpawnUI", AssetLoader.c_SpawnUIContentPath);
            var spawnUI = GameObject.Instantiate(spawnUIPrefab, transform);
            spawnUI.Initialize(1f);

            // setup health bar
            PlayerBarUI healthBar = Finder.FindComponent<PlayerBarUI>(spawnUI.gameObject, "SpawnHealthBar");
            healthBar.Initialize(m_Life.Hp.Value, m_Life.MaxHp.Value);
            m_Life.Hp.OnValueChanged += healthBar.OnValueChanged;
            m_Life.MaxHp.OnValueChanged += healthBar.OnMaxValueChanged;

            // deactivate energy/shield bars
            Finder.FindComponent<PlayerBarUI>(spawnUI.gameObject, "SpawnShieldBar").gameObject.SetActive(false);
            Finder.FindComponent<PlayerBarUI>(spawnUI.gameObject, "SpawnEnergyBar").gameObject.SetActive(false);
        }

        #endregion


        #region Listeners

        void RegisterListeners()
        {
            m_Spell.OnSpellEndedEvent += OnSpellEnded;
            m_Life.OnDeathEvent += OnDiedEvent;
        }

        void UnRegisterListeners()
        {
            m_Spell.OnSpellEndedEvent -= OnSpellEnded;
            m_Life.OnDeathEvent -= OnDiedEvent;
        }

        void OnDiedEvent()
        {
            m_Spell.TryEnd();
            End();
        }

        void OnSpellEnded()
        {
            End();
        }

        #endregion
    }
}