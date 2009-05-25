using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data.SqlClient;

namespace Signum.Engine.Linq
{
    interface ITranslateResult
    {
        Type ElementType { get; }
        string CommandText { get; }
        UniqueFunction? UniqueFunction { get; }
        bool HasFullObjects { get; }
    }

    internal class TranslateResult<T> : ITranslateResult
    {
        internal string CommandText;

        internal Expression<Func<IProjectionRow, SqlParameter[]>> GetParametersExpression;
        internal Func<IProjectionRow, SqlParameter[]> GetParameters;

        internal Expression<Func<IProjectionRow, T>> ProjectorExpression;

        internal UniqueFunction? UniqueFunction;
        internal string Alias;

        internal bool HasFullObjects; 

        public Type ElementType
        {
            get { return typeof(T); }
        }

        string ITranslateResult.CommandText
        {
            get { return CommandText; }
        }

        UniqueFunction? ITranslateResult.UniqueFunction
        {
            get { return UniqueFunction; }
        }

        bool ITranslateResult.HasFullObjects
        {
            get { return HasFullObjects; }
        }

    }
}
