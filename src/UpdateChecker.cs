using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DriveRecorderConverter
{
    internal static class UpdateChecker
    {
        private const string CurrentVersion = "4.0";
        private const string GitHubApiUrl = "https://api.github.com/repos/jbransden/BMW-Drive-Recorder-Processor/releases/latest";

        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("BMW-Drive-Recorder-Processor");

                var release = await client.GetFromJsonAsync<GitHubRelease>(GitHubApiUrl);
                if (release == null) return;

                string latestVersion = release.TagName.TrimStart('v', 'V');

                if (string.Compare(latestVersion, CurrentVersion, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    var result = MessageBox.Show(
                        $"A new version ({latestVersion}) is available.\n\nCurrent version: {CurrentVersion}\n\nWould you like to open the download page?",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = release.HtmlUrl,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch
            {
                // Silently ignore update check failures (no internet, API limit, etc.)
            }
        }

        private class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = string.Empty;
        }
    }
}
