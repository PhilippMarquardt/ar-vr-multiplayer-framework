using System;

namespace NetLib.Script.Rpc
{
    /// <summary>
    /// Indicates that a method can be called remotely from a client and executed on the server.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ServerRpcAttribute : Attribute { }
}
