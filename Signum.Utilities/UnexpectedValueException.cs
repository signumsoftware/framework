using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Utilities
{

    [Serializable]
    public class UnexpectedValueException : Exception
    {
        public UnexpectedValueException() { }
        public UnexpectedValueException(object value) : base("Unexpected " + value == null ? "null " : (value.GetType() + ": " + value.ToString())) { }
    }
}
