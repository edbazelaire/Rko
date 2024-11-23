using Unity.Netcode;
using System;
using Game;
using Tools;

public class MNetworkList<T> : NetworkList<T> where T : unmanaged, IEquatable<T>
{
    public MNetworkList() { }

    public override void Dispose()
    {
        base.Dispose();
    }
}
