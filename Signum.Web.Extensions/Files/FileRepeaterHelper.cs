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
        private static void InternalFileRepeater<T>(this HtmlHelper helper, TypeContext<MList<T>> typeContext, FileRepeater settings)
            where T : FilePathDN
        {
            if (!settings.Visible)
                return;

            string prefix = helper.GlobalName(typeContext.Name);
            MList<T> value = typeContext.Value;  
            Type elementsCleanStaticType = Reflector.ExtractLite(typeof(T)) ?? typeof(T);

            long? ticks = EntityBaseHelper.GetTicks(helper, prefix, settings);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(helper.HiddenSFInfo(prefix, new EntityInfo { StaticType = elementsCleanStaticType, Ticks = ticks }));

            sb.AppendLine(EntityBaseHelper.WriteLabel(helper, prefix, settings));

            sb.AppendLine(ListBaseHelper.WriteCreateButton(helper, settings, new Dictionary<string, object>{{"title", settings.AddElementLinkText}}));
            
           // sb.AppendLine(helper.Div("", "", "clearall", null)); //To keep create and find buttons' space

            sb.AppendLine("<div id='{0}' name='{0}'>".Formato(TypeContext.Compose(prefix, EntityRepeaterKeys.ItemsContainer)));
            if (value != null)
            {
                for (int i = 0; i < value.Count; i++)
                    sb.Append(InternalRepeaterElement(helper, prefix, value[i], i, settings, typeContext));
            }
            sb.AppendLine("</div>");

            sb.AppendLine(EntityBaseHelper.WriteBreakLine());

            helper.ViewContext.HttpContext.Response.Write(sb.ToString());
        }

        private static string InternalRepeaterElement<T>(this HtmlHelper helper, string prefix, T value, int index, FileRepeater settings, TypeContext<MList<T>> typeContext)
            where T : FilePathDN
        {
            string indexedPrefix = TypeContext.Compose(prefix, index.ToString());
            Type cleanStaticType = Reflector.ExtractLite(typeof(T)) ?? typeof(T);
            bool isIdentifiable = typeof(IdentifiableEntity).IsAssignableFrom(typeof(T));
            bool isLite = typeof(Lite).IsAssignableFrom(typeof(T));
            
            Type cleanRuntimeType = null;
            if (value != null)
                cleanRuntimeType = typeof(Lite).IsAssignableFrom(value.GetType()) ? (value as Lite).RuntimeType : value.GetType();

            long? ticks = EntityBaseHelper.GetTicks(helper, indexedPrefix, settings);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<div id='{0}' name='{0}' class='repeaterElement'>".Formato(TypeContext.Compose(indexedPrefix, EntityRepeaterKeys.RepeaterElement)));
            
            sb.AppendLine(helper.Hidden(TypeContext.Compose(indexedPrefix, EntityListBaseKeys.Index), index.ToString()));

            if (settings.Remove)
                sb.AppendLine(
                    helper.Button(TypeContext.Compose(indexedPrefix, "btnRemove"),
                                  "x",
                                  "ERepOnRemoving({0}, '{1}');".Formato(settings.ToJS(), indexedPrefix),
                                  "lineButton remove",
                                  new Dictionary<string, object> { { "title", settings.RemoveElementLinkText } }));

            //Render FileLine for the current item
            sb.AppendLine("<div id='{0}' name='{0}'>".Formato(TypeContext.Compose(indexedPrefix, EntityBaseKeys.Entity)));
            TypeContext<FilePathDN> tc;
            if (isLite)
                tc = new TypeElementContext<FilePathDN>((FilePathDN)Database.Retrieve((Lite)(object)value), typeContext, index);
            else
                tc = new TypeElementContext<FilePathDN>(value, typeContext, index);
            using (FileLine fl = new FileLine(indexedPrefix) { Remove = false })
                sb.AppendLine(helper.InternalFileLine<FilePathDN>(tc, fl));
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

            FileRepeater fl = new FileRepeater(helper.GlobalName(context.Name));
            //Navigator.ConfigureEntityBase(el, Reflector.ExtractLite(typeof(S)) ?? typeof(S), false);
            Common.FireCommonTasks(fl, typeof(T), context);

            if (settingsModifier != null)
                settingsModifier(fl);

            using (fl)
                helper.InternalFileRepeater<S>(context, fl);
        }
    }
}
