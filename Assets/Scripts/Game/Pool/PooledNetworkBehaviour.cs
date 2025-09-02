using Game;
using Game.Pool;
using Unity.Netcode;

namespace Assets.Scripts.Game.Pool
{
    public abstract class PooledNetworkBehaviour : NetworkBehaviour, IPoolLifecycle
    {
        public new bool IsServer => (GameManager.Exists && GameManager.Instance.IsOfflineMode) || base.IsServer;

        public virtual void OnSpawned() { }
        public virtual void OnDespawned() { }

        // ONLINE path: Netcode will call these.
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            OnSpawned(); // defer gameplay init here too
        }

        public override void OnNetworkDespawn()
        {
            OnDespawned(); // defer gameplay cleanup here too
            base.OnNetworkDespawn();
        }
    }
}