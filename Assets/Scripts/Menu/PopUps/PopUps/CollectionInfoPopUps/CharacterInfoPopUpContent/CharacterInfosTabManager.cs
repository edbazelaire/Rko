using System;


namespace Menu.PopUps
{
    public enum ECharacterInfosTabs
    {
        Infos,
        Description
    }

    public class CharacterInfosTabManager : TabsManager
    {
        #region Members

        protected override Type m_TabEnumType { get; set; } = typeof(ECharacterInfosTabs);
        /// <summary> default tab opened </summary>
        protected override Enum m_DefaultTab { get; set; } = ECharacterInfosTabs.Infos;

        #endregion
    }
}
