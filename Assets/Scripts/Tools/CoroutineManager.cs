using Assets;
using System;
using System.Collections;

namespace Tools
{
    public static class CoroutineManager
    {
        public static void Display(Action method)
        {
            DelayMethod(method, 0);
        }

        public static void DelayMethod(Action method, int nFrames = 1)
        {
            if (Main.Instance != null)
                Main.Instance.StartCoroutine(DelayMethodByFrames(method, nFrames));

            else if (Debugger.Instance != null)
                Debugger.Instance.StartCoroutine(DelayMethodByFrames(method, nFrames));

            else
                return;
        }

        static IEnumerator DelayMethodByFrames(Action method, int nFrames = 1)
        {
            while (--nFrames >= 0)
            {
                yield return null;
            }

            method?.Invoke();
        }
    }
}