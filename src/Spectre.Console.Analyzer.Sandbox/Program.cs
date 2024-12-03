using System.IO;
using System.Linq;
using Spectre.Console.Cli;

namespace Spectre.Console.Analyzer.Sandbox;
internal class Settings : CommandSettings
{
    [CommandArgument(0, "<PROGRAM>", typeof(FileInfo))]
    public FileInfo Foo { get; set; }

    [CommandArgument(1, "<PROGRAM>")]
    public ILookup<int, int> Bar3 { get; set; }

    [CommandArgument(2, "<PROGRAM>")]
    public MemoryStream Stream { get; set; }

    [CommandArgument(1, "<PROGRAM>")]
    public int Name { get; set; }

    [CommandOption("-h|--help", typeof(FileInfo))]
    public FileInfo Bar { get; set; }
}