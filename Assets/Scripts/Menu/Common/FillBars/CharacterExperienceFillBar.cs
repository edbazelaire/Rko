using Save;

namespace Menu.Common
{
    public class CharacterExperienceFillBar : CollectionFillBar
    {
        #region Members

        protected override bool m_IsMaxed => base.m_IsMaxed || m_CollectableCloudData.Value.Level >= ProfileCloudData.AccountLevel;

        #endregion
    }
}