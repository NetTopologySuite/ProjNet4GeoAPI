using System;
using System.IO;
#if !NET7_0_OR_GREATER
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ProjNET.Tests.Serialization
{
    public class BaseSerializationTest
    {
        [Obsolete("ISerializable is deprecated")]
        public IFormatter GetFormatter()
        {
            return new BinaryFormatter();
        }

        [Obsolete("ISerializable is deprecated")]
        public static T SanD<T>(T instance, IFormatter formatter)
        {
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, instance);
                ms.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
#endif
