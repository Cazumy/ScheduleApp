using ScheduleApp.ViewModels;
using System.Windows.Controls;

namespace ScheduleApp.Views
{
    public partial class MainPage : Page
    {
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
