using System.ComponentModel;

namespace ScheduleApp.Services
{
    public class UserModeService : IUserModeService
    {
        private bool _isStudentMode;
        private bool _isStudentModeSB;

        public bool IsStudentMode
        {
            get => _isStudentMode;
            set
            {
                if (_isStudentMode == value) return;
                _isStudentMode = value;
                OnPropertyChanged(nameof(IsStudentMode));
            }
        }

        public bool IsStudentModeSB
        {
            get => _isStudentModeSB;
            set
            {
                if (_isStudentModeSB == value) return;
                _isStudentModeSB = value;
                OnPropertyChanged(nameof(IsStudentModeSB));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
