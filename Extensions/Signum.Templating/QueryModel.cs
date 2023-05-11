using System.ComponentModel;

namespace Signum.Templating;

public class QueryModel : ModelEntity
{
    [InTypeScript(false)]
    public object QueryName { get; set; }

    [InTypeScript(false)]
    public List<Filter> Filters { get; set; } = new List<Filter>();

    [InTypeScript(false)]
    public List<Order> Orders { get; set; } = new List<Order>();

    [InTypeScript(false)]
    public Pagination Pagination { get; set; }
}


public enum QueryModelMessage
{
    [Description("Configure your query and press [Search] before [Ok]")]
    ConfigureYourQueryAndPressSearchBeforeOk
}
