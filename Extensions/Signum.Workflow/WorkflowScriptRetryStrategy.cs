using Signum.Entities.UserAssets;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Signum.Workflow;

[EntityKind(EntityKind.Shared, EntityData.Master)]
    public class WorkflowScriptRetryStrategyEntity : Entity, IUserAssetEntity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Rule { get; set; }

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Rule);

        static readonly Regex Regex = new Regex(@"^\s*(?<part>\d+[smhd])(\s*,\s*(?<part>\d+[smhd]))*\s*$", RegexOptions.IgnoreCase);
        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Rule))
            {
                if (!Regex.IsMatch(Rule))
                    return ValidationMessage._0DoesNotHaveAValid1Format.NiceToString(pi.NiceName(), "RetryStrategyRule");

            }

            return base.PropertyValidation(pi);
        }

        public DateTime? NextDate(int retryCount)
        {
            var capture = Regex.Match(Rule).Groups["part"].Captures.Cast<Capture>().ElementAtOrDefault(retryCount);
            if (capture == null)
                return null;

            var unit = capture.Value.End(1);
            var value = int.Parse(capture.Value.RemoveEnd(1));

            switch (unit.ToLower())
            {
            case "s": return Clock.Now.AddSeconds(value);
            case "m": return Clock.Now.AddMinutes(value);
            case "h": return Clock.Now.AddHours(value);
            case "d": return Clock.Now.AddDays(value);
                default: throw new InvalidOperationException("Unexpected unit " + unit);
            }
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("WorkflowScriptRetryStrategy",
                  new XAttribute("Guid", Guid),
                  new XAttribute("Rule", Rule)
                );
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Rule = element.Attribute("Rule")!.Value;
        }
    }

    [AutoInit]
    public static class WorkflowScriptRetryStrategyOperation
    {
        public static readonly ExecuteSymbol<WorkflowScriptRetryStrategyEntity> Save;
        public static readonly DeleteSymbol<WorkflowScriptRetryStrategyEntity> Delete;
    }
