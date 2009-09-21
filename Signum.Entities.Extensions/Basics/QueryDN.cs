using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.Basics
{
    [Serializable]
    public class QueryDN : IdentifiableEntity
    {
        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string queryName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name
        {
            get { return queryName; }
            set { SetToStr(ref queryName, value, "Name"); }
        }

        public static string GetQueryName(object queryKey)
        {
            return
                queryKey is Type ? ((Type)queryKey).FullName :
                queryKey is Enum ? EnumDN.UniqueKey((Enum)queryKey) :
                queryKey.ToString();
        }

        public override string ToString()
        {
            return queryName;
        }
    }
}
