using CommandLine;

namespace KitX.KXP.Helper;

public class Options
{
    [Option('v', "verbose", HelpText = "Prints all messages to standard output.")]
    public bool Verbose { get; set; } = false;
}
