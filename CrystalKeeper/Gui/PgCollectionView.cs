using CrystalKeeper.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Represents the page associated with a collection.
    /// </summary>
    class PgCollectionView
    {
        #region Members
        /// <summary>
        /// The project data associated with the page.
        /// </summary>
        private Project project;

        /// <summary>
        /// The underlying gui.
        /// </summary>
        private PgCollectionGuiView gui;

        /// <summary>
        /// The collection to use.
        /// </summary>
        private DataItem collection;

        /// <summary>
        /// The dataitem to navigate to when selected.
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
        public PgCollectionGuiView Gui
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
        /// The guid of the item selected.
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
        /// Creates a collection page from the given project.
        /// </summary>
        /// <param name="project">
        /// The current project to retrieve item data from.
        /// </param>
        /// <param name="collection">
        /// The collection item used to construct the page.
        /// </param>
        public PgCollectionView(Project project, DataItem collection)
        {
            this.project = project;
            this.collection = collection;
            ConstructPage();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the page content based on existing material.
        /// </summary>
        public void ConstructPage()
        {
            gui = new PgCollectionGuiView();

            #region Collection name
            //Sets the collection name.
            if (string.IsNullOrWhiteSpace((string)collection.GetData("name")))
            {
                gui.TxtblkCollectionName.Text = "Untitled";
            }
            else
            {
                gui.TxtblkCollectionName.Text = (string)collection.GetData("name");
            }
            #endregion

            #region Collection description
            //Sets the collection description.
            if (!string.IsNullOrWhiteSpace((string)collection.GetData("description")))
            {
                gui.TxtbxDescription.Text = (string)collection.GetData("description");
            }
            #endregion

            #region Groupings
            List<DataItem> grps =
                Project.GetCollectionGroupings(collection);

            //Adds each grouping.
            for (int i = 0; i < grps.Count; i++)
            {
                TextBlock blk = new TextBlock();
                blk.Text = (string)grps[i].GetData("name");
                blk.Padding = new Thickness(4);
                blk.TextAlignment = TextAlignment.Center;
                blk.FontSize = 14;
                blk.Width = 300;
                blk.MaxWidth = 300;

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
                        SelectedItem = grps[pos];
                    });

                gui.GuiItems.Children.Add(blk);
            }
            #endregion
        }
        #endregion
    }
}