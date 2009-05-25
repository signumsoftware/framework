namespace Signum.Excel
{
    using System;
    using System.CodeDom;
    using System.ComponentModel;
    using System.ComponentModel.Design.Serialization;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Linq.Expressions;
    using Signum.Utilities.ExpressionTrees;
    using Signum.Utilities;

    internal static class UtilCodeDom
    {
        public static void AddComment(CodeMemberMethod method, string text)
        {
            AddCommentSeparator(method);
            method.Statements.Add(new CodeCommentStatement(" " + text));
            AddCommentSeparator(method);
        }

        public static void AddCommentSeparator(CodeMemberMethod method)
        {
            method.Statements.Add(new CodeCommentStatement("-----------------------------------------------"));
        }

        internal static string CreateSafeName(string strName, string prefix)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < strName.Length; i++)
            {
                char ch = strName[i];
                if ((((ch >= 'A') && (ch <= 'Z')) || ((ch >= 'a') && (ch <= 'z'))) || ((ch == '_') || ((ch >= '0') && (ch <= '9'))))
                {
                    builder.Append(ch);
                }
            }
            if (builder.Length == 0)
            {
                return prefix;
            }
            if ((builder[0] >= '0') && (builder[0] <= '9'))
            {
                return (prefix + builder.ToString());
            }
            return builder.ToString();
        }
      
        internal static CodeExpression CodeSnippet(Expression expression)
        {
            string str = expression.GenerateCSharpCode();
            str = str.Indent(12).RemoveLeft(12);
            return new CodeSnippetExpression(str);
        }
    }
}

