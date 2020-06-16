using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace Signum.Entities.Joyride
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class JoyrideStepStyleEntity : Entity, IUserAssetEntity
    {
        [UniqueIndex]
        [StringLengthValidator(Min = 3, Max = 100)]
        public string Name { get; set; }

        [StringLengthValidator(Min = 3, Max = 50)]
        public string? BackgroundColor { get; set; }

        [StringLengthValidator(Min = 3, Max = 50)]
        public string? Color { get; set; }

        [StringLengthValidator(Min = 3, Max = 50)]
        public string? MainColor { get; set; }

        [StringLengthValidator(Min = 3, Max = 50)]
        public string? BorderRadius { get; set; }

        [StringLengthValidator(Min = 3, Max = 50)]
        public string? TextAlign { get; set; }

        [StringLengthValidator(Min = 3, Max = 50)]
        public string? Width { get; set; }

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("JoyrideStepStyle",
                new XAttribute("Guid", Guid),
                new XElement("Name", Name),
                BackgroundColor.IsNullOrEmpty() ? null : new XElement("BackgroundColor", BackgroundColor),
                MainColor.IsNullOrEmpty() ? null : new XElement("MainColor", MainColor),
                Color.IsNullOrEmpty() ? null : new XElement("Color", Color),
                BorderRadius.IsNullOrEmpty() ? null : new XElement("BorderRadius", BorderRadius),
                TextAlign.IsNullOrEmpty() ? null : new XElement("TextAlign", TextAlign),
                Width.IsNullOrEmpty() ? null : new XElement("Width", Width));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Name = element.Element("Name").Value;
            BackgroundColor = element.Element("BackgroundColor")?.Value;
            MainColor = element.Element("MainColor")?.Value;
            Color = element.Element("Color")?.Value;
            BorderRadius = element.Element("BorderRadius")?.Value;
            TextAlign = element.Element("TextAlign")?.Value;
            Width = element.Element("Width")?.Value;
        }
    }

    [AutoInit]
    public static class JoyrideStepStyleOperation
    {
        public static readonly ExecuteSymbol<JoyrideStepStyleEntity> Save;
    }
}
