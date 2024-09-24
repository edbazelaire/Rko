using Save;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Managers.Tuto
{
    public static class TutoManager
    {
        public static bool IsInitialized;
        public static bool IsActivated;

        public static bool TutoDone => ProfileCloudData.TutoDone;
    }
}