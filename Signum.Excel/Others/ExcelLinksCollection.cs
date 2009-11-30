namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;
    using System.Collections.ObjectModel;
    using System.Linq.Expressions;

    public sealed class ExcelLinksCollection  : CollectionXml<SupBook>, IExpressionWriter
    {        
        public Expression CreateExpression()
        {
            return UtilExpression.ListInit(this);
        }      
    }
}

