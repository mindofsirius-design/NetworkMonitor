using System.Windows;
using NetworkMonitor.ViewModels;

namespace NetworkMonitor.Views
{
    public partial class EventLogView : Window
    {
        public EventLogView(EventLogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
