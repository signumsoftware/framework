using Signum.Utilities;
using System;


namespace Signum.Engine.Linq
{
    public abstract class SystemTime
    {
        static Variable<SystemTime> currentVariable = Statics.ThreadVariable<SystemTime>("systemTime");

        public static SystemTime Current => currentVariable.Value;

        public IDisposable Override(SystemTime systemTime)
        {
            var old = currentVariable.Value;
            currentVariable.Value = systemTime;
            return new Disposable(() => currentVariable.Value = old);
        }

        public class AsOf : SystemTime
        {
            public DateTime DateTime { get; private set; }

            protected AsOf(DateTime dateTime)
            {
                this.DateTime = dateTime;
            }

            public override string ToString() => $"AS OF {DateTime:u}";
        }

        public class FromTo : SystemTime
        {
            public DateTime StartDateTime { get; private set; }
            public DateTime EndtDateTime { get; private set; }

            protected FromTo(DateTime startDateTime, DateTime endDateTime)
            {
                this.StartDateTime = startDateTime;
                this.EndtDateTime = endDateTime;
            }

            public override string ToString() => $"FROM {StartDateTime:u} TO {EndtDateTime:u}";
        }

        public class Between : SystemTime
        {
            public DateTime StartDateTime { get; private set; }
            public DateTime EndtDateTime { get; private set; }

            protected Between(DateTime startDateTime, DateTime endDateTime)
            {
                this.StartDateTime = startDateTime;
                this.EndtDateTime = endDateTime;
            }

            public override string ToString() => $"BETWEEN {StartDateTime:u} AND {EndtDateTime:u}";
        }

        public class ContainerIn : SystemTime
        {
            public DateTime StartDateTime { get; private set; }
            public DateTime EndtDateTime { get; private set; }

            protected ContainerIn(DateTime startDateTime, DateTime endDateTime)
            {
                this.StartDateTime = startDateTime;
                this.EndtDateTime = endDateTime;
            }

            public override string ToString() => $"CONTAINED IN ({StartDateTime:u}, {EndtDateTime:u})";
        }

        public class All : SystemTime
        {
            public DateTime StartDateTime { get; private set; }
            public DateTime EndtDateTime { get; private set; }

            protected All(DateTime startDateTime, DateTime endDateTime)
            {
                this.StartDateTime = startDateTime;
                this.EndtDateTime = endDateTime;
            }

            public override string ToString() => $"ALL";
        }
    }
}
