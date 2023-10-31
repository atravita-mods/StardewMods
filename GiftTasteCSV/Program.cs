using System.Collections.Concurrent;
using Csv;

using GiftTasteCSV.Constants;

using Newtonsoft.Json;

namespace GiftTasteCSV;

internal static class Program
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<GiftTasteLevel, ConcurrentBag<string>>> Data = new ();

    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            string filename = GetValidFile(null);
            ParseFromFile(filename);
        }
        else
        {
            Parallel.ForEach(args, ParseFromFile);
        }

        DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "output"));

        await WriteFiles(directory);
    }

    private static string GetValidFile(string? filename)
    {
        while (string.IsNullOrEmpty(filename) || !File.Exists(Path.Combine(Environment.CurrentDirectory, filename)))
        {
            Console.WriteLine("Please input a valid filename");
            filename = Console.ReadLine();
        }

        return filename;
    }

    private static void ParseFromFile(string filename)
    {
        try
        {
            using FileStream sr = new (File.OpenHandle(filename), FileAccess.Read);

            // we'll parse this ourselves cuz we can't trust the data not to have duplicates.
            CsvOptions options = new ()
            {
                HeaderMode = HeaderMode.HeaderAbsent,
            };

            IEnumerator<ICsvLine> parser = CsvReader.ReadFromStream(sr, options).GetEnumerator();

            if (!parser.MoveNext())
            {
                Console.WriteLine($"{filename} appears empty, huh.");
                return;
            }

            // this gets a mapping from index to NPC.
            string[] indexes = parser.Current.Values.ToArray();

            while (parser.MoveNext())
            {
                ICsvLine line = parser.Current;

                // blank line? weird.
                if (line.ColumnCount < 2)
                {
                    continue;
                }

                if (line[0].Contains(' '))
                {
                    Console.WriteLine($"{line[0]} contains a space, which is not allowed");
                }

                string item = line[0].Replace(' ', '_');
                for (int i = 1; i < line.Values.Length; i++)
                {
                    if (i >= indexes.Length)
                    {
                        Console.WriteLine($"Jagged CSV: item {item} does not have NPC");
                        break;
                    }

                    string tasteString = line[i];
                    if (string.IsNullOrWhiteSpace(tasteString))
                    {
                        continue;
                    }

                    if (!GiftTasteLevelExtensions.TryParse(tasteString, out GiftTasteLevel taste, ignoreCase: true))
                    {
                        Console.WriteLine($"{tasteString} not parse-able as a gift taste, skipping.");
                        continue;
                    }

                    if (!Data.TryGetValue(indexes[i], out ConcurrentDictionary<GiftTasteLevel, ConcurrentBag<string>>? personal))
                    {
                        Data[indexes[i]] = personal = new ();
                    }

                    if (!personal.TryGetValue(taste, out ConcurrentBag<string>? vals))
                    {
                        personal[taste] = vals = new ();
                    }

                    vals.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(filename);
            Console.WriteLine(ex.ToString());
        }
    }

    private static async Task WriteFiles(DirectoryInfo directory)
    {
        // write the small files.
        await Parallel.ForEachAsync(Data, async (d, token) => await WriteFile(d.Key, d.Value, directory, token));

        // write the include.
        await WriteIncludes(directory);
    }

    private static async Task WriteIncludes(DirectoryInfo directory)
    {
        try
        {
            using StreamWriter sw = File.CreateText(Path.Combine(directory.FullName, $"gift-tastes.json"));
            using JsonWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;

            await writer.WriteStartObjectAsync();
            await writer.WritePropertyNameAsync("Changes");
            await writer.WriteStartArrayAsync();
            await writer.WriteStartObjectAsync();

            await writer.WritePropertyNameAsync("Action");
            await writer.WriteValueAsync("Include");

            await writer.WritePropertyNameAsync("LogName");
            await writer.WriteValueAsync("Gift Tastes");

            await writer.WritePropertyNameAsync("FromFile");
            await writer.WriteValueAsync(string.Join(", ", Data.Keys.Select(k => $"gift-tastes-{k}.json")));

            await writer.WriteEndArrayAsync();
            await writer.WriteEndObjectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed while writing includes.");
            Console.WriteLine(ex.ToString());
        }
    }

    private static async Task WriteFile(string name, ConcurrentDictionary<GiftTasteLevel, ConcurrentBag<string>> tastes, DirectoryInfo directory, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }
        try
        {
            using StreamWriter sw = File.CreateText(Path.Combine(directory.FullName, $"gift-tastes-{name}.json"));
            using JsonWriter writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;

            await writer.WriteStartObjectAsync(token);
            await writer.WritePropertyNameAsync("Changes", token);
            await writer.WriteStartArrayAsync(token);
            await writer.WriteStartObjectAsync(token);

            await writer.WritePropertyNameAsync("Action", token);
            await writer.WriteValueAsync("EditData", token);

            await writer.WritePropertyNameAsync("Target", token);
            await writer.WriteValueAsync("Data/NPCGiftTastes", token);

            await writer.WritePropertyNameAsync("LogName", token);
            await writer.WriteValueAsync($"Gift tastes: {name}", token);

            await writer.WritePropertyNameAsync("TextOperations", token);
            await writer.WriteStartArrayAsync(token);

            foreach ((GiftTasteLevel taste, ConcurrentBag<string> items) in tastes)
            {
                await writer.WriteCommentAsync(taste.ToStringFast(), token);
                await writer.WriteStartObjectAsync(token);

                await writer.WritePropertyNameAsync("Operation", token);
                await writer.WriteValueAsync("Append", token);

                await writer.WritePropertyNameAsync("Target", token);
                await writer.WriteStartArrayAsync(token);

                if (name == "Universal")
                {
                    await writer.WriteValueAsync("Entries", token);
                    await writer.WriteValueAsync($"Universal_{taste.ToStringFast()}", token);
                }
                else
                {
                    await writer.WriteValueAsync("Fields", token);
                    await writer.WriteValueAsync(name, token);
                    await writer.WriteValueAsync((int)taste, token);
                }

                await writer.WriteEndArrayAsync(token);

                await writer.WritePropertyNameAsync("Value", token);
                await writer.WriteValueAsync(string.Join(' ', items), token);

                await writer.WritePropertyNameAsync("Delimiter", token);
                await writer.WriteValueAsync(" ", token);

                await writer.WriteEndAsync(token);
            }

            await writer.WriteEndAsync(token);
            await writer.WriteEndAsync(token);

            await writer.WriteEndArrayAsync(token);
            await writer.WriteEndObjectAsync(token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed while writing file for {name}");
            Console.WriteLine(ex.ToString());
        }
    }
}
