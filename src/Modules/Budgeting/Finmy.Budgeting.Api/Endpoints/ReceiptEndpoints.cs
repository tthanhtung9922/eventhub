using Finmy.Budgeting.Application.Receipts;
using Finmy.Modularity.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Finmy.Budgeting.Api.Endpoints;

public sealed class ReceiptEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/receipts", async (IFormFile file, ReceiptService svc, CancellationToken cancellationToken) => 
            {
                using var stream = file.OpenReadStream();
                var result = await svc.UploadAsync(stream, file.Length, file.ContentType, file.FileName, cancellationToken);
                return result.Match(x => Results.Created($"/receipts/{x.Id}", x));
            })
            .DisableAntiforgery();
    }
}
