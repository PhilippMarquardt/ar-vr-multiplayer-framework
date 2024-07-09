using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetLib.Serialization;
using NUnit.Framework;
using UnityEngine.TestTools;
using NetLib.Utils;
using NetLib.Script;
using TestUtils;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using NetLib.Extensions;
using Object = UnityEngine.Object;

namespace Standard
{
    [Category("Standard")]
    public class FileSystemTest 
    {
        private GameObject networkManagerObject;
        private NetworkManager server;
        private NetworkManager client;

        [SetUp]
        public void Setup()
        {
            var transportServer = new BypassServer();
            var transportClient = new BypassClient(transportServer);

            networkManagerObject = new GameObject
            {
                hideFlags = HideFlags.DontSave
            };
            server = networkManagerObject.AddComponent<NetworkManager>();
            client = networkManagerObject.AddComponent<NetworkManager>();

            server.Transport = transportServer;
            client.Transport = transportClient;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(networkManagerObject);
        }

        [Test]
        public void TestFileSerializer()
        {
            var data = FileSerializer.Serialize(TestFilePath.TestFile1);
            string destination = TestFilePath.Folder + "fileSerializerTestResult";
            FileSerializer.Deserialize(data, destination);

            string f1 = HashCode.GetFileHash(TestFilePath.TestFile1);
            string f2 = HashCode.GetFileHash(destination);
            File.Delete(destination);

            // Test correct case
            Assert.AreEqual(f1, f2);

            // Test wrong inputs
            Assert.Throws<ArgumentNullException>(() => FileSerializer.Deserialize(null, destination));
        }

        [Test]
        // Tests if files are added properly to the file system
        public void TestFileHandler()
        {
            var handler = new FileHandler();
            string destination = TestFilePath.Folder + "fileHandlerResult";

            var source = new byte[] {1, 2, 3, 4, 5, 6, 7, 8};
            var splitSource = source.Split(2).ToList();

            for (int i = 0; i < splitSource.Count; i++)
            {
                handler.AddMessage(splitSource.Count, i, "A", splitSource[i].ToArray(), destination);
                // add garbage for different file
                handler.AddMessage(10, 0, "B", new byte[]{42}, "");
            }

            Assert.IsTrue(File.Exists(destination));

            var result = FileSerializer.Serialize(destination);

            Assert.AreEqual(source, result);

            File.Delete(destination);
        }

        [UnityTest]
        // Tests if a file can be send successfully.
        public IEnumerator TestFileSendServer()
        {
            string destination = TestFilePath.Folder + "fileSendResult";

            server.StartServer();
            client.StartClient("localhost");
            yield return new WaitForSeconds(0.1f);

            server.SendFile(TestFilePath.TestFile1, destination, 100, 0, 1);
            yield return new WaitForSeconds(1f);

            Assert.IsTrue(File.Exists(destination));

            Assert.AreEqual(
                HashCode.GetFileHash(TestFilePath.TestFile1),
                HashCode.GetFileHash(destination));

            File.Delete(destination);
            yield return null;
        }

        [UnityTest]
        // Tests if a file can be send successfully.
        public IEnumerator TestFileSendClient()
        {
            string destination = TestFilePath.Folder + "fileSendResult";

            server.StartServer();
            client.StartClient("localhost");
            yield return new WaitForSeconds(0.1f);

            client.SendFile(TestFilePath.TestFile1, destination, 100, 0);
            yield return new WaitForSeconds(1f);

            Assert.IsTrue(File.Exists(destination));

            Assert.AreEqual(
                HashCode.GetFileHash(TestFilePath.TestFile1),
                HashCode.GetFileHash(destination));

            File.Delete(destination);
            yield return null;
        }

        [UnityTest]
        // Tests if a file can be send successfully.
        public IEnumerator TestFileDoesNotExist()
        {
            string destination = TestFilePath.Folder + "bla";

            server.StartServer();
            client.StartClient("localhost");
            yield return new WaitForSeconds(0.1f);

            LogAssert.Expect(LogType.Error, new Regex(".*"));
            client.SendFile(destination, destination, 100, 0);
            
            yield return null;
        }
    }
}
