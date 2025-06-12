using System.ComponentModel;

namespace ScheduleApp.Services
{
    public interface IUserModeService : INotifyPropertyChanged
    {
        bool IsStudentMode { get; set; }
        bool IsStudentModeSB { get; set; }
    }
}
