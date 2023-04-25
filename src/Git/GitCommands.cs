using CliWrap;
using CliWrap.Buffered;

using System.IO;

namespace AtlConsultingIo.DevOps.CliCommands;
internal static class GitCommands
{
    public static async Task<BufferedCommandResult> PushCommit(AtlProject project)
        => await Cli.Wrap("git")
                    .WithWorkingDirectory(project.PhysicalDirectory.FullName )
                    .WithArguments("push")
                    .ExecuteBufferedAsync();
    public static async Task<BufferedCommandResult> AddCommitFiles(AtlProject project)
        => await Cli.Wrap("git")
                    .WithWorkingDirectory(project.PhysicalDirectory.FullName)
                    .WithArguments("add -A")
                    .ExecuteBufferedAsync();

    public static async Task<BufferedCommandResult> AddCommit(AtlProject projectDirectory, string? message, bool useCommitTemplate = true)
    {
        var templateFile = !useCommitTemplate || projectDirectory.GetCommitMessageFile() is null ? null : projectDirectory.GetCommitMessageFile();

        return templateFile is null ?
            await CommitWithMessage(projectDirectory, string.IsNullOrEmpty(message) ? "Default Commit Message" : message!) :
            await CommitWithFile(projectDirectory, templateFile);
    }

    private static async Task<BufferedCommandResult> CommitWithFile(AtlProject project, FileInfo commitTemplate)
        => await Cli.Wrap("git")
                    .WithWorkingDirectory(project.PhysicalDirectory.FullName )
                    .WithArguments($"commit -F {commitTemplate.FullName}")
                    .ExecuteBufferedAsync();

    private static async Task<BufferedCommandResult> CommitWithMessage(AtlProject project, string message)
        => await Cli.Wrap("git")
                    .WithWorkingDirectory(project.PhysicalDirectory.FullName )
                    .WithArguments($"commit -m {message}")
                    .ExecuteBufferedAsync();


    public static string GetGhAccessToken()
    {
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(userDir, ".config", "ghtokens", ".project-builder-token");

        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

}
