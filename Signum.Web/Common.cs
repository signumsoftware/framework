using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using Signum.Engine;
using System.Web;
using Signum.Entities.Basics;

namespace Signum.Web
{
    public static class Common
    {
        public static event Action<LineBase> CommonTask;

        static Common()
        {
            CommonTask += TaskSetLabelText;
            CommonTask += TaskSetFormatText;
            CommonTask += TaskSetUnitText;
            //CommonTask += TaskSetImplementations;
            CommonTask += TaskSetMove;
            CommonTask += TaskSetReadOnly;
            CommonTask += TaskSetHtmlProperties;
        }

        public static void FireCommonTasks(LineBase eb)
        {
            CommonTask(eb);
        }

        #region Tasks
        public static void TaskSetLabelText(LineBase bl)
        {
            if (bl != null && bl.PropertyRoute.PropertyRouteType == PropertyRouteType.FieldOrProperty)
                bl.LabelText = bl.PropertyRoute.PropertyInfo.NiceName();
        }

        static void TaskSetUnitText(LineBase bl)
        {
            ValueLine vl = bl as ValueLine;
            if (vl != null && vl.PropertyRoute.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                UnitAttribute ua = bl.PropertyRoute.PropertyInfo.GetCustomAttribute<UnitAttribute>();
                if (ua != null)
                    vl.UnitText = ua.UnitName;
            }
        }

        static void TaskSetFormatText(LineBase bl)
        {
            ValueLine vl = bl as ValueLine;
            if (vl != null && bl.PropertyRoute.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                string format = Reflector.FormatString(bl.PropertyRoute);
                if (format != null)
                    vl.Format = format;
            }
        }

        public static void TaskSetImplementations(LineBase bl)
        {
            EntityBase eb = bl as EntityBase;
            if (eb != null)
            {
                PropertyRoute route = bl.PropertyRoute;

                if (bl.Type.IsMList())
                    route = route.Add("Item");

                if (route.Type.CleanType().IsIEntity())
                {
                    IImplementationsFinder finder = typeof(ModelEntity).IsAssignableFrom(route.RootType) ?
                        (IImplementationsFinder)Navigator.EntitySettings(route.RootType) : Schema.Current;

                    eb.Implementations = finder.FindImplementations(route);

                    if (eb.Implementations.Value.IsByAll)
                    {
                        EntityLine el = eb as EntityLine;
                        if (el != null)
                            el.Autocomplete = false;
                    }
                }
            }
        }

        public static void TaskSetMove(LineBase bl)
        {
            EntityListBase eb = bl as EntityListBase;
            if (eb != null)
            {
                PropertyRoute route = bl.PropertyRoute;

                eb.Move = Schema.Current.Settings.FieldAttributes(bl.PropertyRoute).OfType<PreserveOrderAttribute>().Any();
            }
        }

        public static void TaskSetReadOnly(LineBase bl)
        {
            if (bl != null && bl.PropertyRoute.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                if (bl.PropertyRoute.PropertyInfo.IsReadOnly() || bl.ReadOnly)
                {
                    bl.ReadOnly = true;
                }
            }
        }

        public static void TaskSetHtmlProperties(LineBase bl)
        {
            ValueLine vl = bl as ValueLine;
            if (vl != null && bl.PropertyRoute.PropertyRouteType == PropertyRouteType.FieldOrProperty)
            {
                var slv = Validator.TryGetPropertyValidator(bl.PropertyRoute)?.Validators.OfType<StringLengthValidatorAttribute>().FirstOrDefault();
                if (slv != null)
                {
                    int max = slv.Max; //-1 if not set
                    if (max != -1)
                    {
                        vl.ValueHtmlProps.Add("maxlength", max);
                        int? maxSize = ValueLineHelper.Configurator.MaxValueLineSize;
                        vl.ValueHtmlProps.Add("size", maxSize.HasValue ? Math.Min(max, maxSize.Value) : max);
                    }

                    if (slv.MultiLine)
                        vl.ValueLineType = ValueLineType.TextArea;
                }
            }
        }
#endregion

        #region TypeContext
        internal static TypeContext UntypedWalkExpression(TypeContext tc, LambdaExpression lambda)
        {
            Type returnType = lambda.Body.Type;
            return miWalkExpression.GetInvoker(tc.Type, returnType)(tc, lambda);
        }

        static GenericInvoker<Func<TypeContext, LambdaExpression, TypeContext>> miWalkExpression = 
            new GenericInvoker<Func<TypeContext, LambdaExpression, TypeContext>>((tc, le) => Common.WalkExpression<TypeEntity, TypeEntity>((TypeContext<TypeEntity>)tc, (Expression<Func<TypeEntity, TypeEntity>>)le));
        public static TypeContext<S> WalkExpression<T, S>(TypeContext<T> tc, Expression<Func<T, S>> lambda)
        {
            return MemberAccessGatherer.WalkExpression(tc, lambda);
        }
        #endregion

        #region HttpContext
        //public static string FullyQualifiedApplicationPath
        //{
        //    get
        //    {
        //        HttpContext context = HttpContext.Current;
        //        if (context == null)
        //            return null;

        //        string appPath = "{0}://{1}{2}{3}".FormatWith(
        //              context.Request.Url.Scheme,
        //              context.Request.Url.Host,
        //              context.Request.Url.Port == 80 ? string.Empty : ":" + context.Request.Url.Port,
        //              context.Request.ApplicationPath);

        //        if (!appPath.EndsWith("/"))
        //            appPath += "/";

        //        return appPath;
        //    }
        //}
        #endregion

        public static object Convert(object obj, Type type)
        {
            if (obj == null) return null;

            Type objType = obj.GetType();

            if (type.IsAssignableFrom(objType))
                return obj;

            if (objType.IsLite() && type.IsAssignableFrom(((Lite<IEntity>)obj).EntityType))
            {
                Lite<IEntity> lite = (Lite<IEntity>)obj;
          

                //return lite.UntypedEntityOrNull ?? Database.RetrieveAndForget(lite);
                return lite.EntityOrNull ?? Database.RetrieveAndForget(lite);
            }

            if (type.IsLite())
            {
                Type liteType = Lite.Extract(type);
                if (liteType.IsAssignableFrom(objType))
                {
                    return ((Entity)obj).ToLite();
                }
            }

            throw new InvalidCastException("Impossible to convert object '{0}' from type '{1}' to type '{2}'".FormatWith(obj, objType.TypeName(), type.TypeName()));
        }
    }
}
