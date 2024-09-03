using System;
using System.Collections;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Externals
{
    public class MNetworkBehavior : NetworkBehaviour
    {
        protected virtual void Start()
        {
            // register commands of the current class
            Debugger.Instance.RegisterClass(this);
        }

        public override void OnDestroy()
        {
            Debugger.Instance.UnregisterClass(this);
            base.OnDestroy();
        }

        protected virtual void OnEnable()
        {

        }

        protected virtual void OnDisable()
        {

        }
    }
}