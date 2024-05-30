using System;
using System.Runtime.Serialization;

namespace Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json
{
    /// <summary>
    /// Represents an exception that is thrown when a known exploitable type is encountered during serialization.
    /// </summary>
    [Serializable]
    public class KnownExploitableTypeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KnownExploitableTypeException"/> class.
        /// </summary>
        public KnownExploitableTypeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnownExploitableTypeException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public KnownExploitableTypeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnownExploitableTypeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public KnownExploitableTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnownExploitableTypeException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected KnownExploitableTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Represents an exception that is thrown when an unknown type is encountered during serialization.
    /// </summary>
    [Serializable]
    public class UnknownTypeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownTypeException"/> class.
        /// </summary>
        public UnknownTypeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownTypeException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnknownTypeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownTypeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnknownTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownTypeException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected UnknownTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Represents an exception that is thrown when insecure serialization settings are detected.
    /// </summary>
    [Serializable]
    public class InsecureSerializationSettingsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InsecureSerializationSettingsException"/> class.
        /// </summary>
        public InsecureSerializationSettingsException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InsecureSerializationSettingsException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InsecureSerializationSettingsException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InsecureSerializationSettingsException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public InsecureSerializationSettingsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InsecureSerializationSettingsException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected InsecureSerializationSettingsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
