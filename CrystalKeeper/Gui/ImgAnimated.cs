using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Displays a cyclically animated, resized image that plays when the user
    /// hovers over it, and optionally when they don't. The user can drag the
    /// mouse left or right over the image to progress frame-by-frame.
    /// </summary>
    class ImgAnimated : Image
    {
        #region Private Members
        /// <summary>
        /// Changes the current frame.
        /// </summary>
        private int frame;

        /// <summary>
        /// The images, preloaded for performance.
        /// </summary>
        private List<BitmapImage> images;

        /// <summary>
        /// The urls storing the image locations.
        /// </summary>
        private List<string> imgUrls;

        /// <summary>
        /// Every mouse press is considered a click until frames are changed.
        /// </summary>
        private bool mouseIsClicking;

        /// <summary>
        /// Whether the user is holding the mouse down on the control.
        /// </summary>
        private bool mouseIsDown;

        /// <summary>
        /// Stores the last recorded mouse position, which is used with the
        /// distance counter to rotate frame-by-frame.
        /// </summary>
        private Point mouseDownPos;

        /// <summary>
        /// Used for automatic frame playback.
        /// </summary>
        private Timer timer;

        /// <summary>
        /// The playback delay in milliseconds.
        /// </summary>
        private int timerDelay;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a blank animated image.
        /// </summary>
        /// <param name="doAutoplay">
        /// Whether to play when the user isn't hovered or not.
        /// </param>
        public ImgAnimated(
            bool doAutoplay,
            int playbackDelay = 42)
            : base()
        {
            frame = 0;
            images = new List<BitmapImage>();
            imgUrls = new List<string>();
            timer = new Timer();
            timerDelay = playbackDelay;
            mouseIsClicking = false;
            mouseIsDown = false;
            mouseDownPos = Mouse.GetPosition(this);

            //Handles automatic playback behavior.
            SetHandlers(doAutoplay);
        }

        /// <summary>
        /// Constructs an animated image from a url.
        /// </summary>
        /// <param name="urls">
        /// File paths relative to the database file.
        /// </param>
        /// <param name="doAutoplay">
        /// Whether to play when the user isn't hovered or not.
        /// </param>
        public ImgAnimated(
            List<string> urls,
            bool doAutoplay,
            int playbackDelay = 42)
            : base()
        {
            frame = 0;
            images = new List<BitmapImage>();
            imgUrls = urls;
            timer = new Timer();
            timerDelay = playbackDelay;
            mouseIsClicking = false;
            mouseIsDown = false;
            mouseDownPos = Mouse.GetPosition(this);

            SetImages();

            //Handles automatic playback behavior.
            SetHandlers(doAutoplay);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Attempts to load all images. Images which cannot be loaded are
        /// not included. Returns true if all images were successfully loaded.
        /// </summary>
        private bool SetImages()
        {
            bool fullSuccess = true;

            for (int i = 0; i < imgUrls.Count; i++)
            {
                //Skips the image if the url is empty or not found.
                if (string.IsNullOrWhiteSpace(imgUrls[i]) || !File.Exists(imgUrls[i]))
                {
                    fullSuccess = false;
                    continue;
                }

                try
                {
                    BitmapImage img = new BitmapImage(new Uri(imgUrls[i], UriKind.Relative));
                    if (img != null && img.Width > 0 && img.Height > 0)
                    {
                        images.Add(img);
                    }
                }

                //Does not add files that cause issues.
                catch (NotSupportedException) { fullSuccess = false; }
                catch (FileNotFoundException) { fullSuccess = false; }
                catch (UriFormatException) { fullSuccess = false; }
            }

            if (images.Count > 0)
            {
                Source = images[0];
            }

            return fullSuccess;
        }

        /// <summary>
        /// Sets up default display behaviors.
        /// </summary>
        private void SetHandlers(bool doAutoplay)
        {
            if (doAutoplay)
            {
                Play();

                MouseUp += new MouseButtonEventHandler((a, b) =>
                {
                    ReleaseMouseCapture();
                    mouseIsDown = false;

                    Play();
                });
            }
            else
            {
                MouseUp += new MouseButtonEventHandler((a, b) =>
                {
                    ReleaseMouseCapture();
                    mouseIsDown = false;

                    if (IsMouseOver)
                    {
                        Play();
                    }
                    else
                    {
                        Stop();
                    }
                });

                MouseLeave += new MouseEventHandler((a, b) =>
                    {
                        Stop();
                    });

                MouseEnter += new MouseEventHandler((a, b) =>
                    {
                        Play();
                    });
            }

            //Displays an image browsing dialog of each frame on click.
            MouseUp += new MouseButtonEventHandler((a, b) =>
                {
                    if (mouseIsClicking)
                    {
                        DlgImgDisplay gui = new DlgImgDisplay();
                        
                        for (int i = 0; i < imgUrls.Count; i++)
                        {
                            gui.Add(imgUrls[i]);
                        }

                        gui.Show();
                    }
                });

            //Sets up to handle frame rotation or clicking.
            MouseDown += new MouseButtonEventHandler((a, b) =>
                {
                    CaptureMouse(); //To handle mouse up while outside control.
                    mouseIsDown = true;
                    mouseDownPos = Mouse.GetPosition(this);
                    mouseIsClicking = true;

                    Stop();
                });

            //Controls frame rotation.
            MouseMove += new MouseEventHandler((a, b) =>
                {
                    if (mouseIsDown)
                    {
                        double distance =
                            Mouse.GetPosition(this).X - mouseDownPos.X;

                        if (Math.Abs(distance) >= 3)
                        {
                            if (distance >= 3)
                            {
                                NextImage();
                                mouseDownPos = Mouse.GetPosition(this);
                            }
                            else if (distance <= -3)
                            {
                                PrevImage();
                                mouseDownPos = Mouse.GetPosition(this);
                            }

                            //If a frame rotates, it's not a click.
                            mouseIsClicking = false;
                        }
                    }
                });
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the number of milliseconds used between cycling images.
        /// </summary>
        /// <param name="ms">
        /// The millisecond delay for showing the next image.
        /// </param>
        public void SetPlaybackDelay(int ms)
        {
            timerDelay = ms;

            //Resets the timer with the given ms delay.
            if (timer.Enabled)
            {
                Play();
            }
        }

        /// <summary>
        /// Advances one frame.
        /// </summary>
        public void NextImage()
        {
            //Exits unless there is something to animate.
            if (images.Count < 1)
            {
                return;
            }

            if (frame == images.Count - 1)
            {
                frame = 0;
            }
            else
            {
                frame++;

                //Synchronizes thread access to change source.
                Action action = delegate
                {
                    images[frame].Freeze();
                    Source = images[frame];
                };

                //Executes the action (unless there's a race condition).
                try { Dispatcher.Invoke(action); } catch {}
            }
        }
        
        /// <summary>
        /// Plays images at the given framerate in milliseconds.
        /// </summary>
        public void Play()
        {
            timer.Stop();
            timer = new Timer(timerDelay);
            timer.AutoReset = true;

            timer.Elapsed += new ElapsedEventHandler((a, b) =>
            {
                NextImage();
            });

            timer.Start();
        }

        /// <summary>
        /// Backtracks one frame.
        /// </summary>
        public void PrevImage()
        {
            //Exits unless there is something to animate.
            if (images.Count < 1)
            {
                return;
            }

            if (frame == 0)
            {
                frame = images.Count - 1;
            }
            else
            {
                frame--;

                //Synchronizes thread access to change source.
                Action action = delegate
                {
                    images[frame].Freeze();
                    Source = images[frame];
                };

                //Executes the action (unless there's a race condition).
                try { Dispatcher.Invoke(action); } catch {}
            }
        }

        /// <summary>
        /// Stops playing images.
        /// </summary>
        public void Stop()
        {
            timer.Stop();
        }

        /// <summary>
        /// Overwrites the urls from which to load images. Returns true if
        /// all images were successfully loaded.
        /// </summary>
        public bool SetUrls(List<string> urls)
        {
            imgUrls = urls;
            return SetImages();
        }

        /// <summary>
        /// Returns the urls that images are loaded from.
        /// </summary>
        public List<string> GetUrls()
        {
            return new List<string>(imgUrls);
        }
        #endregion
    }
}
