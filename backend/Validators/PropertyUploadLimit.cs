using HostGrid.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace HostGrid.Api.Validators;

internal sealed class PropertyUploadLimit(IOptions<PropertyStorageOptions> options) : IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var request = context.HttpContext.Request;
        var photo = string.Equals(request.RouteValues["group"] as string, "photos", StringComparison.OrdinalIgnoreCase);
        var limit = (photo ? options.Value.PhotoBytes : options.Value.DocumentBytes) + 64 * 1024;
        if (request.ContentLength > limit)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge, Title = "The request exceeds the upload limit."
            }) { StatusCode = StatusCodes.Status413PayloadTooLarge };
            return;
        }
        if (context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } size)
            size.MaxRequestBodySize = limit;
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}
