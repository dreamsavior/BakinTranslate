using CommandLine;

namespace BakinTranslate.CLI.Options
{
    [Verb("dump-split", HelpText = "Generate translation dictionaries split by source file.")]
    internal class DumpSplitOptions
    {
        [Value(0, Required = true, MetaName = "game_directory", HelpText = "Root directory of the game (must contain the 'data' folder).")]
        public string GameDirectory { get; set; }

        [Value(1, Required = true, MetaName = "unpack_directory", HelpText = "Directory containing files unpacked by BakinExtractor.")]
        public string UnpackDirectory { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output directory for split translation files.")]
        public string OutputDirectory { get; set; }
    }
}