using Microsoft.AspNetCore.Mvc;
using Signum.API;
using Signum.Basics;
using Signum.UserAssets;

namespace Signum.Tour;

public class TourController : ControllerBase
{
    [HttpGet("api/tour/byEntity/{typeName}")]
    public TourDTO? GetTourByEntity(string typeName)
    {
        var type = TypeLogic.GetType(typeName);
        var typeEntity = type.ToTypeEntity().ToLite();
        var tour = Database.Query<TourEntity>()
            .Where(t => t.ForEntity.Is(typeEntity))
            .FirstOrDefault();

        return tour == null ? null : ToDTO(tour);
    }

    private static TourDTO ToDTO(TourEntity tour)
    {
        return new TourDTO
        {
            ForEntity = tour.ForEntity,
            Animate = tour.Animate,
            ShowCloseButton = tour.ShowCloseButton,
            Steps = tour.Steps.Select(s => new TourStepDTO
            {
                CssSelector = ResolveCssSelector(s.CssSteps),
                Title = s.Title,
                Description = s.Description,
                Side = s.Side?.ToString().ToLower(),
                Align = s.Align?.ToString().ToLower()
            }).ToList()
        };
    }

    private static string? ResolveCssSelector(MList<CssStepEmbedded> cssSteps)
    {
        if (cssSteps == null || cssSteps.Count == 0)
            return null;

        var selectors = new List<string>();

        foreach (var step in cssSteps)
        {
            switch (step.Type)
            {
                case CssStepType.CSSSelector:
                    selectors.Add(step.CssSelector!);
                    break;

                case CssStepType.Property:
                    var propertyRoute = step.Property!.RetrieveFromCache();
                    selectors.Add($"[data-property-path='{propertyRoute.Path}']");
                    break;

                case CssStepType.ToolbarContent:
                    var lite = step.ToolbarContent!;
                    var key = lite is Lite<QueryEntity> q ? q.RetrieveFromCache().Key : lite.Key();
                    selectors.Add($"[data-toolbar-content='{key}']");
                    break;
            }
        }

        return selectors.ToString(" ");
    }
}
