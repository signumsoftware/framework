using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Signum.Eval;

namespace Signum.Dynamic.Controllers;

public class SignumDynamicApiControllerProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        if (EvalLogic.CodeGenControllerAssemblyPath.HasText())
        {
            var assembly = Assembly.LoadFrom(EvalLogic.CodeGenControllerAssemblyPath);
            var candidates = assembly.GetExportedTypes();

            foreach (var candidate in candidates)
            {
                feature.Controllers.Add(candidate.GetTypeInfo());
            }
        }
    }
}
