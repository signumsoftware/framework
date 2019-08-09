using Signum.Entities.Basics;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Signum.Entities.Joyride
{
    [Serializable, EntityKind(EntityKind.SharedPart, EntityData.Master)]
    public class JoyrideStepEntity : Entity, IUserAssetEntity
    {
        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        public Lite<CultureInfoEntity>? Culture { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string Title { get; set; }

        [StringLengthValidator(MultiLine = true)]
        public string Text { get; set; }

        public JoyrideStepStyleEntity? Style { get; set; }

        [StringLengthValidator(Min = 3, Max = 100)]
        public string Selector { get; set; }

        public JoyrideStepPosition Position { get; set; }

        public JoyrideStepType Type { get; set; }

        public bool AllowClicksThruHole { get; set; }

        public bool IsFixed { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Title);

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("JoyrideStep",
                new XAttribute("Guid", Guid),
                Culture == null ? null : new XElement("Culture", Culture.Key()),
                new XElement("Title", Title),
                Text == null ? null : new XElement("Text", Text),
                Style == null ? null : new XElement("Style", ctx.Include(Style).ToString()),
                new XElement("Selector", Selector),
                new XElement("Position", Position.ToString()),
                new XElement("Type", Type.ToString()),
                new XElement("AllowClicksThruHole", AllowClicksThruHole),
                new XElement("IsFixed", IsFixed));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Culture = element.Element("Culture")?.Let(a => Lite.Parse<CultureInfoEntity>(a.Value));
            Title = element.Element("Title").Value;
            Text = element.Element("Text").Value;
            Style = element.Element("Style")?.Value.Let(a => (JoyrideStepStyleEntity)ctx.GetEntity(Guid.Parse(a)));
            Position = element.Element("Position").Value.ToEnum<JoyrideStepPosition>();
            Selector = element.Element("Selector").Value;
            Type = element.Element("Type").Value.ToEnum<JoyrideStepType>();
            AllowClicksThruHole = Boolean.Parse(element.Element("AllowClicksThruHole").Value);
            IsFixed = Boolean.Parse(element.Element("IsFixed").Value);
        }
    }

    [AutoInit]
    public static class JoyrideStepOperation
    {
        public static readonly ExecuteSymbol<JoyrideStepEntity> Save;
    }

    public enum JoyrideStepPosition
    {
        Top,
        TopLeft,
        TopRight,
        Bottom,
        BottomLeft,
        BottomRight,
    }

    public enum JoyrideStepType
    {
        Click,
        Hover,
    }
}
