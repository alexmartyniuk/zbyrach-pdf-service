using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Zbyrach.Pdf
{
    public class ModelStateValidatorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext actionExecutingContext)
        {
            if (!actionExecutingContext.ModelState.IsValid)
            {
                actionExecutingContext.Result = new BadRequestObjectResult(actionExecutingContext.ModelState);
            }
        }
    }
}