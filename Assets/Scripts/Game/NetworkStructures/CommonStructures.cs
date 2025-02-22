using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Netcode;
using UnityEngine;

namespace Game.NetworkStructures
{
    public struct Vector2Short : INetworkSerializable
    {
        public short x;
        public short y;

        public Vector2Short(Vector3 position)
        {
            x = (short)(position.x * 1000); 
            y = (short)(position.y * 1000);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref y);
        }

        // Implicit conversion from Vector2Short to Vector3
        public static implicit operator Vector2(Vector2Short v) => new Vector2(v.x / 1000f, v.y / 1000f);
        public static implicit operator Vector3(Vector2Short v) => new Vector3(v.x / 1000f, v.y / 1000f, 0f);
        public static implicit operator string(Vector2Short v)  => ((Vector2)v).ToString();

    }
}
