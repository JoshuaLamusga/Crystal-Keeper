using CrystalKeeper.Core;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Wraps the functionality of a new project dialog.
    /// </summary>
    class DlgNewProject
    {
        #region Members
        /// <summary>
        /// Stores an instance of the new project gui.
        /// </summary>
        private DlgNewProjectGui gui;
        #endregion

        #region Constructors
        public DlgNewProject()
        {
            gui = new DlgNewProjectGui();
            gui.GuiNew.Click += GuiNew_Click;
            gui.GuiOpen.Click += GuiOpen_Click;
            gui.KeyDown += Gui_KeyDown;

            //Gets recently-opened URLs.
            var recentFiles = Utils.GetRecentlyOpened().Split('|').ToList();

            //Constructs a textblock noting recent files.
            if (recentFiles.Count > 0 && recentFiles.FirstOrDefault() != "")
            {
                TextBlock txtblkRecentFiles = new TextBlock();
                txtblkRecentFiles.Text = "Recent Files";
                txtblkRecentFiles.Foreground = Brushes.DarkGray;
                txtblkRecentFiles.Margin = new Thickness(4);
                txtblkRecentFiles.HorizontalAlignment = HorizontalAlignment.Center;
                gui.GuiPanel.Children.Add(txtblkRecentFiles);
            }
            
            //Adds an entry for each recent file.
            for (int i = 0; i < recentFiles.Count; i++)
            {
                TextBlock txtblk = new TextBlock();
                txtblk.Tag = recentFiles[i].ToString();
                txtblk.Text = Path.GetFileName(recentFiles[i]);
                txtblk.ToolTip = txtblk.Tag;
                txtblk.Margin = new Thickness(4);
                txtblk.HorizontalAlignment = HorizontalAlignment.Center;
                if (txtblk.Text.Length > 40)
                {
                    txtblk.Text = txtblk.Text.Substring(0, 40) + "...";
                }

                //Highlights in bold when hovering the mouse.
                txtblk.MouseEnter += (a, b) =>
                {
                    txtblk.FontWeight = FontWeights.Bold;
                };

                txtblk.MouseLeave += (a, b) =>
                {
                    txtblk.FontWeight = FontWeights.Normal;
                };

                //Attempts to load the given file.
                txtblk.MouseDown += (a, b) =>
                {
                    if (File.Exists((string)txtblk.Tag))
                    {
                        //Constructs project from the file.
                        MainDisplay display = new MainDisplay(Project.Load((string)txtblk.Tag), (string)txtblk.Tag);

                        //Shows the new display and close this one.
                        display.Show();
                        gui.Close();
                    }
                    else
                    {
                        //Removes the file if it can't be found.
                        MessageBox.Show("The project at " + (string)txtblk.Tag +
                            " could not be found.");

                        Utils.RegRemoveRecentlyOpen((string)txtblk.Tag);
                        gui.GuiPanel.Children.Remove(txtblk);
                    }
                };

                gui.GuiPanel.Children.Add(txtblk);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Displays the gui.
        /// </summary>
        public void Show()
        {
            gui.Show();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Closes the gui if the user presses escape.
        /// </summary>
        private void CloseWindow(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                gui.Close();
            }
        }

        /// <summary>
        /// Responds to keyboard events for the gui.
        /// </summary>
        private void Gui_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
                    e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.RightCtrl))
            {
                if (e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.O))
                {
                    GuiOpen_Click(null, null);
                }
                if (e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.N))
                {
                    GuiNew_Click(null, null);
                }
            }
        }

        /// <summary>
        /// Opens a project.
        /// </summary>
        private void GuiOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckPathExists = true;
            dlg.DefaultExt = ".mdat";
            dlg.Filter = "databases|*.mdat|all files|*.*";
            dlg.Title = "Load database";
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                //Constructs the project from the file, then uses
                //it to construct the visuals.
                MainDisplay display = new MainDisplay(Project.Load(dlg.FileName), dlg.FileName);

                //Show the new display and close this one.
                display.Show();
                gui.Close();
            }
        }

        /// <summary>
        /// Creates a new project.
        /// </summary>
        private void GuiNew_Click(object sender, RoutedEventArgs e)
        {
            MainDisplay display = new MainDisplay();

            //Show the new display and close this one.
            display.Show();
            gui.Close();
        }
        #endregion
    }
}