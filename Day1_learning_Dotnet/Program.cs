using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var config = LoadConfig();
var messages = new List<Dictionary<string, string>>();

await RunAssistantLoopAsync(config, messages);

static async Task RunAssistantLoopAsync(AppConfig config, List<Dictionary<string, string>> messages)
{
    using var client = new HttpClient();

    while (true)
    {
        Console.Write("Whats on your mind? ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            continue;
        }

        messages.Add(new Dictionary<string, string>
        {
            ["role"] = "user",
            ["content"] = input
        });

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        var response = await SendPromptAsync(client, config, messages);
        var aiReply = response;

        Console.WriteLine(aiReply);

        messages.Add(new Dictionary<string, string>
        {
            ["role"] = "assistant",
            ["content"] = aiReply
        });

        Console.WriteLine(JsonSerializer.Serialize(messages));
    }
}

static async Task<string> SendPromptAsync(HttpClient client, AppConfig config, List<Dictionary<string, string>> messages)
{
    var endpoint = config.BaseUrl.TrimEnd('/') + "/chat/completions";

    var payload = new
    {
        model = config.Model,
        messages = messages.Select(m => new { role = m["role"], content = m["content"] }).ToArray()
    };

    var json = JsonSerializer.Serialize(payload);
    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();

    var responseBody = await response.Content.ReadAsStringAsync();
    using var document = JsonDocument.Parse(responseBody);

    return document.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString() ?? "No response returned.";
}

static AppConfig LoadConfig()
{
    var possiblePaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")
    };

    string? envPath = possiblePaths.FirstOrDefault(File.Exists);

    if (envPath is null)
    {
        throw new FileNotFoundException("Could not find .env file. Place it in the project folder or one level above.");
    }

    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var line in File.ReadAllLines(envPath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
        {
            continue;
        }

        var parts = trimmed.Split('=', 2);
        if (parts.Length == 2)
        {
            values[parts[0].Trim()] = parts[1].Trim();
        }
    }

    return new AppConfig
    {
        BaseUrl = values.GetValueOrDefault("BASE_URL", "http://localhost:11434/v1"),
        ApiKey = values.GetValueOrDefault("API_KEY", "ollama"),
        Model = values.GetValueOrDefault("MODEL", "gemma3:270M")
    };
}

internal sealed class AppConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
