using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Utilities;

namespace Signum.Windows
{
    /// <summary>
    /// Interaction logic for FilterLine.xaml
    /// </summary>
    public partial class FilterLine : UserControl
    {
        public event EventHandler Remove; 

        public FilterLine()
        {
            InitializeComponent();
        }

        private void btRemove_Click(object sender, RoutedEventArgs e)
        {
            if (Remove != null)
                Remove(this, e);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Grid g = (Grid)sender;

            FilterOption f = (FilterOption)g.DataContext;

            Common.SetIsReadOnly(g, f.Frozen);

            g.Children.Add(GetValueControl(f));
        }

        public static Control GetValueControl(FilterOption f)
        {
            Type type = f.Token.Type;
            if (type.IsLite())
            {
                Implementations implementations = f.Token.GetImplementations().Value;

                Lite<IIdentifiable> lite = f.RealValue as Lite<IIdentifiable>;

                if (lite != null && string.IsNullOrEmpty(lite.ToString()))
                    Server.FillToStr(lite);

                Type cleanType = Lite.Extract(type);

                if (Reflector.IsLowPopulation(cleanType) && !implementations.IsByAll)
                {
                    EntityCombo ec = new EntityCombo
                    {
                        Type = type,
                        Implementations = implementations
                    };

                    ec.SetBinding(EntityCombo.EntityProperty, new Binding
                    {
                        Path = new PropertyPath(FilterOption.RealValueProperty),
                        NotifyOnValidationError = true,
                        ValidatesOnDataErrors = true,
                        ValidatesOnExceptions = true,
                    });

                    return ec;
                }
                else
                {
                    EntityLine el = new EntityLine
                    {
                        Type = type,
                        Create = false,
                        HideAutoCompleteOnLostFocus = false,
                        Implementations = implementations
                    };

                    el.SetBinding(EntityLine.EntityProperty, new Binding
                    {
                        Path = new PropertyPath(FilterOption.RealValueProperty),
                        NotifyOnValidationError = true,
                        ValidatesOnDataErrors = true,
                        ValidatesOnExceptions = true
                    });

                    return el;
                }
            }
            else if (type.IsEmbeddedEntity())
            {
                EntityLine el = new EntityLine
                {
                    Type = type,
                    Create = false,
                    AutoComplete = false,
                    Find = false,
                    HideAutoCompleteOnLostFocus = false,
                    Implementations = null
                };

                el.SetBinding(EntityLine.EntityProperty, new Binding
                {
                    Path = new PropertyPath(FilterOption.RealValueProperty),
                    NotifyOnValidationError = true,
                    ValidatesOnDataErrors = true,
                    ValidatesOnExceptions = true
                });

                return el;
            }
            else
            {
                QueryToken token = f.Token;

                ValueLine vl = new ValueLine()
                {
                    Type = type,
                    Format = token.Format,
                    UnitText = token.Unit
                };

                if (type.UnNullify().IsEnum)
                {
                    vl.ItemSource = EnumEntity.GetValues(type.UnNullify()).PreAndNull(type.IsNullable()).ToObservableCollection();
                }

                vl.SetBinding(ValueLine.ValueProperty, new Binding
                {
                    Path = new PropertyPath("RealValue"), //odd
                    NotifyOnValidationError = true,
                    ValidatesOnDataErrors = true,
                    ValidatesOnExceptions = true,
                    Converter = Reflector.IsNumber(type) ? Converters.Identity : null,
                });

                return vl;
            }
        }
    }
}
