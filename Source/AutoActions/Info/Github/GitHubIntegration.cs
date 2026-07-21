using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodectoryCore.Logging;
using Octokit;

namespace AutoActions.Info.Github
{
    public static class GitHubIntegration
    {
        private static GitHubClient _client = null;

        private static bool Initialized = false;
        private static void InitializeClient()
        {
            if (Initialized)
                return;
            Globals.Logs.Add($"Connecting to GitHub...", false);
            _client = new GitHubClient(new ProductHeaderValue("AutoActions"));
            _client.SetRequestTimeout(new TimeSpan(0, 0, 10));
            Initialized = true;
        }
        public static GitHubData GetGitHubData()
        {
            InitializeClient();
            Globals.Logs.Add($"Requesting releases...", false);
            Release release;
            // Point updates at this Chinese fork so auto-update keeps the localized build
            // instead of overwriting it with the upstream English/German release.
            release = _client.Repository.Release.GetLatest("Squarelan", "AutoActions").Result;
            Version latestGitHubVersion = ParseVersionTag(release.TagName);
            DateTime latestReleaseDate = release.PublishedAt.HasValue ? release.PublishedAt.Value.DateTime : DateTime.MinValue;
            Globals.Logs.Add($"Releases found. Latest version: {latestGitHubVersion}", false);

            List<string> sourceForgeAdditions = new List<string>()
            {
                "\n\n"+ @"[![Download HDR Profile]",
                "\n" + @"[![Download HDR Profile]",
                "\n" + @"[![Download HDR Profile]",
                "\n\n" + @"[![Download AutoActions]",
                "\n" + @"[![Download AutoActions]",
                "\n" + @"[![Download AutoActions]",
                "\n\n" + @"[![Download AutoHDR]",
                "\n" + @"[![Download AutoHDR]",
                "\n" + @"[![Download AutoHDR]"
            };

            string changelog = string.Empty;


            if (!string.IsNullOrEmpty(changelog))
                changelog += "\r\n\r\n\r\n\r\n";
            string releaseChangelog = release.Body;
            foreach (string sourceForgeAddition in sourceForgeAdditions)
            {
                if (releaseChangelog.Contains(sourceForgeAddition))
                    releaseChangelog = releaseChangelog.Substring(0, releaseChangelog.IndexOf(sourceForgeAddition));

            }

            changelog += $"[{release.TagName}]\r\n\r\n{releaseChangelog}";
            Globals.Logs.Add($"Creating GitHubData...", false);
            var assetx64 = release.Assets.FirstOrDefault(a => a.Name.ToUpperInvariant().Contains("_X64"));
            var assetx86 = release.Assets.FirstOrDefault(a => a.Name.ToUpperInvariant().Contains("_X86"));
            return new GitHubData(changelog, latestGitHubVersion, latestReleaseDate, $@"https://github.com/Squarelan/AutoActions/releases/tag/{release.TagName}", assetx64 != null ? assetx64.BrowserDownloadUrl : "", assetx86 != null ? assetx86.BrowserDownloadUrl : "");
        }

        /// <summary>
        /// Parses a release tag into a Version, tolerating a leading "v" and any
        /// non-numeric suffix (e.g. "v1.9.28", "1.9.28-zh"). Falls back to 0.0.0.0
        /// so a malformed tag can never crash the update check.
        /// </summary>
        private static Version ParseVersionTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return new Version(0, 0, 0);
            var match = System.Text.RegularExpressions.Regex.Match(tag, @"\d+(\.\d+){1,3}");
            if (!match.Success)
                return new Version(0, 0, 0);
            var parsed = new Version(match.Value);
            // Normalize to Major.Minor.Build (3 components) to match
            // VersionExtension.ApplicationVersion(), which drops the revision.
            // Otherwise a 4-segment tag like "1.9.28.0" compares as newer than the
            // running 3-segment "1.9.28" and the app updates to itself forever.
            return new Version(parsed.Major, System.Math.Max(parsed.Minor, 0), System.Math.Max(parsed.Build, 0));
        }
    }
}
