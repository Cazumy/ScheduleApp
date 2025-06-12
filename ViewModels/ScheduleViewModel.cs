using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScheduleApp.Models;
using ScheduleApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace ScheduleApp.ViewModels
{
    public partial class ScheduleViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IDatabaseService _databaseService;
        private readonly IUserModeService _userModeService;
        #region property changed
        private void Subject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInternalEvent || e.PropertyName != nameof(ScheduleItem.Subject)) return;
            _isInternalEvent = true;
            if (sender is ScheduleItem item)
            {
                item.Teacher = _databaseService.GetTeacher(item.Subject);
                item.SubjectType = _databaseService.GetSubjectType(item.Subject);
            }
            _isInternalEvent = false;
        }
        private void SubjectType_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInternalEvent || e.PropertyName != nameof(ScheduleItem.SubjectType)) return;
            _isInternalEvent = true;
            if (sender is ScheduleItem item)
            {
                item.Subject = _databaseService.GetSubjectByType(item.SubjectType);
                item.Teacher = _databaseService.GetTeacher(item.Subject);
            }
            _isInternalEvent = false;
        }
        private void Teacher_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInternalEvent || e.PropertyName != nameof(ScheduleItem.Teacher)) return;
            _isInternalEvent = true;
            if (sender is ScheduleItem item)
            {
                item.Subject = _databaseService.GetSubject(item.Teacher);
                item.SubjectType = _databaseService.GetSubjectType(item.Subject);
            }
            _isInternalEvent = false;
        }
        private void Room_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInternalEvent || e.PropertyName != nameof(ScheduleItem.Room)) return;
            _isInternalEvent = true;
            if (sender is ScheduleItem item)
            {
                item.Address = _databaseService.GetFullAddress(item.Room);
            }
            _isInternalEvent = false;
        }
        private void Address_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isInternalEvent || e.PropertyName != nameof(ScheduleItem.Address)) return;
            _isInternalEvent = true;
            if (sender is ScheduleItem item)
            {
                item.Room = _databaseService.GetRoom(item.Address);
            }
            _isInternalEvent = false;
        }
        #endregion
        #region ICommands
        [RelayCommand]
        private void ReturnToHome() => _navigationService.CreateNewWindow(PageType.MainPage);
        [RelayCommand]
        private void AddNewScheduleItem()
        {
            if (ScheduleItems.Count < 8 && SelectedGroupId != 0 && SelectedDate != default)
            {
                var item = new ScheduleItem("", "", "", "", "", "", 0);
                SubscribeScheduleItem(item);
                ScheduleItems.Add(item);
            }
        }
        [RelayCommand]
        private void SaveScheduleItems()
        {
            if (SelectedGroupId != 0 && SelectedDate != default)
                _databaseService.SetScheduleItems(ScheduleItems, SelectedDate.Date);

            ExistingDates = _databaseService.GetDates(SelectedGroupId);
        }
        [RelayCommand]
        private void GetSchedule()
        {
            if (SelectedGroupId != 0 && SelectedDate != default)
                ScheduleItems = _databaseService.GetSchedule(SelectedGroupId, SelectedDate);
        }
        #endregion
        #region collections
        [ObservableProperty]
        private ObservableCollection<Group> groups;

        public ObservableCollection<ScheduleItem> scheduleItems = [];
        public ObservableCollection<ScheduleItem> ScheduleItems
        {
            get => scheduleItems;
            set
            {
                if (scheduleItems == value) return;
                scheduleItems = value;
                OnPropertyChanged();
                scheduleItems?.ToList().ForEach(SubscribeScheduleItem);
            }
        }
        public ObservableCollection<string> Times { get; set; }
        public ObservableCollection<string> SubjectTypes { get; set; }
        public ObservableCollection<string> Subjects { get; set; }
        public ObservableCollection<string> Rooms { get; set; }
        public ObservableCollection<string> Addresses { get; set; }
        public ObservableCollection<string> Teachers { get; set; }
        #endregion
        #region fields
        private int selectedGroupId;
        public int SelectedGroupId
        {
            get => selectedGroupId;
            set
            {
                if (selectedGroupId == value) return;
                selectedGroupId = value;
                OnPropertyChanged();
                if(SelectedDate != default) ScheduleItems = _databaseService.GetSchedule(selectedGroupId, SelectedDate);
                Subjects = _databaseService.GetSubjects(selectedGroupId);
                Teachers = _databaseService.GetTeachers(selectedGroupId);
                ExistingDates = _databaseService.GetDates(SelectedGroupId);
            }
        }
        private ObservableCollection<DateTime> existingDates = []; // будут загружаться все даты, не оптимизировано
        public ObservableCollection<DateTime> ExistingDates
        {
            get => existingDates;
            private set
            {
                existingDates = value;
                OnPropertyChanged();
            }
        }
        private DateTime selectedDate;
        public DateTime SelectedDate
        {
            get => selectedDate;
            set
            {
                if (selectedDate == value) return;
                selectedDate = value;
                if (SelectedGroupId != default) ScheduleItems = _databaseService.GetSchedule(SelectedGroupId, selectedDate);
                OnPropertyChanged();
            }
        }
        private bool _isInternalEvent;
        public bool IsStudentMode => _userModeService.IsStudentMode;
        public bool IsStudentModeSB => _userModeService.IsStudentModeSB;
        #endregion
        public ScheduleViewModel(INavigationService navigationService, IDatabaseService databaseService, IUserModeService userModeService)
        {
            _navigationService = navigationService;
            _databaseService = databaseService;
            _userModeService = userModeService;
            try
            {
                _userModeService.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(_userModeService.IsStudentMode))
                        OnPropertyChanged(nameof(IsStudentMode));
                    if (e.PropertyName == nameof(_userModeService.IsStudentModeSB))
                        OnPropertyChanged(nameof(IsStudentModeSB));
                };
                groups = _databaseService.GetGroups();

                Times = _databaseService.GetTimes();
                SubjectTypes = _databaseService.GetSubjectTypes();
                Rooms = _databaseService.GetRooms();
                Addresses = _databaseService.GetAddresses();
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
        }
        private void SubscribeScheduleItem(ScheduleItem item)
        {
            item.PropertyChanged += Subject_PropertyChanged;
            item.PropertyChanged += Teacher_PropertyChanged;
            item.PropertyChanged += Room_PropertyChanged;
            item.PropertyChanged += Address_PropertyChanged;
            item.PropertyChanged += SubjectType_PropertyChanged;
        }
    }
}
