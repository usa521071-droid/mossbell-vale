using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MossbellVale.Editor
{
    internal sealed class PublishOptions
    {
        public string Title, UniqueId, Version, Repository, ToolRoot, OutputFile;
    }

    internal static class PublishTool
    {
        internal const string OfficialRepository = "usa521071-droid/mossbell-vale";
        internal const string OfficialId = "F14B1";
        private static readonly Regex TitlePattern = new Regex(@"\A[A-Za-z0-9][A-Za-z0-9 ._-]{0,39}\z");
        private static readonly Regex IdPattern = new Regex(@"\AF[0-9A-F]{4}\z", RegexOptions.IgnoreCase);
        private static readonly Regex VersionPattern = new Regex(@"\A(0|[1-9][0-9]?)\.(0|[1-9][0-9]?)\.(0|[1-9][0-9]?)\z");
        private static readonly Regex RepoPattern = new Regex(@"\A[A-Za-z0-9][A-Za-z0-9-]{0,38}/[A-Za-z0-9_.-]{1,100}\z");

        public static string DefaultToolRoot
        {
            get
            {
                var configured = Environment.GetEnvironmentVariable("MOSSBELL_3DS_TOOLS");
                return String.IsNullOrWhiteSpace(configured) ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MossbellValeTools") : configured;
            }
        }

        public static string SafeTitle(string title)
        {
            return TitlePattern.IsMatch(title ?? "") ? title : "My Mossbell Game";
        }

        public static string NewUniqueId()
        {
            using (var random = RandomNumberGenerator.Create())
            {
                var bytes = new byte[2];
                string id;
                do
                {
                    random.GetBytes(bytes);
                    id = "F" + BitConverter.ToUInt16(bytes, 0).ToString("X4", CultureInfo.InvariantCulture);
                } while (id == OfficialId);
                return id;
            }
        }

        private static int BuildNumber(string version)
        {
            var match = VersionPattern.Match(version);
            if (!match.Success) throw new ArgumentException("Enter a version such as 1.0.0.");
            int major = Int32.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            int minor = Int32.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            int patch = Int32.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
            if (major > 63 || minor > 63 || patch > 15)
                throw new ArgumentException("The 3DS supports version components up to 63.63.15.");
            return major * 10000 + minor * 100 + patch;
        }

        public static void Validate(PublishOptions options)
        {
            if (!TitlePattern.IsMatch(options.Title))
                throw new ArgumentException("Enter a title of 1–40 letters, digits, spaces, periods, underscores or hyphens.");
            if (!IdPattern.IsMatch(options.UniqueId) || options.UniqueId.Equals(OfficialId, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Use a five-digit hexadecimal title ID starting with F. F14B1 is used by Mossbell Vale.");
            BuildNumber(options.Version);
            if (options.Version == "0.0.0") throw new ArgumentException("Use a version higher than 0.0.0.");
            if (options.Repository.Length > 0 && !RepoPattern.IsMatch(options.Repository))
                throw new ArgumentException("Enter the GitHub repository as Owner/Repository.");
            if (options.Repository.Equals(OfficialRepository, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose your own GitHub repository for this game.");
            if (!Path.GetExtension(options.OutputFile).Equals(".cia", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose an output file ending in .cia.");
            if (options.ToolRoot.Trim().Length == 0) throw new ArgumentException("Choose a toolchain folder.");
            options.ToolRoot = Path.GetFullPath(options.ToolRoot);
            options.OutputFile = Path.GetFullPath(options.OutputFile);
        }

        public static string SettingsFile(string projectKey, string title)
        {
            var key = String.IsNullOrWhiteSpace(projectKey) ? "title:" + title : "file:" + Path.GetFullPath(projectKey);
            using (var sha = SHA256.Create())
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MossbellVale", "Publish", Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(key.ToUpperInvariant()))) + ".txt");
            }
        }

        public static void Build(PublishOptions options, byte[] project, byte[] art, Action<string> log,
            CancellationToken token)
        {
            Validate(options);
            if (project == null || project.Length == 0) throw new ArgumentException("The project snapshot is empty.");
            Directory.CreateDirectory(options.ToolRoot);
            var staging = Path.Combine(options.ToolRoot, "publish-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                ExtractTemplate(staging, token);
                var assets = Path.Combine(staging, "assets");
                var projectPath = Path.Combine(assets, "project.rpgp");
                File.WriteAllBytes(projectPath, project);
                var arguments = new[] { "-Version", options.Version, "-Title", options.Title, "-UniqueId", options.UniqueId,
                    "-ProjectPack", projectPath, "-ToolRoot", options.ToolRoot, "-OutputDirectory", Path.Combine(staging, "output"),
                    "-CiaOnly" }.ToList();
                if (art != null && art.Length != 0)
                {
                    var artPath = Path.Combine(assets, "project-art.rpga");
                    File.WriteAllBytes(artPath, art);
                    arguments.AddRange(new[] { "-ProjectArt", artPath });
                }
                if (options.Repository.Length != 0)
                    arguments.AddRange(new[] { "-GitHubRepository", options.Repository });
                EnsureTools(staging, options.ToolRoot, log, token);
                log("Building Nintendo 3DS CIA…");
                RunPowerShell(staging, "Build-3DS.ps1", arguments.ToArray(), log, token);
                var result = Path.Combine(staging, "output", "MossbellVale.cia");
                if (!File.Exists(result)) throw new IOException("The build did not produce a CIA file.");
                token.ThrowIfCancellationRequested();
                SaveArtifact(result, options.OutputFile, token);
                log("Saved " + options.OutputFile);
            }
            finally { Clean(staging, log); }
        }

        public static void Upload(PublishOptions options, string expectedHash, Action<string> log, CancellationToken token)
        {
            Validate(options);
            if (options.Repository.Length == 0) throw new ArgumentException("Enter a GitHub repository before building.");
            var staging = Path.Combine(options.ToolRoot, "release-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                var cia = Path.Combine(staging, "MossbellVale.cia");
                File.Copy(options.OutputFile, cia);
                string sha = HashFile(cia);
                if (!String.Equals(sha, expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("The saved CIA changed. Build it again before uploading.");
                var catalog = Path.Combine(staging, "catalog.txt");
                File.WriteAllText(catalog, "MVB1 " + BuildNumber(options.Version).ToString(CultureInfo.InvariantCulture) +
                    " 0 " + sha + "\n", new UTF8Encoding(false));
                var gh = FindGitHubCli();
                var tag = "v" + options.Version;
                log("Creating " + options.Repository + " release " + tag + "…");
                Run(gh, staging, new[] { "release", "create", tag, cia, catalog, "--repo", options.Repository,
                    "--title", options.Title + " " + tag, "--notes", "Nintendo 3DS application from Mossbell Vale Editor.",
                    "--latest" }, log, token);
            }
            finally { Clean(staging, log); }
        }

        public static string HashFile(string file)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(file))
                return Hex(sha.ComputeHash(stream));
        }

        private static string Hex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private static void SaveArtifact(string result, string output, CancellationToken token)
        {
            var folder = Path.GetDirectoryName(output);
            Directory.CreateDirectory(folder);
            var pending = Path.Combine(folder, "." + Guid.NewGuid().ToString("N") + ".cia.tmp");
            try
            {
                File.Copy(result, pending);
                token.ThrowIfCancellationRequested();
                if (File.Exists(output)) File.Replace(pending, output, null);
                else File.Move(pending, output);
            }
            finally { File.Delete(pending); }
        }

        private static void ExtractTemplate(string root, CancellationToken token)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("MossbellVale.PublishSource.zip"))
            {
                if (resource == null) throw new InvalidOperationException("The editor is missing its 3DS build files.");
                using (var zip = new ZipArchive(resource, ZipArchiveMode.Read))
                {
                    foreach (var entry in zip.Entries)
                    {
                        token.ThrowIfCancellationRequested();
                        var path = Path.GetFullPath(Path.Combine(root, entry.FullName));
                        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("The build archive has an invalid path.");
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using (var source = entry.Open())
                        using (var file = File.Create(path))
                            source.CopyTo(file);
                    }
                }
            }
        }

        private static void EnsureTools(string staging, string toolRoot, Action<string> log, CancellationToken token)
        {
            string mutexName;
            using (var sha = SHA256.Create()) mutexName = "Local\\MossbellSDK-" +
                Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(toolRoot.ToUpperInvariant())));
            using (var mutex = new Mutex(false, mutexName))
            {
                bool ownsMutex = false;
                try
                {
                    try { ownsMutex = WaitHandle.WaitAny(new WaitHandle[] { mutex, token.WaitHandle }) == 0; }
                    catch (AbandonedMutexException) { ownsMutex = true; }
                    token.ThrowIfCancellationRequested();
                    log("Checking the portable 3DS tools…");
                    RunPowerShell(staging, "Install-3DSTools.ps1", new[] { "-ToolRoot", toolRoot }, log, token);
                }
                finally { if (ownsMutex) mutex.ReleaseMutex(); }
            }
        }

        private static void RunPowerShell(string working, string script, string[] arguments, Action<string> log,
            CancellationToken token)
        {
            var shell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
            var command = new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass",
                "-File", Path.Combine(working, "build", script) };
            Run(shell, working, command.Concat(arguments).ToArray(), log, token);
        }

        private static string FindGitHubCli()
        {
            var candidates = (Environment.GetEnvironmentVariable("PATH") ?? "")
                .Split(Path.PathSeparator).Concat(new[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GitHub CLI"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "GitHub CLI")
                });
            foreach (var folder in candidates)
            {
                if (String.IsNullOrWhiteSpace(folder)) continue;
                var path = Path.Combine(folder.Trim('"'), "gh.exe");
                if (File.Exists(path)) return path;
            }
            throw new InvalidOperationException("Install GitHub CLI and run gh auth login before uploading.");
        }

        private static void Run(string executable, string working, string[] arguments, Action<string> log,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var settings = new ProcessStartInfo(executable, String.Join(" ", arguments.Select(Quote)))
            {
                WorkingDirectory = working, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            settings.EnvironmentVariables["GH_PROMPT_DISABLED"] = "1";
            using (var process = new Process { StartInfo = settings })
            {
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs line)
                {
                    if (!String.IsNullOrWhiteSpace(line.Data)) log(line.Data);
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs line)
                {
                    if (!String.IsNullOrWhiteSpace(line.Data)) log(line.Data);
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                while (!process.WaitForExit(150))
                {
                    if (!token.IsCancellationRequested) continue;
                    StopTree(process);
                    token.ThrowIfCancellationRequested();
                }
                process.WaitForExit();
                token.ThrowIfCancellationRequested();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(Path.GetFileName(executable) + " exited with code " +
                        process.ExitCode.ToString(CultureInfo.InvariantCulture) + ". See the log for details.");
            }
        }

        private static void StopTree(Process process)
        {
            if (process.HasExited) return;
            var cmd = Path.Combine(Environment.SystemDirectory, "taskkill.exe");
            using (var kill = Process.Start(new ProcessStartInfo(cmd, "/PID " +
                process.Id.ToString(CultureInfo.InvariantCulture) + " /T /F")
                { UseShellExecute = false, CreateNoWindow = true }))
            {
                kill.WaitForExit();
            }
            if (!process.WaitForExit(3000)) process.Kill();
        }

        private static string Quote(string value)
        {
            return "\"" + Regex.Replace(value, @"(\\*)(\""|$)", match =>
                new string('\\', match.Groups[1].Length * 2) +
                (match.Groups[2].Value == "\"" ? "\\\"" : "")) + "\"";
        }

        private static void Clean(string staging, Action<string> log)
        {
            try { Directory.Delete(staging, true); }
            catch (IOException) { log("Temporary build files remain in " + staging); }
            catch (UnauthorizedAccessException) { log("Temporary build files remain in " + staging); }
        }
    }
}
