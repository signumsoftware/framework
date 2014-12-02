using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class PasswordExpiresIntervalEntity : Entity
    {
        decimal days;
        public decimal Days
        {
            get { return days; }
            set { Set(ref days, value); }
        }

        decimal daysWarning;
        public decimal DaysWarning
        {
            get { return daysWarning; }
            set { Set(ref daysWarning, value); }
        }

        bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set { Set(ref enabled, value); }
        }
    }

    public static class PasswordExpiresIntervalOperation
    {
        public static readonly ExecuteSymbol<PasswordExpiresIntervalEntity> Save = OperationSymbol.Execute<PasswordExpiresIntervalEntity>();
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

