using BakinTranslate.CLI.Handler;
using BakinTranslate.CLI.Options;
using CommandLine;
using System;

namespace BakinTranslate.CLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments(args, new Type[] { 
                typeof(DumpOptions),
                typeof(DumpSplitOptions),
                typeof(OverridePlayerOptions)
            }).Value;
            if (options is DumpOptions dumpOptions)
                new DumpHandler().Handle(dumpOptions);
            else if (options is DumpSplitOptions dumpSplitOptions)
                new DumpHandler().Handle(dumpSplitOptions);
            else if (options is OverridePlayerOptions injectOptions)
                new OverridePlayerHandler().Handle(injectOptions);
        }
    }
}
