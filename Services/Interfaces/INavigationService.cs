namespace ScheduleApp.Services
{
    public enum PageType
    {
        MainPage,
        SchedulePage,
        DbAdminPage
    }
    public interface INavigationService
    {
        void CreateNewWindow(PageType page);
    }
}
