using ScheduleApp.Models;
using System;
using System.Collections.ObjectModel;

namespace ScheduleApp.Services
{
    public interface IDatabaseService
    {
        void ReloadDatabase(string path);
        ObservableCollection<Group> GetGroups();
        ObservableCollection<ScheduleItem> GetSchedule(int selectedGroupId, DateTime selectedDate);
        ObservableCollection<string> GetTimes();
        ObservableCollection<string> GetSubjectTypes();
        ObservableCollection<string> GetSubjects(int groupId);
        ObservableCollection<string> GetRooms();
        ObservableCollection<string> GetAddresses();
        ObservableCollection<string> GetTeachers(int selectedGroup);
        string GetTeacher(string subject);
        string GetSubject(string teacher);
        string GetSubjectType(string subject);
        string GetSubjectByType(string subjectType);
        string GetRoom(string address);
        string GetFullAddress(string room);
        public ObservableCollection<string> GetAllFrom(string tableName);
        ObservableCollection<DateTime> GetDates(int selectedGroup);

        void SetScheduleItems(ObservableCollection<ScheduleItem> scheduleItems, DateTime date);
        void DeleteDbValue(string tableName, int valueId);
        void AddDbValue(string tableName, string[] values, bool isWithRelated, int relatedValue = -1, int secondRelatedValue = -1);
    }
}
