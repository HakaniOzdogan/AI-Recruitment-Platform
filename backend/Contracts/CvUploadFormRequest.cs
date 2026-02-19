using Microsoft.AspNetCore.Http;

namespace IkOtomasyon.Api.Contracts;

public sealed class CvUploadFormRequest
{
    public IFormFile? File { get; init; }
}

