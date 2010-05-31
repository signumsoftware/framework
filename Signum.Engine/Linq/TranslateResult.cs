using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data.SqlClient;
using System.Data;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    interface ITranslateResult
    {
        Type ElementType { get; }
        string CommandText { get; }
        UniqueFunction? Unique { get; }

        string CleanCommandText();

        object Execute(IProjectionRow pr); 
    }

    class TranslateResult<T> : ITranslateResult
    {
        public string CommandText { get; set; }
        public UniqueFunction? Unique { get; set; }
        public Type ElementType { get { return typeof(T); } }

        internal Expression<Func<IProjectionRow, SqlParameter[]>> GetParametersExpression;
        internal Func<IProjectionRow, SqlParameter[]> GetParameters;
        internal Expression<Func<IProjectionRow, T>> ProjectorExpression;

        public object Execute(IProjectionRow pr)
        {
            using (Transaction tr = new Transaction())
            using (EntityCache cache = new EntityCache())
            {
                SqlPreCommandSimple command = new SqlPreCommandSimple(CommandText, GetParameters(pr).ToList());

                object result; 
                using (SqlDataReader reader = Executor.UnsafeExecuteDataReader(command))
                {
                    Retriever retriever = pr != null ? pr.Retriever : new Retriever();

                    ProjectionRowEnumerator<T> enumerator = new ProjectionRowEnumerator<T>(reader, ProjectorExpression, pr, retriever);

                    IEnumerable<T> enumerable = new ProjectionRowEnumerable<T>(enumerator);

                    result = Result(enumerable);

                    if (pr == null)
                        retriever.ProcessAll();
                }

                return tr.Commit(result);
            }
        }

        private object Result(IEnumerable<T> enumerable)
        {
            if (Unique == null)
                return enumerable.ToList();

            switch (Unique.Value)
            {
                case UniqueFunction.First: return enumerable.First();
                case UniqueFunction.FirstOrDefault: return enumerable.FirstOrDefault();
                case UniqueFunction.Single: return enumerable.Single();
                case UniqueFunction.SingleOrDefault: return enumerable.SingleOrDefault();
                default:
                    throw new InvalidOperationException();
            }
        } 
        

        #region ITranslateResult Members


        public string CleanCommandText()
        {
            try
            {
                return new SqlPreCommandSimple(CommandText, GetParameters(null).ToList()).PlainSql();
            }
            catch
            {
                return CommandText;
            }
        }

        #endregion
    }

    class CommandResult
    {
        public string CommandText { get; set; }
        internal Expression<Func<SqlParameter[]>> GetParametersExpression;
        internal Func<SqlParameter[]> GetParameters;

        public int Execute()
        {
            SqlPreCommandSimple command = new SqlPreCommandSimple(CommandText, GetParameters().ToList());

            return (int)Executor.ExecuteScalar(command);
        }
    }
}
