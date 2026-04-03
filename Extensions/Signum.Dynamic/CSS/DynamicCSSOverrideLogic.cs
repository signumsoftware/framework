
namespace Signum.Dynamic.CSS;


public static class DynamicCSSOverrideLogic
{
    public static ResetLazy<List<DynamicCSSOverrideEntity>> Cached = null!;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<DynamicCSSOverrideEntity>()
           .WithSave(DynamicCSSOverrideOperation.Save)
           .WithDelete(DynamicCSSOverrideOperation.Delete)
           .WithQuery(() => e => new
           {
               Entity = e,
               e.Id,
               e.Name,
               Script = e.Script.Etc(100),
           });

        Cached = sb.GlobalLazy(() =>
         Database.Query<DynamicCSSOverrideEntity>().Where(a => !a.Mixin<DisabledMixin>().IsDisabled).ToList(),
         new InvalidateWith(typeof(DynamicCSSOverrideEntity)));
    }
}

// In order to work this module, you should apply below mentioned changes to your index.cshtml file
/*
@using Signum.Utilities;
@using Signum.Dynamic; <====*

@{
   ...
var cssOverride = String.Join("\n", DynamicCSSOverrideLogic.Cached.Value.Select(a => a.Script)); <====*
<!doctype html>
<html>
<head>
     ...
</head>
<body>
    <style type="text/css">@cssOverride</style> <====*
    <div id="reactDiv"></div>
   ...
</body>
</html>
 
*/
