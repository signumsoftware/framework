using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Authorization
{

    // TODO: esta entidad debiera ser unica, tal vez estar en negocio
    [Serializable]
    public class PasswordValidIntervalDN : Entity
    {
        decimal days;
        public decimal Days
        {
            get { return days; }
            set { Set(ref days, value, () => Days); }
        }

        decimal daysWarning;
        public decimal DaysWarning
        {
            get { return daysWarning; }
            set { Set(ref daysWarning, value, () => DaysWarning); }
        }

        bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set { Set(ref enabled, value, () => Enabled); }
        }

    }


    [Serializable]
    public class ExpiredPasswordApplicationException : ApplicationException
    {
        public ExpiredPasswordApplicationException() { }
        public ExpiredPasswordApplicationException(string message) : base(message) { }
        public ExpiredPasswordApplicationException(string message, Exception inner) : base(message, inner) { }
        protected ExpiredPasswordApplicationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}

