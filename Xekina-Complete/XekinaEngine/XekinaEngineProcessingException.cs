using System;
using System.Runtime.Serialization;

namespace XekinaEngine
{
    [Serializable]
    internal class XekinaEngineProcessingException : Exception
    {
        public XekinaEngineProcessingException()
        {
        }

        public XekinaEngineProcessingException(string message) : base(message)
        {
        }

        public XekinaEngineProcessingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected XekinaEngineProcessingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}