using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class PasswordExpiresIntervalDN : Entity
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

    public enum PasswordExpiresIntervalOperation
    { 
        Save
    }

    [Serializable]
    public class PasswordExpiredException : ApplicationException
    {
        public PasswordExpiredException() { }
        public PasswordExpiredException(string message) : base(message) { }
        public PasswordExpiredException(string message, Exception inner) : base(message, inner) { }
        protected PasswordExpiredException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}

