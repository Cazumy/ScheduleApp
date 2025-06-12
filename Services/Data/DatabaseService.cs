using ScheduleApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Windows;

namespace ScheduleApp.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly ISettingsService _settingsService;
        private readonly IDatabasePathProvider _pathProvider;

        private string _dbPath;
        private string _connectionString;
        private int currentGroupId;

        public DatabaseService(ISettingsService settingsService, IDatabasePathProvider pathProvider)
        {
            _settingsService = settingsService;
            _pathProvider = pathProvider;

            _dbPath = _settingsService.GetLastUsedDbPath();
            if (String.IsNullOrEmpty(_dbPath))
            {
                _dbPath = _pathProvider.SelectDatabasePath("Выберите файл базы данных");
                if (string.IsNullOrWhiteSpace(_dbPath))
                {
                    MessageBox.Show("Файл базы данных не выбран.");
                    return;
                }
                _settingsService.SaveDbPath(_dbPath);
            }
            TryInitializeConnection(_dbPath);
        }
        public void ReloadDatabase(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("Файл базы данных не существует.");
                return;
            }
            TryInitializeConnection(path);
            _settingsService.SaveDbPath(path);
        }
        private void TryInitializeConnection(string dbPath)
        {
            foreach (var provider in (string[])["Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0"])
            {
                try
                {
                    var testConnection = new OleDbConnection($"Provider={provider};Data Source={dbPath}");
                    testConnection.Open();
                    testConnection.Close();

                    _dbPath = dbPath;
                    _connectionString = $"Provider={provider};Data Source={_dbPath}";
                    return;
                }
                catch { MessageBox.Show("Не удалось открыть подключение ни с одним из провайдеров."); }
            }
        }

        public ObservableCollection<Group> GetGroups()
        {
            ObservableCollection<Group> groups = [];
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();

            using var command = new OleDbCommand("SELECT Код, Группа FROM Группы", connection);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                groups.Add(new Group(reader.GetInt32(0), reader.GetString(1)));
            }
            return groups;
        }
        public ObservableCollection<ScheduleItem> GetSchedule(int selectedGroupId, DateTime selectedDate)
        {
            if (selectedGroupId == 0 && selectedDate == default) { return null; }
            currentGroupId = selectedGroupId;
            ObservableCollection<ScheduleItem> scheduleItems = [];
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var command = new OleDbCommand(
@"SELECT Время.Начало,
    Время.Конец,
    Тип_Дисциплины.Тип,
    Дисциплины.Дисциплина,
    Аудитории.Номер,
    Адреса.Корпус,
    Адреса.Улица,
    Адреса.Дом,
    Преподаватели.Фамилия,
    Преподаватели.Имя,
    Преподаватели.Отчество,
    Расписание.Дата,
    Расписание.Код AS [ScheduleId],
    Группы.Код
FROM Тип_Дисциплины
INNER JOIN (
    (Преподаватели INNER JOIN (
            (Дисциплины INNER JOIN (
                    Группы INNER JOIN Дисциплины_Группы ON Группы.Код = Дисциплины_Группы.Группа )
                    ON Дисциплины.Код = Дисциплины_Группы.Дисциплина)
            INNER JOIN Преподаватели_Дисциплины 
                ON Дисциплины_Группы.Код = Преподаватели_Дисциплины.Дисциплина_Группы)
            ON Преподаватели.Код = Преподаватели_Дисциплины.Преподаватель)
    INNER JOIN (Время
        INNER JOIN (
            (Аудитории INNER JOIN (
                    Адреса INNER JOIN Адрес_Аудитории 
                        ON Адреса.Код = Адрес_Аудитории.Адрес)
                    ON Аудитории.Код = Адрес_Аудитории.Аудитория)
            INNER JOIN Расписание 
                ON Адрес_Аудитории.Код = Расписание.Адрес_Аудитории)
            ON Время.Код = Расписание.Время)
        ON Преподаватели_Дисциплины.Код = Расписание.Преподаватели_Дисциплины)
    ON Тип_Дисциплины.Код = Дисциплины_Группы.Тип_Дисциплины
WHERE Группы.Код = ? AND Расписание.Дата = ?;", connection);
            command.Parameters.AddWithValue(null, selectedGroupId);
            command.Parameters.AddWithValue(null, selectedDate.Date);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var item = new ScheduleItem(
                    $"{reader.GetString(0)} - {reader.GetString(1)}",
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    $"{reader.GetString(5)}, {reader.GetString(6)} {reader.GetString(7)}",
                    $"{reader.GetString(8)} {reader.GetString(9)} {reader.GetString(10)}",
                    int.Parse(reader["ScheduleId"].ToString()));

                scheduleItems.Add(item);
            }
            return scheduleItems;
        }
        #region tableValues
        public ObservableCollection<string> GetTimes()
        {
            const string sqlQuery =
@"SELECT Время.Начало AS [Start],
    Время.Конец AS [End]
FROM Время";
            var readerNames = new[] { "Start", "End" };
            return GetCollectionExecuteReader(sqlQuery, readerNames, " - ");
        }
        public ObservableCollection<string> GetSubjectTypes()
        {
            const string sqlQuery =
@"SELECT Тип_Дисциплины.Тип AS [Type]
FROM Тип_Дисциплины";
            var readerNames = new[] { "Type" };
            return GetCollectionExecuteReader(sqlQuery, readerNames);
        }
        public ObservableCollection<string> GetSubjects(int groupId)
        {
            const string sqlQuery =
@"SELECT DISTINCT Дисциплины.Дисциплина AS [Subject],
    Группы.Группа
FROM Дисциплины INNER JOIN (Группы 
        INNER JOIN Дисциплины_Группы ON Группы.Код = Дисциплины_Группы.Группа) 
    ON Дисциплины.Код = Дисциплины_Группы.Дисциплина
WHERE Группы.Код=?;";
            var readerNames = new[] { "Subject" };
            return GetCollectionExecuteReader(sqlQuery, readerNames, " ", groupId);
        }
        public ObservableCollection<string> GetRooms()
        {
            const string sqlQuery =
@"SELECT Аудитории.Номер AS [Room]
FROM Аудитории";
            var readerNames = new[] { "Room" };
            return GetCollectionExecuteReader(sqlQuery, readerNames);
        }
        public ObservableCollection<string> GetAddresses()
        {
            const string sqlQuery =
@"SELECT Адреса.Улица AS [Street],
    Адреса.Дом AS [Home],
    Адреса.Корпус AS [Building]
FROM Адреса";
            var readerNames = new[] { "Street", "Home", "Building" };
            return GetCollectionExecuteReader(sqlQuery, readerNames, ", ");
        }
        public ObservableCollection<string> GetTeachers(int selectedGroup)
        {
            const string sqlCommand =
@"SELECT DISTINCT Преподаватели.Фамилия AS[Surname],
    Преподаватели.Имя AS[Name],
    Преподаватели.Отчество AS[Patronymic],
    Группы.Код
FROM Преподаватели INNER JOIN ((Группы
    INNER JOIN Дисциплины_Группы ON Группы.Код = Дисциплины_Группы.Группа)
        INNER JOIN Преподаватели_Дисциплины ON Дисциплины_Группы.Код = Преподаватели_Дисциплины.Дисциплина_Группы)
    ON Преподаватели.Код = Преподаватели_Дисциплины.Преподаватель
WHERE Группы.Код=?;";
            var readerNames = new[] { "Surname", "Name", "Patronymic" };
            return GetCollectionExecuteReader(sqlCommand, readerNames, " ", selectedGroup);
        }
        public ObservableCollection<DateTime> GetDates(int selectedGroup)
        {
            var dates = new ObservableCollection<DateTime>();
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var command = new OleDbCommand(
@"SELECT DISTINCT Расписание.Дата as [Date],
    Группы.Группа
FROM ((Группы INNER JOIN Дисциплины_Группы ON Группы.Код = Дисциплины_Группы.Группа)
    INNER JOIN Преподаватели_Дисциплины ON Дисциплины_Группы.Код = Преподаватели_Дисциплины.Дисциплина_Группы)
        INNER JOIN Расписание ON Преподаватели_Дисциплины.Код = Расписание.Преподаватели_Дисциплины
Where Группы.Код=?;", connection);
            command.Parameters.AddWithValue("?", selectedGroup);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                dates.Add((DateTime)reader["Date"]);
            }
            return dates;
        }
        #endregion
        #region get value by another
        public string GetTeacher(string subject)
        {
            const string sqlQuery =
@"SELECT DISTINCT TOP 1 Преподаватели.Фамилия as [Surname],
    Преподаватели.Имя as [Name],
    Преподаватели.Отчество as [Patronymic],
    Дисциплины.Дисциплина
FROM Дисциплины INNER JOIN (Дисциплины_Группы
    INNER JOIN (Преподаватели
        INNER JOIN Преподаватели_Дисциплины ON Преподаватели.Код = Преподаватели_Дисциплины.Преподаватель)
    ON Дисциплины_Группы.Код = Преподаватели_Дисциплины.Дисциплина_Группы)
ON Дисциплины.Код = Дисциплины_Группы.Дисциплина
WHERE Дисциплины.Дисциплина=?;";
            var paramNames = new[] { "Surname", "Name", "Patronymic" };
            return ExecuteReader<string>(sqlQuery, paramNames, " ", subject);
        }
        public string GetSubject(string teacher)
        {
            const string sqlQuery =
@"SELECT DISTINCT TOP 1 Дисциплины.Дисциплина,
    Преподаватели.Фамилия,
    Преподаватели.Имя,
    Преподаватели.Отчество,
    Группы.Код
FROM Группы INNER JOIN (Преподаватели
    INNER JOIN (Дисциплины
        INNER JOIN (Дисциплины_Группы
            INNER JOIN Преподаватели_Дисциплины ON Дисциплины_Группы.Код = Преподаватели_Дисциплины.Дисциплина_Группы)
        ON Дисциплины.Код = Дисциплины_Группы.Дисциплина)
    ON Преподаватели.Код = Преподаватели_Дисциплины.Преподаватель)
ON Группы.Код = Дисциплины_Группы.Группа
WHERE Преподаватели.Фамилия=? AND Преподаватели.Имя=? AND Преподаватели.Отчество=?
    AND Группы.Код=?";
            var teacherParts = teacher.Split(' ');
            return ExecuteScalar(sqlQuery, teacherParts[0], teacherParts[1], teacherParts[2], currentGroupId);
        }
        public string GetSubjectType(string subject)
        {
            const string sqlQuery =
@"SELECT DISTINCT TOP 1 Тип_Дисциплины.Тип AS [SubjectType],
    Дисциплины.Дисциплина
FROM Тип_Дисциплины INNER JOIN (Дисциплины
    INNER JOIN Дисциплины_Группы ON Дисциплины.Код = Дисциплины_Группы.Дисциплина)
ON Тип_Дисциплины.Код = Дисциплины_Группы.Тип_Дисциплины
WHERE Дисциплины.Дисциплина=?;";
            return ExecuteScalar(sqlQuery, subject);
        }
        public string GetSubjectByType(string subjectType)
        {
            const string sqlQuery =
@"SELECT DISTINCT TOP 1 Дисциплины.Дисциплина,
    Тип_Дисциплины.Тип,
    Группы.Код
FROM Группы INNER JOIN (Тип_Дисциплины
    INNER JOIN (Дисциплины
        INNER JOIN Дисциплины_Группы ON Дисциплины.Код = Дисциплины_Группы.Дисциплина)
    ON Тип_Дисциплины.Код = Дисциплины_Группы.Тип_Дисциплины)
ON Группы.Код = Дисциплины_Группы.Группа
WHERE Тип_Дисциплины.Тип=? AND Группы.Код=?";
            return ExecuteScalar(sqlQuery, subjectType, currentGroupId);
        }
        public string GetRoom(string address)
        {
            const string sqlQuery =
@"SELECT DISTINCT TOP 1 Аудитории.Номер,
    Адреса.Корпус
FROM Аудитории INNER JOIN (Адреса
    INNER JOIN Адрес_Аудитории ON Адреса.Код = Адрес_Аудитории.Адрес)
ON Аудитории.Код = Адрес_Аудитории.Аудитория
WHERE Адреса.Корпус = ?;";
            var street = address.Split(',')[2].Trim();
            return ExecuteScalar(sqlQuery, street);
        }
        public string GetFullAddress(string room)
        {
            const string sqlQuery =
@"SELECT DISTINCT TOP 1 Аудитории.Номер,
Адреса.Улица AS [Street],
Адреса.Дом AS [Home],
Адреса.Корпус AS [Building]
FROM Аудитории INNER JOIN (Адреса
    INNER JOIN Адрес_Аудитории ON Адреса.Код = Адрес_Аудитории.Адрес)
ON Аудитории.Код = Адрес_Аудитории.Аудитория
WHERE Аудитории.Номер=?;";
            var readerNames = new[] { "Building", "Street", "Home" };
            return ExecuteReader<string>(sqlQuery, readerNames, ", ", room);
        }
        #endregion
        public ObservableCollection<string> GetAllFrom(string tableName)
        {
            var result = new ObservableCollection<string>();
            string sqlQuery = $"SELECT * FROM {tableName}";
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();

            using var command = new OleDbCommand(sqlQuery, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var rowParts = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    rowParts.Add(reader[i]?.ToString() ?? string.Empty);
                }

                result.Add(string.Join(" ", rowParts));
            }

            return result;
        }

        public void SetScheduleItems(ObservableCollection<ScheduleItem> scheduleItems, DateTime currentDate)
        {
            if (!FieldsValidation(scheduleItems)) return;
            if (!TimeValidation(scheduleItems)) return;

            using var connection = new OleDbConnection(_connectionString);
            connection.Open();

            var existingIDs = new HashSet<int>();
            using var commandGetIDs = new OleDbCommand(
@"SELECT Расписание.Код,
    Расписание.Дата,
    Группы.Код
FROM (Группы INNER JOIN Дисциплины_Группы ON Группы.Код = Дисциплины_Группы.Группа)
    INNER JOIN (Преподаватели_Дисциплины
        INNER JOIN Расписание ON Преподаватели_Дисциплины.Код = Расписание.Преподаватели_Дисциплины)
    ON Дисциплины_Группы.Код = Преподаватели_Дисциплины.Дисциплина_Группы
WHERE Расписание.Дата=? AND Группы.Код=?", connection);
            commandGetIDs.Parameters.AddWithValue("?", currentDate.Date);
            commandGetIDs.Parameters.AddWithValue("?", currentGroupId);
            using (var reader = commandGetIDs.ExecuteReader())
            {
                while (reader.Read())
                {
                    existingIDs.Add(reader.GetInt32(0));
                }
            }

            var newIDs = new HashSet<int>();
            foreach (var item in scheduleItems)
            {
                string sqlCommand;
                newIDs.Add(item.Id);
                if (item.Id == 0)
                {
                    sqlCommand = "INSERT INTO Расписание (Дата, Время, Преподаватели_Дисциплины, Адрес_Аудитории)\nVALUES (?, ?, ?, ?);";
                }
                else
                {
                    sqlCommand = "UPDATE Расписание SET Время = ?, Преподаватели_Дисциплины = ?, Адрес_Аудитории = ?\nWHERE Код = ?";
                }
                using var command = new OleDbCommand(sqlCommand, connection);
                if (item.Id == 0) command.Parameters.AddWithValue("?", currentDate);

                int timeId = GetTimeId(item.Time);
                int teacherSubjectId = GetTeacher_SubjectID(item);
                int fullAddressId = GetFullAddressID(item);
                if (timeId == 0 || teacherSubjectId == 0 || fullAddressId == 0)
                {
                    MessageBox.Show("Непредвиденная ошибка в базе данных, изменения не будут сохранены");
                    return;
                }
                command.Parameters.AddWithValue("?", timeId);
                command.Parameters.AddWithValue("?", teacherSubjectId);
                command.Parameters.AddWithValue("?", fullAddressId);
                if (item.Id != 0) command.Parameters.AddWithValue("?", item.Id);

                command.ExecuteNonQuery();
            }
            foreach (var id in existingIDs)
            {
                if (!newIDs.Contains(id))
                {
                    using var deleteCommand = new OleDbCommand("DELETE FROM Расписание WHERE Код = ?", connection);
                    deleteCommand.Parameters.AddWithValue("?", id);
                    deleteCommand.ExecuteNonQuery();
                }
            }
        }
        public void DeleteDbValue(string tableName, int valueId)
        {
            if (valueId <= 0) return;
            var existInSchedule = tableName switch
            {
                "Адреса" => CheckAddressInSchedule(valueId),
                "Аудитории" => CheckRoomInSchedule(valueId),
                "Группы" => CheckGroupInSchedule(valueId),
                "Дисциплины" => CheckSubjectInSchedule(valueId),
                "Преподаватели" => CheckTeacherInSchedule(valueId),
                _ => true,
            };
            if (existInSchedule)
            {
                MessageBox.Show("Значение используется в расписании, удаление невозможно\nОбратитесь к методисту");
                return;
            }
            switch (tableName)
            {
                case "Адреса":
                    ExecuteNonQueryDelete([
@"DELETE FROM Адрес_Аудитории
WHERE Адрес = ?",
@"DELETE FROM Адреса
WHERE Код = ?"], valueId);
                    break;
                case "Аудитории":
                    ExecuteNonQueryDelete([
@"DELETE FROM Адрес_Аудитории
WHERE Аудитория = ?;",
@"DELETE FROM Аудитории
WHERE Код = ?;"], valueId);
                    break;
                case "Группы":
                    ExecuteNonQueryDelete([@"DELETE FROM Преподаватели_Дисциплины
WHERE Дисциплина_Группы IN (
    SELECT Код 
    FROM Дисциплины_Группы 
    WHERE Группа = ?
);",
@"DELETE FROM Дисциплины_Группы
WHERE Группа = ?;",
@"DELETE FROM Группы
WHERE Код = ?;"], valueId);
                    break;
                case "Дисциплины":
                    ExecuteNonQueryDelete([@"DELETE FROM Преподаватели_Дисциплины
WHERE Дисциплина_Группы IN (
    SELECT Код 
    FROM Дисциплины_Группы 
    WHERE Дисциплина = ?
);",
@"DELETE FROM Дисциплины_Группы
WHERE Дисциплина = ?;",
@"DELETE FROM Дисциплины
WHERE Код = ?;"], valueId);
                    break;
                case "Преподаватели":
                    ExecuteNonQueryDelete([@"DELETE FROM Преподаватели_Дисциплины
WHERE Преподаватель = ?;",
@"DELETE FROM Преподаватели
WHERE Код = ?;"], valueId);
                    break;
            }
        }
        public void AddDbValue(string tableName, string[] values, bool isWithRelated, int relatedValue = -1, int secondRelatedValue = -1)
        {
            List<string> inserts = [];
            List<string> checks = [];
            bool isTeacher = false;
            switch (tableName)
            {
                case "Адреса":
                    inserts.Add("INSERT INTO Адреса (Корпус, Улица, Дом) VALUES (?, ?, ?)");
                    checks.Add("SELECT Код FROM Адреса WHERE Корпус = ? AND Улица=? AND Дом=?");
                    if (isWithRelated)
                    {
                        inserts.Add("INSERT INTO Адрес_Аудитории (Адрес, Аудитория) VALUES (?, ?)");
                        checks.Add("SELECT COUNT(*) FROM Адрес_Аудитории WHERE Адрес=? AND Аудитория=?");
                    }
                    break;

                case "Аудитории":
                    inserts.Add("INSERT INTO Аудитории (Номер) VALUES (?)");
                    checks.Add("SELECT Код FROM Аудитории WHERE Номер = ?");
                    if (isWithRelated)
                    {
                        inserts.Add("INSERT INTO Адрес_Аудитории (Аудитория, Адрес) VALUES (?, ?)");
                        checks.Add("SELECT COUNT(*) FROM Адрес_Аудитории WHERE Аудитория = ? AND Адрес = ?");
                    }
                    break;

                case "Группы":
                    inserts.Add("INSERT INTO Группы (Группа) VALUES (?)");
                    checks.Add("SELECT Код FROM Группы WHERE Группа=?");
                    if (isWithRelated)
                    {
                        inserts.Add("INSERT INTO Дисциплины_Группы (Группа, Дисциплина, Тип_Дисциплины) VALUES (?, ?, ?)");
                        checks.Add("SELECT COUNT(*) FROM Дисциплины_Группы\nWHERE Группа = ? AND Дисциплина = ? AND Тип_Дисциплины = ?");
                    }
                    break;

                case "Дисциплины":
                    inserts.Add("INSERT INTO Дисциплины (Дисциплина) VALUES (?)");
                    checks.Add("SELECT Код FROM Дисциплины WHERE Дисциплина=?");
                    if (isWithRelated)
                    {
                        inserts.Add("INSERT INTO Дисциплины_Группы (Дисциплина, Группа, Тип_Дисциплины) VALUES (?, ?, ?)");
                        checks.Add("SELECT COUNT(*) FROM Дисциплины_Группы\nWHERE Дисциплина = ? AND Группа = ? AND Тип_Дисциплины = ?");
                    }
                    break;

                case "Преподаватели":
                    inserts.Add("INSERT INTO Преподаватели (Фамилия, Имя, Отчество) VALUES (?, ?, ?)");
                    checks.Add("SELECT Код FROM Преподаватели WHERE Фамилия=? AND Имя=? AND Отчество=?");
                    if (isWithRelated)
                    {
                        inserts.Add("INSERT INTO Преподаватели_Дисциплины (Преподаватель, Дисциплина_Группы) VALUES (?, ?)");
                        checks.Add("SELECT COUNT(*) FROM Преподаватели_Дисциплины\nWHERE Преподаватель=? AND Дисциплина_Группы=?");
                        isTeacher = true;
                    }
                    break;
                default: return;
            }
            ExecuteNonQueryAdd([.. inserts], values, [.. checks], isWithRelated, relatedValue, isTeacher, secondRelatedValue);
        }

        #region check in table
        private bool CheckAddressInSchedule(int id)
        {
            const string query = @"
SELECT Расписание.Код
FROM ((Расписание
INNER JOIN Адрес_Аудитории ON Расписание.Адрес_Аудитории = Адрес_Аудитории.Код)
INNER JOIN Адреса ON Адрес_Аудитории.Адрес = Адреса.Код)
WHERE Адреса.Код = ?";
            return CheckInSchedule(query, id);
        }
        private bool CheckRoomInSchedule(int id)
        {
            const string query = @"
SELECT Расписание.Код
FROM ((Расписание
INNER JOIN Адрес_Аудитории ON Расписание.Адрес_Аудитории = Адрес_Аудитории.Код)
INNER JOIN Аудитории ON Адрес_Аудитории.Аудитория = Аудитории.Код)
WHERE Аудитории.Код = ?";
            return CheckInSchedule(query, id);
        }
        private bool CheckGroupInSchedule(int id)
        {
            const string query = @"
SELECT Расписание.Код
FROM ((((Расписание
INNER JOIN Преподаватели_Дисциплины 
    ON Расписание.Преподаватели_Дисциплины = Преподаватели_Дисциплины.Код)
INNER JOIN Дисциплины_Группы 
    ON Преподаватели_Дисциплины.Дисциплина_Группы = Дисциплины_Группы.Код)
INNER JOIN Группы 
    ON Дисциплины_Группы.Группа = Группы.Код)
INNER JOIN Тип_Дисциплины 
    ON Дисциплины_Группы.Тип_Дисциплины = Тип_Дисциплины.Код)
WHERE Группы.Код = ?;";
            return CheckInSchedule(query, id);
        }
        private bool CheckSubjectInSchedule(int id)
        {
            const string query = @"
SELECT Расписание.Код
FROM (((Расписание 
INNER JOIN Преподаватели_Дисциплины 
    ON Расписание.Преподаватели_Дисциплины = Преподаватели_Дисциплины.Код)
INNER JOIN Дисциплины_Группы 
    ON Преподаватели_Дисциплины.Дисциплина_Группы = Дисциплины_Группы.Код)
INNER JOIN Дисциплины 
    ON Дисциплины_Группы.Дисциплина = Дисциплины.Код)
INNER JOIN Тип_Дисциплины 
    ON Дисциплины_Группы.Тип_Дисциплины = Тип_Дисциплины.Код
WHERE Дисциплины.Код = [?];";
            return CheckInSchedule(query, id);
        }
        private bool CheckTeacherInSchedule(int id)
        {
            const string query = @"
SELECT Расписание.Код
FROM (Расписание INNER JOIN Преподаватели_Дисциплины ON Расписание.Преподаватели_Дисциплины = Преподаватели_Дисциплины.Код)
INNER JOIN Преподаватели ON Преподаватели_Дисциплины.Преподаватель = Преподаватели.Код
WHERE Преподаватели.Код=[?];";
            return CheckInSchedule(query, id);
        }
        #endregion
        #region get id in teble
        private int GetTeacher_SubjectID(ScheduleItem item)
        {
            const string sqlQuery =
@"SELECT Преподаватели_Дисциплины.Код,
    Преподаватели.Фамилия,
    Преподаватели.Имя,
    Преподаватели.Отчество,
    Дисциплины.Дисциплина,
    Тип_Дисциплины.Тип,
    Группы.Код
FROM Преподаватели INNER JOIN (Тип_Дисциплины
    INNER JOIN (Группы
        INNER JOIN (Дисциплины
            INNER JOIN (Дисциплины_Группы
                INNER JOIN Преподаватели_Дисциплины ON Дисциплины_Группы.Код = Преподаватели_Дисциплины.Дисциплина_Группы)
                ON Дисциплины.Код = Дисциплины_Группы.Дисциплина)
            ON Группы.Код = Дисциплины_Группы.Группа)
        ON Тип_Дисциплины.Код = Дисциплины_Группы.Тип_Дисциплины)
    ON Преподаватели.Код = Преподаватели_Дисциплины.Преподаватель
WHERE Преподаватели.Фамилия=? AND Преподаватели.Имя=? AND Преподаватели.Отчество=?
    AND Дисциплины.Дисциплина=?
    AND Тип_Дисциплины.Тип=?
    AND Группы.Код=?;";
            var teacherParts = item.Teacher.Split(' ');
            return int.TryParse(ExecuteScalar(sqlQuery, teacherParts[0], teacherParts[1], teacherParts[2], item.Subject, item.SubjectType, currentGroupId), out var id) ? id : 0;
        }
        private int GetFullAddressID(ScheduleItem item)
        {
            const string sqlQuery =
@"SELECT Адрес_Аудитории.Код,
    Адреса.Улица,
    Аудитории.Номер
FROM Аудитории INNER JOIN (Адреса
    INNER JOIN Адрес_Аудитории ON Адреса.Код = Адрес_Аудитории.Адрес)
ON Аудитории.Код = Адрес_Аудитории.Аудитория
WHERE Адреса.Корпус=? AND Аудитории.Номер=?";
            var addressParts = item.Address.Split(',');
            int.TryParse(ExecuteScalar(sqlQuery, addressParts[2].Trim(), item.Room), out int id); // надо исправь. сам уже запутался
            if (id == 0)
            {
                int.TryParse(ExecuteScalar(sqlQuery, addressParts[0].Trim(), item.Room), out id);
            }
            return id;
        }
        private int GetTimeId(string time)
        {
            const string sqlQuery =
@"SELECT Время.Код,
    Время.Начало,
    Время.Конец
FROM Время
WHERE Время.Начало=? AND Время.Конец=?";
            var timeParts = time.Split('-');
            return int.TryParse(ExecuteScalar(sqlQuery, timeParts[0].Trim(), timeParts[1].Trim()), out var id) ? id : 0;
        }
        #endregion
        #region helpers
        private bool CheckInSchedule(string query, int id)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            using var command = new OleDbCommand(query, connection);
            command.Parameters.AddWithValue("?", id);
            using var reader = command.ExecuteReader();
            return reader.HasRows;
        }
        private T ExecuteReader<T>(string sqlQuery, string[] readerNames, string separation = " ", params object[] parameters)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            var command = new OleDbCommand(sqlQuery, connection);
            parameters?.ToList().ForEach(p => command.Parameters.AddWithValue("?", p));

            using var reader = command.ExecuteReader();
            var stringOutput = new System.Text.StringBuilder();
            var intOutput = 0;
            while (reader.Read())
            {
                for (int i = 0; i < readerNames.Length; ++i)
                {
                    var paramName = readerNames[i];
                    if (typeof(T) == typeof(String))
                    {
                        stringOutput.Append(reader[paramName]);
                        if (i < readerNames.Length - 1) { stringOutput.Append(separation); }
                    }
                    else if (typeof(T) == typeof(int)) { intOutput = int.Parse(reader[paramName].ToString()); }
                }
            }
            if (typeof(T) == typeof(String)) // !антипаттерн!, но для этой реализации большего не надо
            {
                return (T)(object)Convert.ToString(stringOutput);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)intOutput;
            }
            else { return default; }
        }
        private string ExecuteScalar(string sqlQuery, params object[] parameters)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            var command = new OleDbCommand(sqlQuery, connection);
            parameters?.ToList().ForEach(p => command.Parameters.AddWithValue("?", p));

            var result = command.ExecuteScalarAsync();
            return Convert.ToString(result.Result);
        }
        private void ExecuteNonQueryDelete(string[] query, int id)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var sql in query)
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    command.Transaction = transaction;
                    command.Connection = connection;
                    command.Parameters.AddWithValue("?", id);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch { transaction.Rollback(); MessageBox.Show("Непредвиденная ошибка при удалении. Все изменения отменены"); }
        }
        private void ExecuteNonQueryAdd(string[] query, string[] values, string[] sqlCheck, bool isWithRelated, int relatedValue = -1, bool isTeacher = false, int groupId = -1)
        {
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                int baseId;
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = sqlCheck[0];
                    if (isWithRelated)
                    {
                        for (int i = 0; i < values.Length - 1; ++i)
                            command.Parameters.AddWithValue("?", values[i]);
                    }
                    else
                    {
                        for (int i = 0; i < values.Length; ++i)
                            command.Parameters.AddWithValue("?", values[i]);
                    }

                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        baseId = Convert.ToInt32(command.ExecuteScalar());
                    }
                    else
                    {
                        var insertCommand = connection.CreateCommand();
                        insertCommand.Transaction = transaction;
                        insertCommand.CommandText = query[0];
                        if (isWithRelated)
                        {
                            for (int i = 0; i < values.Length - 1; ++i)
                                insertCommand.Parameters.AddWithValue("?", values[i]);
                        }
                        else
                        {
                            for (int i = 0; i < values.Length; ++i)
                                insertCommand.Parameters.AddWithValue("?", values[i]);
                        }

                        insertCommand.ExecuteNonQuery();

                        insertCommand.CommandText = "SELECT @@IDENTITY";
                        insertCommand.Parameters.Clear();
                        baseId = Convert.ToInt32(insertCommand.ExecuteScalar());
                    }
                }
                if (isWithRelated)
                {
                    int relatedId;
                    if (isTeacher)
                    {
                        using var findDG = connection.CreateCommand();
                        findDG.Transaction = transaction;
                        findDG.CommandText = @"
                    SELECT Код FROM Дисциплины_Группы 
                    WHERE Дисциплина = ? AND Группа = ? AND Тип_Дисциплины = ?";
                        findDG.Parameters.AddWithValue("?", int.Parse(values[values.Length - 1]));
                        findDG.Parameters.AddWithValue("?", groupId);
                        findDG.Parameters.AddWithValue("?", relatedValue);

                        var dgResult = findDG.ExecuteScalar() ?? throw new Exception("Не найдена запись в Дисциплины_Группы");
                        relatedId = Convert.ToInt32(dgResult);
                    }

                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = sqlCheck[1];
                    command.Parameters.AddWithValue("?", baseId);
                    if(!isTeacher)  command.Parameters.AddWithValue("?", int.Parse(values[values.Length - 1]));
                    if(relatedValue != -1) command.Parameters.AddWithValue("?", relatedValue);
                    if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                    {
                        var insertRel = connection.CreateCommand();
                        insertRel.Transaction = transaction;
                        insertRel.CommandText = query[1];
                        insertRel.Parameters.AddWithValue("?", baseId);
                        if (!isTeacher) insertRel.Parameters.AddWithValue("?", int.Parse(values[values.Length - 1]));
                        if (relatedValue != -1) insertRel.Parameters.AddWithValue("?", relatedValue);
                        insertRel.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
            }
            catch { transaction.Rollback(); MessageBox.Show("Непредвиденная ошибка при изменении базы данных.\nИзменения не были сохранены"); }
        }
        private ObservableCollection<string> GetCollectionExecuteReader
            (string sqlQuery, string[] readerNames, string separation = " ", params object[] parameters)
        {
            ObservableCollection<string> stringData = [];
            using var connection = new OleDbConnection(_connectionString);
            connection.Open();
            var command = new OleDbCommand(sqlQuery, connection);
            parameters?.ToList().ForEach(p => command.Parameters.AddWithValue("?", p));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var stringOutput = new System.Text.StringBuilder();
                for (int i = 0; i < readerNames.Length; ++i)
                {
                    var paramName = readerNames[i];
                    stringOutput.Append(reader[paramName]);
                    if (i != readerNames.Length - 1) { stringOutput.Append(separation); }
                }
                stringData.Add(stringOutput.ToString());
            }
            return stringData;
        }
        #endregion
        #region validation
        private bool FieldsValidation(ObservableCollection<ScheduleItem> scheduleItems)
        {
            foreach (var scheduleItem in scheduleItems)
            {
                if (string.IsNullOrEmpty(scheduleItem.Time) || string.IsNullOrEmpty(scheduleItem.SubjectType) ||
                    string.IsNullOrEmpty(scheduleItem.Subject) || string.IsNullOrEmpty(scheduleItem.Room) ||
                    string.IsNullOrEmpty(scheduleItem.Address) || string.IsNullOrEmpty(scheduleItem.Teacher))
                {
                    MessageBox.Show("Заполните все поля");
                    return false;
                }
            }
            return true;
        }
        private bool TimeValidation(ObservableCollection<ScheduleItem> scheduleItems)
        {
            for (int i = 0; i < scheduleItems.Count - 1; i++)
            {
                var current = scheduleItems[i];
                var next = scheduleItems[i + 1];

                if (string.Compare(current.Time, next.Time) >= 0)
                {
                    MessageBox.Show("Неверно указано время");
                    return false;
                }
            }
            return true;
        }
        #endregion
    }
}