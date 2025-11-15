using UnityEditor;
using UnityEngine;

namespace Game.Pool
{
    public interface IPoolLifecycle
    {
        /// Called whenever the object becomes "active/ready" for use.
        void OnSpawned();

        /// Called right before the object is returned to the pool (cleanup/reset).
        void OnDespawned();
    }
}