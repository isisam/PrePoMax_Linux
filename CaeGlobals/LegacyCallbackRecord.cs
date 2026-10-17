using System;
using System.Runtime.Serialization;

namespace CaeGlobals
{
    [Serializable]
    internal sealed class LegacyCallbackRecord : ISerializable
    {
        private LegacyCallbackRecord(SerializationInfo info, StreamingContext context)
        {
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException("Runtime callback records are never written.");
        }
    }
}
