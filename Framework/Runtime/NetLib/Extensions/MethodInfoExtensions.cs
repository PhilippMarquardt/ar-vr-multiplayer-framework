using System.Reflection;

namespace NetLib.Extensions
{
    /// <summary>
    /// Contains extension methods for the <see cref="MethodInfo"/> type.
    /// </summary>
    public static class MethodInfoExtensions
    {
        /// <summary>
        /// Calculates a stable hash value from a method signature.
        /// </summary>
        /// <remarks>
        /// The same method signature will always have the same hash value.
        /// Method signatures include the method name and the full names of the parameter types.
        /// Return values, attributes and parameter names are not part of the method signature.
        /// </remarks>
        /// <param name="methodInfo">The method for which to calculate the hash value.</param>
        /// <returns>A stable 32-bit hash for the method signature.</returns>
        public static uint GetMethodSignatureHash(this MethodInfo methodInfo)
        {
            string signature = methodInfo.Name;
            foreach (var parameter in methodInfo.GetParameters())
            {
                // use space as separator since it cannot be part of the method or type names
                signature += " " + parameter.ParameterType.FullName;
            }

            return Utils.HashCode.GetStableHash32(signature);
        }
    }
}
