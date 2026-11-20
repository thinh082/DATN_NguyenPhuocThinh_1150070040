using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Server.Services;

public sealed class CloudinaryMinhChung(HttpClient http, IConfiguration config, ILogger<CloudinaryMinhChung> logger)
{
    private const string DeliveryType = "authenticated";

    public bool Enabled => config.GetSection("Storage:Cloudinary").GetChildren().Any();

    public async Task<string> UploadAsync(IFormFile file, string publicId, string mimeType)
    {
        var (url, fields) = Sign("raw/upload", new()
        {
            ["overwrite"] = "false",
            ["public_id"] = publicId,
            ["type"] = DeliveryType,
        });

        using var content = new MultipartFormDataContent();
        foreach (var field in fields)
            content.Add(new StringContent(field.Value), field.Key);
        var media = new StreamContent(file.OpenReadStream());
        media.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        content.Add(media, "file", Path.GetFileName(publicId));

        using var response = await PostAsync(url, content);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        if (!body.RootElement.TryGetProperty("asset_id", out var asset) || asset.GetString() is not { Length: > 0 } id)
            throw new IOException("Cloudinary không trả về mã tệp minh chứng.");
        return id;
    }

    public async Task<Stream> DownloadAsync(string assetId)
    {
        var (url, fields) = Sign("asset/download", new() { ["asset_id"] = assetId });
        using var content = new FormUrlEncodedContent(fields);
        using var response = await PostAsync(url, content);
        return new MemoryStream(await response.Content.ReadAsByteArrayAsync(), writable: false);
    }

    public async Task TryDeleteAsync(string publicId)
    {
        try
        {
            var (url, fields) = Sign("raw/destroy", new() { ["public_id"] = publicId, ["type"] = DeliveryType });
            using var content = new FormUrlEncodedContent(fields);
            using var response = await PostAsync(url, content);
        }
        catch (IOException)
        {
            logger.LogWarning("Không dọn được tệp minh chứng Cloudinary {PublicId}.", publicId);
        }
    }

    private (Uri Url, Dictionary<string, string> Fields) Sign(string operation, Dictionary<string, string> fields)
    {
        var cloudName = config["Storage:Cloudinary:CloudName"];
        var apiKey = config["Storage:Cloudinary:ApiKey"];
        var apiSecret = config["Storage:Cloudinary:ApiSecret"];
        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            throw new IOException("Cấu hình lưu tệp minh chứng Cloudinary chưa đầy đủ.");

        fields["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var payload = string.Join("&", fields.OrderBy(field => field.Key, StringComparer.Ordinal)
            .Select(field => $"{field.Key}={field.Value}"));
        fields["signature"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload + apiSecret))).ToLowerInvariant();
        fields["api_key"] = apiKey;
        return (new Uri($"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(cloudName)}/{operation}"), fields);
    }

    private async Task<HttpResponseMessage> PostAsync(Uri url, HttpContent content)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsync(url, content);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new IOException("Không kết nối được dịch vụ lưu tệp minh chứng.");
        }

        if (response.IsSuccessStatusCode)
            return response;

        var statusCode = response.StatusCode;
        response.Dispose();
        if (statusCode == HttpStatusCode.NotFound)
            throw new FileNotFoundException("Không tìm thấy tệp trên Cloudinary.");

        logger.LogWarning("Cloudinary trả về HTTP {StatusCode} cho {Operation}.", (int)statusCode, url.Segments[^1]);
        throw new IOException("Cloudinary không xử lý được tệp minh chứng.");
    }
}
