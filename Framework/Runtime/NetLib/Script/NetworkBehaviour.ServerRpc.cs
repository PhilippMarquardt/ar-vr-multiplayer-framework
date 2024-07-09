using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetLib.Extensions;
using NetLib.Serialization;

namespace NetLib.Script
{
    // Extension for NetworkBehaviour with rpc invoke methods
    public abstract partial class NetworkBehaviour
    {
        /// <summary>
        /// Invokes a given method with no parameters on the server.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ServerRpcAttribute"/> specified.
        /// This method may only be called from a client instance.
        /// </remarks>
        /// <param name="method">The rpc method to invoke on the server.</param>
        protected void InvokeServerRpc(Action method)
        {
            SendServerRpc(method.Method, null);
        }

        /// <summary>
        /// Invokes a given method with 1 parameter on the server.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ServerRpcAttribute"/> specified.
        /// This method may only be called from a client instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on the server.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        protected void InvokeServerRpc<T1>(Action<T1> method, T1 t1)
        {
            var parameters = new object[]
            {
                t1
            };

            SendServerRpc(method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 2 parameters on the server.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ServerRpcAttribute"/> specified.
        /// This method may only be called from a client instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on the server.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        protected void InvokeServerRpc<T1, T2>(Action<T1, T2> method, T1 t1, T2 t2)
        {
            var parameters = new object[]
            {
                t1, t2
            };

            SendServerRpc(method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 3 parameters on the server.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ServerRpcAttribute"/> specified.
        /// This method may only be called from a client instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on the server.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        /// <param name="t3">The third parameter for the rpc method.</param>
        protected void InvokeServerRpc<T1, T2, T3>(Action<T1, T2, T3> method, T1 t1, T2 t2, T3 t3)
        {
            var parameters = new object[]
            {
                t1, t2, t3
            };

            SendServerRpc(method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 4 parameters on the server.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ServerRpcAttribute"/> specified.
        /// This method may only be called from a client instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the rpc method.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on the server.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        /// <param name="t3">The third parameter for the rpc method.</param>
        /// <param name="t4">The fourth parameter for the rpc method.</param>
        protected void InvokeServerRpc<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            var parameters = new object[]
            {
                t1, t2, t3, t4
            };

            SendServerRpc(method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 5 parameters on the server.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ServerRpcAttribute"/> specified.
        /// This method may only be called from a client instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the rpc method.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter of the rpc method.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on the server.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        /// <param name="t3">The third parameter for the rpc method.</param>
        /// <param name="t4">The fourth parameter for the rpc method.</param>
        /// <param name="t5">The fifth parameter for the rpc method.</param>
        protected void InvokeServerRpc<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            var parameters = new object[]
            {
                t1, t2, t3, t4, t5
            };

            SendServerRpc(method.Method, parameters);
        }


        private void SendServerRpc(MethodInfo method, object[] args)
        {
            if (IsServer)
            {
                Utils.Logger.LogError("NetworkBehaviour", "Cannot invoke a ServerRpc from a server instance.");
                return;
            }
            
            if (!IsServerRpc(method) || method.DeclaringType != GetType())
            {
                Utils.Logger.LogError("NetworkBehaviour", "The method invoked as ServerRpc is not marked as ServerRpc or not declared in this class.");
                return;
            }

            var msg = new RpcMessage()
            {
                MethodSignature = method.GetMethodSignatureHash(),
                Parameters = args != null ? args.ToList() : new List<object>()
            };

            parent.SendServerRpc(Index, Serializer.Serialize(msg));
        }
    }
}
