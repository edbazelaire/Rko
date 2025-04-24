using Enums;
using System.Collections;
using UnityEngine;


namespace Game.StateEffects.Interfaces
{
    public interface IHealInterceptor
    {
        void OnPreHeal(ref int heal, ulong casterId);
    }
}
