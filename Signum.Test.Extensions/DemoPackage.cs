using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Entities.Processes
{
    [Serializable, EntityKind(EntityKind.System)]
    public class DemoPackageDN : IdentifiableEntity, IProcessDataDN
    {
        [SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = true , Max = 200)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }
      
        int requestedLines;
        public int RequestedLines
        {
            get { return requestedLines; }
            set { SetToStr(ref requestedLines, value, () => RequestedLines); }
        }

        int delayMilliseconds;
        public int DelayMilliseconds
        {
            get { return delayMilliseconds; }
            set { Set(ref delayMilliseconds, value, () => DelayMilliseconds); }
        }

        double errorRate;
        [NumberBetweenValidator(0,1), Format("p")]
        public double ErrorRate
        {
            get { return errorRate; }
            set { Set(ref errorRate, value, () => ErrorRate); }
        }

        bool mainError;
        public bool MainError
        {
            get { return mainError; }
            set { Set(ref mainError, value, () => MainError); }
        }

        int numErrors;
        public int NumErrors
        {
            get { return numErrors; }
            set { SetToStr(ref numErrors, value, () => NumErrors); }
        }

        public override string ToString()
        {
            return "Demo {0} ({1} lines{2})".Formato(Name, requestedLines, numErrors == 0 ? "" : ", {0} errors".Formato(numErrors));
        }
    }

    [Serializable, EntityKind(EntityKind.System)]
    public class DemoPackageLineDN : IdentifiableEntity
    {
        Lite<DemoPackageDN> package;
        public Lite<DemoPackageDN> Package
        {
            get { return package; }
            set { Set(ref package, value, () => Package); }
        }
      
        DateTime? finishTime;
        public DateTime? FinishTime
        {
            get { return finishTime; }
            set { Set(ref finishTime, value, () => FinishTime); }
        }

        Lite<ExceptionDN> exception;
        public Lite<ExceptionDN> Exception
        {
            get { return exception; }
            set { Set(ref exception, value, () => Exception); }
        }

        public override string ToString()
        {
            string result = "{0} {1}".Formato(package, finishTime);

            if (exception != null)
                result += " Error";

            return result;
        }
    }

    public enum DemoPackageOperations
    {
        CreateProcess
    }

    public enum DemoPackageProcess
    {
        DemoProcess
    }
}
