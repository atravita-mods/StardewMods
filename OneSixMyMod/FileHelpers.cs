namespace OneSixMyMod;

using Newtonsoft.Json;

/// <summary>
/// Helpers to get the right file location.
/// </summary>
internal static class FileHelpers
{
    internal static async Task<TModel?> ReadJsonFile<TModel>(this FileInfo file, CancellationToken token)
    {
        string[] lines = await File.ReadAllLinesAsync(file.FullName, token);

        // following SMAPI behavior for curly quotes.
        string concat = string.Join('\n', lines.Select(static line => line.Replace('“', '"').Replace('”', '"')));
        return JsonConvert.DeserializeObject<TModel>(concat);
    }

    /// <summary>
    /// Tries to get a single file by a specific name for a directory.
    /// </summary>
    /// <param name="dir">Directory to search in.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="file">The relevant fileinfo.</param>
    /// <returns>True if found, false otherwise.</returns>
    internal static bool TryGetSingleFile(this DirectoryInfo? dir, string? fileName, [NotNullWhen(true)] out FileInfo? file)
    {
        file = null;
        if (dir is null || string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        FileInfo[] candidates = dir.GetFiles(fileName);
        if (candidates.Length == 1)
        {
            file = candidates[0];
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the path to the stardew directory, which we will work from.
    /// </summary>
    /// <param name="initial">Initial search directory.</param>
    /// <returns>Directory of the unpacked game content and mods dir.</returns>
    /// <exception cref="InvalidDataException">No clue how you could manage two directories in some places, will need to fix later.</exception>
    internal static (DirectoryInfo unpacked, DirectoryInfo mods)? GetStardewPath(string? initial)
    {
        DirectoryInfo dir = new (initial ?? Environment.CurrentDirectory);
        while (true)
        {
            Console.WriteLine($"Checking {dir.FullName}");
            if (!dir.Exists)
            {
                Console.WriteLine("That directory doesn't seem to exist.");
            }
            else if (dir.GetFiles("Stardew Valley.dll").Length == 0)
            {
                Console.WriteLine("Stardew does not seem to be here.");
            }
            else
            {
                DirectoryInfo[] unpackedCandidates = dir.GetDirectories("Content (unpacked)");
                DirectoryInfo? unpacked;
                if (unpackedCandidates.Length == 0)
                {
                    Console.WriteLine("Hmmm, default unpacked dir does not seem to exist. You need to unpack Stardew to use the converter.");
                    unpacked = GetDirOf(dir, "unpacked");
                    if (unpacked is null)
                    {
                        return null;
                    }
                }
                else if (unpackedCandidates.Length > 1)
                {
                    throw new InvalidDataException(); // uh, not sure how you got here, fix later.
                }
                else
                {
                    unpacked = unpackedCandidates[0];
                    Console.WriteLine($"{unpacked.FullName} is the unpacked game directory, is that correct?");
                    ConsoleKeyInfo key = Console.ReadKey();
                    Console.WriteLine();
                    if (key.Key is not ConsoleKey.Y or ConsoleKey.Enter)
                    {
                        Console.WriteLine("Okay! Can you enter the path to the unpacked dir?");
                        unpacked = GetDirOf(dir, "unpacked");
                        if (unpacked is null)
                        {
                            return null;
                        }
                    }
                }

                DirectoryInfo[] modCandidates = dir.GetDirectories("Mods");
                DirectoryInfo? mods;
                if (modCandidates.Length == 0)
                {
                    Console.WriteLine("Hmmm, no mods dir found to convert.");
                    mods = GetDirOf(dir, "mods");
                    if (mods is null)
                    {
                        return null;
                    }
                }
                else if (modCandidates.Length > 1)
                {
                    throw new InvalidDataException(); // uh, not sure how you got here, fix later.
                }
                else
                {
                    mods = modCandidates[0];
                    Console.WriteLine($"{mods.FullName} is the mods directory, is that correct?");
                    ConsoleKeyInfo key = Console.ReadKey();
                    Console.WriteLine();
                    if (key.Key is not ConsoleKey.Y or ConsoleKey.Enter)
                    {
                        Console.WriteLine("Okay! Can you enter the path to the mods dir?");
                        mods = GetDirOf(dir, "mods");
                        if (mods is null)
                        {
                            return null;
                        }
                    }
                }

                return (unpacked, mods);
            }

            Console.WriteLine("Please enter the path to Stardew");
            string? next = Console.ReadLine();
            if (next is null)
            {
                continue;
            }

            dir = new (next);
        }
    }

    private static DirectoryInfo? GetDirOf(DirectoryInfo gamedir, string type)
    {
        string? read = null;
        while (true)
        {
            Console.WriteLine($"Please either enter in the name of the {type} directory or type 'exit' to exit.");
            read = Console.ReadLine();
            if (read == "exit")
            {
                return null;
            }
            if (read is null)
            {
                continue;
            }

            DirectoryInfo[] infos = gamedir.GetDirectories(read);
            if (infos.Length > 1)
            {
                Console.WriteLine($"More than one candidate found, please specify: {string.Join('\n', infos.Select(static info => info.FullName))}");
            }
            if (infos.Length == 1)
            {
                return infos[0];
            }
        }
    }

}