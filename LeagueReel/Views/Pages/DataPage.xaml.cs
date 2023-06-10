using LeagueReel.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Wpf.Ui.Common.Interfaces;

namespace LeagueReel.Views.Pages
{
    /// <summary>
    /// Interaction logic for DataView.xaml
    /// </summary>
    public partial class DataPage : INavigableView<ViewModels.DataViewModel>
    {
        public ViewModels.DataViewModel ViewModel
        {
            get;
        }

        public DataPage(ViewModels.DataViewModel viewModel)
        {
            ViewModel = viewModel;

            InitializeComponent();
        }
        

        private async void listView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //TODO --> Call to start this in ViewModel instead of here
            var gifFile = (GifFile)listView.SelectedItem;
            if (gifFile != null)
            {
                Debug.WriteLine("Clicked");
                await Task.Run(() => ViewModel.LoadGifAsync(gifFile.FilePath));
            }
        }
    }
}
