using System.Collections;
using UnityEngine;


namespace Tools
{
    public class ClickInteractor : MonoBehaviour
    {
        #region Members

        public bool Skip;

        #endregion


        #region Activation / Deactivation

        public static ClickInteractor Create(bool activate = true)
        {
            var ci = new GameObject("ClickInteractor").AddComponent<ClickInteractor>();
            ci.Refresh();
            ci.Activate(activate);

            return ci;
        }

        public void Activate(bool activate)
        {
            gameObject.SetActive(activate);
        }

        public void Refresh()
        {
            Skip = false;
        }

        #endregion


        #region Update

        protected void Update()
        {
            if (Input.touchCount > 0 || Input.GetMouseButtonDown(0))
            {
                Skip = true;
            }
        }

        #endregion
    }
}
