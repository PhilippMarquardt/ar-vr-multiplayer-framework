using System;

namespace NetLib.Script.Rpc
{
    /// <summary>
    /// Indicates that a method can be called remotely from the server and executed on a client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ClientRpcAttribute : Attribute { }
}
