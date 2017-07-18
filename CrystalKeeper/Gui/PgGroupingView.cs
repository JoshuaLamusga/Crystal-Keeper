using CrystalKeeper.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Represents the page associated with a grouping.
    /// </summary>
    class PgGroupingView
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
        /// The grouping to use.
        /// </summary>
        private DataItem grouping;

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
        /// Creates a grouping page from the given project.
        /// </summary>
        /// <param name="project">
        /// The current project to retrieve item data from.
        /// </param>
        /// <param name="grouping">
        /// The grouping item used to construct the page.
        /// </param>
        public PgGroupingView(Project project, DataItem grouping)
        {
            this.project = project;
            this.grouping = grouping;
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

            #region Grouping name
            //Sets the grouping name.
            if (string.IsNullOrWhiteSpace((string)grouping.GetData("name")))
            {
                gui.TxtblkCollectionName.Text = "Untitled";
            }
            else
            {
                gui.TxtblkCollectionName.Text = (string)grouping.GetData("name");
            }
            #endregion

            #region Groupings
            List<DataItem> entryRefs =
                Project.GetGroupingEntryRefs(grouping);

            //If there is nothing to display, shows a message saying so.
            if (entryRefs.Count == 0)
            {
                //Creates the message as a textblock.
                TextBlock emptyMessage = new TextBlock();
                emptyMessage.Text = "No entries have been added to this group";
                emptyMessage.HorizontalAlignment = HorizontalAlignment.Center;
                emptyMessage.VerticalAlignment = VerticalAlignment.Center;
                emptyMessage.Foreground = Brushes.LightGray;

                //Adds the message to the center of the grid.
                Grid.SetRow(emptyMessage, 1);
                gui.GuiItems.Children.Add(emptyMessage);
            }

            //Adds each entry.
            for (int i = 0; i < entryRefs.Count; i++)
            {
                TextBlock blk = new TextBlock();
                blk.Text = (string)project.GetEntryRefEntry(entryRefs[i]).GetData("name");
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
                        SelectedItem = entryRefs[pos];
                    });

                gui.GuiItems.Children.Add(blk);
            }
            #endregion
        }
        #endregion
    }
}
