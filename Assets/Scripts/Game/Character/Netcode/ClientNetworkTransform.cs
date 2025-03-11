using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Game.Character.Netcode
{
    public enum AuthorityMode
    {
        Server,
        Client
    }

    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        public AuthorityMode AuthorityMode = AuthorityMode.Client;

        protected override bool OnIsServerAuthoritative() => AuthorityMode == AuthorityMode.Server;
    }
}