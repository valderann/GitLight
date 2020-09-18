using System;
using System.IO;
using System.Text.Json;

namespace GitLite.Storage
{
    public class ConfigStore
    {
        public ConfigStore()
        {
        }

        public void Store(Settings data)
        {
            var rootPath = GetRootPath();
            var location = GetSettingsPath();
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

            var jsonString = JsonSerializer.Serialize<Settings>(data);
            File.WriteAllText(location, jsonString);
        }

        public Settings Load()
        {
            var location = GetSettingsPath();
            if (!File.Exists(location)) Store(new Settings());

            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(location));
        }

        public static string GetRootPath()
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GitStat");

        public static string GetSettingsPath()
            => Path.Combine(GetRootPath(), "Settings.json");
    }

    public class Settings
    {
        public Settings()
        {
            Repositories = Array.Empty<RepoSettings>();
        }

        public string SelectedRepoName { get; set; }
        public string SelectedBranchName { get; set; }

        public RepoSettings[] Repositories { get; set; }
    }

    public class RepoSettings
    {
        public string Name { get; set; }
        public string Location { get; set; }

        public override bool Equals(object obj)
        {
            var settings = obj as RepoSettings;
            if (settings == null) return false;
            return Name.Equals(settings.Name, StringComparison.Ordinal) && Location.Equals(settings.Location, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Name.GetHashCode(StringComparison.Ordinal) + Location.GetHashCode(StringComparison.Ordinal);
            }
        }

        public override string ToString()
            => Name;
    }
}
