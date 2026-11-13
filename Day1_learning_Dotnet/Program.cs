using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var config = LoadConfig();

if (args.Length > 0 && (args[0].Equals("hello", StringComparison.OrdinalIgnoreCase) || args[0].Equals("--hello", StringComparison.OrdinalIgnoreCase)))
{
    await AskSingleQuestionAsync(config);
    return;
}

await RunAssistantLoopAsync(config);

static async Task RunAssistantLoopAsync(AppConfig config)
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

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        var response = await SendPromptAsync(client, config, input);
        Console.WriteLine(response);
    }
}

static async Task AskSingleQuestionAsync(AppConfig config)
{
    Console.Write("Whats on your mind? ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        Console.WriteLine("No input provided.");
        return;
    }

    using var client = new HttpClient();
    var response = await SendPromptAsync(client, config, $"What is {input}?");
    Console.WriteLine(response);
}

static async Task<string> SendPromptAsync(HttpClient client, AppConfig config, string prompt)
{
    var endpoint = config.BaseUrl.TrimEnd('/') + "/chat/completions";
    var payload = new
    {
        model = config.Model,
        messages = new[]
        {
            new { role = "user", content = prompt }
        }
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
