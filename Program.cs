using System.Diagnostics;

public static class Program
{
    public static int Main(string[] args)
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."))
        };

        var rootPath = candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(path => File.Exists(Path.Combine(path, "CooperativaSolution", "Cooperativa.Web", "Cooperativa.Web.csproj")));

        if (rootPath is null)
        {
            Console.Error.WriteLine("Não foi possível localizar a pasta da solução ou o projeto web.");
            return 1;
        }

        var webProjectPath = Path.Combine(rootPath, "CooperativaSolution", "Cooperativa.Web", "Cooperativa.Web.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{webProjectPath}\" {string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}",
            WorkingDirectory = rootPath,
            UseShellExecute = false,
        };

        var process = Process.Start(startInfo);
        if (process is null)
        {
            Console.Error.WriteLine("Não foi possível iniciar o projeto web.");
            return 1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}


