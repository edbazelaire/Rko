using System;


namespace Menu.PopUps
{
    public enum ESpellInfosTabs
    {
        Infos,
        Description,
        Clip
    }

    public class SpellInfosTabManager : TabsManager
    {
        #region Members

        protected override Type m_TabEnumType { get; set; } = typeof(ESpellInfosTabs);
        /// <summary> default tab opened </summary>
        protected override Enum m_DefaultTab { get; set; } = ESpellInfosTabs.Infos;

        #endregion
    }
}
