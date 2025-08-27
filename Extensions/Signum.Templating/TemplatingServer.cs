using Signum.API;

namespace Signum.Templating;

public static class TemplatingServer
{
    public static Func<bool>? TemplateTokenMessageAllowed;

    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        ReflectionServer.RegisterLike(typeof(TemplateTokenMessage), () => TemplateTokenMessageAllowed.GetInvocationListTyped().Any(f => f()));
    }
}
