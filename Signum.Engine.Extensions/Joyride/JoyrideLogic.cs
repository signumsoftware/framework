using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.UserAssets;
using Signum.Entities.Joyride;
using System.Reflection;

namespace Signum.Engine.Joyride
{
    public static class JoyrideLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<JoyrideEntity>()
                    .WithSave(JoyrideOperation.Save)
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Culture
                    });

                sb.Include<JoyrideStepEntity>()
                    .WithSave(JoyrideStepOperation.Save)
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Title,
                        e.Culture,
                        e.Selector,
                        e.Type,
                        e.Position,
                    });

                sb.Include<JoyrideStepStyleEntity>()
                  .WithSave(JoyrideStepStyleOperation.Save)
                  .WithQuery(() => e => new
                  {
                      Entity = e,
                      e.Id,
                      e.Name
                  });

                UserAssetsImporter.RegisterName<JoyrideEntity>("Joyride");
                UserAssetsImporter.RegisterName<JoyrideStepEntity>("JoyrideStep");
                UserAssetsImporter.RegisterName<JoyrideStepStyleEntity>("JoyrideStepStyle");
            }
        }
    }
}
