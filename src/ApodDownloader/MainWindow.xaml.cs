namespace ApodDownloader
{
    using System;
    using System.Configuration;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media.Imaging;
    
    using WinForms = System.Windows.Forms;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public ImageContext Context { get; }
        
        private readonly HttpClient httpClient;
        private readonly Regex imgRegex = new Regex("<br>\\s+<a href=\"(?<link>.*?)\">\\s<img src=\"(?<src>.*?)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex titleRegex = new Regex("<b>(?<title>.*?)</b>\\s?<br>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        private const string BaseUrl = "https://apod.nasa.gov/apod/";

        public MainWindow()
        {
            this.httpClient = new HttpClient();
            this.Context = new ImageContext
            {
                Title = "Loading..."
            };
            
            this.InitializeComponent();
            this.InitializeTrayIcon();
            
            this.DataContext = this;
        }

        private void InitializeTrayIcon()
        {
            var bitmap = new Bitmap("saturn.png");
            var handle = bitmap.GetHicon();
            
            var notifyIcon = new WinForms.NotifyIcon
            {
                Icon = System.Drawing.Icon.FromHandle(handle),
                Visible = true,
                Text = "Apod Downloader"
            };

            notifyIcon.DoubleClick += (sender, args) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

            this.ShowInTaskbar = false;
        }

        private void SaveImageAsync(object sender, RoutedEventArgs e)
        {
            if (!this.Context.ValidImage)
            {
                MessageBox.Show("Unable to save, no valid image was downloaded");
                
                return;
            }

            this.Context.Status = "Saving...";
            
            var path = ConfigurationManager.AppSettings["SaveLocation"];
            var fullPath = Path.Combine(path, Path.GetFileNameWithoutExtension(this.Context.Filename)) + ".png";

            try
            {
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(this.Context.ImageSource));
                    encoder.Save(fileStream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to save image: " + ex.Message);
            }
            
            this.Context.Status = "Saved to: " + fullPath;
        }

        private async void RefreshImageAsync(object sender, RoutedEventArgs e)
        {
            await this.GetLatestImageAsync();
        }

        private async Task GetLatestImageAsync()
        {
            this.Context.ValidImage = false;
            var html = await this.GetHtmlAsync();
            var matches = this.imgRegex.Match(html);

            var imageUrl = matches.Groups["link"]?.Value ?? matches.Groups["src"]?.Value;
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                MessageBox.Show("Unable to parse image url from html");
                this.Context.Title = "Error";
                this.Context.Status = "Unable to parse image url from html";
                
                return;
            }

            var imageData = await this.httpClient.GetAsync(BaseUrl + imageUrl);
            if (!imageData.IsSuccessStatusCode)
            {
                MessageBox.Show("Unable to download latest image. status code: " + imageData.StatusCode);
                this.Context.Title = "Error";
                this.Context.Status = "Unable to download latest image. status code: " + imageData.StatusCode;
                
                return;
            }

            var stream = await imageData.Content.ReadAsStreamAsync();
            try
            {
                this.Context.ImageSource = BitmapFrame.Create(stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
                this.Context.Filename = imageUrl.Split('/').Last();

                var titleMatch = this.titleRegex.Match(html).Groups["title"]?.Value;
            
                this.Context.Title = titleMatch ?? this.Context.Filename;

                this.Context.ValidImage = true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error creating image context - this might be a video.\n" + e.Message);
                this.Context.Status = "Error creating image context";
            }
        }

        private async Task<string> GetHtmlAsync()
        {
            var response = await this.httpClient.GetAsync(BaseUrl + "astropix.html");
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Unable to open apod homepage. status code: " + response.StatusCode);
                this.Context.Title = "Error";

                return string.Empty;
            }

            var html = await response.Content.ReadAsStringAsync();
            return html;
        }

        private async void MainWindow_OnInitialized(object sender, EventArgs e)
        {
            await this.GetLatestImageAsync();
        }
    }
}