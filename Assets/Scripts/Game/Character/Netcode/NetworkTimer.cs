using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Game.Character.Netcode
{
    public class NetworkTimer
    {
        float m_Timer;
        int m_CurrentTick;
        public float MinTimeBetweenTicks { get; }
        public int CurrentTick => m_CurrentTick;

        public NetworkTimer(float serverTickRate)
        {
            MinTimeBetweenTicks = 1f / serverTickRate;
        }

        public void Update(float deltaTime)
        {
            m_Timer += deltaTime;
        }

        public bool ShouldTick()
        {
            if (m_Timer >= MinTimeBetweenTicks)
            {
                m_Timer -= MinTimeBetweenTicks;
                m_CurrentTick++;
                return true;
            }
            return false;
        }
    }
}