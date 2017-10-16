using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Displays a single image that can be clicked to perform an
    /// action.
    /// </summary>
    class ImgThumbnail : Image
    {
        #region Members
        private string imgUrl;

        /// <summary>
        /// The image, preloaded for performance.
        /// </summary>
        private BitmapImage img;

        /// <summary>
        /// The url storing the image location.
        /// </summary>
        public string ImgUrl
        {
            get
            {
                return imgUrl;
            }

            set
            {
                imgUrl = value;
                SetImage();
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a blank thumbnail.
        /// </summary>
        public ImgThumbnail()
            : base()
        {
            img = null;
            imgUrl = String.Empty;

            SetHandlers();
            SetImage();
        }

        /// <summary>
        /// Constructs a thumbnail from a url.
        /// </summary>
        /// <param name="url">
        /// File path relative to the database file.
        /// </param>
        public ImgThumbnail(string url)
            : base()
        {
            img = null;
            imgUrl = url;

            SetHandlers();
            SetImage();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets up so clicking the image opens a window to view it fully.
        /// </summary>
        private void SetHandlers()
        {
            //Displays an image browsing dialog on click.
            MouseUp += new MouseButtonEventHandler((a, b) =>
            {
                DlgImgDisplay gui = new DlgImgDisplay();
                gui.Add(ImgUrl);
                gui.Show();
            });
        }

        /// <summary>
        /// Attempts to load the image. If it can't be loaded, it's not
        /// included.
        /// </summary>
        private void SetImage()
        {
            //Sets no image if the url is empty or not found.
            if (string.IsNullOrEmpty(ImgUrl) || !File.Exists(ImgUrl))
            {
                return;
            }

            //Loads a bitmap and ensures it has content before using it.
            try
            {
                BitmapImage image = new BitmapImage(new Uri(ImgUrl, UriKind.Relative));
                if (image != null && image.Width > 0 && image.Height > 0)
                {
                    img = image;
                }
            }

            //Does not add files that cause issues.
            catch (NotSupportedException) { }
            catch (FileNotFoundException) { }
            catch (UriFormatException) { }

            //Synchronizes thread access to change source.
            Action action = delegate
            {
                img.Freeze();
                Source = img;
            };

            //Executes the action (unless there's a race condition).
            try { Dispatcher.Invoke(action); } catch { }
        }

        /// <summary>
        /// This image is rendered with the fant resizing algorithm.
        /// </summary>
        protected override void OnRender(System.Windows.Media.DrawingContext drawContext)
        {
            VisualBitmapScalingMode = System.Windows.Media.BitmapScalingMode.Fant;
            base.OnRender(drawContext);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the longest dimension of the image to the given size,
        /// preserving the width:height ratio.
        /// </summary>
        /// <param name="size">
        /// The preferred size of the image in pixels.
        /// </param>
        public void SetSize(int size)
        {
            if (!Double.IsNaN(ActualWidth) && !Double.IsNaN(ActualHeight)
                && ActualWidth > ActualHeight)
            {
                Height = GetSourceHeight() * (size / GetSourceWidth());
                Width = size;

            }
            else
            {
                Width = GetSourceWidth() * (size / GetSourceHeight());
                Height = size;
            }
        }

        /// <summary>
        /// Returns the height of the underlying image source.
        /// </summary>
        public double GetSourceHeight()
        {
            return img?.Height ?? 0;
        }

        /// <summary>
        /// Returns the width of the underlying image source.
        /// </summary>
        public double GetSourceWidth()
        {
            return img?.Width ?? 0;
        }
        #endregion
    }
}