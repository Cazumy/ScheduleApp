namespace ScheduleApp.Services
{
    public interface IDatabasePathProvider
    {
        string SelectDatabasePath(string title = null);
    }
}
