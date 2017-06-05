using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using Signum.Utilities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using System.Windows;
using System.Windows.Media;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;

namespace Signum.Windows
{
    public static class Converters
    {
        public static readonly IValueConverter Identity =
            ConverterFactory.New((object v) => v, (object v) => v);

        public static readonly IValueConverter EntityKey =
            ConverterFactory.New((ResultRow row) => row.Entity.Key());

        public static readonly IValueConverter LiteKey =
          ConverterFactory.New((object lite) => lite is Lite<Entity> ?  ((Lite<Entity>)lite).Key() : null);

        public static readonly IValueConverter ToLite =
           ConverterFactory.New((IEntity ei) => ei?.ToLite());

        public static readonly IValueConverter Retrieve =
                   ConverterFactory.New((Lite<IEntity> lite) => lite == null ? null : Server.Retrieve(lite));

        public static readonly IValueConverter NullableEnum =
            ConverterFactory.New((object v) => v == null ? VoidEnumMessage.Instance : v, (object v) => v.Equals(VoidEnumMessage.Instance) ? null : v);

        public static readonly IValueConverter EnumDescription =
            ConverterFactory.New((object v) => v is Enum? ((Enum)v).NiceToString(): v);

        public static readonly IValueConverter ErrorListToToolTipString =
            ConverterFactory.New((IEnumerable<ValidationError> err) => err.Select(e => DoubleListConverter.CleanErrorMessage(e)).FirstOrDefault());

        public static readonly IValueConverter ErrorListToErrorCount =
            ConverterFactory.New((string[] str) => str == null ? null :
                str.Length == 0 ? NormalWindowMessage.NoDirectErrors.NiceToString() :
                str.Length == 1 ? NormalWindowMessage._1Error.NiceToString().FormatWith(str[0]) :
                NormalWindowMessage._0Errors1.NiceToString().FormatWith(str.Length, str[0]));

        public static readonly IValueConverter ErrorListToBool =
            ConverterFactory.New((string[] str) => str != null && str.Length > 0);

        public static readonly IValueConverter ErrorToInt =
            ConverterFactory.New((string str) => str.HasText() ? 1 : 0);

        public static readonly IValueConverter BoolToInt =
            ConverterFactory.New((bool b) => b ? 1 : 0);

        public static readonly IValueConverter BoolToBold =
            ConverterFactory.New((bool b) => b ? FontWeights.Bold : FontWeights.Normal);

        public static readonly IValueConverter CollapseStringEmpty =
            ConverterFactory.New((string s) => s == "" ? null : s);

        public static readonly IValueConverter BoolToVisibility =
            ConverterFactory.New((bool b) => b ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IValueConverter NotBoolToVisibility =
            ConverterFactory.New((bool b) => b ? Visibility.Collapsed : Visibility.Visible);

        public static readonly IValueConverter ZeroToVisibility =
            ConverterFactory.New((int count) => count == 0 ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IValueConverter NotZeroToVisibility =
            ConverterFactory.New((int count) => count == 0 ? Visibility.Collapsed : Visibility.Visible);

        public static readonly IValueConverter NullToVisibility =
          ConverterFactory.New((object o) => o != null ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IValueConverter NotNullToVisibility =
           ConverterFactory.New((object o) => o != null ? Visibility.Collapsed : Visibility.Visible);

        public static readonly IValueConverter IsNull =
            ConverterFactory.New((object o) => o == null);

        public static readonly IValueConverter IsNotNull =
            ConverterFactory.New((object o) => o != null);

        public static readonly IValueConverter ToInt =
            ConverterFactory.New((int? val) => val?.ToString(), (string str) => str.ToInt());

        public static readonly IValueConverter BoolToSelectionMode =
            ConverterFactory.New((bool b) => b ? SelectionMode.Extended : SelectionMode.Single);

        public static readonly IValueConverter Not = ConverterFactory.New((bool b) => !b, (bool b) => !b);

        public static readonly IValueConverter TypeContextName =
            ConverterFactory.New((FrameworkElement b) => b?.Let(fe => Common.GetPropertyRoute(fe))?.Type?.NiceName() ?? "??");

        public static readonly IValueConverter NiceName =
            ConverterFactory.New((Type type) => type?.NiceName() ?? "??");

        public static readonly IValueConverter TypeImage =
            ConverterFactory.New((Type type) => type?.Let(t => Navigator.Manager.GetEntityIcon(type, true)));

        public static readonly IValueConverter ThicknessToCornerRadius =
            ConverterFactory.New((Thickness b) => new CornerRadius
            {
                BottomLeft = 2 * Math.Max(b.Bottom, b.Left),
                BottomRight = 2 * Math.Max(b.Bottom, b.Right),
                TopLeft = 2 * Math.Max(b.Top, b.Left),
                TopRight = 2 * Math.Max(b.Top, b.Right)
            });

        public static readonly IValueConverter ToStringConverter = ConverterFactory.New(
            (object d) => d?.ToString());

        public static readonly IValueConverter TokenOperations = ConverterFactory.New(
            (QueryToken token) => token == null ? null : QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(token.Type)));

        public static readonly IValueConverter Color = ConverterFactory.New(
            (ColorEmbedded c) => c == null ? null : (Color?)System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B),
            (Color? c) => c == null ? null : ColorEmbedded.FromARGB(c.Value.A, c.Value.R, c.Value.G, c.Value.B));

        public static readonly IMultiValueConverter And = ConverterFactory.New(
            (bool a, bool b) => a && b);

        public static readonly IMultiValueConverter AndToVisibility = ConverterFactory.New(
            (bool a, bool b) => a && b ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IMultiValueConverter Or = ConverterFactory.New(
            (bool a, bool b) => a || b);

        public static readonly IMultiValueConverter OrToVisibility = ConverterFactory.New(
            (bool a, bool b) => a || b ? Visibility.Visible : Visibility.Collapsed);

        public static readonly IValueConverter LabelCount = ConverterFactory.New(
            (ResultRow r) => "{0} ({1})".FormatWith(r[0] is Enum ? ((Enum)r[0]).NiceToString() : r[0], r[1]));


        public static readonly IValueConverter DirtyOpacity = ConverterFactory.New(
            (bool isDirty) => isDirty ? .5 : 1);
    }

    public static class ColorExtensions
    {
        public static readonly IValueConverter ToDarkColor = ConverterFactory.New(
                (Color color) =>new SolidColorBrush(Lerp(color, 0.8f, Colors.Black)));

        public static readonly IValueConverter ToLightColor = ConverterFactory.New(
                (Color color) =>new SolidColorBrush( Lerp(color, 0.8f, Colors.White)));

        public static Color Lerp(Color a, float coef, Color b)
        {
            return Color.FromScRgb(
                (1 - coef) * a.ScA + coef * b.ScA,
                (1 - coef) * a.ScR + coef * b.ScR,
                (1 - coef) * a.ScG + coef * b.ScG,
                (1 - coef) * a.ScB + coef * b.ScB);

        }

        public static Color Alpha(this Color color, float alpha)
        {
            return Color.FromScRgb(alpha, color.ScR, color.ScG, color.ScB);
        }

        public static Color Lerp(Color a, float coef, Color b, float alpha)
        {
            return Color.FromScRgb(
                alpha,
                (1 - coef) * a.ScR + coef * b.ScR,
                (1 - coef) * a.ScG + coef * b.ScG,
                (1 - coef) * a.ScB + coef * b.ScB);
        }
    }

}
