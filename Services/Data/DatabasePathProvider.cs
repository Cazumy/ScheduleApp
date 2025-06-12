#nullable enable
using Microsoft.Win32;
using System.Windows;
using System;

namespace ScheduleApp.Services
{
    public class DatabasePathProvider() : IDatabasePathProvider
    {
        public string? SelectDatabasePath(string? title = null) => OpenFileDialog(title);

        private string? OpenFileDialog(string? title = "")
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = "Access Database (*.accdb)|*.accdb|Mdb (*.mdb)|*.mdb",
                Multiselect = false,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            bool? result = dialog.ShowDialog();
            if (result == false) MessageBox.Show("Необходимо выбрать файл с базой данных");
            return result == true ? dialog.FileName : null;
        }
    }
}