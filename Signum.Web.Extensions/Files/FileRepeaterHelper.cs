#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections;
using System.Linq.Expressions;
using System.Web.Mvc.Html;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Configuration;
using Signum.Web.Properties;
using Signum.Engine;
using Signum.Entities.Files;
#endregion

namespace Signum.Web.Files
{
    public static class FileRepeaterHelper
    {
        private static string InternalFileRepeater(this HtmlHelper helper, FileRepeater fileRepeater)
        {
            if (!fileRepeater.Visible)
                return "";

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(EntityBaseHelper.BaseLineLabel(helper, fileRepeater));

            sb.AppendLine(helper.Hidden(fileRepeater.Compose(EntityBaseKeys.StaticInfo), new StaticInfo(fileRepeater.ElementType.CleanType()) { IsReadOnly = fileRepeater.ReadOnly }.ToString(), new { disabled = "disabled" }));
            sb.AppendLine(helper.Hidden(fileRepeater.Compose(TypeContext.Ticks), EntityInfoHelper.GetTicks(helper, fileRepeater).TryToString() ?? ""));

            sb.AppendLine(ListBaseHelper.WriteCreateButton(helper, fileRepeater, new Dictionary<string, object> { { "title", fileRepeater.AddElementLinkText } }));

            sb.AppendLine("<div id='{0}' name='{0}'>".Formato(fileRepeater.Compose(EntityRepeaterKeys.ItemsContainer)));
            if (fileRepeater.UntypedValue != null)
            {
                foreach (var itemTC in TypeContextUtilities.TypeElementContext((TypeContext<MList<FilePathDN>>)fileRepeater.Parent))
                    sb.Append(InternalRepeaterElement(helper, itemTC, fileRepeater));
            }
            sb.AppendLine("</div>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine(helper, fileRepeater));

            return sb.ToString();
        }

        private static string InternalRepeaterElement(this HtmlHelper helper, TypeElementContext<FilePathDN> itemTC, FileRepeater fileRepeater)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id='{0}' name='{0}' class='repeaterElement'>".Formato(itemTC.Compose(EntityRepeaterKeys.RepeaterElement)));

            sb.AppendLine(helper.Hidden(itemTC.Compose(EntityListBaseKeys.Index), itemTC.Index.ToString()));

            if (fileRepeater.Remove)
                sb.AppendLine(
                    helper.Button(itemTC.Compose("btnRemove"),
                                  "x",
                                  "ERepOnRemoving({0}, '{1}');".Formato(fileRepeater.ToJS(), itemTC.ControlID),
                                  "lineButton remove",
                                  new Dictionary<string, object> { { "title", fileRepeater.RemoveElementLinkText } }));

            //Render FileLine for the current item
            sb.AppendLine("<div id='{0}' name='{0}'>".Formato(itemTC.Compose(EntityBaseKeys.Entity)));
            TypeContext<FilePathDN> tc = (TypeContext<FilePathDN>)TypeContextUtilities.CleanTypeContext(itemTC);

            using (FileLine fl = new FileLine(typeof(FilePathDN), tc.Value, itemTC, "", tc.PropertyRoute) { Remove = false })
                sb.AppendLine(helper.InternalFileLine(fl));
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");

            return sb.ToString();
        }

        public static void FileRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property)
            where S : FilePathDN
        {
            helper.FileRepeater(tc, property, null);
        }

        public static void FileRepeater<T, S>(this HtmlHelper helper, TypeContext<T> tc, Expression<Func<T, MList<S>>> property, Action<FileRepeater> settingsModifier)
            where S : FilePathDN
        {
            TypeContext<MList<S>> context = Common.WalkExpression(tc, property);

            FileRepeater fl = new FileRepeater(context.Type, context.UntypedValue, context, null, context.PropertyRoute);

            //Navigator.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S), false);
            Common.FireCommonTasks(fl);

            if (settingsModifier != null)
                settingsModifier(fl);

            helper.Write(helper.InternalFileRepeater(fl));
        }
    }
}
