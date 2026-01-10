using BakinTranslate.CLI.Options;
using System;
using System.IO;
using System.IO.Compression;

namespace BakinTranslate.CLI.Handler
{
    internal class OverridePlayerHandler
    {
        public void Handle(OverridePlayerOptions options)
        {
            var gameDirectory = options.GameDirectory;
            var playerPath = options.PlayerPath;
            var backupPlayerPath = Path.Combine(gameDirectory, "data", "bakinplayer.exe.bak");
            var originalPlayerPath = Path.Combine(gameDirectory, "data", "bakinplayer.exe");
            var zipPath = Path.Combine(gameDirectory, "data", "bakinplayer.pak");
            if (!File.Exists(backupPlayerPath))
            {
                if (File.Exists(originalPlayerPath))
                {
                    File.Copy(originalPlayerPath, backupPlayerPath);
                    Console.WriteLine($"Backup created at {backupPlayerPath}");
                }
                else if (File.Exists(zipPath))
                {
                    using (var fileStream = File.OpenRead(zipPath))
                    {
                        var zip = new ZipArchive(fileStream, ZipArchiveMode.Read);
                        using (var entryStream = zip.GetEntry("bakinplayer.exe").Open())
                        using (var backupStream = File.OpenWrite(backupPlayerPath))
                            entryStream.CopyTo(backupStream);
                    }
                    Console.WriteLine($"Backup created at {backupPlayerPath}");
                }
                else
                {
                    Console.WriteLine("Original player not found. Cannot create backup.");
                    return;
                }
            }

            if (File.Exists(zipPath))
            {
                using (var fileStream = File.OpenWrite(zipPath))
                {
                    var zip = new ZipArchive(fileStream, ZipArchiveMode.Create);
                    var entry = zip.CreateEntry("bakinplayer.exe");
                    using (var entryStream = entry.Open())
                    {
                        using (var playerStream = File.OpenRead(playerPath))
                        {
                            playerStream.CopyTo(entryStream);
                        }
                    }
                }
                Console.WriteLine($"Player injected into pak file {zipPath} successfully.");
            }
            else
            {
                File.Copy(playerPath, originalPlayerPath, overwrite: true);
                Console.WriteLine($"Player injected into {originalPlayerPath} successfully.");
            }
        }
    }
}
