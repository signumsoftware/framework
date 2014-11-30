using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class NamespaceHelpEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 300)]
        string name;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 300)]
        public string Name
        {
            get { return name; }
            set { Set(ref name, value); }
        }

        [NotNullable]
        CultureInfoEntity culture;
        [NotNullValidator]
        public CultureInfoEntity Culture
        {
            get { return culture; }
            set { Set(ref culture, value); }
        }

        [NotNullable, SqlDbType(Size = 200)]
        string title;
        public string Title
        {
            get { return title; }
            set { Set(ref title, value); }
        }
      
        [SqlDbType(Size = int.MaxValue)]
        string description;
        public string Description
        {
            get { return description; }
            set { Set(ref description, value); }
        }

        static Expression<Func<NamespaceHelpEntity, string>> ToStringExpression = e => e.Name;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    public static class NamespaceHelpOperation
    {
        public static readonly ExecuteSymbol<NamespaceHelpEntity> Save = OperationSymbol.Execute<NamespaceHelpEntity>();
    }


}
