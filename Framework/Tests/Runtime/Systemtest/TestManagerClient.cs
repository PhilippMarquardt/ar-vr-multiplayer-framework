using System.Collections;
using UnityEngine;
using NetLib.Script;
using TestLogger = NetLib.Utils.Logger;

namespace SystemTest
{
    public class TestManagerClient : MonoBehaviour
    {
        public NetworkManager client;

        private bool barrier;
        private int syncCount;

        private void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            client.OnCustomMessage = OnCustomMessage;
        }

        private void Start()
        {
            Log("Starting TestManager");

            client.StartClient("localhost");

            StartCoroutine(RunTest());
        }

        private IEnumerator RunTest()
        {
            Log("Starting SystemTest");

            //yield return StartCoroutine(Sync());

            //yield return StartCoroutine(Sync());

            //yield return StartCoroutine(Sync());

            yield return StartCoroutine(Sync());

            Application.Quit();
        }

        private IEnumerator Sync()
        {
            barrier = true;
            yield return StartCoroutine(WaitForBarrier());
            Log("Sync " + ++syncCount + " ------------------------------------------------------------------------------------------");
            SceneLog.DumpScene("TestManagerServer");
            EndBarrier();
        }

        private IEnumerator WaitForBarrier()
        {
            while (barrier)
                yield return null;
        }

        private void EndBarrier()
        {
            client.SendCustomMessage(new BarrierMessage());
        }

        private void OnCustomMessage(CustomMessage msg)
        {
            if (msg is BarrierMessage)
                barrier = false;
        }

        private static void Log(string text)
        {
            TestLogger.Log("TestManagerClient", text);
        }
    }
}
