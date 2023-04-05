using CliWrap;
using CliWrap.Buffered;

using System.IO;

namespace AtlConsultingIo.DevOps.CliCommands;
internal static class GitCLI
{
    public static async Task<BufferedCommandResult> PushCommit(ProjectDirectory project)
        => await Cli.Wrap("git")
                    .WithWorkingDirectory(project.Path)
                    .WithArguments("push")
                    .ExecuteBufferedAsync();
    public static async Task<BufferedCommandResult> AddCommitFiles(ProjectDirectory project)
        => await Cli.Wrap("git")
                    .WithWorkingDirectory(project.Path)
                    .WithArguments("add -A")
                    .ExecuteBufferedAsync();

    public static async Task<BufferedCommandResult> AddCommit(ProjectDirectory projectDirectory, string? message, bool useCommitTemplate = true)
    {
        var templateFile = !useCommitTemplate || projectDirectory.GetCommitMessageFile() is null ? null : projectDirectory.GetCommitMessageFile();

        return templateFile is null ?
            await CommitWithMessage(projectDirectory, string.IsNullOrEmpty(message) ? "Default Commit Message" : message!) :
            await CommitWithFile(projectDirectory, templateFile);
    }

    private static async Task<BufferedCommandResult> CommitWithFile(ProjectDirectory project, FileInfo commitTemplate)
        => await Cli.Wrap("git")
                    .WithWorkingDirectory(project.Path)
                    .WithArguments($"commit -F {commitTemplate.FullName}")
                    .ExecuteBufferedAsync();

    private static async Task<BufferedCommandResult> CommitWithMessage(ProjectDirectory project, string message)
        => await Cli.Wrap("git")
                    .WithWorkingDirectory(project.Path)
                    .WithArguments($"commit -m {message}")
                    .ExecuteBufferedAsync();

    private static string GetGhPackagesToken()
    {
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(userDir, ".config", "ghtokens", ".packages-token");

        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
    public static string GetGhAccessToken()
    {
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(userDir, ".config", "ghtokens", ".project-builder-token");

        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

}
