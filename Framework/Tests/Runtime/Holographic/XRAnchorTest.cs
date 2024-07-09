#if !UNITY_2019_3_OR_NEWER
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NetLib.XR;

namespace Holographic
{
    [Category("Holographic")]
    public class XrAnchorTest
    {
        private GameObject anchorManagerGameObject;
        private GameObject testAnchorGameObject;
        private Anchor testAnchor;

        [SetUp]
        public void Setup()
        {
            // start with empty anchor store
            UnityEngine.XR.WSA.Persistence.WorldAnchorStore.GetAsync(
                store => { store.Clear(); Debug.Log("store cleared"); });

            // init anchor manager
            anchorManagerGameObject = new GameObject();
            anchorManagerGameObject.AddComponent<AnchorManager>();

            // init local anchor
            testAnchorGameObject = new GameObject();
            testAnchorGameObject.SetActive(false);
            testAnchor = testAnchorGameObject.AddComponent<Anchor>();
            testAnchor.id = "TestAnchor";
            testAnchor.loadOnStart = false;
            testAnchor.isShared = false;
            testAnchorGameObject.SetActive(true);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(testAnchor);
            Object.DestroyImmediate(anchorManagerGameObject);
        }

        [UnityTest]
        public IEnumerator XrAnchorTestInit()
        {
            yield return null;

            Assert.IsFalse(testAnchor.IsLocked);
            Assert.IsFalse(testAnchor.IsLocated);
            Assert.IsFalse(testAnchor.IsExportable);
            Assert.IsFalse(testAnchor.IsPersisted);
        }

        [UnityTest]
        public IEnumerator XrAnchorTestLockUnlock()
        {
            // initial state
            Assert.IsFalse(testAnchor.IsLocked);
            Assert.IsFalse(testAnchor.IsLocated);
            Assert.IsFalse(testAnchor.IsPersisted);

            // lock anchor
            testAnchor.LockAnchor();
            yield return null;

            Assert.IsTrue(testAnchor.IsLocked);
            Assert.IsTrue(testAnchor.IsLocated);
            Assert.IsFalse(testAnchor.IsPersisted);

            // unlock anchor
            testAnchor.UnlockAnchor();
            yield return null;

            Assert.IsFalse(testAnchor.IsLocked);
            Assert.IsFalse(testAnchor.IsLocated);
            Assert.IsFalse(testAnchor.IsPersisted);

            // lock anchor twice
            testAnchor.LockAnchor();
            yield return null;
            testAnchor.LockAnchor();
            yield return null;

            Assert.IsTrue(testAnchor.IsLocked);
            Assert.IsTrue(testAnchor.IsLocated);
            Assert.IsFalse(testAnchor.IsPersisted);

            // unlock anchor twice
            testAnchor.UnlockAnchor();
            yield return null;
            testAnchor.UnlockAnchor();
            yield return null;

            Assert.IsFalse(testAnchor.IsLocked);
            Assert.IsFalse(testAnchor.IsLocated);
            Assert.IsFalse(testAnchor.IsPersisted);
        }

        [UnityTest]
        public IEnumerator XrAnchorTestStoreSaveLoad()
        {
            testAnchor.LockAnchor();
            yield return null;

            // initial state
            Assert.IsFalse(testAnchor.IsPersisted);

            // save anchor
            testAnchor.SaveToStore();
            yield return null;

            Assert.IsTrue(testAnchor.IsPersisted);
            Assert.IsTrue(testAnchor.IsLocked);
            Assert.IsTrue(testAnchor.IsLocated);

            // unlock anchor
            testAnchor.UnlockAnchor();
            yield return null;

            Assert.IsTrue(testAnchor.IsPersisted);
            Assert.IsFalse(testAnchor.IsLocked);
            Assert.IsFalse(testAnchor.IsLocated);

            // load anchor
            testAnchor.LoadFromStore();
            yield return null;

            Assert.IsTrue(testAnchor.IsPersisted);
            Assert.IsTrue(testAnchor.IsLocked);
            Assert.IsTrue(testAnchor.IsLocated);

            // update anchor
            testAnchor.SaveToStore();
            yield return null;

            Assert.IsTrue(testAnchor.IsPersisted);
            Assert.IsTrue(testAnchor.IsLocked);
            Assert.IsTrue(testAnchor.IsLocated);
        }

        [UnityTest]
        public IEnumerator XrAnchorTestPersistence()
        {
            // save anchor
            testAnchor.LockAnchor();
            yield return null;
            testAnchor.SaveToStore();
            yield return null;

            Assert.IsTrue(testAnchor.IsLocked);
            Assert.IsTrue(testAnchor.IsLocated);
            Assert.IsTrue(testAnchor.IsPersisted);

            // destroy anchor
            Object.Destroy(testAnchor);
            yield return new WaitForSeconds(0.1f);

            // recreate anchor
            testAnchorGameObject.SetActive(false);
            testAnchor = testAnchorGameObject.AddComponent<Anchor>();
            testAnchor.id = "TestAnchor";
            testAnchor.loadOnStart = true;
            testAnchor.isShared = false;
            testAnchorGameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(testAnchor.IsLocked);
            Assert.IsTrue(testAnchor.IsLocated);
            Assert.IsTrue(testAnchor.IsPersisted);
        }

        [UnityTest]
        public IEnumerator XrAnchorTestStoreDelete()
        {
            // save anchor
            testAnchor.LockAnchor();
            yield return null;
            testAnchor.SaveToStore();
            yield return null;

            Assert.IsTrue(testAnchor.IsLocked);
            Assert.IsTrue(testAnchor.IsLocated);
            Assert.IsTrue(testAnchor.IsPersisted);

            // delete anchor
            testAnchor.DeleteFromStore();
            yield return null;

            Assert.IsTrue(testAnchor.IsLocked);
            Assert.IsTrue(testAnchor.IsLocated);
            Assert.IsFalse(testAnchor.IsPersisted);
        }
    }
}
#endif
