using System;
using System.Runtime.Serialization;

namespace Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json
{
    [Serializable]
    public class KnownExploitableTypeException : Exception
    {
        public KnownExploitableTypeException()
        {
        }

        public KnownExploitableTypeException(string message) : base(message)
        {
        }

        public KnownExploitableTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected KnownExploitableTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class UnknownTypeException : Exception
    {
        public UnknownTypeException()
        {
        }

        public UnknownTypeException(string message) : base(message)
        {
        }

        public UnknownTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class InsecureSerializationSettingsException : Exception
    {
        public InsecureSerializationSettingsException()
        {
        }

        public InsecureSerializationSettingsException(string message) : base(message)
        {
        }

        public InsecureSerializationSettingsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InsecureSerializationSettingsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

}
