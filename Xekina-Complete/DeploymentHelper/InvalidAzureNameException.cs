using System;
using System.Runtime.Serialization;

namespace DeploymentHelper
{
    [Serializable]
    internal class InvalidAzureNameException : Exception
    {
        public InvalidAzureNameException()
        {
        }

        public InvalidAzureNameException(string message) : base(message)
        {
        }

        public InvalidAzureNameException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidAzureNameException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}