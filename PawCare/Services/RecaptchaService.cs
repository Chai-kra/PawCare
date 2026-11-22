using System.Text.Json;

namespace PawCare.Services;

public interface IRecaptchaService
{
    bool Enabled { get; }
    string SiteKey { get; }
    Task<bool> VerifyAsync(string? response);
}

public class RecaptchaService(HttpClient http, IConfiguration config) : IRecaptchaService
{
    private readonly string _secret = config["Recaptcha:SecretKey"] ?? "";

    public string SiteKey { get; } = config["Recaptcha:SiteKey"] ?? "";

    // Disabled when either key is empty; set both in production.
    public bool Enabled => !string.IsNullOrWhiteSpace(SiteKey) && !string.IsNullOrWhiteSpace(_secret);

    public async Task<bool> VerifyAsync(string? response)
    {
        if (!Enabled) return true;
        if (string.IsNullOrWhiteSpace(response)) return false;

        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["secret"] = _secret,
                ["response"] = response
            });
            using var result = await http.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            if (!result.IsSuccessStatusCode) return false;

            using var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            return json.RootElement.TryGetProperty("success", out var ok) && ok.GetBoolean();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return false;   // fail closed if the check cannot complete
        }
    }
}
