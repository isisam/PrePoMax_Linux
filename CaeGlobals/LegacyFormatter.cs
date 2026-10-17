using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CaeGlobals
{
    // Temporary compatibility factory. Hosts on .NET 8 must explicitly enable BinaryFormatter.
    // Use the Framework UnitySerializationHolder wire representation for System.Type, so files
    // can also be read by the existing net48 application (without changing model field identities).
    public static class LegacyFormatter
    {
        public static BinaryFormatter Create()
        {
            var formatter = new BinaryFormatter { Binder = PmxSerializationBinder.Instance };
#if !NETFRAMEWORK
            var selector = new SurrogateSelector();
            var context = new StreamingContext(StreamingContextStates.All);
            var surrogate = new RuntimeTypeSurrogate();
            selector.AddSurrogate(typeof(object).GetType(), context, surrogate);
            selector.AddSurrogate(typeof(Type), context, surrogate);
            formatter.SurrogateSelector = selector;
#endif
            return formatter;
        }
#if !NETFRAMEWORK
        private sealed class RuntimeTypeSurrogate : ISerializationSurrogate
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                var type = (Type)obj;
                info.FullTypeName = "System.UnitySerializationHolder";
                info.AssemblyName = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
                PmxSerializationBinder.Instance.BindToName(type, out string wireAssembly, out string wireName);
                info.AddValue("Data", wireName ?? type.FullName);
                info.AddValue("UnityType", 4);
                info.AddValue("AssemblyName", wireAssembly ?? type.Assembly.FullName);
            }
            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context,
                                        ISurrogateSelector selector) => throw new NotSupportedException();
        }
        [Serializable]
        internal sealed class FrameworkTypeReference : ISerializable, IObjectReference
        {
            private readonly string name;
            private readonly string assembly;
            private FrameworkTypeReference(SerializationInfo info, StreamingContext context)
            {
                if (info.GetInt32("UnityType") != 4)
                    throw new SerializationException("Only the legacy System.Type holder is supported.");
                name = info.GetString("Data");
                assembly = info.GetString("AssemblyName");
            }
            public object GetRealObject(StreamingContext context) =>
                PmxSerializationBinder.Instance.BindToType(assembly, name) ?? throw new SerializationException("Unknown Type record: " + name);
            public void GetObjectData(SerializationInfo info, StreamingContext context) => throw new NotSupportedException();
        }
#endif
    }
}
