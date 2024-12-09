using System;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Data.Interfaces
{
    public interface ILevel<T> where T : ScriptableObject
    {
        public string Name
        {
            get
            {
                string myName = (this as T).name;
                if (myName.EndsWith("(Clone)"))
                    myName = myName[..^"(Clone)".Length];

                return myName;
            }
        }

        public T Clone(int level = 0, bool destroy = false)
        {
            // Cast `this` to `T` to access instance members.
            T clone = GameObject.Instantiate(this as T);

            if (clone == null)
                throw new InvalidOperationException("Failed to instantiate the object.");

            if (destroy)
                CoroutineManager.DelayMethod(() => GameObject.Destroy(clone));

            if (clone is ILevel<T> levelable)
                levelable.SetLevel(level);

            return clone;
        }

        public void SetLevel(int level);
    }
}