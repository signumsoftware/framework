namespace Signum.Tour;

public class TourDTO
{    
    public Lite<Entity> ForEntity { get; set; }
    public List<TourStepDTO> Steps { get; set; } = new List<TourStepDTO>();
    public bool ShowProgress { get; set; }
    public bool Animate { get; set; }
    public bool ShowCloseButton { get; set; }
}

public class TourStepDTO
{
    public string? CssSelector { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Side { get; set; }
    public string? Align { get; set; }
}
