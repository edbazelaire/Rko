using System;

namespace Managers.Friends
{
    public interface IRelationshipBarView
    {
        Action onShowAddFriend { get; set; }
        void Refresh();
    }


}

