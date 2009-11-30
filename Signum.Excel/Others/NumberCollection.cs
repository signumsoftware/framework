namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Reflection;
    using System.Xml;
    using System.Collections.ObjectModel;
using System.Linq.Expressions;

    public sealed class NumberCollection  : Collection<string>, IWriter, IExpressionWriter
    {
        public void WriteTo(CodeTypeDeclaration type, CodeMemberMethod method, CodeExpression targetObject)
        {
            for (int i = 0; i < base.Count; i++)
            {
                string str = this[i];
                CodeMethodInvokeExpression expression = new CodeMethodInvokeExpression(new CodePropertyReferenceExpression(targetObject, "Numbers"), "Add", new CodeExpression[] { new CodePrimitiveExpression(str) });
                method.Statements.Add(expression);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            for (int i = 0; i < base.Count; i++)
            {
                string str = this[i];
                writer.WriteElementString("Number", Namespaces.Excel, str);
            }
        }

        public Expression CreateExpression()
        {
            return UtilExpression.ListInit(this, s => Expression.Constant(s)); 
        }
    }
}

