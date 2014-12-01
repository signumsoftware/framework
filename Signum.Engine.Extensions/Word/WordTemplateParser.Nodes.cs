using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Translation;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.UserAssets;
using Signum.Entities.Word;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signum.Engine.Word
{
    public class MatchNode : Run
    {
        public Match Match;

        public MatchNode(Match match)
        {
            this.Match = match;
        }
    }

    public class TokenNode : Run
    {
        public readonly bool IsRaw;

        public readonly ParsedToken Token;
        public readonly QueryToken EntityToken;
        public readonly string Format;
        public readonly PropertyRoute Route;

        internal TokenNode(ParsedToken token, string format, bool isRaw, WordTemplateParser parser)
        {
            this.Token = token;
            this.Format = format;
            this.IsRaw = isRaw;

            if (token.QueryToken != null && IsTranslateInstanceCanditate(token.QueryToken))
            {
                Route = token.QueryToken.GetPropertyRoute();
                string error = DeterminEntityToken(token.QueryToken, out EntityToken);
                if (error != null)
                    parser.AddError(false, error);
            }
        }

        static bool IsTranslateInstanceCanditate(QueryToken token)
        {
            if (token.Type != typeof(string))
                return false;

            var pr = token.GetPropertyRoute();
            if (pr == null)
                return false;

            if (TranslatedInstanceLogic.RouteType(pr) == null)
                return false;

            return true;
        }

        string DeterminEntityToken(QueryToken token, out QueryToken entityToken)
        {
            entityToken = token.Follow(a => a.Parent).FirstOrDefault(a => a.Type.IsLite() || a.Type.IsIEntity());

            if (entityToken == null)
                entityToken = QueryUtils.Parse("Entity", DynamicQueryManager.Current.QueryDescription(token.QueryName), 0);

            if (entityToken.Type.IsAssignableFrom(Route.RootType))
                return "The entity of {0} ({1}) is not compatible with the property route {2}".Formato(token.FullKey(), entityToken.FullKey(), Route.RootType.NiceName());

            return null;
        }
    }

    public class DeclareNode : Run
    {
        public readonly ParsedToken Token;

        internal DeclareNode(ParsedToken token, WordTemplateParser walker)
        {
            if (!token.Variable.HasText())
                walker.AddError(true, "declare[{0}] should end with 'as $someVariable'".Formato(token));

            this.Token = token;
        }
    }

    public class ModelNode : Run
    {
        public bool IsRaw { get; set; }

        string fieldOrPropertyChain;
        List<MemberInfo> members;

        public const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal ModelNode(string fieldOrPropertyChain, WordTemplateParser walker)
        {
            if (walker.ModelType == null)
            {
                walker.AddError(false, WordTemplateMessage.ModelShouldBeSetToUseModel0.NiceToString().Formato(fieldOrPropertyChain));
                return;
            }

            this.fieldOrPropertyChain = fieldOrPropertyChain;

            members = new List<MemberInfo>();
            var type = walker.ModelType;
            foreach (var field in fieldOrPropertyChain.Split('.'))
            {
                var info = (MemberInfo)type.GetField(field, flags) ??
                           (MemberInfo)type.GetProperty(field, flags);

                if (info == null)
                {
                    walker.AddError(false, WordTemplateMessage.Type0DoesNotHaveAPropertyWithName1.NiceToString().Formato(type.Name, field));
                    members = null;
                    break;
                }

                members.Add(info);

                type = info.ReturningType();
            }
        }
    }

    public class BlockNode : Paragraph
    {
        public static string UserString(Type type)
        {
            if (type == typeof(ForeachNode))
                return "foreach";

            if (type == typeof(IfNode))
                return "if";

            if (type == typeof(AnyNode))
                return "any";

            return "block";
        }
    }

    public class ForeachNode : BlockNode
    {
        public readonly ParsedToken Token;

        public MatchNode ForeachToken;
        public MatchNode EndForeachToken;

        public ForeachNode(ParsedToken token)
        {
            this.Token = token;
        }

        internal void ReplaceChild()
        {
            throw new NotImplementedException();
        }
    }

    public class AnyNode : BlockNode
    {
        public readonly ParsedToken Token;
        public readonly FilterOperation? Operation;
        public string Value;

        public MatchNode AnyToken;
        public MatchNode NotAnyToken;
        public MatchNode EndAnyToken;

        public OpenXmlElement AnyBlock;
        public OpenXmlElement NotAnyBlock;

        public AnyNode(ParsedToken token, WordTemplateParser parser)
        {
            if (token.QueryToken != null && token.QueryToken.HasAllOrAny())
                parser.AddError(false, "Any {0} can not contains Any or All".Formato(token.QueryToken));

            this.Token = token;
        }

        internal AnyNode(ParsedToken token, string operation, string value, WordTemplateParser parser)
        {
            if (token.QueryToken != null && token.QueryToken.HasAllOrAny())
                parser.AddError(false, "Any {0} can not contains Any or All");

            this.Token = token;
            this.Operation = FilterValueConverter.ParseOperation(operation);
            this.Value = value;

            if (Token.QueryToken != null)
            {
                object rubish;
                string error = FilterValueConverter.TryParse(Value, Token.QueryToken.Type, out rubish, Operation == FilterOperation.IsIn);

                if (error.HasText())
                    parser.AddError(false, error);
            }
        }

        internal void ReplaceChild()
        {
            throw new NotImplementedException();
        }
    }

    public class IfNode : BlockNode
    {
        public readonly ParsedToken Token;

        private FilterOperation? Operation;
        private string Value;

        public MatchNode IfToken;
        public MatchNode ElseToken;
        public MatchNode EndIfToken;

        public OpenXmlElement IfBlock;
        public OpenXmlElement ElseBlock;
        
        internal IfNode(ParsedToken token, WordTemplateParser parser)
        {
            this.Token = token;
        }

        internal IfNode(ParsedToken token, string operation, string value, WordTemplateParser walker)
        {
            this.Token = token;
            this.Operation = FilterValueConverter.ParseOperation(operation);
            this.Value = value;

            if (Token.QueryToken != null)
            {
                object rubish;
                string error = FilterValueConverter.TryParse(Value, Token.QueryToken.Type, out rubish, Operation == FilterOperation.IsIn);

                if (error.HasText())
                    walker.AddError(false, error);
            }
        }

        internal void ReplaceChild()
        {
            throw new NotImplementedException();
        }
    }
}
