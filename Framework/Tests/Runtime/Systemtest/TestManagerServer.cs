using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetLib.Script;
using TestLogger = NetLib.Utils.Logger;

namespace SystemTest
{
    public class TestManagerServer : MonoBehaviour
    {
        public NetworkManager server;
        public GameObject testPrefab;
        public GameObject spawnParent;
        // number of clients
        public int clients; 

        private int barriers;
        private int syncCount;
        private List<GameObject> objects;

        private void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            server.OnCustomMessage += OnCustomMessage;
            objects = new List<GameObject>();
        }

        private void Start()
        {
            Log("Starting TestManager");

            server.StartServer();

            StartCoroutine(RunTest());
        }

        private IEnumerator RunTest()
        {
            Log("Starting SystemTest");

            yield return new WaitForSeconds(10);

            yield return StartCoroutine(SpawnObjects(200, 0.1f));

            //yield return StartCoroutine(Sync());

            yield return StartCoroutine(SpawnObjects(200, 0.1f, spawnParent));

            //yield return StartCoroutine(Sync());

            yield return StartCoroutine(ModifyObjects(0));

            //yield return StartCoroutine(Sync());

            yield return StartCoroutine(DeleteObjects(20, 0.5f));

            yield return new WaitForSeconds(10);

            yield return StartCoroutine(Sync());

            server.Stop();

            Application.Quit();
        }


        private IEnumerator SpawnObjects(int amount, float interval, GameObject parent = null)
        {
            for (int i = 0; i < amount; ++i)
            {
                TestLogger.Log("TestManagerServer", $"Spawning Object {i + 1} of {amount}");

                var obj = Instantiate(testPrefab, parent == null ? null: parent.transform);
                server.SpawnLocalObject(obj);
                objects.Add(obj);

                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator DeleteObjects(int amount, float interval)
        {
            for (int i = 0; i < amount; ++i)
            {
                TestLogger.Log("TestManagerServer", $"Destroying Object {i + 1} of {amount}");

                var obj = objects[Random.Range(0, objects.Count)];
                objects.Remove(obj);
                server.DestroyNetworkObject(obj);
                
                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator ModifyObjects(float interval)
        {
            foreach (var obj in objects)
            {
                obj.transform.position += new Vector3(Random.Range(1, 100), Random.Range(1, 100), Random.Range(1, 100));
                var b = obj.GetComponent<NetworkBehaviourA>();
                if (b != null)
                    b.variable.Value = Random.Range(1, 100);
                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator Sync()
        {
            // wait for current state to be distributed
            yield return new WaitForSeconds(server.updateInterval);
            // start sync process
            StartBarrier();
            Log("Sync " + ++syncCount + " ------------------------------------------------------------------------------------------");
            SceneLog.DumpScene("TestManagerServer");
            yield return StartCoroutine(WaitForBarrier());
        }

        private IEnumerator WaitForBarrier()
        {
            while (barriers > 0)
                yield return null;
        }

        private void StartBarrier()
        {
            server.SendCustomMessage(new BarrierMessage());
            barriers = clients;
        }

        private void OnCustomMessage(CustomMessage msg)
        {
            if (msg is BarrierMessage)
                barriers--;
        }

        private static void Log(string text)
        {
            TestLogger.Log("TestManagerServer", text);
        }
    }
}
