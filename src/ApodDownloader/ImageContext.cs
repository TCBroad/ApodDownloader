namespace ApodDownloader
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Media.Imaging;
    using Annotations;

    public sealed class ImageContext : INotifyPropertyChanged
    {
        private string title;
        private BitmapSource imageSource;
        private string status;

        public string Title
        {
            get => this.title;
            set
            {
                if (value == this.title) return;
                this.title = value;
                this.OnPropertyChanged();
            }
        }

        public string Filename { get; set; }
        
        public bool ValidImage { get; set; }

        public string Status
        {
            get => this.status;
            set
            {
                if (value == this.status) return;
                this.status = value;
                this.OnPropertyChanged();
            }
        }

        public BitmapSource ImageSource
        {
            get => this.imageSource;
            set
            {
                if (Equals(value, this.imageSource)) return;
                this.imageSource = value;
                this.OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}