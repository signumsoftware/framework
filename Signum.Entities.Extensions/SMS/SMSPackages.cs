using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Processes;
using Signum.Utilities;

namespace Signum.Entities.SMS
{
    [Serializable, EntityKind(EntityKind.System)]
    public class SMSSendPackageDN : SMSPackageDN
    {

    }

    [Serializable, EntityKind(EntityKind.System)]
    public class SMSUpdatePackageDN : SMSPackageDN
    {

    }

    [Serializable]
    public abstract class SMSPackageDN : IdentifiableEntity, IProcessDataDN
    {
        [SqlDbType(Size = 200)]
        string name;
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        int numLines;
        public int NumLines
        {
            get { return numLines; }
            set { SetToStr(ref numLines, value, () => NumLines); }
        }

        int numErrors;
        public int NumErrors
        {
            get { return numErrors; }
            set { SetToStr(ref numErrors, value, () => NumErrors); }
        }

        public override string ToString()
        {
            return "{0} ({1} lines{2})".Formato(Name, numLines, numErrors == 0 ? "" : ", {0} errors".Formato(numErrors));
        }
    }
}
