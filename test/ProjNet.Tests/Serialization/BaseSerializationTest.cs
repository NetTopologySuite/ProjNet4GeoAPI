﻿using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ProjNET.Tests.Serialization
{
    public class BaseSerializationTest
    {
        public IFormatter GetFormatter()
        {
            return new BinaryFormatter();
        }

        [Obsolete]
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
