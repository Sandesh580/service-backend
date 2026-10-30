using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace LoginCheck.Infrastructure.Services;

public sealed class WhatsAppOtpSender(HttpClient httpClient, IConfiguration configuration)
{
    public static string NormalizePhone(string phone)
    {
        var trimmed = (phone ?? string.Empty).Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal)) digits = digits[2..];
        if (digits.StartsWith("977", StringComparison.Ordinal) && digits.Length == 13)
            return $"+{digits}";
        if (digits.Length == 10 && digits.StartsWith('9'))
            return $"+977{digits}";
        if (trimmed.StartsWith('+') && digits.Length is >= 8 and <= 15)
            return $"+{digits}";
        throw new ArgumentException("Enter a valid mobile number with its country code, or a 10-digit Nepal mobile number.", nameof(phone));
    }

    public async Task SendAsync(string recipient, string code, CancellationToken cancellationToken)
    {
        var phoneNumberId = configuration["WhatsApp:PhoneNumberId"];
        var accessToken = configuration["WhatsApp:AccessToken"];
        var templateName = configuration["WhatsApp:AuthenticationTemplate"];
        var version = configuration["WhatsApp:GraphApiVersion"] ?? "v26.0";
        var language = configuration["WhatsApp:TemplateLanguage"] ?? "en_US";
        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(templateName))
            throw new InvalidOperationException("WhatsApp Cloud API is not configured.");

        var uri = $"https://graph.facebook.com/{version}/{Uri.EscapeDataString(phoneNumberId)}/messages";
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(recipient).TrimStart('+'),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = language },
                components = new object[]
                {
                    new { type = "body", parameters = new[] { new { type = "text", text = code } } },
                    new { type = "button", sub_type = "url", index = "0", parameters = new[] { new { type = "text", text = code } } }
                }
            }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"WhatsApp did not accept the authentication message ({(int)response.StatusCode}). {details}");
        }
    }
}
