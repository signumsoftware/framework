using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Globalization;
using Signum.Utilities;

namespace Signum.Entities.Localization
{
    [Serializable]
    public class CultureInfoDN : Entity
    {
        private CultureInfoDN() { }

        public CultureInfoDN(CultureInfo ci) 
        {
            Name = ci.Name;
            DisplayName = ci.DisplayName;
        }

        [NotNullable, SqlDbType(Size = 10), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 5, Max = 10)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value, () => DisplayName); }
        }

        public CultureInfo CultureInfo
        {
            get 
            {
                return CultureInfo.GetCultureInfo(Name);
            }
        }

        static Expression<Func<CultureInfoDN, string>> ToStringExpression = e => e.DisplayName;
        public override string ToString()
        { 
            return ToStringExpression.Evaluate(this);
        }
    }
}
