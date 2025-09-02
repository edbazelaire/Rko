using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;


namespace Game.NetworkStructures
{
    public static class ReplicatedVar
    {
        public static IReplicatedVar<T> Create<T>(NetworkVariable<T> netVar, T offlineInitial = default)
        {
            if (GameManager.Instance.IsOfflineMode)
                return new LocalVar<T>(offlineInitial);

            return new NetworkVarAdapter<T>(netVar);
        }
    }

    public interface IReplicatedVar<T>
    {
        T Value { get; set; }
        event Action<T, T> OnValueChanged;
    }

    public sealed class LocalVar<T> : IReplicatedVar<T>
    {
        private T m_Value;

        public event Action<T, T> OnValueChanged;

        public LocalVar(T initial = default) => m_Value = initial;

        public T Value
        {
            get => m_Value;
            set
            {
                if (Equals(m_Value, value)) return;
                var old = m_Value;
                m_Value = value;
                OnValueChanged?.Invoke(old, m_Value);
            }
        }
    }

    public sealed class NetworkVarAdapter<T> : IReplicatedVar<T>
    {
        private readonly NetworkVariable<T> m_NetVar;

        public event Action<T, T> OnValueChanged;

        public NetworkVarAdapter(NetworkVariable<T> netVar)
        {
            m_NetVar = netVar;
            m_NetVar.OnValueChanged += HandleNetChanged;
        }

        private void HandleNetChanged(T prev, T next) => OnValueChanged?.Invoke(prev, next);

        public T Value
        {
            get => m_NetVar.Value;
            set
            {
                // Let NGO enforce permissions (Server/Owner).
                // If you want hard checks here, you can assert IsServer/IsOwner before writing.
                if (Equals(m_NetVar.Value, value)) return;
                m_NetVar.Value = value;
            }
        }
    }
}
