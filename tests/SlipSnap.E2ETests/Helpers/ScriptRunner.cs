using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SlipSnap.E2ETests.Helpers;

public static class ScriptRunner
{
    private static readonly string ScriptsDir = FindScriptsDir();

    public static JsonDocument Run(string scriptName, string arguments = "")
    {
        var scriptPath = Path.Combine(ScriptsDir, scriptName);
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Script not found: {scriptPath}");

        var psi = new ProcessStartInfo
        {
            FileName = "pwsh",
            Arguments = $"-NoLogo -NonInteractive -ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = FindRepoRoot()
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start pwsh for {scriptName}");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(TimeSpan.FromSeconds(30));

        if (string.IsNullOrWhiteSpace(stdout))
            throw new InvalidOperationException(
                $"Script {scriptName} produced no output. Exit code: {process.ExitCode}. Stderr: {stderr}");

        return JsonDocument.Parse(stdout);
    }

    private static string FindScriptsDir()
    {
        var root = FindRepoRoot();
        return Path.Combine(root, ".github", "skills", "uia-testing", "scripts");
    }

    private static string FindRepoRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "src")) &&
                Directory.Exists(Path.Combine(dir, "tests")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new DirectoryNotFoundException("Could not find repository root");
    }
}
