using Microsoft.Extensions.DependencyInjection;
using ModernWpf;
using ScheduleApp.Services;
using ScheduleApp.ViewModels;
using ScheduleApp.Views;
using System;
using System.Windows;

namespace ScheduleApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
            navigationService.CreateNewWindow(PageType.MainPage);
        }

        private void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<ISettingsService, SettingService>();
            services.AddSingleton<IDatabasePathProvider, DatabasePathProvider>();

            services.AddSingleton<INavigationService, NavigationService>();
            services.AddTransient<IDatabaseService, DatabaseService>();

            services.AddSingleton<IUserModeService, UserModeService>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<ScheduleViewModel>();
            services.AddTransient<DbAdminViewModel>();
            services.AddSingleton<MainPage>();
            services.AddTransient<SchedulePage>();
            services.AddTransient<DbAdminPage>();
        }
    }
}
