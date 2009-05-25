using System;
using System.Text;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.CSharp;
using System.Collections;
using System.Linq.Expressions;
using Microsoft.VisualStudio.DebuggerVisualizers;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Linq;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System.Collections.Generic;
using Signum.Utilities.DataStructures;

[assembly: CLSCompliant(true)]

namespace ExpressionVisualizer
{
    public static class ExpressionTreeNodeBuilder
    {
        [ThreadStatic]
        static Dictionary<ParameterExpression, Color> paramColors;

        public static readonly Color ParameterColor = Color.PeachPuff; 
        public static readonly Color MethodColor = Color.Plum;
        public static readonly Color LambdaColor = Color.DodgerBlue;
        public static readonly Color ExpressionColor = Color.SkyBlue;
        public static readonly Color ForeingExpressionColor = Color.Gold;
        public static readonly Color ExpressionForeColor = Color.Black;
        public static readonly Color OthersForeColor = Color.Gray;

        public static ExpressionTreeNode Build(string starting, object value)
        {
            bool original = paramColors == null;
            if (original) paramColors = new Dictionary<ParameterExpression, Color>();
            try
            {
                return new ExpressionTreeNode(GetBackColor(value), GetForeColor(value), "{0} = {1}".Formato(starting, GetString(value)).CleanIdentifiers(), value is Expression ? value.ToString().CleanIdentifiers() : "", GetChildNodes(value));
            }
            finally
            {
                if (original) paramColors = null;
            }
        }

        private static Color GetBackColor(object value)
        {
            return new Switch<object, Color>(value)
                .Case<MethodCallExpression>(MethodColor)
                .Case<LambdaExpression>(LambdaColor)
                .Case<ParameterExpression>(ParameterColor)
                .Case<Expression>(e => e.GetType().Namespace == typeof(Expression).Namespace ? ExpressionColor : ForeingExpressionColor)
                .Default(Color.White);
        }

        private static Color GetForeColor(object value)
        {
            return new Switch<object, Color>(value)
                .Case<ParameterExpression>(p => paramColors.GetOrCreate(p, () => Color.FromArgb(MyRandom.Current.NextColor(0, 150, 0, 150, 0, 150))))
                .Case<Expression>(ExpressionForeColor)
                .Default(OthersForeColor);
        }

        private static ExpressionTreeNode[] GetChildNodes(object value)
        {
            return new Switch<object, ExpressionTreeNode[]>(value)
                .Case((object)null, (ExpressionTreeNode[])null)
                .Case(o => o.GetType().IsInstantiationOf(typeof(ReadOnlyCollection<>)), e => GetChildForCollection((IEnumerable)e))
                .Case<MethodCallExpression>(mce => GetChildForMethodExpression(mce))
                .Case<ParameterExpression>(new ExpressionTreeNode[0])
                .Case<Expression>(e => GetChildForExpansible(e))
                .Case(o=>!o.GetType().Namespace.StartsWith("System"),o => GetChildForExpansible(o))
                .Case<MemberBinding>(m => GetChildForExpansible(m))
                .Default((ExpressionTreeNode[])null);
        }

        private static ExpressionTreeNode[] GetChildForMethodExpression(MethodCallExpression mce)
        {
            return new[]
            {
                Build("MethodInfo Method", mce.Method.Name),
                Build("Expression Object", mce.Object),
                new ExpressionTreeNode(Color.White, OthersForeColor, "Arguments", null,mce.Arguments.Zip(mce.Method.GetParameters(), (e,p)=> Build(p.ParameterName(), e)).ToArray())
            };
        }

        private static ExpressionTreeNode[] GetChildForCollection(IEnumerable collection)
        {
            return collection.Cast<object>().Select((val, i) => Build("[{0}]".Formato(i), val)).ToArray();
        }

        private static ExpressionTreeNode[] GetChildForExpansible(object expansible)
        {
            MemberInfo[] pis = expansible.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance| BindingFlags.GetProperty | BindingFlags.GetField)
                .Where(mi => mi.Name != "Type" && mi.Name != "NodeType" && ((mi as PropertyInfo).TryCS(pi=> pi.GetIndexParameters().Length == 0) ?? true))
                .OrderBy(mi => mi.ReturningType().IsAssignableFrom(typeof(Expression))).ThenBy(pi => pi.Name).ToArray();

            return pis.Select(mi => Build(mi.MemberName(), expansible.GetValue(mi).Map(o =>
                o == null ? null :
                o is IQueryable ? o.ToString() : o))).ToArray();
        }


        public static object GetValue(this object obj, MemberInfo mi)
        {
            if (mi.MemberType == MemberTypes.Field)
                return ((FieldInfo)mi).GetValue(obj);
            else if (mi.MemberType == MemberTypes.Property)
                return ((PropertyInfo)mi).GetValue(obj, null);
            return null;
        }
        private static string GetString(object value)
        {
            return new Switch<object, string>(value)
                .Case((object)null, "null")
                .Case(o => o.GetType().IsInstantiationOf(typeof(ReadOnlyCollection<>)), o => "{0}( Count = {1})".Formato(o.GetType().TypeName(), ((IList)o).Count))
                .Case<ParameterExpression>(pe => "{0} : {1} {2}".Formato(pe.GetType().TypeName(), pe.Type.TypeName(), pe.Name))
                .Case<Expression>(e => "{0} ({1}) of type {2}".Formato(e.GetType().TypeName(), EnumExtensions.IsDefined(e.NodeType)?e.NodeType.ToString():"", e.Type.TypeName()))
                .Case<MethodInfo>(mi => mi.MethodName())
                .Case<MemberAssignment>(ma => "({0} = {1})".Formato(ma.Member.Name, ma.Expression))
                .Case<Type>(t => t.TypeName())
                .Default(o => o.ToString());
        }
    }


    [Serializable]
    public class ExpressionTreeNode : TreeNode
    {
        protected ExpressionTreeNode(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ExpressionTreeNode() { }

        public ExpressionTreeNode(Color backColor, Color foreColor, string text, string tag, ExpressionTreeNode[] childs)
        {
            this.Text = text;
            this.Tag = tag;
            this.BackColor = backColor;
            this.ForeColor = foreColor;

            if (childs != null)
                this.Nodes.AddRange(childs);
        }

        protected override void Serialize(System.Runtime.Serialization.SerializationInfo si, System.Runtime.Serialization.StreamingContext context)
        {
            base.Serialize(si, context);
        }
    }

}
