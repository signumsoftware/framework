using System.Threading.Tasks;

namespace Signum.React.Filters;

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class SignumAllowAnonymousAttribute : Attribute
{
}
