namespace Lightforge;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var form = new Mainform();
        if (args.Length > 0 && File.Exists(args[0]))
            form.OpenFromCommandLine(args[0]);
        Application.Run(form);
    }
}
