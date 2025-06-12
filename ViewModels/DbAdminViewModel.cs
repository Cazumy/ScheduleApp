using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScheduleApp.Models;
using ScheduleApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ScheduleApp.ViewModels
{
    public partial class DbAdminViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IDatabaseService _databaseService;
        private enum DbEntitieIds
        {
            Адреса,
            Аудитории,
            Группы,
            Дисциплины,
            Преподаватели
        }

        [RelayCommand]
        private void ReturnToHome() => _navigationService.CreateNewWindow(PageType.MainPage);
        [RelayCommand]
        private void DeleteDbValue()
        {
            if (dbEntityId == -1) return;
            _databaseService.DeleteDbValue(Enum.GetName(typeof(DbEntitieIds), dbEntityId), DbEntityValueId);
            _navigationService.CreateNewWindow(PageType.DbAdminPage);
        }
        [RelayCommand]
        private void AddDbValue()
        {
            if (dbEntityId == -1 || InputText?.Length == 0) return;

            string[] values = [.. InputText.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim())];
            bool isRelated = relatedValueId != -1;
            if (isRelated)
            {
                Array.Resize(ref values, values.Length + 1);
                values[values.Length - 1] = relatedValueId.ToString();
            }
           _databaseService.AddDbValue(Enum.GetName(typeof(DbEntitieIds), dbEntityId), values, isRelated, SecondRelatedValueId, ThirdRelatedValueId);

            _navigationService.CreateNewWindow(PageType.DbAdminPage);
        }

        [ObservableProperty]
        private ObservableCollection<DbEntity> dbEntities = [];

        private ObservableCollection<DbEntityValue> dbEntitieValues;
        public ObservableCollection<DbEntityValue> DbEntitieValues
        {
            get => dbEntitieValues;
            set => SetProperty(ref dbEntitieValues, value);
        }

        private ObservableCollection<DbEntityValue> relatedValues;
        public ObservableCollection<DbEntityValue> RelatedValues
        {
            get => relatedValues;
            set => SetProperty(ref relatedValues, value);
        }
        private ObservableCollection<DbEntityValue> secondRelatedValues;
        public ObservableCollection<DbEntityValue> SecondRelatedValues
        {
            get => secondRelatedValues;
            set => SetProperty(ref secondRelatedValues, value);
        }
        private ObservableCollection<DbEntityValue> thirdRelatedValues;
        public ObservableCollection<DbEntityValue> ThirdRelatedValues
        {
            get => thirdRelatedValues;
            set => SetProperty(ref thirdRelatedValues, value);
        }

        private int dbEntityId = -1;
        public int DbEntityId
        {
            get => dbEntityId;
            set
            {
                if (SetProperty(ref dbEntityId, value))
                {
                    var values = _databaseService.GetAllFrom(Enum.ToObject(typeof(DbEntitieIds), value).ToString());

                    DbEntitieValues = new ObservableCollection<DbEntityValue>(
                        values.Select(val =>
                        {
                            int spaceIndex = val.IndexOf(' ');
                            string name = spaceIndex != -1 ? val.Substring(spaceIndex + 1).Trim() : val;
                            int id = spaceIndex != -1 ? int.Parse(val.Substring(0, spaceIndex)) : 0;

                            return new DbEntityValue(id, name);
                        })
                    );

                    var relatedId = value switch
                    {
                        0 => 1,
                        1 => 0,
                        2 => 3,
                        3 => 2,
                        4 => 3,
                        _ => -1
                    };
                    var relatedValues = _databaseService.GetAllFrom(Enum.ToObject(typeof(DbEntitieIds), relatedId).ToString());
                    RelatedValues = new ObservableCollection<DbEntityValue>(
                        relatedValues.Select(val =>
                        {
                            int spaceIndex = val.IndexOf(' ');
                            string name = spaceIndex != -1 ? val.Substring(spaceIndex + 1).Trim() : val;
                            int id = spaceIndex != -1 ? int.Parse(val.Substring(0, spaceIndex)) : 0;
                            return new DbEntityValue(id, name);
                        })
                    );

                    HelperText = Enum.ToObject(typeof(DbEntitieIds), value) switch
                    {
                        DbEntitieIds.Адреса => "(Корпус, Улица, Дом)",
                        DbEntitieIds.Аудитории => "(Номер)",
                        DbEntitieIds.Группы => "(Именование)",
                        DbEntitieIds.Дисциплины => "(Имя)",
                        DbEntitieIds.Преподаватели => "(Фамилия, Имя, Отчество)",
                        _ => ""
                    };

                    IsNotSimpleSelected = value >= 2 && value <=3;
                    IsSimpleSelected = value < 2;
                    IsTeacherSelected = value == 4;

                    if (IsNotSimpleSelected || IsTeacherSelected)
                    {
                        var secondRelatedValues = _databaseService.GetAllFrom("Тип_Дисциплины");
                        SecondRelatedValues = new ObservableCollection<DbEntityValue>(
                            secondRelatedValues.Select(val =>
                            {
                                int spaceIndex = val.IndexOf(' ');
                                string name = spaceIndex != -1 ? val.Substring(spaceIndex + 1).Trim() : val;
                                int id = spaceIndex != -1 ? int.Parse(val.Substring(0, spaceIndex)) : 0;
                                return new DbEntityValue(id, name);
                            })
                        );
                        if (IsTeacherSelected)
                        {
                            var thirdRelatedValues = _databaseService.GetAllFrom("Группы");
                            ThirdRelatedValues = new ObservableCollection<DbEntityValue>(
                                thirdRelatedValues.Select(val =>
                                {
                                    int spaceIndex = val.IndexOf(' ');
                                    string name = spaceIndex != -1 ? val.Substring(spaceIndex + 1).Trim() : val;
                                    int id = spaceIndex != -1 ? int.Parse(val.Substring(0, spaceIndex)) : 0;
                                    return new DbEntityValue(id, name);
                                })
                            );
                        }
                    }
                }
            }
        }

        private int dbEntityValueId = -1;
        public int DbEntityValueId
        {
            get => dbEntityValueId;
            set => SetProperty(ref dbEntityValueId, value);
        }

        private string inputText;
        public string InputText
        {
            get => inputText;
            set => SetProperty(ref inputText, value);
        }

        private string helperText;
        public string HelperText
        {
            get => helperText;
            set => SetProperty(ref helperText, value);
        }

        private int relatedValueId = -1;
        public int RelatedValueId
        {
            get => relatedValueId;
            set => SetProperty(ref relatedValueId, value);
        }
        private int secondRelatedValueId = -1;
        public int SecondRelatedValueId
        {
            get => secondRelatedValueId;
            set => SetProperty(ref secondRelatedValueId, value);
        }
        private int thirdRelatedValueId = -1;
        public int ThirdRelatedValueId
        {
            get => thirdRelatedValueId;
            set => SetProperty(ref thirdRelatedValueId, value);
        }

        private bool isNotSimpleSelected;
        public bool IsNotSimpleSelected
        {
            get => isNotSimpleSelected;
            set => SetProperty(ref isNotSimpleSelected, value);
        }
        private bool isSimpleSelected = true;
        public bool IsSimpleSelected
        {
            get => isSimpleSelected;
            set => SetProperty(ref isSimpleSelected, value);
        }
        private bool isTeacherSelected = false;
        public bool IsTeacherSelected
        {
            get => isTeacherSelected;
            set => SetProperty(ref isTeacherSelected, value);
        }
        public DbAdminViewModel(INavigationService navigationService, IDatabaseService databaseService)
        {
            _navigationService = navigationService;
            _databaseService = databaseService;

            foreach (var item in Enum.GetValues(typeof(DbEntitieIds)))
                dbEntities.Add(new DbEntity((int)item, item.ToString()));
        }
    }
}
