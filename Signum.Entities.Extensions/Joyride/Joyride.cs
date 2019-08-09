using Signum.Entities.Basics;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Signum.Entities.Joyride
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class JoyrideEntity : Entity, IUserAssetEntity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        public Lite<CultureInfoEntity>? Culture { get; set; }

        public JoyrideType Type { get; set; }

        [PreserveOrder]
        [NoRepeatValidator]
        public MList<JoyrideStepEntity> Steps { get; set; } = new MList<JoyrideStepEntity>();

        public bool ShowSkipButton { get; set; }

        public bool ShowStepsProgress { get; set; }

        public bool KeyboardNavigation { get; set; }

        public bool Debug { get; set; }

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Joyride",
                new XAttribute("Guid", Guid),
                new XElement("Name", Name),
                Culture == null ? null : new XElement("Culture", Culture.Key()),
                new XElement("Type", Type.ToString()),
                new XElement("Steps", Steps.Select(p => p.ToXml(ctx))),
                new XElement("ShowSkipButton", ShowSkipButton),
                new XElement("ShowStepsProgress", ShowStepsProgress),
                new XElement("KeyboardNavigation", KeyboardNavigation),
                new XElement("Debug", Debug));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Name = element.Element("Name").Value;
            Culture = element.Element("Culture")?.Let(a => Lite.Parse<CultureInfoEntity>(a.Value));
            Type = element.Element("Type").Value.ToEnum<JoyrideType>();
            Steps.Synchronize(element.Element("Steps").Elements().ToList(), (s, x) => s.FromXml(x, ctx));
            ShowSkipButton = Boolean.Parse(element.Element("ShowSkipButton").Value);
            ShowStepsProgress = Boolean.Parse(element.Element("ShowStepsProgress").Value);
            KeyboardNavigation = Boolean.Parse(element.Element("KeyboardNavigation").Value);
            Debug = Boolean.Parse(element.Element("Debug").Value);
        }
    }

    public enum JoyrideType
    {
        Continuous,
        Single
    }

    [AutoInit]
    public static class JoyrideOperation
    {
        public static readonly ExecuteSymbol<JoyrideEntity> Save;
    }
}
