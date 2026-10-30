using System.IO;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

// Shows the git branch of this checkout in the main toolbar, so editors opened
// from different worktrees can be told apart at a glance.
static class GitBranchToolbar
{
    const string ElementPath = "Git/Branch";
    const double PollInterval = 2.0;

    static string s_HeadPath;
    static string s_Branch;
    static double s_NextPoll;

    [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
    static MainToolbarElement CreateBranchLabel()
    {
        s_Branch ??= ReadBranch();
        return new MainToolbarLabel(new MainToolbarContent("git: " + s_Branch, "Current git branch of this project"));
    }

    [InitializeOnLoadMethod]
    static void Init()
    {
        EditorApplication.update -= Poll;
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < s_NextPoll)
            return;
        s_NextPoll = EditorApplication.timeSinceStartup + PollInterval;

        var branch = ReadBranch();
        if (branch == s_Branch)
            return;
        s_Branch = branch;
        MainToolbar.Refresh(ElementPath);
    }

    static string ReadBranch()
    {
        s_HeadPath ??= FindHeadPath();
        if (s_HeadPath == null)
            return "(no git)";

        string head;
        try { head = File.ReadAllText(s_HeadPath).Trim(); }
        catch (IOException) { return "(unknown)"; }

        const string refPrefix = "ref: refs/heads/";
        if (head.StartsWith(refPrefix))
            return head.Substring(refPrefix.Length);
        return head.Length >= 7 ? "detached " + head.Substring(0, 7) : head;
    }

    // Walks up from the project folder to the checkout root. In a worktree
    // `.git` is a file pointing at the worktree's own git dir.
    static string FindHeadPath()
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(Application.dataPath));
        for (; dir != null; dir = dir.Parent)
        {
            var dotGit = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(dotGit))
                return Path.Combine(dotGit, "HEAD");
            if (!File.Exists(dotGit))
                continue;

            var line = File.ReadAllText(dotGit).Trim();
            const string gitDirPrefix = "gitdir:";
            if (!line.StartsWith(gitDirPrefix))
                return null;
            var gitDir = line.Substring(gitDirPrefix.Length).Trim();
            if (!Path.IsPathRooted(gitDir))
                gitDir = Path.GetFullPath(Path.Combine(dir.FullName, gitDir));
            return Path.Combine(gitDir, "HEAD");
        }
        return null;
    }
}
