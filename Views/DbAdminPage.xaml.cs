using ScheduleApp.ViewModels;
using System.Windows.Controls;

namespace ScheduleApp.Views
{
    public partial class DbAdminPage : Page
    {
        public DbAdminPage(DbAdminViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}