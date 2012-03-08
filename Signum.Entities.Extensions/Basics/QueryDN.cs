using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.ServiceModel;
using Signum.Services;
using Signum.Entities.Basics;
using System.Linq.Expressions;

namespace Signum.Entities.Basics
{
    [Serializable]
    public class QueryDN : IdentifiableEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string displayName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetToStr(ref displayName, value, () => DisplayName); }
        }

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        string key;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Key
        {
            get { return key; }
            set { SetToStr(ref key, value, () => Key); }
        }

        static readonly Expression<Func<QueryDN, string>> ToStringExpression = e => e.displayName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}


namespace Signum.Services
{
    [ServiceContract]
    public interface IQueryServer
    {
        [OperationContract, NetDataContract]
        QueryDN RetrieveOrGenerateQuery(object queryName);
    }
}
