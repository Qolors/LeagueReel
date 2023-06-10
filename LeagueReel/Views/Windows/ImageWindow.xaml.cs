using System;
using System.Windows;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace LeagueReel.Views.Windows
{
    /// <summary>
    /// Eventually will be implemented to allow users to fully open gifs
    /// </summary>
    public partial class ImageWindow : Window
    {
        public ImageWindow(string filePath)
        {
            InitializeComponent();
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(filePath);
            image.EndInit();

            ImageBehavior.SetAnimatedSource(gifImage, image);
        }
    }
}
