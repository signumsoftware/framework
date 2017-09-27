using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.Basics;
using static System.Int32;

namespace Signum.Entities.Rest
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), InTypeScript(Undefined = false)]
    public class RestLogEntity : Entity
    {
        [NotNullable, SqlDbType(Size = MaxValue)]
        public string Url { get; set; }

        public DateTime StartDate { get; set; }


        [NotNullable, SqlDbType(Size = MaxValue)]
        public string RequestBody { get; set; }

        [NotNullable, PreserveOrder]
        public MList<QueryStringValueEmbedded> QueryString { get; set; } = new MList<QueryStringValueEmbedded>();

        public Lite<IUserEntity> User { get; set; }
        
        [SqlDbType(Size = int.MaxValue)]
        public string UserHostAddress { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string UserHostName { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        public string Referrer { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        public string Controller { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        public string Action { get; set; }

        public Lite<ExceptionEntity> Exception { get; set; }

        [SqlDbType(Size = MaxValue)]
        public string ResponseBody { get; set; }

        public DateTime EndDate { get; set; }

        static Expression<Func<RestLogEntity, double?>> DurationExpression =
          log => (double?)(log.EndDate - log.StartDate).TotalMilliseconds;
        [Unit("ms"), ExpressionField("DurationExpression")]
        public double? Duration
        {
            get { return DurationExpression.Evaluate(this); }
        }
    }

    [Serializable]
    public class QueryStringValueEmbedded : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = MaxValue)]
        public string Key { get; set; }

        [SqlDbType(Size = MaxValue)]
        public string Value { get; set; }


    }

}
