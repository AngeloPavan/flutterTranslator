using Google.Cloud.Translation.V2;
using System.Text.RegularExpressions;

class Program
{
    private static HashSet<string> foundStrings = new HashSet<string>();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter your Google Translate API key:");
        string? apiKey = Console.ReadLine();

        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("API key is required!");
            return;
        }

        Console.WriteLine("Flutter project path:");
        var flutterProjectPath = Console.ReadLine();

        if (string.IsNullOrEmpty(flutterProjectPath))
        {
            Console.WriteLine("Invalid path");
            return;
        }

        var client = TranslationClient.CreateFromApiKey(apiKey);

        await ScanFolder(flutterProjectPath);

        await TranslateAndSave(client, foundStrings, ["en_US", "it_IT"]);

        Console.WriteLine("\nDone");
    }

    static async Task ScanFolder(string folderPath)
    {
        var fileTasks = new List<Task>();
        foreach (var file in Directory.GetFiles(folderPath))
        {
            fileTasks.Add(ScanFileAsync(file));
        }

        foreach (var directory in Directory.GetDirectories(folderPath))
        {
            fileTasks.Add(ScanFolder(directory));
        }

        await Task.WhenAll(fileTasks);
    }

    static async Task ScanFileAsync(string filePath)
    {
        var fileContent = await File.ReadAllTextAsync(filePath);

        var regex = new Regex("([\"'])(.+?)\\1\\.tr");
        var matches = regex.Matches(fileContent);

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                foundStrings.Add(match.Groups[2].Value);
            }
        }
    }

    static async Task TranslateAndSave(TranslationClient client, HashSet<string> stringsToTranslate, string[] targetLanguages)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "translations.txt");
        using var fileStream = new StreamWriter(filePath, false);

        foreach (var targetLanguage in targetLanguages)
        {
            fileStream.WriteLine($"'{targetLanguage}': {{");

            foreach (var text in stringsToTranslate)
            {
                string translatedText = text;

                if (targetLanguage != "en_US")
                {
                    Console.WriteLine("Translating a word");
                    var response = await client.TranslateTextAsync(text, targetLanguage);
                    translatedText = response.TranslatedText;
                }

                fileStream.WriteLine($"'{text}': '{translatedText}',");
            }

            fileStream.WriteLine("},");
        }
    }
}