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
using System.Collections;
using System.Collections.ObjectModel;
using Signum.Utilities.Reflection;

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
            Remove?.Invoke(this, e);
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            FilterOption f = (FilterOption)valueContainer.DataContext;

            Common.SetIsReadOnly(valueContainer, f.Frozen);
      
            RecreateControls();
        }

        private void ComboBoxOperation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterOption f = DataContext as FilterOption;
            if (f == null)
                return;

            var newValue = e.AddedItems.Cast<FilterOperation?>().SingleOrDefault();

            if (newValue.HasValue && newValue.Value.IsList())
            {
                if (!(f.Value is IList))
                {
                    valueContainer.Children.Clear();
                    var list = (IList)Activator.CreateInstance(typeof(ObservableCollection<>).MakeGenericType(f.Token.Type.Nullify()));
                    list.Add(f.Value);
                    f.Value = list; 
                    RecreateControls();
                }
            }
            else
            {
                if (f.Value is IList)
                {
                    valueContainer.Children.Clear();
                    f.Value = ((IList)f.Value)[0];
                    RecreateControls();
                }
            }
        }

        private void RecreateControls()
        {
            FilterOption f = (FilterOption)DataContext;

            valueContainer.Children.Clear();

            if (!f.Operation.IsList())
            {
                FillLite(f.RealValue as Lite<IEntity>);

                valueContainer.Children.Add(GetValueControl(f.Token, "RealValue"));
            }
            else
            {
                StackPanel sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
                var list = (IList)f.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    FillLite(list[i] as Lite<IEntity>);

                    sp.Children.Add(new DockPanel
                    {
                        LastChildFill = true,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Children =
                        {
                            (i == 0? 
                            new Button { Style = (Style)FindResource("RoundButton"), Focusable = false, Content = FindResource("Create")}.Handle(Button.ClickEvent, CreateItem):
                            new Button { Style = (Style)FindResource("RoundButton"), Focusable = false, Content = FindResource("Remove")}.Handle(Button.ClickEvent, RemoveItem)
                            ).Set(DockPanel.DockProperty, Dock.Left),
                            GetValueControl(f.Token, "RealValue[" + i + "]"),
                        }
                    });
                }
                valueContainer.Children.Add(sp);
            }
        }

        private static void FillLite(Lite<IEntity> lite)
        {
            if (lite != null && string.IsNullOrEmpty(lite.ToString()))
                Server.FillToStr(lite);
        }

        void RemoveItem(object sender, RoutedEventArgs args)
        {
            Button b = (Button)sender;
            DockPanel line = (DockPanel)b.Parent;
            StackPanel sp = (StackPanel)line.Parent;

            int index = sp.Children.IndexOf(line);

            FilterOption f = (FilterOption)DataContext;
            var list = (IList)f.Value;
            list.RemoveAt(index);

            RecreateControls();
        }

        void CreateItem(object sender, RoutedEventArgs args)
        {
            FilterOption f = (FilterOption)DataContext;
            var list = (IList)f.Value;
            list.Add(null);

            RecreateControls();
        }


        public static Control GetValueControl(QueryToken token, string bindingPath)
        {
            Type type = token.Type;
            if (type.IsLite())
            {
                Implementations implementations = token.GetImplementations().Value;

                Type cleanType = Lite.Extract(type);

                if (EntityKindCache.IsLowPopulation(cleanType) && !implementations.IsByAll)
                {
                    EntityCombo ec = new EntityCombo
                    {
                        Type = type,
                        Implementations = implementations,
                    };

                    ec.SetBinding(EntityCombo.EntityProperty, new Binding
                    {
                        Path = new PropertyPath(bindingPath),
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
                        Implementations = implementations,
                    };

                    el.SetBinding(EntityLine.EntityProperty, new Binding
                    {
                        Path = new PropertyPath(bindingPath),
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
                    Autocomplete = false,
                    Find = false,
                    Implementations = null,
                };

                el.SetBinding(EntityLine.EntityProperty, new Binding
                {
                    Path = new PropertyPath(bindingPath),
                    NotifyOnValidationError = true,
                    ValidatesOnDataErrors = true,
                    ValidatesOnExceptions = true
                });

                return el;
            }
            else
            {
                ValueLine vl = new ValueLine()
                {
                    Type = type,
                    Format = token.Format,
                    UnitText = token.Unit,
                };

                if (type.UnNullify().IsEnum)
                {
                    vl.ItemSource = EnumEntity.GetValues(type.UnNullify()).PreAndNull(type.IsNullable()).ToObservableCollection();
                }

                vl.SetBinding(ValueLine.ValueProperty, new Binding
                {
                    Path = new PropertyPath(bindingPath), //odd
                    NotifyOnValidationError = true,
                    ValidatesOnDataErrors = true,
                    ValidatesOnExceptions = true,
                    Converter = ReflectionTools.IsNumber(type) ? Converters.Identity : null,
                });

                return vl;
            }
        }

      
    }
}
