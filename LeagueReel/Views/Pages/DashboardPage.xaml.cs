using System.Threading.Tasks;
using Wpf.Ui.Common.Interfaces;

namespace LeagueReel.Views.Pages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : INavigableView<ViewModels.DashboardViewModel>
    {
        public ViewModels.DashboardViewModel ViewModel
        {
            get;
        }

        public DashboardPage(ViewModels.DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            progRing.IsIndeterminate = true;
            Task.Run(() => RotateRing());
        }

        public Task RotateRing()
        {
            while(ViewModel.IsConnecting) { }

            progRing.Dispatcher.Invoke(() => progRing.Visibility = System.Windows.Visibility.Collapsed);
            checkMark.Dispatcher.Invoke(() => checkMark.Visibility = System.Windows.Visibility.Visible);

            return Task.CompletedTask;
            
        }
    }
}