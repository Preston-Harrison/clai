using System.Data;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public record Message(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

internal class Program
{
    private static async Task Main(string[] args)
    {
        var (inputText, language, context) = ParseArguments(args);
        inputText ??= await EditFileWithVim();

        string apiKey = GetApiKey();
        string response = await CallOpenAiApi(inputText, language, context, apiKey);

        DisplayResponse(response, language);
    }

    private static (string? inputText, string? language, string? context) ParseArguments(string[] args)
    {
        string? inputText = null;
        string? language = null;
        string? context = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-l":
                    if (i + 1 >= args.Length) UsageExit();
                    language = args[++i];
                    break;
                case "-f":
                    if (i + 1 >= args.Length) UsageExit();
                    var file = args[++i];
                    context = File.ReadAllText(file);
                    break;
                default:
                    inputText = args[i];
                    break;
            }
        }

        return (inputText, language, context);
    }

    private static void UsageExit()
    {
        Console.WriteLine("Usage: clai [-l|<LANGUAGE>] [-f|<CONTEXT_FILE>] <INPUT>");
        Environment.Exit(1);
    }

    private static string GetApiKey()
    {
        var configPath = ResolvePath("~/.clai.env");
        if (File.Exists(configPath))
        {
            string contents = File.ReadAllText(configPath);
            return contents.Split('=')[1].Trim(['"', '\n']);
        }
        else if (Environment.GetEnvironmentVariable("OPENAI_API_KEY") != null)
        {
            return Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
        }
        else
        {
            throw new InvalidOperationException("API key not set in ~/.clai.env or in env var 'OPENAI_API_KEY'");
        }
    }

    private static async Task<string> CallOpenAiApi(string inputText, string? language, string? context, string apiKey)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var messages = CreateMessages(inputText, language, context);

        var requestBody = new
        {
            model = "gpt-4o",
            messages,
        };

        var httpResponse = await client.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        );

        string body = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode)
        {
            Console.Error.WriteLine(body);
            httpResponse.EnsureSuccessStatusCode();
        }

        return body;
    }

    private static List<Message> CreateMessages(string inputText, string? language, string? context)
    {
        var messages = new List<Message>
        {
            new Message("system", """
                You are an expert code assistant that is always concise, and always answers the user's question exactly. 
                You always provide a code block in your answer if your answer contains code, and the block must have the correct language annotation.
            """)
        };

        if (language != null) messages.Add(new Message("user", "My preferred language is " + language + "."));
        if (context != null) messages.Add(new Message("user", "Some context that may help you answer my question is:\n" + context));
        messages.Add(new Message("user", inputText));

        return messages;
    }

    private static void DisplayResponse(string response, string? language)
    {
        string contentStr = ExtractContentFromResponse(response);

        if (language != null)
        {
            var blocks = ExtractCodeBlocks(contentStr, language);
            foreach (var block in blocks)
            {
                Console.WriteLine(block);
            }
        }
        else
        {
            Console.WriteLine(contentStr);
        }
    }

    private static string ExtractContentFromResponse(string response)
    {
        using (JsonDocument doc = JsonDocument.Parse(response))
        {
            JsonElement choicesElement = doc.RootElement.GetProperty("choices");
            JsonElement firstChoice = choicesElement[0];
            JsonElement message = firstChoice.GetProperty("message");
            JsonElement content = message.GetProperty("content");

            return content.GetString() ?? throw new InvalidExpressionException("content must not be null");
        }
    }

    public static List<string> ExtractCodeBlocks(string markdown, string language)
    {
        var codeBlocks = new List<string>();
        string pattern = @$"```{language}(.*?)```"; // Non-greedy match to get content between backticks

        foreach (Match match in Regex.Matches(markdown, pattern, RegexOptions.Singleline))
        {
            string code = match.Groups[1].Value;
            codeBlocks.Add(code.Trim());
        }

        return codeBlocks;
    }

    static async Task<string> EditFileWithVim()
    {
        string tempFilePath = Path.GetTempFileName();
        try
        {
            Process vimProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "vim",
                    Arguments = tempFilePath,
                    UseShellExecute = true
                }
            };

            vimProcess.Start();
            vimProcess.WaitForExit();

            if (vimProcess.ExitCode != 0)
            {
                throw new InvalidOperationException("Vim exited with an error.");
            }

            string fileContent = await File.ReadAllTextAsync(tempFilePath);
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                throw new InvalidOperationException("No content was written in the file.");
            }

            return fileContent;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private static string ResolvePath(string path)
    {
        if (path.StartsWith('~'))
        {
            string home = Environment.GetEnvironmentVariable("HOME")!;
            path = Path.Combine(home, path.TrimStart('~').TrimStart('/').TrimStart('\\'));
        }
        return path;
    }
}