using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Files;
using Signum.Engine.Mailing;
using System.Web.UI;
using System.IO;
using Signum.Entities.Mailing;
using System.Web.Routing;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;
using Signum.Web.Operations;
using Signum.Web.UserQueries;
using System.Text.RegularExpressions;
using Signum.Entities.UserAssets;
using Signum.Web.UserAssets;
using Signum.Web.Basic;
using Signum.Entities.Processes;
using Signum.Web.Cultures;
using Signum.Web.Templating;
using Signum.Web.Omnibox;
using Signum.Entities.Omnibox;
using Signum.Entities.Map;
using Signum.Entities.Authorization;

namespace Signum.Web.Maps
{
    public static class MapClient
    {
        public static string ViewPrefix = "~/Maps/Views/{0}.cshtml";
        public static JsModule SchemaModule = new JsModule("Extensions/Signum.Web.Extensions/Maps/Scripts/SchemaMap");
        public static JsModule OperationModule = new JsModule("Extensions/Signum.Web.Extensions/Maps/Scripts/OperationMap");
        public static JsModule ColorModule = new JsModule("Extensions/Signum.Web.Extensions/Maps/Scripts/MapColors");
        public static readonly object NodesConstant = new object();

        public static Func<MapColorProvider[]> GetColorProviders;

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(MapClient));


                GetColorProviders += () => new[]
                {
                    new MapColorProvider
                    { 
                        Name = "namespace", 
                        NiceName = MapMessage.Namespace.NiceToString(), 
                        GetJsProvider =  ColorModule["namespace"](NodesConstant) 
                    },
                };

                GetColorProviders += () => new[]
                {
                    new MapColorProvider
                    { 
                        Name = "entityKind", 
                        NiceName = typeof(EntityKind).Name, 
                        GetJsProvider =  ColorModule["entityKind"](NodesConstant) 
                    }
                };

                GetColorProviders += () => new[]
                {
                    new MapColorProvider
                    { 
                        Name = "columns", 
                        NiceName = MapMessage.Columns.NiceToString(), 
                        GetJsProvider =  ColorModule["columns"](NodesConstant, MapMessage.Columns.NiceToString()) 
                    }
                };

                GetColorProviders += () => new[]
                {
                    new MapColorProvider
                    { 
                        Name = "entityData", 
                        NiceName = typeof(EntityData).Name, 
                        GetJsProvider =  ColorModule["entityData"](NodesConstant) 
                    }
                };

                GetColorProviders += () => new[]
                {
                    new MapColorProvider
                    { 
                        Name = "rows", 
                        NiceName = MapMessage.Rows.NiceToString(), 
                        GetJsProvider =  ColorModule["rows"](NodesConstant, MapMessage.Rows.NiceToString()) 
                    }
                };

                GetColorProviders += () => new[]
                {
                    new MapColorProvider
                    { 
                        Name = "tableSize", 
                        NiceName = MapMessage.TableSize.NiceToString(), 
                        GetJsProvider =  ColorModule["tableSize"](NodesConstant) 
                    },
                };
            }
        }
    }

    public class MapColorProvider
    {
        public string Name;
        public JsFunction GetJsProvider;
        public string NiceName; 
        public Action<TableInfo> AddExtra;
        public MvcHtmlString Defs;

        public decimal Order { get; set; }
    }

    public class MapOmniboxProvider : OmniboxClient.OmniboxProvider<MapOmniboxResult>
    {
        public override OmniboxResultGenerator<MapOmniboxResult> CreateGenerator()
        {
            return new MapOmniboxResultGenerator();
        }

        public override MvcHtmlString RenderHtml(MapOmniboxResult result)
        {
            MvcHtmlString html = result.KeywordMatch.ToHtml();

            if (result.TypeMatch != null)
                html = html.Concat(" {0}".FormatHtml(result.TypeMatch.ToHtml()));
            
            html = Icon().Concat(html);

            return html;
        }

        public override string GetUrl(MapOmniboxResult result)
        {
            if (result.TypeMatch != null)
                return RouteHelper.New().Action("Operation", "Map", new { typeName = Finder.ResolveWebQueryName(result.Type) });

            return RouteHelper.New().Action("Index", "Map");
        }

        public override MvcHtmlString Icon()
        {
            return ColoredGlyphicon("glyphicon-map-marker", "green");
        }
    }

    public class MapOmniboxResultGenerator : OmniboxResultGenerator<MapOmniboxResult>
    {
        public Func<string> NiceName = () => MapMessage.Map.NiceToString();

        Regex regex = new Regex(@"^II?$", RegexOptions.ExplicitCapture);
        public override IEnumerable<MapOmniboxResult> GetResults(string rawQuery, List<OmniboxToken> tokens, string tokenPattern)
        {
            if (!OmniboxParser.Manager.AllowedPermission(MapPermission.ViewMap))
                yield break;

            Match m = regex.Match(tokenPattern);

            if (!m.Success)
                yield break;

            string key = tokens[0].Value;

            var keyMatch = OmniboxUtils.Contains(NiceName(), NiceName(), key);

            if (keyMatch == null)
                yield break;

            if (tokens.Count == 1)
                yield return new MapOmniboxResult { Distance = keyMatch.Distance, KeywordMatch = keyMatch };

            else
            {
                string pattern = tokens[1].Value;

                bool isPascalCase = OmniboxUtils.IsPascalCasePattern(pattern);

                var types = OmniboxParser.Manager.Types().Where(a => OperationLogic.TypeOperations(a.Value).Any()).ToDictionary();

                foreach (var match in OmniboxUtils.Matches(types, OmniboxParser.Manager.AllowedType, pattern, isPascalCase).OrderBy(ma => ma.Distance))
                {
                    var type = match.Value;
                    if (OmniboxParser.Manager.AllowedQuery(type))
                    {
                        yield return new MapOmniboxResult { Distance = keyMatch.Distance + match.Distance, KeywordMatch = keyMatch, Type = (Type)type, TypeMatch = match };
                    }
                }
            }
        }

        public override List<HelpOmniboxResult> GetHelp()
        {
            var resultType = typeof(MapOmniboxResult);
            return new List<HelpOmniboxResult>
            {
                new HelpOmniboxResult 
                { 
                    Text =  MapMessage.Map.NiceToString() + " " + typeof(TypeEntity).NiceName(), 
                    OmniboxResultType = resultType 
                }
            };
        }
    }

    public class MapOmniboxResult : OmniboxResult
    {
        public OmniboxMatch KeywordMatch { get; set; }

        public Type Type { get; set; }
        public OmniboxMatch TypeMatch { get; set; }

        public override string ToString()
        {
            if (Type == null)
                return KeywordMatch.Value.ToString();

            return "{0} {1}".FormatWith(KeywordMatch.Value, Type.NicePluralName().ToOmniboxPascal());
        }
    }

  
  
}
