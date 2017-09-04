using System;
using System.Runtime.Serialization;

namespace XekinaEngine
{
    [Serializable]
    internal class XekinaEngineConfigurationException : Exception
    {
        public XekinaEngineConfigurationException()
        {
        }

        public XekinaEngineConfigurationException(string message) : base(message)
        {
        }

        public XekinaEngineConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected XekinaEngineConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}