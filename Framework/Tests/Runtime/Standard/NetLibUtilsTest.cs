using System.Text.RegularExpressions;
using NUnit.Framework;
using NetLib.Utils;
using UnityEngine;
using UnityEngine.TestTools;
using Logger = NetLib.Utils.Logger;

namespace Standard
{
    [Category("Standard")]
    public class NetLibUtilsTest
    {
        [TearDown]
        public void TearDown()
        {
            Logger.Verbosity = Logger.LogLevel.Debug;
        }

        [Test]
        public void TestStableStringHash()
        {
            Assert.AreEqual(
                HashCode.GetStableHash32("Hello World!"), 
                HashCode.GetStableHash32("Hello World!"));

            Assert.AreNotEqual(
                HashCode.GetStableHash32("Hello World!"),
                HashCode.GetStableHash32("Goodbye World!"));
        }

        [Test]
        public void TestFileHash()
        {
            // Test same hash for same file
            string f1 = HashCode.GetFileHash(TestUtils.TestFilePath.TestFile1);
            string f2 = HashCode.GetFileHash(TestUtils.TestFilePath.TestFile1);

            Assert.AreEqual(f1, f2);

            // Test different hash for different file
            f1 = HashCode.GetFileHash(TestUtils.TestFilePath.TestFile1);
            f2 = HashCode.GetFileHash(TestUtils.TestFilePath.TestFile2);

            Assert.AreNotEqual(f1, f2);
        }


        // Test logger verbosity = debug ------------------------------------------------------------------------------

        [Test]
        public void TestLogLevelDebugLogDebug()
        {
            LogAssert.Expect(LogType.Log, new Regex(".*"));

            Logger.Verbosity = Logger.LogLevel.Debug;
            Logger.Log("", "");
        }

        [Test]
        public void TestLogLevelDebugLogWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex(".*"));

            Logger.Verbosity = Logger.LogLevel.Debug;
            Logger.LogWarning("", "");
        }

        [Test]
        public void TestLogLevelDebugLogError()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*"));

            Logger.Verbosity = Logger.LogLevel.Debug;
            Logger.LogError("", "");
        }


        // Test logger verbosity = warning ----------------------------------------------------------------------------

        [Test]
        public void TestLogLevelWarningLogDebug()
        {
            LogAssert.NoUnexpectedReceived();

            Logger.Verbosity = Logger.LogLevel.Warning;
            Logger.Log("", "");
        }

        [Test]
        public void TestLogLevelWarningLogWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex(".*"));

            Logger.Verbosity = Logger.LogLevel.Warning;
            Logger.LogWarning("", "");
        }

        [Test]
        public void TestLogLevelWarningLogError()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*"));

            Logger.Verbosity = Logger.LogLevel.Warning;
            Logger.LogError("", "");
        }


        // Test logger verbosity = error ------------------------------------------------------------------------------

        [Test]
        public void TestLogLevelErrorLogDebug()
        {
            LogAssert.NoUnexpectedReceived();

            Logger.Verbosity = Logger.LogLevel.Error;
            Logger.Log("", "");
        }

        [Test]
        public void TestLogLevelErrorLogWarning()
        {
            LogAssert.NoUnexpectedReceived();

            Logger.Verbosity = Logger.LogLevel.Error;
            Logger.LogWarning("", "");
        }

        [Test]
        public void TestLogLevelErrorLogError()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*"));

            Logger.Verbosity = Logger.LogLevel.Error;
            Logger.LogError("", "");
        }
    }
}
