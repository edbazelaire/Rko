using UnityEditor;
using UnityEngine;

namespace Game.GameManagers.Interfaces
{
    public interface IGameModule
    {
        /// <summary> is the module currently valid at this stage of the game </summary>
        public bool CheckIsValid();
    }
}