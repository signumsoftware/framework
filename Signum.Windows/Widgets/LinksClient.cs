using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Windows
{

    public static class LinksClient
    {
        public static Polymorphic<Func<Lite<Entity>, Control, QuickLink[]>> EntityLinks =
            new Polymorphic<Func<Lite<Entity>, Control, QuickLink[]>>(
                merger: (currentVal, baseVal, interfaces) => currentVal.Value + baseVal.Value,
                minimumType: typeof(Entity));

        public static void RegisterEntityLinks<T>(Func<Lite<T>, Control, QuickLink[]> getQuickLinks)
            where T : Entity
        {
            var current = EntityLinks.GetDefinition(typeof(T));

            current += (t, p0) => getQuickLinks((Lite<T>)t, p0);

            EntityLinks.SetDefinition(typeof(T), current);
        }

        public static ObservableCollection<QuickLink> GetForEntity(Lite<Entity> ident, Control control)
        {
            ObservableCollection<QuickLink> links = new ObservableCollection<QuickLink>();

            var func = EntityLinks.TryGetValue(ident.EntityType);
            if (func != null)
            {
                foreach (var item in func.GetInvocationListTyped())
                {
                    var array = item(ident, control);
                    if (array != null)
                        links.AddRange(array.NotNull().Where(l => l.IsVisible));
                }
            }

            return links;
        }

        public static void Start(bool widget, bool contextualMenu)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (widget)
                    WidgetPanel.GetWidgets += (obj, mainControl) => new LinksWidget() { Control = mainControl };

                if (contextualMenu)
                    SearchControl.GetContextMenuItems += SearchControl_GetContextMenuItems;
            }
        }

        internal static IEnumerable<MenuItem> SearchControl_GetContextMenuItems(SearchControl sc)
        {
            if (sc.SelectedItems == null || sc.SelectedItems.Count != 1)
                return null;

            return from ql in LinksClient.GetForEntity(sc.SelectedItem.Clone(), sc).NotNull()
                   where ql.IsVisible
                   select GetMenuItem(ql);
        }

        static MenuItem GetMenuItem(QuickLink ql)
        {
            var mi = new MenuItem
            {
                DataContext = ql,
                Header = ql.Label,
                Icon = ql.Icon.ToSmallImage(),
            }
            .Set(AutomationProperties.NameProperty, ql.Name)
            .Bind(MenuItem.HeaderProperty, "Label");

            if (ql.ToolTip.HasText())
            {
                mi.ToolTip = ql.ToolTip;
                ToolTipService.SetShowOnDisabled(mi, true);
                AutomationProperties.SetHelpText(mi, ql.ToolTip);
            }

            mi.Click += (sender, args) => ql.Execute();

            return mi;
        }
    }


    /// <summary>
    /// Represents an item of the left navigation panel
    /// </summary>
    public abstract class QuickLink : INotifyPropertyChanged // http://www.benbarefield.com/blog/?p=59
    {
        protected QuickLink() { }

        string label;
        public string Label
        {
            get { return label; }

            set
            {
                label = value;
                RaisePropertyChanged("Label");
            }
        }

        public abstract string Name { get; }

        public bool IsVisible { get; set; }

        public bool IsShy { get; set; }

        public string ToolTip { get; set; }

        public ImageSource Icon { get; set; }

        public abstract void Execute();


        void Never() { PropertyChanged(null, null); }
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }

    public class QuickLinkAction : QuickLink
    {
        string name; 
        Action action;
        public QuickLinkAction(Enum enumValue, Action action)
            : this(enumValue.ToString(), enumValue.NiceToString(), action)
        {

        }
        
        public QuickLinkAction(string name, string label, Action action)
        {
            this.name = name;
            this.Label = label;
            this.action = action;
            this.IsVisible = true;
        }

        public override void Execute()
        {
            action();
        }


        public override string Name
        {
            get { return name; }
        }
    }

    public class QuickLinkExplore : QuickLink
    {
        public ExploreOptions Options { get; set; }
        public bool ShowResultCount { get; set; }

        public QuickLinkExplore(object queryName, string columnName, Func<object> valueFactory) :
            this(queryName, columnName, (object)valueFactory)
        {
        }

        public QuickLinkExplore(object queryName, string columnName, object value) :
            this(new ExploreOptions(queryName)
            {
                ShowFilters = false,
                SearchOnLoad = true,
                ColumnOptionsMode = ColumnOptionsMode.Remove ,
                ColumnOptions = { new ColumnOption(columnName) } ,

                FilterOptions = { new FilterOption(columnName, value) }
            })
        {
        }

        public QuickLinkExplore(ExploreOptions options)
        {
            Options = options;
            Label = QueryUtils.GetNiceName(Options.QueryName);
            //Icon = Navigator.Manager.GetFindIcon(Options.QueryName, false);
            IsVisible = Finder.IsFindable(Options.QueryName);

            if (ShowResultCount && IsVisible)
            {
                EvaluateFunValues();

                DynamicQueryServer.QueryCountBatch(new QueryCountOptions(Options.QueryName)
                {
                    FilterOptions = options.FilterOptions,
                }, count =>
                {
                    Label = "{0} ({1})".FormatWith(Label, count);
                }, () => { });
            }
        }

        public override void Execute()
        {
            EvaluateFunValues();

            Finder.Explore(Options);
        }

        private void EvaluateFunValues()
        {
            foreach (var item in Options.FilterOptions)
            {
                if (item.Value is Func<object>)
                    item.Value = ((Func<object>)item.Value)();
            }
        }

        public override string Name
        {
            get { return QueryUtils.GetKey(Options.QueryName); }
        }
    }

    public class QuickLinkNavigate<T> : QuickLink
        where T : Entity
    {
        public NavigateOptions NavigateOptions { get; set; }

        public UniqueOptions FindUniqueOptions { get; set; }


        public QuickLinkNavigate(string columnName, Func<object> valueFactory, UniqueType unique = UniqueType.Single, object queryName = null) :
            this(columnName, (object)valueFactory, unique, queryName)
        {
        }

        public QuickLinkNavigate(string columnName, object value, UniqueType unique = UniqueType.Single, object queryName = null) :
            this(new UniqueOptions(queryName ?? typeof(T))
            {
                UniqueType = unique,
                FilterOptions = new List<FilterOption>()
                 {
                     new FilterOption(columnName, value)
                 }
            })
        {
        }

        public QuickLinkNavigate(UniqueOptions options)
        {
            FindUniqueOptions = options;
            Label = typeof(T).NiceName();
            //Icon = Navigator.Manager.GetEntityIcon(typeof(T), false);
            IsVisible = Finder.IsFindable(FindUniqueOptions.QueryName) && Navigator.IsNavigable(typeof(T), isSearch: true);
        }

        public override void Execute()
        {
            EvaluateFunValues();

            Lite<T> lite = DynamicQueryServer.QueryUnique<T>(FindUniqueOptions);

            if (lite == null)
            {
                MessageBox.Show(QuickLinkMessage.No0Found.NiceToString().ForGenderAndNumber(typeof(T).GetGender()).FormatWith(typeof(T).NiceName()));
                return;
            }

            if (NavigateOptions != null)
                Navigator.Navigate(lite, NavigateOptions);
            else
                Navigator.Navigate(lite);
        }

        private void EvaluateFunValues()
        {
            foreach (var item in FindUniqueOptions.FilterOptions)
            {
                if (item.Value is Func<object>)
                    item.Value = ((Func<object>)item.Value)();
            }
        }

        public override string Name
        {
            get { return typeof(T).FullName; }
        }
    }

    
    

}
