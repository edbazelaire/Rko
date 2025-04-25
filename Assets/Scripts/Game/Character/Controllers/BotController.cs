using Game.Character.BotComponents;
using Managers;
using Tools;

namespace Game.Character.Controllers
{
    public class BotController : Controller
    {
        #region Members

        BotEmotHandler m_BotEmotHandler;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_BotEmotHandler = Finder.FindComponent<BotEmotHandler>(gameObject);
        }

        protected override void InitializeCharacterData(SPlayerData playerData)
        {
            base.InitializeCharacterData(playerData);

            if (!IsServer)
                return;

            m_BotEmotHandler.Initialize();
        }

        #endregion


        #region Game Over

        public override void ActivateActionComponent(bool activate)
        {
            base.ActivateActionComponent(activate);

            m_BotEmotHandler.Activate(activate);
        }

        #endregion
    }
}