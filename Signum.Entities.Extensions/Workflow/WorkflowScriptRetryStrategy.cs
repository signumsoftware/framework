using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Signum.Entities.Workflow
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class WorkflowScriptRetryStrategyEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Rule { get; set; }

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
                case "s": return TimeZoneManager.Now.AddSeconds(value);
                case "m": return TimeZoneManager.Now.AddMinutes(value);
                case "h": return TimeZoneManager.Now.AddHours(value);
                case "d": return TimeZoneManager.Now.AddDays(value);
                default: throw new InvalidOperationException("Unexpected unit " + unit);
            }
        }
    }

    [AutoInit]
    public static class WorkflowScriptRetryStrategyOperation
    {
        public static readonly ExecuteSymbol<WorkflowScriptRetryStrategyEntity> Save;
        public static readonly DeleteSymbol<WorkflowScriptRetryStrategyEntity> Delete;
    }
}
