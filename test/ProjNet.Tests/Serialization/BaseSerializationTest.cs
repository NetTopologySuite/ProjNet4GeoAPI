// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if !NET7_0_OR_GREATER
namespace ProjNET.Tests.Serialization
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

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
