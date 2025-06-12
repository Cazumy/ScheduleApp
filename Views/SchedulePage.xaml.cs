using ScheduleApp.ViewModels;
using System.Windows.Controls;

namespace ScheduleApp.Views
{
    public partial class SchedulePage : Page
    {
        public SchedulePage(ScheduleViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
