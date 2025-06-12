using Microsoft.Extensions.DependencyInjection;
using ScheduleApp.Views;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ScheduleApp.Services
{
    public class NavigationService(IServiceProvider serviceProvider) : INavigationService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        public void CreateNewWindow(PageType type)
        {
            var oldWindow = Application.Current.MainWindow;
            var newWindow = type switch
            {
                PageType.MainPage => GetWindowMainPage(),
                PageType.SchedulePage => GetWindowSchedulePage(),
                PageType.DbAdminPage => GetWindowDbAdminPage(),
                _ => null
            };
            Application.Current.MainWindow = newWindow;
            newWindow?.Show();
            oldWindow?.Close();
        }

        private Window GetWindowMainPage() => new()
        {
            WindowStyle = WindowStyle.None,
            Width = 800,
            Height = 500,
            Title = "Расписание",
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Content = new Frame { Content = _serviceProvider.GetRequiredService<MainPage>() }
        };
        private Window GetWindowSchedulePage() => new()
        {
            WindowStyle = WindowStyle.SingleBorderWindow,
            Width = 1235,
            Height = 560,
            MinWidth = 1235,
            MinHeight = 560,
            Title = "Расписание",
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.CanResize,
            Content = new Frame { Content = _serviceProvider.GetRequiredService<SchedulePage>() }
        };
        private Window GetWindowDbAdminPage()
            => new()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                Width = 802,
                Height = 530,
                Title = "Администрирование базы данных",
                ResizeMode = ResizeMode.NoResize,
                Content = new Frame { Content = _serviceProvider.GetRequiredService<DbAdminPage>() }
            };
    }
}
