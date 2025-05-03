using Enums;
using System;
using UnityEditor;
using UnityEngine;

namespace Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public struct SSpellRelocation
    {
        public SGFXLifetime<ESpellEvent> Lifetime;
        public float Speed;

        public bool Exists => Lifetime != null && Lifetime.StartSpellPart != ESpellEvent.None && Lifetime.EndSpellPart != ESpellEvent.None;
    }
}