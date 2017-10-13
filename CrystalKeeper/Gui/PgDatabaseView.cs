using CrystalKeeper.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Represents the page associated with a project.
    /// </summary>
    class PgDatabaseView
    {
        #region Members
        /// <summary>
        /// The project data associated with the page.
        /// </summary>
        private Project project;

        /// <summary>
        /// The underlying gui.
        /// </summary>
        private PgDatabaseGuiView gui;

        /// <summary>
        /// The guid of the item selected.
        /// </summary>
        private DataItem selectedItem;

        /// <summary>
        /// Fires when the treeview item should change.
        /// </summary>
        public event EventHandler SelectedItemChanged;
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
        public PgDatabaseGuiView Gui
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
        /// The dataitem to navigate to.
        /// </summary>
        public DataItem SelectedItem
        {
            get
            {
                return selectedItem;
            }
            private set
            {
                selectedItem = value;
                SelectedItemChanged(this, null);
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
        public PgDatabaseView(Project project)
        {
            this.project = project;
            selectedItem = null;
            ConstructPage();
        }

        #endregion

        #region Methods
        /// <summary>
        /// Sets the page content based on existing material.
        /// </summary>
        public void ConstructPage()
        {
            gui = new PgDatabaseGuiView();
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
            #endregion

            #region Description
            //Sets the description.
            gui.TxtbxDescription.Text = (string)dat.GetData("description");
            #endregion

            #region Collections
            List<DataItem> collections =
                Project.GetItemsByType(DataItemTypes.Collection);

            //If there is nothing to display, shows a message saying so.
            if (collections.Count == 0)
            {
                //Creates the message as a textblock.
                TextBlock emptyMessage = new TextBlock();
                emptyMessage.Text = GlobalStrings.HintNoCollections;
                emptyMessage.HorizontalAlignment = HorizontalAlignment.Center;
                emptyMessage.VerticalAlignment = VerticalAlignment.Center;
                emptyMessage.Foreground = Brushes.LightGray;

                //Adds the message to the center of the grid.
                Grid.SetRow(emptyMessage, 1);
                gui.GuiItems.Children.Add(emptyMessage);
            }

            //Adds each collection.
            for (int i = 0; i < collections.Count; i++)
            {
                TextBlock blk = new TextBlock();
                blk.Text = (string)collections[i].GetData("name");
                blk.Padding = new Thickness(4);
                blk.TextAlignment = TextAlignment.Center;
                blk.TextWrapping = TextWrapping.Wrap;
                blk.HorizontalAlignment = HorizontalAlignment.Stretch;
                blk.FontSize = 14;

                //Bold when hovered.
                blk.MouseEnter +=
                    new System.Windows.Input.MouseEventHandler((a, b) =>
                    {
                        blk.FontWeight = FontWeights.Bold;
                    });

                //Normal when not hovered.
                blk.MouseLeave +=
                    new System.Windows.Input.MouseEventHandler((a, b) =>
                    {
                        blk.FontWeight = FontWeights.Normal;
                    });

                //Navigates to the collection when clicked.
                int pos = i; //Captured for the lambda.

                blk.MouseDown +=
                    new System.Windows.Input.MouseButtonEventHandler((a, b) =>
                    {
                        SelectedItem = collections[pos];
                    });

                gui.GuiItems.Children.Add(blk);
            }
            #endregion
        }
        #endregion
    }
}
