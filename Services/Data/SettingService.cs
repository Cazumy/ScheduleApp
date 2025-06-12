using System;
using System.IO;

namespace ScheduleApp.Services
{
    public class SettingService : ISettingsService
    {
        private readonly string dbConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "_ScheduleApp", "config.txt");

        public string GetLastUsedDbPath() =>
            File.Exists(dbConfigPath) ? File.ReadAllText(dbConfigPath) : null;

        public void SaveDbPath(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dbConfigPath));
            File.WriteAllText(dbConfigPath, path);
        }

        public void DeleteDbPath()
        {
            if (File.Exists(dbConfigPath))
                File.Delete(dbConfigPath);
        }
    }
}
