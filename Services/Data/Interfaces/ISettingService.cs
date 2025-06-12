namespace ScheduleApp.Services
{
    public interface ISettingsService
    {
        string GetLastUsedDbPath();
        void SaveDbPath(string path);
        void DeleteDbPath();
    }
}
