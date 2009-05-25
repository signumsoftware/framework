namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Linq.Expressions;

    public interface IExpressionWriter
    {
        Expression CreateExpression();
    }
   
}

