using System;
using Tools;
using UnityEngine;

namespace Menu
{
    public enum EInvetoryItemTab
    {
        SpellsTab,
        CharactersTab,
        RunesTab,
    }

    public class ItemsTabManager: TabsManager
    {
        #region Members

        protected override Type m_TabEnumType { get; set; } = typeof(EInvetoryItemTab);
        protected override Enum m_DefaultTab { get; set; } = EInvetoryItemTab.SpellsTab;

        #endregion

    }
}
