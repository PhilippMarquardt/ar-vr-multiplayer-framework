using System;
using System.Collections.Generic;
using System.IO;
using NetLib.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace Standard
{
    [Category("Standard")]
    public class SerializerTest
    {
        [Test]
        public void TestSerializeNull()
        {
            Assert.Throws<ArgumentNullException>(() => Serializer.Serialize<object>(null));
        }

        [Test]
        public void TestDeserializeNull()
        {
            Assert.Throws<ArgumentNullException>(() => Serializer.Deserialize<object>(null));

            var obj = new object();
            Assert.Throws<ArgumentNullException>(() => Serializer.Deserialize(ref obj, null));

            obj = null;
            Assert.Throws<ArgumentNullException>(() => Serializer.Deserialize(ref obj, null));
        }

        [Test]
        public void TestDeserializeEmpty()
        {
            Assert.Throws<ArgumentException>(() => Serializer.Deserialize<object>(Array.Empty<byte>()));

            var obj = new object();
            Assert.Throws<ArgumentException>(() => Serializer.Deserialize(ref obj, Array.Empty<byte>()));
        }

        [Test]
        public void TestDeserializeWrongType()
        {
            var obj = Serializer.Serialize(new BinarySerializableImpl());
            Assert.Throws<InvalidOperationException>(() => Serializer.Deserialize<object>(obj));

            var result = new object();
            Assert.Throws<InvalidOperationException>(() => Serializer.Deserialize(ref result, obj));
        }

        [Test]
        public void TestPrimitives()
        {
            var data = Serializer.Serialize(42);
            Assert.AreEqual(42, Serializer.Deserialize<int>(data));

            data = Serializer.Serialize(42u);
            Assert.AreEqual(42u, Serializer.Deserialize<uint>(data));

            data = Serializer.Serialize(3.14f);
            Assert.AreEqual(3.14f, Serializer.Deserialize<float>(data));

            data = Serializer.Serialize(3.14);
            Assert.AreEqual(3.14, Serializer.Deserialize<double>(data));

            data = Serializer.Serialize((byte)2);
            Assert.AreEqual((byte)2, Serializer.Deserialize<byte>(data));

            data = Serializer.Serialize("string");
            Assert.AreEqual("string", Serializer.Deserialize<string>(data));

            data = Serializer.Serialize('c');
            Assert.AreEqual('c', Serializer.Deserialize<char>(data));
        }

        [Test]
        public void TestArrays()
        {
            var data = Serializer.Serialize(new[] { 1, 2, 3 });
            Assert.AreEqual(new[] { 1, 2, 3 }, Serializer.Deserialize<int[]>(data));

            data = Serializer.Serialize(new[] { "one", "two", "three" });
            Assert.AreEqual(new[] { "one", "two", "three" }, Serializer.Deserialize<string[]>(data));
        }

        [Test]
        public void TestContainer()
        {
            var data = Serializer.Serialize(new List<int> { 1, 2, 3, 4, 5 });
            Assert.AreEqual(
                new List<int> { 1, 2, 3, 4, 5 }, 
                Serializer.Deserialize<List<int>>(data));

            data = Serializer.Serialize(new Dictionary<string, int> { { "one", 1 }, { "two", 2 } });
            Assert.AreEqual(
                new Dictionary<string, int> { { "one", 1 }, { "two", 2 } }, 
                Serializer.Deserialize<Dictionary<string, int>>(data));
        }

        [Test]
        public void TestVector3()
        {
            var data = Serializer.Serialize(new Vector3(1, 2, 3));
            Assert.AreEqual(new Vector3(1, 2, 3), Serializer.Deserialize<Vector3>(data));
        }

        [Test]
        public void TestQuaternion()
        {
            var data = Serializer.Serialize(new Quaternion(1, 2, 3, 4));
            Assert.AreEqual(new Quaternion(1, 2, 3, 4), Serializer.Deserialize<Quaternion>(data));
        }

        [Test]
        public void TestPrimitivesInClass()
        {
            var data = Serializer.Serialize(new SerializablePrimitiveClass { i = 42, f = 3.14f, s = "test" });
            Assert.AreEqual(
                new SerializablePrimitiveClass { i = 42, f = 3.14f, s = "test" }, 
                Serializer.Deserialize<SerializablePrimitiveClass>(data));
        }

        [Test]
        public void TestVectorInClass()
        {
            var data = Serializer.Serialize(new SerializableVectorClass { vector = new Vector3(1, 2, 3) });
            Assert.AreEqual(
                new SerializableVectorClass { vector = new Vector3(1, 2, 3) },
                Serializer.Deserialize<SerializableVectorClass>(data));
        }

        [Test]
        public void TestDeserializeClassByReference()
        {
            var data = Serializer.Serialize(new SerializablePrimitiveClass { i = 42, f = 3.14f, s = "test" });
            var result = new SerializablePrimitiveClass();
            Serializer.Deserialize(ref result, data);

            Assert.AreEqual(
                new SerializablePrimitiveClass { i = 42, f = 3.14f, s = "test" },
                result);
        }

        [Test]
        public void TestDeserializePrimitiveByReference()
        {
            var data = Serializer.Serialize(42);
            int result = 0;
            Serializer.Deserialize(ref result, data);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void TestSerializableInterface()
        {
            var data = Serializer.Serialize(new BinarySerializableImpl() { I = 42, F = 3.14f, S = "test" });

            Assert.AreEqual(
                new BinarySerializableImpl { I = 42, F = 3.14f, S = "test" },
                Serializer.Deserialize<BinarySerializableImpl>(data));
        }

        [Test]
        public void TestSerializableInterfacePolymorphism()
        {
            var data = Serializer.Serialize<BinarySerializableImpl>(new BinarySerializableImplDerived() { I = 42, F = 3.14f, S = "test", J = 25 });

            BinarySerializableImpl result = new BinarySerializableImplDerived();
            Serializer.Deserialize(ref result, data);

            Assert.AreEqual(
                new BinarySerializableImplDerived { I = 42, F = 3.14f, S = "test", J = 25 },
                (BinarySerializableImplDerived)result);
        }

        [Test]
        public void TestSerializableInterfaceByReference()
        {
            var data = Serializer.Serialize(new BinarySerializableImpl() { I = 42, F = 3.14f, S = "test" });
            var result = new BinarySerializableImpl();
            Serializer.Deserialize(ref result, data);

            Assert.AreEqual(
                new BinarySerializableImpl { I = 42, F = 3.14f, S = "test" },
                result);
        }

        [Test]
        public void TestSerializableInterfaceForcedIgnore()
        {
            var data = Serializer.Serialize(new BinarySerializableImplSmall() { I = 42 }, true);
            var result = new BinarySerializableImplSmall();
            Serializer.Deserialize(ref result, data);

            Assert.AreEqual(
                42,
                result.I);

            Assert.AreEqual(
                42,
                Serializer.Deserialize<BinarySerializableImplSmall>(data).I);

            // Test that full serialization is used
            Assert.Greater(
                Serializer.Serialize(new BinarySerializableImplSmall() { I = 42 }, true).Length, 
                Serializer.Serialize(new BinarySerializableImplSmall() { I = 42 }).Length);
        }

        [Serializable]
        private class SerializablePrimitiveClass
        {
            public int i;
            public float f;
            public string s;

            public override bool Equals(object obj) =>
                (obj is SerializablePrimitiveClass o) &&
                o.i.Equals(i) &&
                o.f.Equals(f) &&
                o.s.Equals(s);

            public override int GetHashCode() => base.GetHashCode();
        }

        [Serializable]
        private class SerializableVectorClass
        {
            public Vector3 vector;

            public override bool Equals(object obj) =>
                (obj is SerializableVectorClass o) &&
                o.vector.Equals(vector);

            public override int GetHashCode() => base.GetHashCode();
        }

        [Serializable]
        private class BinarySerializableImplSmall : IBinarySerializable
        {
            public int I;

            public void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);
                I = reader.ReadInt32();
            }

            public void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);
                writer.Write(I);
            }
        }

        private class BinarySerializableImpl : IBinarySerializable
        {
            public int I;
            public float F;
            public string S = "";

            public virtual void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);
                writer.Write(I);
                writer.Write(F);
                writer.Write(S);
            }

            public virtual void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);
                I = reader.ReadInt32();
                F = reader.ReadSingle();
                S = reader.ReadString();
            }

            public override bool Equals(object obj) =>
                (obj is BinarySerializableImpl o) &&
                o.I.Equals(I) &&
                o.F.Equals(F) &&
                o.S.Equals(S);

            public override int GetHashCode() => base.GetHashCode();
        }

        private class BinarySerializableImplDerived : BinarySerializableImpl
        {
            public int J;

            public override void Serialize(Stream stream)
            {
                var writer = new BinaryWriter(stream);
                writer.Write(I);
                writer.Write(F);
                writer.Write(S);
                writer.Write(J);
            }

            public override void Deserialize(Stream stream)
            {
                var reader = new BinaryReader(stream);
                I = reader.ReadInt32();
                F = reader.ReadSingle();
                S = reader.ReadString();
                J = reader.ReadInt32();
            }

            public override bool Equals(object obj) =>
                (obj is BinarySerializableImplDerived o) &&
                o.I.Equals(I) &&
                o.F.Equals(F) &&
                o.S.Equals(S) &&
                o.J.Equals(J);

            public override int GetHashCode() => base.GetHashCode();
        }
    }
}
