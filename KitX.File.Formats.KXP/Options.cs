using CommandLine;

namespace KitX.File.Formats.KXP;

public class Options
{
    [Option('v', "verbose", HelpText = "Prints all messages to standard output.")]
    public bool Verbose { get; set; } = false;
}
