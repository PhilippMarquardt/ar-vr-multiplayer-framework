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
        /// Invokes a given method with no parameters on one client.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <param name="receiver">The connection id of the client on which to invoke the rpc method.</param>
        /// <param name="method">The rpc method to invoke on the client.</param>
        protected void InvokeClientRpc(ulong receiver, Action method)
        {
            SendClientRpc(receiver, method.Method, null);
        }

        /// <summary>
        /// Invokes a given method with 1 parameter on one client.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <param name="receiver">The connection id of the client on which to invoke the rpc method.</param>
        /// <param name="method">The rpc method to invoke on the client.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        protected void InvokeClientRpc<T1>(ulong receiver, Action<T1> method, T1 t1)
        {
            var parameters = new object[]
            {
                t1
            };

            SendClientRpc(receiver, method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 2 parameter on one client.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <param name="receiver">The connection id of the client on which to invoke the rpc method.</param>
        /// <param name="method">The rpc method to invoke on the client.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        protected void InvokeClientRpc<T1, T2>(ulong receiver, Action<T1, T2> method, T1 t1, T2 t2)
        {
            var parameters = new object[]
            {
                t1, t2
            };

            SendClientRpc(receiver, method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 3 parameter on one client.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the rpc method.</typeparam>
        /// <param name="receiver">The connection id of the client on which to invoke the rpc method.</param>
        /// <param name="method">The rpc method to invoke on the client.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        /// <param name="t3">The third parameter for the rpc method.</param>
        protected void InvokeClientRpc<T1, T2, T3>(ulong receiver, Action<T1, T2, T3> method, T1 t1, T2 t2, T3 t3)
        {
            var parameters = new object[]
            {
                t1, t2, t3
            };

            SendClientRpc(receiver, method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 4 parameter on one client.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the rpc method.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter of the rpc method.</typeparam>
        /// <param name="receiver">The connection id of the client on which to invoke the rpc method.</param>
        /// <param name="method">The rpc method to invoke on the client.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        /// <param name="t3">The third parameter for the rpc method.</param>
        /// <param name="t4">The fourth parameter for the rpc method.</param>
        protected void InvokeClientRpc<T1, T2, T3, T4>(ulong receiver, Action<T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            var parameters = new object[]
            {
                t1, t2, t3, t4
            };

            SendClientRpc(receiver, method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 5 parameter on one client.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the rpc method.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter of the rpc method.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter of the rpc method.</typeparam>
        /// <param name="receiver">The connection id of the client on which to invoke the rpc method.</param>
        /// <param name="method">The rpc method to invoke on the client.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        /// <param name="t3">The third parameter for the rpc method.</param>
        /// <param name="t4">The fourth parameter for the rpc method.</param>
        /// <param name="t5">The fifth parameter for the rpc method.</param>
        protected void InvokeClientRpc<T1, T2, T3, T4, T5>(ulong receiver, Action<T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            var parameters = new object[]
            {
                t1, t2, t3, t4, t5
            };

            SendClientRpc(receiver, method.Method, parameters);
        }

        //-------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Invokes a given method with no parameters on all clients.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <param name="method">The rpc method to invoke on all clients.</param>
        protected void InvokeClientRpcOnAll(Action method)
        {
            SendClientRpcToAll(method.Method, null);
        }

        /// <summary>
        /// Invokes a given method with 1 parameters on all clients.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on all clients.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        protected void InvokeClientRpcOnAll<T1>(Action<T1> method, T1 t1)
        {
            var parameters = new object[]
            {
                t1
            };

            SendClientRpcToAll(method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 2 parameters on all clients.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on all clients.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        protected void InvokeClientRpcOnAll<T1, T2>(Action<T1, T2> method, T1 t1, T2 t2)
        {
            var parameters = new object[]
            {
                t1, t2
            };

            SendClientRpcToAll(method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 3 parameters on all clients.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on all clients.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        /// <param name="t3">The third parameter for the rpc method.</param>
        protected void InvokeClientRpcOnAll<T1, T2, T3>(Action<T1, T2, T3> method, T1 t1, T2 t2, T3 t3)
        {
            var parameters = new object[]
            {
                t1, t2, t3
            };

            SendClientRpcToAll(method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 4 parameters on all clients.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the rpc method.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on all clients.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        /// <param name="t3">The third parameter for the rpc method.</param>
        /// <param name="t4">The fourth parameter for the rpc method.</param>
        protected void InvokeClientRpcOnAll<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            var parameters = new object[]
            {
                t1, t2, t3, t4
            };

            SendClientRpcToAll(method.Method, parameters);
        }

        /// <summary>
        /// Invokes a given method with 5 parameters on all clients.
        /// </summary>
        /// <remarks>
        /// The method must be declared in the same class from which this method is called.
        /// The method must have the <see cref="Rpc.ClientRpcAttribute"/> specified.
        /// This method may only be called from a server instance.
        /// </remarks>
        /// <typeparam name="T1">The type of the first parameter of the rpc method.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the rpc method.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the rpc method.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter of the rpc method.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter of the rpc method.</typeparam>
        /// <param name="method">The rpc method to invoke on all clients.</param>
        /// <param name="t1">The first parameter for the rpc method.</param>
        /// <param name="t2">The second parameter for the rpc method.</param>
        /// <param name="t3">The third parameter for the rpc method.</param>
        /// <param name="t4">The fourth parameter for the rpc method.</param>
        /// <param name="t5">The fifth parameter for the rpc method.</param>
        protected void InvokeClientRpcOnAll<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            var parameters = new object[]
            {
                t1, t2, t3, t4, t5
            };

            SendClientRpcToAll(method.Method, parameters);
        }

        //-------------------------------------------------------------------------------------------------------------

        private void SendClientRpc(ulong receiver, MethodInfo method, object[] args)
        {
            if (IsClient)
            {
                Utils.Logger.LogError("NetworkBehaviour", "Cannot invoke a ClientRpc from a client instance.");
                return;
            }

            if (!IsClientRpc(method) || method.DeclaringType != GetType())
            {
                Utils.Logger.LogError("NetworkBehaviour", "The method invoked as ClientRpc is not marked as ClientRpc or not declared in this class.");
                return;
            }

            var msg = new RpcMessage()
            {
                MethodSignature = method.GetMethodSignatureHash(),
                Parameters = args != null ? args.ToList() : new List<object>()
            };

            parent.SendClientRpc(Index, Serializer.Serialize(msg), receiver);
        }

        private void SendClientRpcToAll(MethodInfo method, object[] args)
        {
            if (IsClient)
            {
                Utils.Logger.LogError("NetworkBehaviour", "Cannot invoke a ClientRpc from a client instance.");
                return;
            }

            if (!IsClientRpc(method) || method.DeclaringType != GetType())
            {
                Utils.Logger.LogError("NetworkBehaviour", "The method invoked as ClientRpc is not marked as ClientRpc or not declared in this class.");
                return;
            }

            var msg = new RpcMessage()
            {
                MethodSignature = method.GetMethodSignatureHash(),
                Parameters = args != null ? args.ToList() : new List<object>()
            };

            parent.SendClientRpcToAll(Index, Serializer.Serialize(msg));
        }
    }
}
