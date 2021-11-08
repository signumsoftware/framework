using Signum.Engine.UserAssets;
using Signum.Entities.Joyride;

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

                UserAssetsImporter.Register<JoyrideEntity>("Joyride", JoyrideOperation.Save);
                UserAssetsImporter.Register<JoyrideStepEntity>("JoyrideStep", JoyrideStepOperation.Save);
                UserAssetsImporter.Register<JoyrideStepStyleEntity>("JoyrideStepStyle", JoyrideStepStyleOperation.Save);
            }
        }
    }
}
