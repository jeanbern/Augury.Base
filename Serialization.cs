using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Augury.Base
{
    public static class Serialization
    {
        public static Type GetSerializer(Type serializerInterface)
        {
            var matchedSerializers = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(t => t.GetInterfaces().Contains(serializerInterface)).ToList();
            if (matchedSerializers.Count == 0)
            {
                throw new Exception("Could not find a serializer implementing: " + serializerInterface);
            }

            if (matchedSerializers.Count > 1)
            {
                throw new Exception("More than one serializer implements: " + serializerInterface);
            }

            return matchedSerializers[0];
        }

        public static void SerializeInterface<T>(Stream stream, T obj)
        {
            var classType = obj.GetType();
            var genericInterface = typeof(ISerializer<>);
            var serializerInterface = genericInterface.MakeGenericType(classType);
            var serializerType = GetSerializer(serializerInterface);
            var bytes = Serialize(serializerType.AssemblyQualifiedName);
            stream.Write(bytes, 0, bytes.Length);

            var serializer = Activator.CreateInstance(serializerType);
            
            var method = serializerType.GetMethod("Serialize");
            method.Invoke(serializer, new object[] { stream, obj });
        }

        public static T DeserializeInterface<T>(Stream stream)
        {
            var bytes = ReadChunk(stream, "Class name");
            var className = DeserializeString(bytes, 0, bytes.Length);
            var classType = Type.GetType(className);
            if (classType == null)
            {
                throw new TypeAccessException($"Failed to find a type matching name: {className}");
            }

            var serializer = Activator.CreateInstance(classType);

            var method = classType.GetMethod("Deserialize");
            var value = method.Invoke(serializer, new object[] { stream });
            return (T)value;
        }

        public static byte[] ReadChunk(Stream stream, string member)
        {
            var bytes = new byte[4];
            stream.Read(bytes, 0, 4);
            var len = BitConverter.ToInt32(bytes, 0);
            bytes = new byte[len];
            var read = stream.Read(bytes, 0, len);
            if (read != len)
            {
                throw new EndOfStreamException("Was not able to read enough bytes for " + member);
            }

            return bytes;
        }
        
        public static Dictionary<int, uint> DeserializeDictionaryIntUint(byte[] bytes, int start, int length)
        {
            var dict = new Dictionary<int, uint>();
            var index = 0;
            var maxLen = length;
            while (index < maxLen)
            {
                var key = BitConverter.ToInt32(bytes, start + index);
                index += 4;
                var value = BitConverter.ToUInt32(bytes, start + index);
                index += 4;
                dict.Add(key, value);
            }

            return dict;
        }

        public static byte[] Serialize(Dictionary<int, uint> data)
        {
            var bytes = new byte[data.Count * 8];
            var index = 0;
            foreach (var u in data)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(u.Key), 0, bytes, index * 4, 4);
                ++index;
                Buffer.BlockCopy(BitConverter.GetBytes(u.Value), 0, bytes, index * 4, 4);
                ++index;
            }

            return bytes;
        }

        public static byte[] Serialize(KeyValuePair<string, int> data)
        {
            return Concat(Serialize(data.Key), BitConverter.GetBytes(data.Value));
        }

        //cheating, this is actually length + 4
        public static KeyValuePair<string, int> DeserializeKvpStringint(byte[] bytes, int start, int keyLength)
        {
            var key = string.Intern(Encoding.UTF8.GetString(bytes, start, keyLength));
            var value = BitConverter.ToInt32(bytes, start + keyLength);
            return new KeyValuePair<string, int>(key, value);
        }

        private static byte[] Serialize(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            return Encapsulate(bytes);
        }

        private static string DeserializeString(byte[] bytes, int start, int length)
        {
            return string.Intern(Encoding.UTF8.GetString(bytes, start, length));
        }

        public static byte[] Serialize(IReadOnlyList<int> data)
        {
            var ret = new byte[data.Count * 4];

            for (var x = 0; x < data.Count; x++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(data[x]), 0, ret, 4 * x, 4);
            }

            return ret;
        }

        public static List<int> DeserializeListInt(byte[] bytes, int start, int length)
        {
            var results = new List<int>(length / 4);
            for (var x = 0; x < results.Capacity; x++)
            {
                results.Add(BitConverter.ToInt32(bytes, start + 4 * x));
            }

            return results;
        }

        public static int[] DeserializeArrayInt(byte[] bytes, int start, int length)
        {
            var results = new int[length / 4];
            for (var x = 0; x < results.Length; x++)
            {
                results[x] = BitConverter.ToInt32(bytes, start + 4 * x);
            }

            return results;
        }

        public static byte[] Serialize(IReadOnlyList<ushort> data)
        {
            var ret = new byte[data.Count * 2];

            for (var x = 0; x < data.Count; x++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(data[x]), 0, ret, 2 * x, 2);
            }

            return ret;
        }

        public static ushort[] DeserializeArrayUShort(byte[] bytes, int start, int length)
        {
            var results = new ushort[length / 2];
            for (var x = 0; x < results.Length; x++)
            {
                results[x] = BitConverter.ToUInt16(bytes, start + 2 * x);
            }

            return results;
        }

        public static byte[] Serialize(char[] data)
        {
            var chars = Encoding.UTF8.GetBytes(data);
            var bytes = new byte[chars.Length + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(chars.Length), 0, bytes, 0, 4);
            Buffer.BlockCopy(chars, 0, bytes, 4, chars.Length);
            return bytes;
        }

        public static char[] DeserializeArrayChar(byte[] bytes, int start, int length)
        {
            var results = new char[Encoding.UTF8.GetCharCount(bytes, start, length)];
            Encoding.UTF8.GetChars(bytes, start, length, results, 0);
            return results;
        }

        //Assumes the 2nd dimension is already encapsulated, or does not need to be
        public static byte[] Encapsulate(params byte[][] bytes)
        {
            var size = bytes.Aggregate(0, (intt, bytee) => intt + bytee.Length);
            var ret = new byte[size + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(size), 0, ret, 0, 4);
            var index = 4;
            foreach (var bytee in bytes)
            {
                Buffer.BlockCopy(bytee, 0, ret, index, bytee.Length);
                index += bytee.Length;
            }

            return ret;
        }

        public static byte[] Concat(IEnumerable<byte[]> bytes)
        {
            return Concat(bytes.ToArray());
        }

        public static byte[] Concat(params byte[][] bytes)
        {
            var size = bytes.Aggregate(0, (intt, bytee) => intt + bytee.Length);
            var ret = new byte[size];
            var index = 0;
            foreach (var bytee in bytes)
            {
                Buffer.BlockCopy(bytee, 0, ret, index, bytee.Length);
                index += bytee.Length;
            }

            return ret;

        }
    }
}
