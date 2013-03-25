using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using System.Linq.Expressions;
using System.ServiceModel;

namespace Signum.Entities.Basics
{
    [Serializable, EntityKind(EntityKind.SystemString)]
    public class FacadeMethodDN : IdentifiableEntity
    {
        private FacadeMethodDN() { }

        public FacadeMethodDN(MethodInfo mi)
        {
            InterfaceName = mi.DeclaringType.Name;
            var oca = mi.SingleAttribute<OperationContractAttribute>();
            MethodName = oca.Name ?? mi.Name;
        }

        [NotNullable, SqlDbType(Size = 100)]
        string interfaceName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string InterfaceName
        {
            get { return interfaceName; }
            set { Set(ref interfaceName, value, () => InterfaceName); }
        }

        [NotNullable, SqlDbType(Size = 100)]
        string methodName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string MethodName
        {
            get { return methodName; }
            set { Set(ref methodName, value, () => MethodName); }
        }

        public override string ToString()
        {
            return "{0}.{1}".Formato(interfaceName, methodName);
        }

        static Expression<Func<FacadeMethodDN, MethodInfo, bool>> MatchExpression =
            (fm, mi) => mi.DeclaringType.Name == fm.InterfaceName && mi.Name == fm.MethodName;
        public bool Match(MethodInfo mi)
        {
            return MatchExpression.Evaluate(this, mi);
        }

    }
}
