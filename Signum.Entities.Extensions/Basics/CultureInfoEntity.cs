using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Globalization;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.Authorization;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.String, EntityData.Master)]
    public class CultureInfoEntity : Entity
    {
        public CultureInfoEntity() { }

        public CultureInfoEntity(CultureInfo ci)
        {
            Name = ci.Name;
            NativeName = ci.NativeName;
            EnglishName = ci.EnglishName;
        }

        [NotNullable, SqlDbType(Size = 10), UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 2, Max = 10)]
        public string Name { get; set; }

        public string NativeName { get; private set; }

        public string EnglishName { get; private set; }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Name) && Name.HasText())
            {
                try
                {
                    CultureInfo.GetCultureInfo(this.Name);
                }
                catch (CultureNotFoundException)
                {
                    return "'{0}' is not a valid culture name".FormatWith(Name);
                }
            }

            return base.PropertyValidation(pi);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            try
            {
                var ci = CultureInfo.GetCultureInfo(Name);
                EnglishName = ci.EnglishName;
                NativeName = ci.NativeName;
            }
            catch (CultureNotFoundException)
            {
            }

            base.PreSaving(ref graphModified);
        }

        static Expression<Func<CultureInfoEntity, string>> ToStringExpression = e => e.EnglishName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class CultureInfoOperation
    {
        public static ExecuteSymbol<CultureInfoEntity> Save;
    }
}
