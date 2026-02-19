using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace IkOtomasyon.Api.Services;

public class CvStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/octet-stream"
        },
        [".doc"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/msword",
            "application/octet-stream"
        },
        [".docx"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/zip",
            "application/octet-stream"
        }
    };

    private readonly CvStorageOptions _options;

    public CvStorageService(IOptions<CvStorageOptions> options)
    {
        _options = options.Value;
    }

    public bool IsAllowedExtension(string extension) => AllowedExtensions.Contains(extension);

    public long MaxFileSizeBytes => _options.MaxFileSizeBytes;

    public async Task<CvStoredFile> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Unsupported file type.");
        }

        ValidateContentType(file, extension);
        await ValidateFileSignatureAsync(file, extension, ct);

        var safeOriginalName = Path.GetFileName(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";

        var root = _options.RootPath;
        if (!Path.IsPathRooted(root))
        {
            root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, root));
        }

        Directory.CreateDirectory(root);

        var fullPath = Path.GetFullPath(Path.Combine(root, storedFileName));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(fs, ct);
        }

        return new CvStoredFile(
            StoredFileName: storedFileName,
            StoragePath: fullPath,
            FileType: extension.TrimStart('.'),
            FileSize: file.Length,
            OriginalFileName: safeOriginalName);
    }

    private static void ValidateContentType(IFormFile file, string extension)
    {
        if (!AllowedContentTypes.TryGetValue(extension, out var allowedTypes))
        {
            throw new InvalidOperationException("Unsupported file type.");
        }

        var contentType = file.ContentType?.Trim();
        if (string.IsNullOrWhiteSpace(contentType) || !allowedTypes.Contains(contentType))
        {
            throw new InvalidOperationException("Invalid content-type for uploaded file.");
        }
    }

    private static async Task ValidateFileSignatureAsync(IFormFile file, string extension, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[8];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (read < 4)
        {
            throw new InvalidOperationException("Uploaded file is too small.");
        }

        stream.Position = 0;
        var isValidSignature = extension switch
        {
            ".pdf" => IsPdf(header),
            ".doc" => IsDoc(header),
            ".docx" => IsDocx(header),
            _ => false
        };

        if (!isValidSignature)
        {
            throw new InvalidOperationException("File signature validation failed.");
        }

        if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > 1500)
            {
                throw new InvalidOperationException("DOCX contains too many entries.");
            }

            var totalUncompressedBytes = 0L;
            var hasDocumentXml = false;
            foreach (var entry in archive.Entries)
            {
                totalUncompressedBytes += entry.Length;
                if (entry.FullName.Equals("word/document.xml", StringComparison.OrdinalIgnoreCase))
                {
                    hasDocumentXml = true;
                }

                if (totalUncompressedBytes > 50 * 1024 * 1024)
                {
                    throw new InvalidOperationException("DOCX uncompressed payload exceeds limit.");
                }
            }

            if (!hasDocumentXml)
            {
                throw new InvalidOperationException("DOCX structure is invalid.");
            }
        }
    }

    private static bool IsPdf(byte[] header)
        => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46;

    private static bool IsDoc(byte[] header)
        => header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0
           && header[4] == 0xA1 && header[5] == 0xB1 && header[6] == 0x1A && header[7] == 0xE1;

    private static bool IsDocx(byte[] header)
        => header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04;
}
