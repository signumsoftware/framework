using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Omnibox;
using Signum.Entities.DynamicQuery;
using System.Windows;
using Signum.Utilities;
using Signum.Entities;
using System.Windows.Documents;
using System.Windows.Media;
using Signum.Entities.Basics;
using Signum.Entities.UserAssets;

namespace Signum.Windows.Omnibox
{
    public class DynamicQueryOmniboxProvider : OmniboxProvider<DynamicQueryOmniboxResult>
    {
        public override OmniboxResultGenerator<DynamicQueryOmniboxResult> CreateGenerator()
        {
            return new DynamicQueryOmniboxResultGenerator();
        }

        public override void OnSelected(DynamicQueryOmniboxResult r, Window window)
        {
            Finder.Explore(new ExploreOptions(r.QueryNameMatch.Value)
            {
                FilterOptions = r.Filters.Select(f =>
                {
                    FilterType ft = QueryUtils.GetFilterType(f.QueryToken.Type);

                    var operation = f.Operation;
                    if (operation != null && !QueryUtils.GetFilterOperations(ft).Contains(f.Operation.Value))
                    {
                        MessageBox.Show(window, "Operation {0} not compatible with {1}".FormatWith(operation, f.QueryToken.ToString()));
                        operation = FilterOperation.EqualTo;
                    }

                    object value = f.Value;
                    if (value == DynamicQueryOmniboxResultGenerator.UnknownValue)
                    {
                        MessageBox.Show(window, "Unknown value for {0}".FormatWith(f.QueryToken.ToString()));
                        value = null;
                    }
                    else
                    {
                        if (value is Lite<IEntity>)
                            Server.FillToStr((Lite<IEntity>)value);
                    }

                    return new FilterOption
                    {
                        Token = f.QueryToken,
                        Operation = operation ?? FilterOperation.EqualTo,
                        Value = value,
                    };
                }).ToList(),
                SearchOnLoad = r.Filters.Any(),
            });
        }

        public override void RenderLines(DynamicQueryOmniboxResult result, InlineCollection lines)
        {
            lines.AddMatch(result.QueryNameMatch);


            foreach (var item in result.Filters)
            {
                lines.Add(" ");

                QueryToken last = null;
                if (item.QueryTokenMatches != null)
                {
                    foreach (var tokenMatch in item.QueryTokenMatches)
                    {
                        if (last != null)
                            lines.Add(".");

                        lines.AddMatch(tokenMatch);

                        last = (QueryToken)tokenMatch.Value;
                    }
                }

                if (item.QueryToken != last)
                {
                    if (last != null)
                        lines.Add(".");

                    lines.Add(new Run(item.QueryToken.Key) { Foreground = Brushes.Gray });
                }

                if (item.CanFilter.HasText())
                {
                    lines.Add(new Run(item.CanFilter) { Foreground = Brushes.Red });
                }
                else if (item.Operation != null)
                {
                    lines.Add(new Bold(new Run(FilterValueConverter.ToStringOperation(item.Operation.Value))));

                    if (item.Value == DynamicQueryOmniboxResultGenerator.UnknownValue)
                        lines.Add(new Run(OmniboxMessage.Unknown.NiceToString()) { Foreground = Brushes.Red });
                    else if (item.ValuePack != null)
                        lines.AddMatch(item.ValuePack);
                    else if (item.Syntax != null && item.Syntax.Completion == FilterSyntaxCompletion.Complete)
                        lines.Add(new Bold(new Run(DynamicQueryOmniboxResultGenerator.ToStringValue(item.Value))));
                    else
                        lines.Add(new Run(DynamicQueryOmniboxResultGenerator.ToStringValue(item.Value)) { Foreground = Brushes.Gray });
                }
            }

        }

        public override Run GetIcon()
        {
            return new Run("({0})".FormatWith(typeof(QueryEntity).NiceName())) { Foreground = Brushes.Orange };
        }

        public override string GetName(DynamicQueryOmniboxResult result)
        {
            return "Q:" + QueryUtils.GetQueryUniqueKey(result.QueryName);
        }
    }
}
