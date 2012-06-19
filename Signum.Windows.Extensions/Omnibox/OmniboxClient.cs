using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Omnibox;
using Signum.Windows.Authorization;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using System.Windows.Documents;
using System.Windows;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Windows.Omnibox
{
    public class OmniboxClient
    {
        public static Polymorphic<Action<OmniboxResult>> OnResultSelected = new Polymorphic<Action<OmniboxResult>>();

        public static List<DataTemplate> Templates = new List<DataTemplate>();

        public static void Start(bool entities, bool dynamicQueries, params OmniboxProvider[] providers)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (entities)
                {
                    OmniboxParser.Providers.Add(new WindowEntityOmniboxProvider());
                    RegisterTemplate(typeof(EntityOmniboxResult), () => new EntityOmniboxTemplate());
                    OnResultSelected.Register((EntityOmniboxResult r) =>
                    {
                        if (r.Lite != null)
                            Navigator.NavigateUntyped(r.Lite);
                    });
                }

                if (dynamicQueries)
                {
                    OmniboxParser.Providers.Add(new WindowDynamicQueryOmniboxProvider());
                    RegisterTemplate(typeof(DynamicQueryOmniboxResult), () => new DynamicQueryOmniboxTemplate());
                    OnResultSelected.Register((DynamicQueryOmniboxResult r) =>
                    {
                        Navigator.Explore(new ExploreOptions(r.QueryNameMatch.Value)
                        {
                            FilterOptions = r.Filters.Select(f => 
                            {
                                FilterType ft = QueryUtils.GetFilterType(f.QueryToken.Type);

                                var operation = f.Operation;
                                if (operation != null && !QueryUtils.GetFilterOperations(ft).Contains(f.Operation.Value))
                                {
                                    MessageBox.Show("Operation {0} not compatible with {1}".Formato(operation, f.QueryToken.ToString()));
                                    operation = FilterOperation.EqualTo;
                                }

                                object value = f.Value;
                                if (value == WindowDynamicQueryOmniboxProvider.UnknownValue)
                                {
                                    MessageBox.Show("Unknown value for {0}".Formato(f.QueryToken.ToString()));
                                    value = null;
                                }
                                else
                                {
                                    if(value is Lite)
                                        Server.FillToStr((Lite)value);
                                }

                                return new FilterOption
                                {
                                    Token = f.QueryToken,
                                    Operation = operation ?? FilterOperation.EqualTo,
                                    Value = value,
                                };
                            }).ToList(),
                            SearchOnLoad = true,
                        });
                    });
                }

                OmniboxParser.Providers.AddRange(providers);
            }
        }

        internal static IEnumerable<Inline> PackInlines(OmniboxMatch distancePack)
        {
            return distancePack.BoldSpans().Select(t =>
                t.Item2 ? (Inline)new Bold(new Run(t.Item1)) : new Run(t.Item1));
        }

        public static void RegisterTemplate(Type dataType, Expression<Func<FrameworkElement>> controlFactory)
        {
            DataTemplate dt = Fluent.GetDataTemplate(controlFactory);
            dt.DataType = dataType;
            Templates.Add(dt);
        }

    }

    public class WindowDynamicQueryOmniboxProvider : DynamicQueryOmniboxProvider
    {
        public WindowDynamicQueryOmniboxProvider()
            : base(QueryClient.queryNames.Values)
        {
        }

        protected override bool Allowed(object queryName)
        {
            return Navigator.IsFindable(queryName);
        }

        protected override QueryDescription GetDescription(object queryName)
        {
            return Navigator.Manager.GetQueryDescription(queryName); 
        }

        protected override List<Lite> AutoComplete(Type cleanType, Implementations implementations, string subString)
        {
            return Server.Return((IBaseServer bs) => bs.FindLiteLike(cleanType, implementations, subString, 5));
        }
    }

    public class WindowEntityOmniboxProvider : EntityOmniboxProvider
    {
        public WindowEntityOmniboxProvider()
            : base(Server.ServerTypes.Keys)
        {
        }

        protected override bool Allowed(Type type)
        {
            return Navigator.IsViewable(type, true);
        }

        protected override Lite RetrieveLite(Type type, int id)
        {
            if (!Server.Return((IBaseServer bs) => bs.Exists(type, id)))
                return null;
            return Server.FillToStr(Lite.Create(type, id));
        }

        protected override List<Lite> AutoComplete(Type type, string subString)
        {
            if (string.IsNullOrEmpty(subString))
                return new List<Lite>();

            return Server.Return((IBaseServer bs) => bs.FindLiteLike(type, null, subString, 5));
        }
    }
}
