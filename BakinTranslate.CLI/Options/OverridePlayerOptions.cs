using CommandLine;

namespace BakinTranslate.CLI.Options
{
    [Verb("override-player", HelpText = "Enable dictionary support by overriding the player executable.")]
    internal class OverridePlayerOptions
    {
        [Value(0, Required = true, MetaName = "game_directory", HelpText = "Root directory of the game (must contain the 'data' folder).")]
        public string GameDirectory { get; set; }
        [Value(1, Required = false, MetaName = "player_path", Default = "bakinplayer.exe", HelpText = "Path of the bakinplayer.exe to be injected.")]
        public string PlayerPath { get; set; }
    }
}
