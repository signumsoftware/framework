using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Signum.Entities.Dynamic;


namespace Signum.React.Extensions.Dynamic;

public class SignumDynamicApiControllerProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        if (DynamicCode.CodeGenControllerAssemblyPath.HasText())
        {
            var assembly = Assembly.LoadFrom(DynamicCode.CodeGenControllerAssemblyPath);
            var candidates = assembly.GetExportedTypes();

            foreach (var candidate in candidates)
            {
                feature.Controllers.Add(candidate.GetTypeInfo());
            }
        }
    }
}
