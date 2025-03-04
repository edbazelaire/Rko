using System;
using UnityEditor;
using UnityEngine;

namespace Game.Character.Netcode
{
    public class CircularBuffer<T>
    {
        T[] m_Buffer;
        int m_BufferSize;

        public CircularBuffer(int  bufferSize)
        {
            m_BufferSize = bufferSize;
            m_Buffer = new T[bufferSize];
        }

        public void Add(T item, int index) => m_Buffer[index % m_BufferSize] = item;
        public T Get(int index) => m_Buffer[index % m_BufferSize];
        public void Clear() => m_Buffer = new T[m_BufferSize];
    }
}