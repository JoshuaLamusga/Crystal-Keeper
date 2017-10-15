using CrystalKeeper.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Represents a dialog that displays any number of images in full size
    /// loaded from urls.
    /// </summary>
    class DlgImgDisplay
    {
        #region Members
        /// <summary>
        /// Contains and encapsulates gui functionality.
        /// </summary>
        private DlgImgDisplayGui gui;

        /// <summary>
        /// Stores friendly names for every picture.
        /// </summary>
        private List<string> names;

        /// <summary>
        /// Stores file paths, relative to the database location, for all
        /// images.
        /// </summary>
        private List<string> urls;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs an empty image display.
        /// </summary>
        public DlgImgDisplay()
        {
            urls = new List<string>();
            names = new List<string>();
            gui = new DlgImgDisplayGui();

            gui.GuiList.SelectionChanged += ChooseItem;
            gui.GuiGrid.KeyDown += GuiGrid_KeyDown;

            //Auto-selects the first image if it exists.
            if (gui.GuiList.HasItems)
            {
                gui.GuiList.SelectedIndex = 0;
            }
            else
            {
                gui.GuiList.SelectedIndex = -1;
            }

            Refresh();
        }

        /// <summary>
        /// Constructs an image display from a set of images.
        /// </summary>
        public DlgImgDisplay(params string[] urls)
        {
            this.urls = urls.ToList();
            names = new List<string>();

            for (int i = 0; i < urls.Length; i++)
            {
                names.Add(Path.GetFileNameWithoutExtension(urls[i]));
            }

            gui.GuiList.SelectionChanged += ChooseItem;
            gui.GuiGrid.KeyDown += GuiGrid_KeyDown;

            //Auto-selects the first image if it exists.
            if (gui.GuiList.HasItems)
            {
                gui.GuiList.SelectedIndex = 0;
            }
            else
            {
                gui.GuiList.SelectedIndex = -1;
            }

            Refresh();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Reacts to choosing an item in the list.
        /// </summary>
        private void ChooseItem(object sender, SelectionChangedEventArgs e)
        {
            //If nothing is selected, do nothing.
            if (gui.GuiList.SelectedIndex < 0)
            {
                return;
            }

            //Loads the image from the url and writes errors to a log.
            try
            {
                gui.GuiImageBorder.Reset();

                //Synchronizes thread access to change source.
                Action action = delegate
                {
                    BitmapImage img = new BitmapImage(
                        new Uri(urls[gui.GuiList.SelectedIndex], UriKind.Relative));

                    img.Freeze();
                    gui.GuiImage.Source = img;
                };

                //Executes the action (unless there's a race condition).
                try { gui.Dispatcher.Invoke(action); } catch { }
            }
            catch
            {
                Utils.Log("An image in display dialog " +
                    "did not load correctly.");
            }
        }

        /// <summary>
        /// Redraws the gui based on stored values.
        /// </summary>
        private void Refresh()
        {
            //Hides the image list if there is 1 or fewer images.
            if (gui.GuiList.Items.Count < 2)
            {
                gui.GuiList.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                gui.GuiList.Visibility = System.Windows.Visibility.Visible;
            }
        }

        /// <summary>
        /// Closes the image viewer if escape is pressed.
        /// </summary>
        private void GuiGrid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                gui.Close();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds an image to the listbox list, making it accessible.
        /// </summary>
        public void Add(string url)
        {
            urls.Add(url);
            names.Add(Path.GetFileNameWithoutExtension(url));
            gui.GuiList.ItemsSource = names;

            //Auto-selects the first image if it was just added.
            if (gui.GuiList.Items.Count == 1)
            {
                gui.GuiList.SelectedIndex = 0;
            }

            Refresh();
        }

        /// <summary>
        /// Removes all images from the listbox list.
        /// </summary>
        public void Clear()
        {
            urls.Clear();
            names.Clear();

            Refresh();
        }

        /// <summary>
        /// Displays the dialog.
        /// </summary>
        public void Show()
        {
            gui.Show();
        }
        #endregion
    }
}