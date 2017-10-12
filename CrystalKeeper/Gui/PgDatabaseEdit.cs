using CrystalKeeper.Core;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Represents the page associated with a project.
    /// </summary>
    class PgDatabaseEdit
    {
        #region Members
        /// <summary>
        /// The database item associated with the page.
        /// </summary>
        private Project project;

        /// <summary>
        /// The underlying gui.
        /// </summary>
        private PgDatabaseGuiEdit gui;

        /// <summary>
        /// Fires when project data is changed.
        /// </summary>
        public event EventHandler DataNameChanged;

        /// <summary>
        /// Fires when the background image changes.
        /// </summary>
        public event EventHandler BgImageChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the data used to construct the page.
        /// </summary>
        public Project Project
        {
            set
            {
                project = value;
                ConstructPage();
            }
            get
            {
                return project;
            }
        }

        /// <summary>
        /// Gets or sets the gui that controls the page appearance.
        /// </summary>
        public PgDatabaseGuiEdit Gui
        {
            private set
            {
                gui = value;
            }
            get
            {
                return gui;
            }
        }

        /// <summary>
        /// Returns the current background image url.
        /// </summary>
        public string BgImage
        {
            get
            {
                return (string)Project.GetDatabase().GetData("imageUrl");
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a database page from the given project.
        /// </summary>
        /// <param name="project">
        /// The database item given.
        /// </param>
        public PgDatabaseEdit(Project project)
        {
            this.project = project;
            ConstructPage();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the page content based on existing material.
        /// </summary>
        public void ConstructPage()
        {
            gui = new PgDatabaseGuiEdit();
            DataItem dat = Project.GetDatabase();

            #region Database name
            //Sets the database name.
            if (string.IsNullOrWhiteSpace((string)dat.GetData("name")))
            {
                gui.TxtblkDatabaseName.Text = GlobalStrings.NameUntitled;
            }
            else
            {
                gui.TxtblkDatabaseName.Text = (string)dat.GetData("name");
            }

            //Handles changes to the database name.
            gui.TxtblkDatabaseName.TextChanged += new TextChangedEventHandler((a, b) =>
                {
                    if (!string.IsNullOrWhiteSpace(gui.TxtblkDatabaseName.Text))
                    {
                        dat.SetData("name", gui.TxtblkDatabaseName.Text);
                    }

                    //If the textbox is empty, it will keep the last character.
                    gui.TxtblkDatabaseName.Text = (string)dat.GetData("name");

                    DataNameChanged?.Invoke(this, null);
                });
            #endregion

            #region Mode combobox
            //Sets the mode combobox.
            if ((bool)dat.GetData("defUseEditMode"))
            {
                gui.CmbxDefaultEditMode.SelectedValue =
                    GlobalStrings.DatabaseEditDefEditModeEdit;
            }
            else
            {
                gui.CmbxDefaultEditMode.SelectedValue =
                    GlobalStrings.DatabaseEditDefEditModeView;
            }

            //Handles changes to mode combobox.
            gui.CmbxDefaultEditMode.SelectionChanged +=
                new SelectionChangedEventHandler((a, b) =>
            {
                dat.SetData("defUseEditMode",
                    (string)gui.CmbxDefaultEditMode.SelectedValue ==
                    GlobalStrings.DatabaseEditDefEditModeEdit);
            });
            #endregion

            #region Description
            //Sets the description.
            gui.TxtbxDescription.Text = (string)dat.GetData("description");

            //Handles changes to the description.
            gui.TxtbxDescription.TextChanged += new TextChangedEventHandler((a, b) =>
                {
                    dat.SetData("description", gui.TxtbxDescription.Text);
                });
            #endregion

            #region Background image
            //Sets the background image data.
            string bgUrl = (string)dat.GetData("imageUrl");
            if (!string.IsNullOrWhiteSpace(bgUrl))
            {
                if (File.Exists(bgUrl))
                {
                    gui.ImgDeleteBgImage.IsEnabled = true;
                    gui.ImgDeleteBgImage.Visibility =
                        System.Windows.Visibility.Visible;
                    gui.ImgBgImage.IsEnabled = true;
                    gui.ImgBgImage.Visibility =
                        System.Windows.Visibility.Visible;
                    gui.ImgBgImage.Source =
                        new BitmapImage(new Uri(bgUrl, UriKind.Absolute));
                }
                else
                {
                    gui.ImgDeleteBgImage.IsEnabled = false;
                    gui.ImgDeleteBgImage.Visibility =
                        System.Windows.Visibility.Collapsed;
                    gui.ImgBgImage.IsEnabled = false;
                    gui.ImgBgImage.Visibility =
                        System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                gui.ImgDeleteBgImage.IsEnabled = false;
                gui.ImgDeleteBgImage.Visibility =
                    System.Windows.Visibility.Collapsed;
                gui.ImgBgImage.Visibility =
                    System.Windows.Visibility.Collapsed;
                gui.ImgBgImage.IsEnabled = false;
            }

            //The deletion button responds to mouse interaction.
            gui.ImgDeleteBgImage.MouseEnter +=
                new System.Windows.Input.MouseEventHandler((a, b) =>
                    {
                        gui.ImgDeleteBgImage.Source = new BitmapImage(
                            new Uri(Assets.BttnDeleteHover));
                    });

            gui.ImgDeleteBgImage.MouseLeave +=
                new System.Windows.Input.MouseEventHandler((a, b) =>
                {
                    gui.ImgDeleteBgImage.Source = new BitmapImage(
                        new Uri(Assets.BttnDelete));
                });

            gui.ImgDeleteBgImage.MouseDown +=
                new System.Windows.Input.MouseButtonEventHandler((a, b) =>
                {
                    dat.SetData("imageUrl", String.Empty);
                    gui.ImgDeleteBgImage.IsEnabled = false;
                    gui.ImgDeleteBgImage.Visibility =
                        System.Windows.Visibility.Collapsed;
                    gui.ImgBgImage.IsEnabled = false;
                    gui.ImgBgImage.Visibility =
                        System.Windows.Visibility.Collapsed;

                    BgImageChanged?.Invoke(this, null);
                });

            //Shows the background image in full size if clicked.
            gui.ImgBgImage.MouseDown +=
                new System.Windows.Input.MouseButtonEventHandler((a, b) =>
                {
                    DlgImgDisplay gui = new DlgImgDisplay();
                    gui.Add(bgUrl);
                    gui.Show();
                });

            //Allows the user to browse to an image if selected.
            gui.BttnBrowseBgImage.Click +=
                new System.Windows.RoutedEventHandler((a, b) =>
                {
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.CheckPathExists = true;
                    dlg.Filter = GlobalStrings.FilterPictures;
                    dlg.Title = GlobalStrings.CaptionLoadDatabase;

                    dlg.FileOk +=
                        new System.ComponentModel.CancelEventHandler((c, d) =>
                        {
                            bgUrl = dlg.FileName;
                            dat.SetData("imageUrl", bgUrl);

                            gui.ImgDeleteBgImage.IsEnabled = true;
                            gui.ImgDeleteBgImage.Visibility =
                                System.Windows.Visibility.Visible;
                            gui.ImgBgImage.IsEnabled = true;
                            gui.ImgBgImage.Visibility =
                                System.Windows.Visibility.Visible;
                            gui.ImgBgImage.Source = new BitmapImage(
                                new Uri(bgUrl, UriKind.Absolute));

                            BgImageChanged?.Invoke(this, null);
                        });

                    dlg.ShowDialog();
                });
            #endregion
        }
        #endregion
    }
}
