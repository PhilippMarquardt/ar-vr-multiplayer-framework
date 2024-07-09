using System.Linq;
using NetLib.Script;
using NetLib.Spawning;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SystemTest
{
    public static class SceneLog
    {
        public static void DumpScene(string caller)
        {
            string str = "Scene dump begin: ";
            foreach (var obj in SceneManager.GetActiveScene().GetRootGameObjects().Where(go => go.GetComponent<NetworkObject>() != null).OrderBy(x => x.GetComponent<NetworkObjectBase>().SceneOrderIndex))
            {
                DumpObjectsInHierarchy(obj, ref str, 0);
            }
            str += "\nScene dump end;";

            NetLib.Utils.Logger.Log(caller, str);
        }

        private static void DumpObjectsInHierarchy(GameObject obj, ref string str, int indent)
        {
            str += "\n" + new string(' ', indent) + "--GameObject:";
            str += "\n" + new string(' ', indent) + "    Name: " + obj.name;
            str += "\n" + new string(' ', indent) + "    Position: " + obj.transform.position;
            str += "\n" + new string(' ', indent) + "    Rotation: " + obj.transform.rotation;

            var networkObject = obj.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                str += "\n" + new string(' ', indent) + "    NetworkObject:";
                str += "\n" + new string(' ', indent) + "      UUID: " + networkObject.Uuid;
                str += "\n" + new string(' ', indent) + "      PrefabHash: " + networkObject.prefabHash;
            }

            var networkBehaviours = obj.GetComponents<NetworkBehaviourA>();
            foreach (var networkBehaviour in networkBehaviours)
            {
                str += "\n" + new string(' ', indent) + "    " + networkBehaviour.GetType();
                str += "\n" + new string(' ', indent) + "    " + networkBehaviour.variable.Value;
            }

            str += "\n" + new string(' ', indent) + "    Children: " + obj.transform.childCount;
            foreach (Transform child in obj.transform)
            {
                DumpObjectsInHierarchy(child.gameObject, ref str, indent + 4);
            }
        }
    }
}
