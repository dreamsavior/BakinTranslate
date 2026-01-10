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
            if (!File.Exists(backupPlayerPath) && File.Exists(originalPlayerPath))
            {
                File.Copy(originalPlayerPath, backupPlayerPath);
                Console.WriteLine($"Backup created at {backupPlayerPath}");
            }
            var zipPath = Path.Combine(gameDirectory, "data", "bakinplayer.pak");
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
                File.Copy(playerPath, originalPlayerPath);
                Console.WriteLine($"Player injected into {originalPlayerPath} successfully.");
            }
        }
    }
}
