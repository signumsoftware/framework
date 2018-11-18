using System;

namespace Signum.Utilities
{

    [Serializable]
    public class UnexpectedValueException : Exception
    {
        public UnexpectedValueException() { }
        public UnexpectedValueException(object value) : base("Unexpected " + value == null ? "null " : (value.GetType() + ": " + value.ToString())) { }
    }
}
