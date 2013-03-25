using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Globalization;
using Signum.Utilities;
using System.Reflection;

namespace Signum.Entities.Translation
{
    [Serializable, EntityKind(EntityKind.String)]
    public class CultureInfoDN : Entity
    {
        public CultureInfoDN() { }

        public CultureInfoDN(CultureInfo ci)
        {
            Name = ci.Name;
            DisplayName = ci.DisplayName;
        }

        [NotNullable, SqlDbType(Size = 10), UniqueIndex]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 10)]
        public string Name
        {
            get { return name; }
            set { SetToStr(ref name, value, () => Name); }
        }

        string displayName;
        public string DisplayName
        {
            get { return displayName; }
            private set { Set(ref displayName, value, () => DisplayName); }
        }

        public CultureInfo CultureInfo
        {
            get
            {
                return CultureInfo.GetCultureInfo(Name);
            }
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Name) && Name.HasText())
            {
                try
                {
                    CultureInfo culture = CultureInfo;
                }
                catch (CultureNotFoundException)
                {
                    return "'{0}' is not a valid culture name".Formato(Name);
                }
            }

            return base.PropertyValidation(pi);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            try
            {
                DisplayName = CultureInfo.DisplayName;
            }
            catch (CultureNotFoundException)
            {
            }

            base.PreSaving(ref graphModified);
        }

        static Expression<Func<CultureInfoDN, string>> ToStringExpression = e => e.DisplayName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public enum CultureInfoOperation
    {
        Save
    }

    public enum TranslationPermission
    {
        TranslateCode,
        TranslateInstances
    }
}
