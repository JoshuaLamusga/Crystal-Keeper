using CrystalKeeper.Core;
using CrystalKeeper.GuiCore;
using System.Collections.Generic;
using System.Windows;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Wraps the functionality of a new collection dialog.
    /// </summary>
    class DlgNewCollection
    {
        #region Members
        /// <summary>
        /// Stores an instance of the new project gui.
        /// </summary>
        private DlgNewCollectionGui gui;

        /// <summary>
        /// The project associated with the page.
        /// </summary>
        private Project project;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a dialog to provide the template for a collection.
        /// </summary>
        /// <param name="project">The associated project.</param>
        /// <param name="collection">The collection involved.</param>
        public DlgNewCollection(Project project, string collectionName)
        {
            gui = new DlgNewCollectionGui();
            this.project = project;

            //The Ok button is disabled by default.
            gui.BttnOk.IsEnabled = false;

            //Populates the template combobox.
            List<DataItem> items = project.GetItemsByType(DataItemTypes.Template);
            List<CmbxDataItem> itemMenus = new List<CmbxDataItem>();
            for (int i = 0; i < items.Count; i++)
            {
                CmbxDataItem item = new CmbxDataItem(items[i]);
                itemMenus.Add(item);
                gui.CmbxTemplate.Items.Add(item);

                //Selects the first item.
                if (i == 0)
                {
                    item.IsSelected = true;
                    gui.BttnOk.IsEnabled = true;
                }
            }

            //Sets the template for the new collection.
            gui.BttnOk.Click += new RoutedEventHandler((a, b) =>
                {
                    if (!string.IsNullOrWhiteSpace(collectionName))
                    {
                        if (!project.Items.Contains(
                            ((CmbxDataItem)gui.CmbxTemplate.SelectedItem).GetItem()))
                        {
                            gui.BttnOk.IsEnabled = false;
                        }
                        else
                        {
                            ulong col = this.project.AddCollection(
                                collectionName,
                                string.Empty,
                                ((CmbxDataItem)gui.CmbxTemplate.SelectedItem).GetItem().guid);

                            this.project.AddGrouping("all", col);

                            gui.Close();
                        }
                    }
                });

            //Cancels creating the collection.
            gui.BttnCancel.Click += new RoutedEventHandler((a, b) =>
                {
                    gui.Close();
                });

            //Handles selecting an item.
            gui.CmbxTemplate.SelectionChanged +=
                new System.Windows.Controls.SelectionChangedEventHandler((a, b) =>
                {
                    if (project.Items.Contains(
                    ((CmbxDataItem)gui.CmbxTemplate.SelectedItem).GetItem()))
                    {
                        gui.BttnOk.IsEnabled = true;
                    }
                    else
                    {
                        gui.BttnOk.IsEnabled = false;
                    }
                });
        }
        #endregion

        #region Methods
        /// <summary>
        /// Displays the gui.
        /// </summary>
        public void Show()
        {
            gui.Show();
        }
        #endregion
    }
}
