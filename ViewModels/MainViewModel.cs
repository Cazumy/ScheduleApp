using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScheduleApp.Services;
using System;
using System.Windows;

namespace ScheduleApp.ViewModels
{
    public partial class MainViewModel(INavigationService navigationService, IDatabasePathProvider databasePathProvider,
        IDatabaseService databaseService, IUserModeService userModeService) : ObservableObject
    {
        private readonly INavigationService _navigationService = navigationService;
        private readonly IDatabasePathProvider _databasePathProvider = databasePathProvider;
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly IUserModeService _userModeService = userModeService;

        [RelayCommand]
        private void NavigateToSchedule()
        {
            _userModeService.IsStudentMode = true;
            _userModeService.IsStudentModeSB = false;
            _navigationService.CreateNewWindow(PageType.SchedulePage);
        }
        [RelayCommand]
        private void NavigateToScheduleMethodist()
        {
            _userModeService.IsStudentMode = false;
            _userModeService.IsStudentModeSB = true;
            _navigationService.CreateNewWindow(PageType.SchedulePage);
        }
        [RelayCommand]
        private void NavigateToScheduleAdmin() => _navigationService.CreateNewWindow(PageType.DbAdminPage);

        [RelayCommand]
        private void SelectNewBdFile()
        {
            var newPath = _databasePathProvider.SelectDatabasePath("Выберите новую базу данных");
            if (string.IsNullOrWhiteSpace(newPath)) return;

            try
            {
                _databaseService.ReloadDatabase(newPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке базы данных:\n" + ex.Message);
            }
        }
    }
}
