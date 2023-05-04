using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Signum.API.Filters;

public class ValidateModelFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var exceptions = new List<Exception>();

            foreach (var state in context.ModelState)
            {
                if (state.Value.Errors.Count != 0)
                {
                    exceptions.AddRange(state.Value.Errors.Select(error => error.Exception).NotNull());
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            context.Result = new BadRequestObjectResult(context.ModelState);
        }
    }
}
