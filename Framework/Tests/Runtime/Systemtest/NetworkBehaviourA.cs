using NetLib.NetworkVar;
using NetLib.Script;

namespace SystemTest
{
    public class NetworkBehaviourA : NetworkBehaviour
    {
        public NetworkVar<int> variable = new NetworkVar<int>();
    }
}
