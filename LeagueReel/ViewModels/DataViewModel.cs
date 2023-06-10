using CommunityToolkit.Mvvm.ComponentModel;
using LeagueReel.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Wpf.Ui.Common.Interfaces;

namespace LeagueReel.ViewModels
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private BitmapSource _currentFrame;

        private List<BitmapSource> _frames = new List<BitmapSource>();
        private DispatcherTimer _timer = new DispatcherTimer();
        private int _currentFrameIndex;

        [ObservableProperty]
        private ObservableCollection<GifFile> gifFiles;

        [ObservableProperty]
        private BitmapImage selectedGifFile;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
            {
                _timer.Interval = TimeSpan.FromMilliseconds(50);
                _timer.Tick += Timer_Tick;
            }
            InitializeViewModel();
        }

        public void OnNavigatedFrom()
        {
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _currentFrameIndex = (_currentFrameIndex + 1) % _frames.Count;
            CurrentFrame = _frames[_currentFrameIndex];
        }

        public async void LoadGifAsync(string path)
        {
            await Task.Run(() =>
            {
                Debug.WriteLine("Doin it");
                var gifDecoder = new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.Default);
                foreach (BitmapFrame frame in gifDecoder.Frames)
                {
                    var bmpFrame = frame;
                    bmpFrame.Freeze();
                    _frames.Add(bmpFrame);
                }
            });

            _timer.Start();
        }



        private void InitializeViewModel()
        {
            GifFiles = new ObservableCollection<GifFile>();
            //TODO --> Move this to a service
            string folderPath = "C:\\LeagueGif";

            foreach (var file in Directory.GetFiles(folderPath, "*.gif"))
            {
                GifFiles.Add(new GifFile { FilePath = file });
            }

            _isInitialized = true;
        }
    }
}
