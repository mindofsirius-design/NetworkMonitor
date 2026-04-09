using System.Windows;
using NetworkMonitor.ViewModels;

namespace NetworkMonitor.Views
{
    public partial class StatisticsView : Window
    {
        public StatisticsView(StatisticsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
