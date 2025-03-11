using System;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character.Netcode
{
    public struct SInputPayload : INetworkSerializable
    {
        public int      Tick;
        public DateTime Timestamp;
        public int      Direction;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Timestamp);
            serializer.SerializeValue(ref Direction);
        }
    }

    public struct SStatePayload : INetworkSerializable
    { 
        public int      Tick;
        public Vector2  Position;
        public float    Velocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
        }
    }
}