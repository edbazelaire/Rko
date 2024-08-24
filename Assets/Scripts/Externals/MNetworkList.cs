using Unity.Netcode;
using System;
using UnityEngine;
using Game;
using Tools;

public class MNetworkList<T> : NetworkList<T> where T : unmanaged, IEquatable<T>
{
    public MNetworkList() { }

    public override void Dispose()
    {
        // Log or trigger an event before disposal
        if (! GameManager.IsGameOver)
            ErrorHandler.Warning(this.Name + " is being disposed while game is still Running");

        base.Dispose();
    }
}
