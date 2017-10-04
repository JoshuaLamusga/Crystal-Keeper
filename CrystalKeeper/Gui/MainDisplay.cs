using CrystalKeeper.Core;
using CrystalKeeper.GuiCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Binds an instance of the main display gui to a database.
    /// </summary>
    class MainDisplay
    {
        #region Members
        /// <summary>
        /// Contains the main gui.
        /// </summary>
        private MainDisplayGui gui;

        /// <summary>
        /// Contains the logic.
        /// </summary>
        private Project project;

        /// <summary>
        /// The location to save the project; set on first save.
        /// </summary>
        private string saveUrl;

        /// <summary>
        /// The location to save the project; set on first save.
        /// </summary>
        private string SaveUrl
        {
            get
            {
                return saveUrl;
            }
            set
            {
                saveUrl = value;
                UpdateRecentFiles();
            }
        }

        /// <summary>
        /// Stores the treeview selected item whenever it's not null.
        /// </summary>
        private TreeViewDataItem selection;

        /// <summary>
        /// Stores whether edit mode is active or not.
        /// </summary>
        private bool isEditing;

        /// <summary>
        /// When this timer goes off, the project attempts to autosave.
        /// </summary>
        private Timer autosaveTimer;

        /// <summary>
        /// The template field to filter results with.
        /// </summary>
        private DataItem treeviewFilterField;

        /// <summary>
        /// The text used to filter treeview results.
        /// </summary>
        private string treeviewFilterText;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new display.
        /// </summary>
        public MainDisplay()
        {
            Initialize(null);
            SaveUrl = String.Empty;
        }

        /// <summary>
        /// Constructs the visuals according to the project provided.
        /// </summary>
        /// <param name="project">
        /// The project to provide.
        /// </param>
        public MainDisplay(Project project, string saveUrl)
        {
            Initialize(project);
            SaveUrl = saveUrl;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Attempts to save a backup file to the same directory as the
        /// current one. It can only be saved for projects that have been
        /// saved before. It will save with numerically-increasing file
        /// numbers.
        /// </summary>
        private void Autosave(object sender, ElapsedEventArgs e)
        {
            //If the project has been saved successfully before.
            if (File.Exists(saveUrl))
            {
                int backupNumber = 0;
                string dirName = Path.GetDirectoryName(saveUrl);
                string fName = Path.GetFileNameWithoutExtension(saveUrl);
                string fullPath = dirName + "\\" + fName + "-bak" +
                        backupNumber + ".mdat";

                //Increases the backup number rather than overwriting.
                while (File.Exists(fullPath))
                {
                    backupNumber++;
                    fullPath = dirName + "\\" + fName + "-bak" +
                        backupNumber + ".mdat";
                }

                //Attempts to save to the given path.
                lock (project)
                {
                    project.Save(fullPath);
                }
            }
        }

        /// <summary>
        /// Sets default values and loads a project if necessary. Sets
        /// event handlers and updates the gui appearance.
        /// </summary>
        private void Initialize(Project project)
        {
            //Sets defaults for variables.
            gui = new MainDisplayGui();

            //Accepts a loaded project if it exists, or makes one.
            if (project != null)
            {
                this.project = project;
            }
            else
            {
                this.project = new Project();
                InitializeDefaultProject();
            }

            //Clears any selections and reverts to viewing mode.
            selection = null;
            isEditing = false;

            //This timer saves the project every ten minutes automatically.
            autosaveTimer = new Timer(600000);
            autosaveTimer.Elapsed += Autosave;
            autosaveTimer.Start();

            //Hooks all handlers to events.
            gui.GuiTreeViewSearch.TextChanged += RefreshTreeviewFilter;
            this.project.Items.CollectionChanged += ChangeTreeview;
            gui.GuiTreeView.KeyDown += GuiTreeView_KeyDown;
            gui.GuiTreeView.SelectedItemChanged += GuiTreeView_SelectedItemChanged;
            gui.Closing += _gui_Closing;
            gui.GuiFileNew.Click += GuiFileNew_Click;
            gui.GuiFileOpen.Click += GuiFileOpen_Click;
            gui.GuiFileSave.Click += GuiFileSave_Click;
            gui.GuiFileSaveAs.Click += GuiFileSaveAs_Click;
            gui.KeyDown += _gui_KeyDown;
            gui.GuiHelpAbout.Click += GuiHelpAbout_Click;
            gui.GuiNewCollection.KeyDown += GuiNewCollection_KeyDown;
            gui.GuiNewGrouping.KeyDown += GuiNewGrouping_KeyDown;
            gui.GuiNewEntry.KeyDown += GuiNewEntry_KeyDown;
            gui.GuiToggleMode.MouseEnter += GuiToggleMode_MouseEnter;
            gui.GuiToggleMode.MouseLeave += GuiToggleMode_MouseLeave;
            gui.GuiToggleMode.MouseDown += GuiToggleMode_MouseDown;
            gui.GuiPrint.MouseEnter += GuiPrint_MouseEnter;
            gui.GuiPrint.MouseLeave += GuiPrint_MouseLeave;
            gui.GuiPrint.MouseDown += GuiPrint_MouseDown;
            gui.GuiTemplateNew.Click += GuiTemplateNew_Click;

            UpdateRecentFiles();
            ConstructVisuals();
        }

        /// <summary>
        /// Sets up templates and collections for new projects.
        /// </summary>
        private void InitializeDefaultProject()
        {
            //Sets up a layout for minerals.
            ulong mineralGuid = project.AddTemplate("Minerals", true, true,
                3, TemplateImagePos.Under, String.Empty, 0, 0, 0, 0, 0, 0);
            ulong col1Guid = project.AddTemplateColumnData(true, mineralGuid);
            ulong col2Guid = project.AddTemplateColumnData(false, mineralGuid);
            project.AddTemplateField("Images", col1Guid,
                TemplateFieldType.EntryImages, true, false, false, 0);
            project.AddTemplateField("Extra Images", col1Guid,
                TemplateFieldType.Images, true, false, false, 1);
            project.AddTemplateField("Primary Mineral Species", col1Guid,
                TemplateFieldType.Min_Name, true, true, false, 2);
            project.AddTemplateField("Secondary Mineral Species", col1Guid,
                TemplateFieldType.Min_Name, true, true, false, 3);
            project.AddTemplateField("Primary Chemical Formula", col2Guid,
                TemplateFieldType.Min_Formula, true, true, false, 0);
            project.AddTemplateField("Species Group", col2Guid,
                TemplateFieldType.Min_Group, true, true, false, 1);
            project.AddTemplateField("Origin / Location", col2Guid,
                TemplateFieldType.Text, true, true, false, 2);
            project.AddTemplateField("GPS Location", col2Guid,
                TemplateFieldType.Hyperlink, true, true, false, 3);
            project.AddTemplateField("Market Value", col2Guid,
                TemplateFieldType.MoneyUSD, true, true, false, 4);
            project.AddTemplateField("Notes", col2Guid,
                TemplateFieldType.Text, true, true, false, 5);

            //Sets up a layout for locality notes.
            ulong localitiesGuid = project.AddTemplate("Localities", false, false,
                3, TemplateImagePos.Under, String.Empty, 0, 0, 0, 0, 0, 0);
            col1Guid = project.AddTemplateColumnData(true, localitiesGuid);
            project.AddTemplateField("Images", col1Guid,
                TemplateFieldType.EntryImages, true, false, false, 0);
            project.AddTemplateField("Locality Name", col1Guid,
                TemplateFieldType.Text, true, true, false, 1);
            project.AddTemplateField("GPS Location", col1Guid,
                TemplateFieldType.Hyperlink, true, true, false, 2);
            project.AddTemplateField("Notes", col1Guid,
                TemplateFieldType.Text, true, true, false, 3);

            //Sets up a layout for other notes.
            ulong notesGuid = project.AddTemplate("Notes", false, false,
                3, TemplateImagePos.Under, String.Empty, 0, 0, 0, 0, 0, 0);
            col1Guid = project.AddTemplateColumnData(true, notesGuid);
            project.AddTemplateField("Images", col1Guid,
                TemplateFieldType.EntryImages, true, false, false, 0);
            project.AddTemplateField("Notes", col1Guid,
                TemplateFieldType.Text, true, true, false, 0);

            //Sets up default collections with each template.
            ulong mnrlCol = project.AddCollection("Minerals", "My mineral collection.", mineralGuid);
            ulong lctnCol = project.AddCollection("Localities", "A collection of localities.", localitiesGuid);
            ulong noteCol = project.AddCollection("Notes", "Some miscellaneous notes.", notesGuid);
            project.AddGrouping("all", mnrlCol);
            project.AddGrouping("all", lctnCol);
            project.AddGrouping("all", noteCol);

            ConstructVisuals();
        }

        /// <summary>
        /// Opens a modal dialog to define or edit a template.
        /// </summary>
        private void GuiTemplateNew_Click(object sender, RoutedEventArgs e)
        {
            //True if opening an existing template, else false.
            bool isEditing = false;

            //Copies the entire project so changes can be saved or discarded.
            Project projectCopy = new Project(project);

            MenuDataItem newMenuItem = null;
            DataItem template = null;

            //Opens an associated template for the menu if it exists.
            if (sender is MenuDataItem &&
                ((MenuDataItem)sender).GetItem().type == DataItemTypes.Template)
            {
                newMenuItem = (MenuDataItem)sender;
                template = newMenuItem.GetItem();
                isEditing = true;
            }

            //If there is no associated template, a new one is created.
            else
            {
                template = projectCopy.GetItemByGuid(projectCopy.AddTemplate(
                String.Empty, true, true, 3, TemplateImagePos.Under,
                "Arial", 0, 0, 0, 0, 0, 0));

                //Creates two template columns.
                ulong col1 = projectCopy.AddTemplateColumnData(true, template.guid);
                projectCopy.AddTemplateColumnData(false, template.guid);

                //Creates the required entry name and images fields.
                projectCopy.AddTemplateField("images", col1,
                    TemplateFieldType.EntryImages, true, true, true, 0);

                //Adds the template to the list of templates.
                newMenuItem = new MenuDataItem(template);
                newMenuItem.Click += GuiTemplateNew_Click;
            }

            //Opens the dialog to set the template data.
            var dlg = new DlgEditTemplate(projectCopy, template);
            if (dlg.ShowDialog() == true)
            {
                //Copies the project and resets event handlers.
                project = projectCopy;

                //Updates the name of the item.
                newMenuItem.Header = (string)project
                    .GetItemByGuid(template.guid).GetData("name");

                //Rebinds project event hooks lost during copy.
                project.Items.CollectionChanged += ChangeTreeview;

                //Adds the new menu item if one was created.
                if (!isEditing)
                {
                    gui.GuiMenuTemplates.Items.Add(newMenuItem);
                }

                if (dlg.ReferencesInvalidated)
                {
                    ConstructVisuals();
                    SetPage();
                };
            }

            //Handles template deletion.
            else if (!projectCopy.Items.Contains(template))
            {
                project.Items.Remove(template);
                gui.GuiMenuTemplates.Items.Remove(newMenuItem);
            }
        }

        /// <summary>
        /// Toggles between edit and hover mode for the current item.
        /// </summary>
        private void GuiToggleMode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isEditing = !isEditing;

            if (isEditing)
            {
                gui.GuiToggleMode.Source = new BitmapImage(new Uri(
                    Assets.BttnConfirmHover));

                GuiTreeView_SelectedItemChanged(this, null);
            }
            else
            {
                gui.GuiToggleMode.Source = new BitmapImage(new Uri(
                    Assets.BttnEditHover));

                GuiTreeView_SelectedItemChanged(this, null);
            }
        }

        /// <summary>
        /// Sets the bitmap for toggle mode to acknowledge no hovering.
        /// </summary>
        private void GuiToggleMode_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isEditing)
            {
                gui.GuiToggleMode.Source = new BitmapImage(new Uri(
                    Assets.BttnConfirm));
            }
            else
            {
                gui.GuiToggleMode.Source = new BitmapImage(new Uri(
                    Assets.BttnEdit));
            }
        }

        /// <summary>
        /// Sets the bitmap for toggle mode to acknowledge hovering.
        /// </summary>
        private void GuiToggleMode_MouseEnter(object sender, MouseEventArgs e)
        {
            if (isEditing)
            {
                gui.GuiToggleMode.Source = new BitmapImage(new Uri(
                    Assets.BttnConfirmHover));
            }
            else
            {
                gui.GuiToggleMode.Source = new BitmapImage(new Uri(
                    Assets.BttnEditHover));
            }
        }

        /// <summary>
        /// Shows a dialog for the user to print the current page.
        /// </summary>
        private void GuiPrint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; //Prevents an unknown double-call of the method.
            Print();
        }

        /// <summary>
        /// Sets the bitmap for the print icon to acknowledge no hovering.
        /// </summary>
        private void GuiPrint_MouseLeave(object sender, MouseEventArgs e)
        {
            gui.GuiPrint.Source = new BitmapImage(new Uri(
                Assets.BttnPrint));
        }

        /// <summary>
        /// Sets the bitmap for the print icon to acknowledge hovering.
        /// </summary>
        private void GuiPrint_MouseEnter(object sender, MouseEventArgs e)
        {
            gui.GuiPrint.Source = new BitmapImage(new Uri(
                Assets.BttnPrintHover));
        }

        /// <summary>
        /// Generates a new entry object.
        /// </summary>
        private void GuiNewEntry_KeyDown(object sender, KeyEventArgs e)
        {
            //When enter is pressed.
            if (e.Key == Key.Enter && e.IsDown)
            {
                e.Handled = true;

                //Exits if there is no reasonable name.
                if (string.IsNullOrWhiteSpace(gui.GuiNewEntry.Text))
                {
                    return;
                }

                //Refers to the selected entry and its parent collection.
                var item = (TreeViewDataItem)gui.GuiTreeView.SelectedItem;
                var col = item;

                //Traverses up to the collection.
                if (col != null)
                {
                    while (col.GetItem().type != DataItemTypes.Database &&
                        col.GetItem().type != DataItemTypes.Collection)
                    {
                        if (col.GetParent() != null)
                        {
                            col = col.GetParent();
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                //If the supposed collection item is indeed a collection.
                if (col != null &&
                    col.GetItem().type == DataItemTypes.Collection)
                {
                    ulong entryguid = project.AddEntry(gui.GuiNewEntry.Text,
                        col.GetItem().guid);

                    //Adds all fields to the entry.
                    var template = project.GetCollectionTemplate(col.GetItem());
                    var templateCols = project.GetTemplateColumns(template);
                    for (int i = 0; i < templateCols.Count; i++)
                    {
                        var templateColFields = project.GetTemplateColumnFields(templateCols[i]);

                        for (int j = 0; j < templateColFields.Count; j++)
                        {
                            var dataType = (TemplateFieldType)((int)templateColFields[j].GetData("dataType"));

                            //Adds default values matching the represented data.
                            if (dataType == TemplateFieldType.Text ||
                                dataType == TemplateFieldType.Min_Formula ||
                                dataType == TemplateFieldType.Min_Name ||
                                dataType == TemplateFieldType.Min_Group ||
                                dataType == TemplateFieldType.Min_Locality ||
                                dataType == TemplateFieldType.Hyperlink ||
                                dataType == TemplateFieldType.EntryImages ||
                                dataType == TemplateFieldType.Images)
                            {
                                project.AddField(entryguid, templateColFields[j].guid, String.Empty);
                            }
                            else if (dataType == TemplateFieldType.MoneyUSD)
                            {
                                project.AddField(entryguid, templateColFields[j].guid, new string[2]);
                            }
                            else if (dataType == TemplateFieldType.EntryImages)
                            {
                                //TODO: Handle the generation of empty image data in fields.
                            }
                        }
                    }

                    //Automatically adds the entry to the 'all' group.
                    List<DataItem> grps = project.GetCollectionGroupings(col.GetItem());
                    ulong allGrp = ulong.MaxValue;

                    //Finds the group named 'all' with the smallest
                    //guid since it will be the auto-added one.
                    if (grps != null && grps.Count > 0)
                    {
                        for (int i = 0; i < grps.Count; i++)
                        {
                            if ((string)grps[i].GetData("name") == "all")
                            {
                                if (grps[i].guid < allGrp)
                                {
                                    allGrp = grps[i].guid;
                                }
                            }
                        }
                    }

                    if (allGrp != ulong.MaxValue)
                    {
                        //Entries are added based on the selection, which
                        //auto-selects new items. Since two items in different
                        //areas are added, we need to remember where the
                        //second item should go.
                        TreeViewDataItem oldSel = selection;

                        //Entry references are created for the 'all'
                        //group and the selected group (if distinct).
                        project.AddGroupingEntryRef(allGrp, entryguid);

                        //If adding an entry with a grouping selected.
                        if (item.GetParent() == col &&
                            item.GetItem().type == DataItemTypes.Grouping &&
                            item.GetItem().guid != allGrp)
                        {
                            selection = oldSel;
                            project.AddGroupingEntryRef(
                                item.GetItem().guid, entryguid);
                        }

                        //If adding an entry with an entry reference selected.
                        else if (item.GetParent()?.GetParent() == col &&
                            item.GetParent().GetItem().type == DataItemTypes.Grouping &&
                            item.GetParent().GetItem().guid != allGrp)
                        {
                            selection = oldSel;
                            project.AddGroupingEntryRef(
                                item.GetParent().GetItem().guid, entryguid);
                        }
                    }
                }

                //Resets the text for the entry naming box.
                gui.GuiNewEntry.Text = String.Empty;
            }
        }

        /// <summary>
        /// Generates a new grouping object.
        /// </summary>
        private void GuiNewGrouping_KeyDown(object sender, KeyEventArgs e)
        {
            //When enter is pressed.
            if (e.Key == Key.Enter && e.IsDown)
            {
                e.Handled = true;

                if (!string.IsNullOrWhiteSpace(gui.GuiNewGrouping.Text))
                {
                    var item = (TreeViewDataItem)gui.GuiTreeView.SelectedItem;

                    //Traverses up to the collection.
                    if (item != null)
                    {
                        while (item.GetItem().type != DataItemTypes.Collection)
                        {
                            if (item == null)
                            {
                                break;
                            }

                            if (item.GetItem().type == DataItemTypes.Database)
                            {
                                item = null;
                                break;
                            }

                            item = item.GetParent();
                        }
                    }

                    if (item != null &&
                        item.GetItem().type == DataItemTypes.Collection)
                    {
                        project.AddGrouping(gui.GuiNewGrouping.Text,
                            item.GetItem().guid);
                    }
                }

                gui.GuiNewGrouping.Text = String.Empty;
            }
        }

        /// <summary>
        /// Generates a new collection object.
        /// </summary>
        private void GuiNewCollection_KeyDown(object sender, KeyEventArgs e)
        {
            //When enter is pressed.
            if (e.Key == Key.Enter && e.IsDown)
            {
                e.Handled = true;

                //If there are no templates, a collection cannot be made.
                if (project.GetItemsByType(DataItemTypes.Template).Count == 0)
                {
                    if (MessageBox.Show("A template must exist before you " +
                        "can create a collection. Would you like to create " +
                        "a template?", "Create template?",
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        GuiTemplateNew_Click(this, null);
                    }

                    gui.GuiNewCollection.Text = String.Empty;
                    return;
                }

                //If there is only one template, no prompt is needed.
                if (project.GetItemsByType(DataItemTypes.Template).Count == 1)
                {
                    if (!string.IsNullOrWhiteSpace(gui.GuiNewCollection.Text))
                    {
                        ulong col = project.AddCollection(
                            gui.GuiNewCollection.Text,
                            string.Empty,
                            project.GetItemsByType(DataItemTypes.Template)[0].guid);

                        project.AddGrouping("all", col);
                    }

                    gui.GuiNewCollection.Text = String.Empty;
                    return;
                }

                //If there is more than one template, prompts to select one.
                DlgNewCollection dlg = new DlgNewCollection(
                project, gui.GuiNewCollection.Text);
                dlg.Show();

                gui.GuiNewCollection.Text = String.Empty;
            }
        }

        /// <summary>
        /// Open an about dialog.
        /// </summary>
        private void GuiHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            new DlgAboutGui().Show();
        }

        /// <summary>
        /// Sets keyboard shortcuts.
        /// </summary>
        private void _gui_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) ||
                e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
            {
                if (e.KeyboardDevice.IsKeyDown(Key.S))
                {
                    e.Handled = true;

                    //If Ctrl + Alt + S is pressed, save as.
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) ||
                        e.KeyboardDevice.IsKeyDown(Key.RightAlt))
                    {
                        GuiFileSaveAs_Click(null, null);
                    }

                    //If Ctrl + S is pressed, save.
                    else
                    {
                        GuiFileSave_Click(null, null);
                    }
                }

                //If Ctrl + O is pressed, open a file.
                if (e.KeyboardDevice.IsKeyDown(Key.O))
                {
                    e.Handled = true;
                    GuiFileOpen_Click(null, null);
                }

                //If Ctrl + N is pressed, start a new database.
                if (e.KeyboardDevice.IsKeyDown(Key.N))
                {
                    e.Handled = true;
                    GuiFileNew_Click(null, null);
                }

                //If Ctrl + P is pressed, print the current page.
                if (e.KeyboardDevice.IsKeyDown(Key.P))
                {
                    e.Handled = true;
                    Print();
                }
            }
        }

        /// <summary>
        /// Prompts the user to save their file before closing the program.
        /// </summary>
        private void _gui_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Prompts to close.
            MessageBoxResult result = MessageBox.Show(
                "Close and discard any unsaved changes?",
                "Confirm closing the program",
                MessageBoxButton.YesNo);

            //Cancels closing if "no" is chosen.
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Saves the existing database and always specifies the file.
        /// </summary>
        private void GuiFileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            //Configures a save file dialog.
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.CheckPathExists = true;
            dlg.DefaultExt = ".mdat";
            dlg.Filter = "databases|*.mdat|all files|*.*";
            dlg.Title = "Save database";

            //Uses the old save directory if one exists.
            if (File.Exists(saveUrl))
            {
                dlg.InitialDirectory = Path.GetDirectoryName(saveUrl);
            }

            //If selected, saved the project data.
            if (dlg.ShowDialog() == true)
            {
                SaveUrl = dlg.FileName;
                project.Save(saveUrl);
            };
        }

        /// <summary>
        /// Saves the existing database.
        /// </summary>
        private void GuiFileSave_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(saveUrl))
            {
                project.Save(saveUrl);
            }
            else
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.CheckPathExists = true;
                dlg.DefaultExt = ".mdat";
                dlg.Filter = "databases|*.mdat|all files|*.*";
                dlg.Title = "Save database";

                if (dlg.ShowDialog() == true)
                {
                    SaveUrl = dlg.FileName;
                    project.Save(saveUrl);
                }
            }
        }

        /// <summary>
        /// Opens an existing database.
        /// </summary>
        private void GuiFileOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckPathExists = true;
            dlg.Filter = "databases|*.mdat|all files|*.*";
            dlg.Title = "Load database";

            if (dlg.ShowDialog() == true)
            {
                Project tempProj = Project.Load(dlg.FileName);
                if (tempProj != null)
                {
                    SaveUrl = dlg.FileName;

                    //Sets the project and resets bindings.
                    project = tempProj;
                    project.Items.CollectionChanged += ChangeTreeview;

                    //Reconstructs the treeview and expands it.
                    ConstructVisuals();
                }
            }
        }

        /// <summary>
        /// Creates a new database.
        /// </summary>
        private void GuiFileNew_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                    "Create a new database, discarding any unsaved changes?",
                    "New database",
                    MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                project.Reset();

                InitializeDefaultProject();
                saveUrl = String.Empty;
            }
        }

        /// <summary>
        /// Enables deleting items except the database and auto-generated
        /// groups named "all".
        /// </summary>
        private void GuiTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            //When delete is pressed.
            if (gui.GuiTreeView.SelectedItem != null &&
                e.Key == Key.Delete && e.IsDown)
            {
                var selItem = (TreeViewDataItem)gui.GuiTreeView.SelectedItem;

                //If the selection is null, it cannot be deleted.
                if (selItem == null || selItem.GetItem() == null)
                {
                    return;
                }

                //The database cannot be deleted.
                else if (selItem.GetItem().type == DataItemTypes.Database)
                {
                    return;
                }

                //The all group cannot be deleted.
                else if (selItem.GetItem().type == DataItemTypes.Grouping &&
                    (string)selItem.GetItem().GetData("name") == "all" &&
                    GetAutoAddedGroup(selItem) == selItem.GetItem().guid)
                {
                    return;
                }

                //Deleting an item in the auto-added "all" group deletes it everywhere.
                else if (selItem.GetItem().type == DataItemTypes.GroupingEntryRef &&
                    (string)selItem.GetParent().GetItem().GetData("name") == "all" &&
                    GetAutoAddedGroup(selItem) == selItem.GetParent().GetItem().guid)
                {
                    var result = MessageBox.Show("Deleting this entry will " +
                        "delete it everywhere. Continue?", "Confirm deletion",
                        MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        //Gets the entry and all its entry references.
                        var delEntry = project.GetEntryRefEntry(selItem.GetItem());
                        var delEntryRefs = project.GetEntryEntryRefs(delEntry);

                        //Deletes the entry.
                        project.DeleteItem(delEntry);

                        //Deletes all entry references.
                        for (int i = 0; i < delEntryRefs.Count; i++)
                        {
                            project.DeleteItem(delEntryRefs[i]);
                        }
                    }
                }

                //Deletes other treeview item types.
                else
                {
                    if (selItem.GetItem().type == DataItemTypes.Collection)
                    {
                        //Deletes all groups and their entry references.
                        var childGrps = project.GetCollectionGroupings(selItem.GetItem());
                        for (int i = 0; i < childGrps.Count; i++)
                        {
                            //Deletes all entry references per group.
                            var grpEntryRefs = project.GetGroupingEntryRefs(childGrps[i]);
                            for (int j = 0; j < grpEntryRefs.Count; j++)
                            {
                                project.DeleteItem(grpEntryRefs[i]);
                            }

                            project.DeleteItem(childGrps[i]);
                        }

                        //Deletes all entries.
                        var childEnts = project.GetCollectionEntries(selItem.GetItem());
                        for (int i = 0; i < childEnts.Count; i++)
                        {
                            //Deletes all entry fields per entry.
                            var entryFields = project.GetEntryFields(childEnts[i]);
                            for (int j = 0; j < entryFields.Count; j++)
                            {
                                project.DeleteItem(entryFields[i]);
                            }

                            project.DeleteItem(childEnts[i]);
                        }
                    }
                    else if (selItem.GetItem().type == DataItemTypes.Grouping)
                    {
                        //Gets the grouping entry references and entry references
                        //of the entries, which might be more encompassing.
                        var entries = project.GetGroupingEntries(selItem.GetItem());

                        //Iterates through each entry to get its references.
                        for (int i = 0; i < entries.Count; i++)
                        {
                            //Delete entries with fields whose only ref is in this group.
                            if (project.GetEntryEntryRefs(entries[i]).Count == 1)
                            {
                                var fields = project.GetEntryFields(entries[i]);

                                for (int j = 0; j < fields.Count; j++)
                                {
                                    project.DeleteItem(fields[j]);
                                }

                                project.DeleteItem(entries[i]);
                            }
                        }
                    }

                    project.DeleteItem(selItem.GetItem());
                }
            }
        }

        /// <summary>
        /// Displays items selected in the treeview. Sets the visibility of
        /// search fields and new item textboxes.
        /// </summary>
        private void GuiTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = (TreeViewDataItem)gui.GuiTreeView.SelectedItem;

            if (item == null || item.GetItem() == null)
            {
                return;
            }

            //Tracks the selected item.
            selection = item;

            //Resets all filters.
            treeviewFilterText = String.Empty;
            treeviewFilterField = null;

            //No fields searchable from the database itself.
            if (item.GetItem().type == DataItemTypes.Database)
            {
                gui.GuiNewCollection.Visibility = Visibility.Visible;
                gui.GuiNewEntry.Visibility = Visibility.Collapsed;
                gui.GuiNewGrouping.Visibility = Visibility.Collapsed;
                gui.GuiNewCollection.IsEnabled = true;
                gui.GuiNewEntry.IsEnabled = false;
                gui.GuiNewGrouping.IsEnabled = false;
                gui.GuiSearchField.IsEnabled = false;
                gui.GuiTreeViewSearch.IsEnabled = false;
            }

            //Fields are searchable. Populates fields via selected item.
            else if (item.GetItem().type == DataItemTypes.Collection ||
                item.GetItem().type == DataItemTypes.Grouping ||
                item.GetItem().type == DataItemTypes.GroupingEntryRef)
            {
                //Determines items eligible for creation.
                gui.GuiNewEntry.Visibility = Visibility.Visible;
                gui.GuiNewCollection.Visibility = Visibility.Collapsed;
                gui.GuiNewEntry.IsEnabled = true;
                gui.GuiSearchField.IsEnabled = true;
                gui.GuiNewCollection.IsEnabled = false;
                gui.GuiTreeViewSearch.IsEnabled = true;

                if (item.GetItem().type == DataItemTypes.GroupingEntryRef)
                {
                    gui.GuiNewGrouping.Visibility = Visibility.Collapsed;
                    gui.GuiNewGrouping.IsEnabled = false;
                }
                else
                {
                    gui.GuiNewGrouping.Visibility = Visibility.Visible;
                    gui.GuiNewGrouping.IsEnabled = true;
                }

                //Populates the textual fields that can be used to sort.
                List<DataItem> cols = new List<DataItem>();
                List<DataItem> items = new List<DataItem>();

                if (item.GetItem().type == DataItemTypes.Collection)
                {
                    //Gets all fields of all columns.
                    cols = project.GetTemplateColumns(
                        project.GetCollectionTemplate(item.GetItem()));
                }

                else if (item.GetItem().type == DataItemTypes.Grouping)
                {
                    cols = project.GetTemplateColumns(
                        project.GetCollectionTemplate(
                        project.GetGroupingCollection(item.GetItem())));
                }

                else if (item.GetItem().type == DataItemTypes.GroupingEntryRef)
                {
                    cols = project.GetTemplateColumns(
                        project.GetCollectionTemplate(
                        project.GetEntryCollection(
                        project.GetEntryRefEntry(item.GetItem()))));
                }

                for (int i = 0; i < cols.Count; i++)
                {
                    items.AddRange(project.GetTemplateColumnFields(cols[i]));
                }

                //Clears filter field options before recreating them.
                gui.GuiSearchField.Items.Clear();

                //Adds an item to search by name.
                CmbxDataItem defaultItem = new CmbxDataItem(null);
                defaultItem.Content = "Name";
                defaultItem.MouseDown += (c, d) =>
                {
                    treeviewFilterField = null;
                };
                gui.GuiSearchField.Items.Add(defaultItem);
                defaultItem.IsSelected = true;


                //Adds each other non-image template field for searching.
                for (int i = 0; i < items.Count; i++)
                {
                    var fieldType = (TemplateFieldType)(int)items[i].GetData("dataType");

                    //Does not allow filtering by image types.
                    if (fieldType == TemplateFieldType.Images ||
                        fieldType == TemplateFieldType.EntryImages)
                    {
                        continue;
                    }

                    //Creates the combobox item.
                    CmbxDataItem comboItem = new CmbxDataItem(items[i]);

                    //Sets the filter field on click.
                    comboItem.Selected += (c, d) =>
                    {
                        treeviewFilterField = comboItem.GetItem();
                    };

                    //Adds the item.
                    gui.GuiSearchField.Items.Add(comboItem);
                }
            }

            SetPage();
        }

        /// <summary>
        /// Filters visibility for items of the active collection. Resets all
        /// nodes to be visible.
        /// </summary>
        private void RefreshTreeviewFilter(object sender, TextChangedEventArgs e)
        {
            //FIXME: Prevents focus from resetting to the database and
            //disabling the search textbox. Filter all collections when
            //database is selected instead, then don't disable event.
            gui.GuiTreeView.SelectedItemChanged -= GuiTreeView_SelectedItemChanged;

            treeviewFilterText = gui.GuiTreeViewSearch.Text;
            ConstructVisuals();

            gui.GuiTreeView.SelectedItemChanged += GuiTreeView_SelectedItemChanged;
        }

        /// <summary>
        /// Updates the treeview when the underlying data changes.
        /// Adds/removes from the treeview based on the data.
        /// </summary>
        private void ChangeTreeview(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            //Handles adding items to the treeview.
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    var item = AddToTreeview((DataItem)e.NewItems[i]);

                    //Addded items are usually under the selected item.
                    if (selection.HasItems)
                    {
                        selection.ExpandSubtree();
                    }

                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                }
            }

            //Handles removing items from the treeview.
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                //Do nothing to the tree view.
                for (int i = 0; i < e.OldItems.Count; i++)
                {
                    //Gets the item and skips if null.
                    var item = (DataItem)e.OldItems[i];

                    if (item == null)
                    {
                        continue;
                    }

                    //Does not update the treeview for these.
                    if (item.type == DataItemTypes.EntryField ||
                        item.type == DataItemTypes.Entry ||
                        item.type == DataItemTypes.Template ||
                        item.type == DataItemTypes.TemplateColumnData ||
                        item.type == DataItemTypes.TemplateField)
                    {
                        return;
                    }

                    //Gets the corresponding treeview item.
                    var treeItem = ((TreeViewDataItem)gui.GuiTreeView.Items[0])
                        .Find((a) => { return a.GetItem().guid == item.guid; });

                    //Refreshes the entire treeview if there's a
                    //bad link between dataitem and treeview for any reason.
                    if (treeItem == null)
                    {
                        ConstructVisuals();
                        return;
                    }

                    //The item is removed directly.
                    if (treeItem.GetParent() == null)
                    {
                        gui.GuiTreeView.Items.Remove(treeItem);
                    }

                    //The parent removes a reference to the item.
                    else
                    {
                        treeItem.GetParent().Items.Remove(treeItem);
                    }
                }
            }

            //Handles resetting items in the treeview.
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                ConstructVisuals();
            }
        }

        /// <summary>
        /// Adds visual information based on project data. Returns the
        /// new item.
        /// TODO: Make an ext method for finding TreeViews by dataitem,
        /// and clean up all this code!
        /// </summary>
        private TreeViewDataItem AddToTreeview(DataItem item)
        {
            TreeViewDataItem newItem = null;

            //Adds databases.
            if (selection == null || item.type == DataItemTypes.Database)
            {
                ConstructVisuals();
                return null;
            }

            //Adds collections.
            if (selection.GetItem().type == DataItemTypes.Database &&
                item.type == DataItemTypes.Collection)
            {
                newItem = new TreeViewDataItem(item);
                newItem.SetParent(selection);
                selection.Items.Add(newItem);
            }

            //Adds groupings to collections added to the database with it selected.
            else if (selection.GetItem().type == DataItemTypes.Database &&
                item.type == DataItemTypes.Grouping)
            {
                DataItem col = project.GetGroupingCollection(item);
                for (int i = 0; i < selection.Items.Count; i++)
                {
                    //If the group's collection matches this treeview item.
                    if (((TreeViewDataItem)selection.Items[i])
                        .GetItem().Equals(col))
                    {
                        newItem = new TreeViewDataItem(item);
                        newItem.SetParent(((TreeViewDataItem)selection.Items[i]));

                        ((TreeViewDataItem)selection.Items[i]).Items.Add(newItem);
                    }
                }
            }

            //Adds groupings to collections added to the database with
            //the collection selected.
            else if (selection.GetItem().type == DataItemTypes.Collection &&
                item.type == DataItemTypes.Grouping)
            {
                newItem = new TreeViewDataItem(item);
                newItem.SetParent(selection);
                selection.Items.Add(newItem);
            }

            //Adds entries to a collection with the collection or database selected.
            else if (item.type == DataItemTypes.GroupingEntryRef &&
                (selection.GetItem().type == DataItemTypes.Database ||
                selection.GetItem().type == DataItemTypes.Collection))
            {
                //Gets the groupings of the collection that indirectly
                //contains the entry reference.
                List<DataItem> grps =
                    project.GetCollectionGroupings(
                    project.GetGroupingCollection(
                    project.GetEntryRefGrouping(item)));

                //Finds the first group called 'all' (has the smallest guid).
                ulong allGrpId = ulong.MaxValue;

                if (grps != null && grps.Count > 0)
                {
                    for (int i = 0; i < grps.Count; i++)
                    {
                        if ((string)grps[i].GetData("name") == "all")
                        {
                            if (grps[i].guid < allGrpId)
                            {
                                allGrpId = grps[i].guid;
                            }
                        }
                    }
                }

                //Gets the 'all' group and in the collection treeview, finds
                //the corresponding child treeviewitem of the 'all' group
                //dataitem.
                DataItem allGrpItem = project.GetItemByGuid(allGrpId);
                if (selection.GetItem().type == DataItemTypes.Database)
                {
                    for (int i = 0; i < selection.Items.Count; i++)
                    {
                        //Finds the collection containing the 'all' group.
                        if (((TreeViewDataItem)selection.Items[i]).GetItem()
                            .Equals(project.GetGroupingCollection(allGrpItem)))
                        {
                            //Finds which group is the 'all' group.
                            for (int j = 0; j < selection.Items.Count; j++)
                            {
                                //The treeview dataitem matches the 'all' group dataitem.
                                if (((TreeViewDataItem)((
                                    (TreeViewDataItem)selection.Items[i]).Items[j]))
                                    .GetItem().Equals(allGrpItem))
                                {
                                    newItem = new TreeViewDataItem(item);
                                    newItem.Header = project.GetEntryRefEntry(item).GetData("name");
                                    newItem.SetParent(((TreeViewDataItem)((
                                    (TreeViewDataItem)selection.Items[i]).Items[j])));

                                    //Adds to the treeview.
                                    ((TreeViewDataItem)((
                                    (TreeViewDataItem)selection.Items[i]).Items[j]))
                                    .Items.Add(newItem);
                                }
                            }
                        }
                    }
                }
                else if (selection.GetItem().type == DataItemTypes.Collection)
                {
                    for (int i = 0; i < selection.Items.Count; i++)
                    {
                        //The treeview dataitem matches the 'all' group dataitem.
                        if (((TreeViewDataItem)selection.Items[i]).GetItem()
                            .Equals(allGrpItem))
                        {
                            newItem = new TreeViewDataItem(item);
                            newItem.Header = project.GetEntryRefEntry(item).GetData("name");
                            newItem.SetParent(((TreeViewDataItem)selection.Items[i]));

                            //Adds to the treeview.
                            ((TreeViewDataItem)selection.Items[i]).Items.Add(newItem);
                        }
                    }
                }
            }

            //Adds groups or entries with a group selected.
            else if (selection.GetItem().type ==
                DataItemTypes.Grouping &&
                (item.type == DataItemTypes.Grouping ||
                item.type == DataItemTypes.GroupingEntryRef))
            {
                if (item.type == DataItemTypes.Grouping)
                {
                    newItem = new TreeViewDataItem(item);
                    newItem.SetParent(selection.GetParent());
                    selection.GetParent().Items.Add(newItem);
                }
                else if (item.type == DataItemTypes.GroupingEntryRef)
                {
                    newItem = new TreeViewDataItem(item);
                    newItem.Header = project.GetEntryRefEntry(item).GetData("name");

                    var grpToAddTo = ((TreeViewDataItem)gui.GuiTreeView.Items[0])
                        .GetContainer(project.GetItemByGuid((ulong)item.GetData("refGuid")));

                    newItem.SetParent(grpToAddTo);
                    grpToAddTo.Items.Add(newItem);
                }
            }

            //Adds entries with an entry selected.
            else if (selection.GetItem().type == DataItemTypes.GroupingEntryRef &&
                item.type == DataItemTypes.GroupingEntryRef)
            {
                newItem = new TreeViewDataItem(item);
                newItem.Header = project.GetEntryRefEntry(item).GetData("name");

                var grpToAddTo = ((TreeViewDataItem)gui.GuiTreeView.Items[0])
                    .GetContainer(project.GetItemByGuid((ulong)item.GetData("refGuid")));

                newItem.SetParent(grpToAddTo);
                grpToAddTo.Items.Add(newItem);
            }

            return newItem;
        }

        /// <summary>
        /// Constructs visual information based on project data.
        /// </summary>
        private void ConstructVisuals()
        {
            //Acquires the filter collection dataitem.
            TreeViewDataItem treeviewFilterItem = selection;
            while (treeviewFilterItem != null &&
                treeviewFilterItem.GetItem() != null &&
                treeviewFilterItem.GetItem().type !=
                DataItemTypes.Collection)
            {
                treeviewFilterItem =
                    treeviewFilterItem.GetParent();
            }

            //Erases old data.
            gui.GuiTreeView.Items.Clear();
            gui.GuiContent.Content = null;
            gui.GuiContent.Background = Brushes.Transparent;

            //The first item is a database.
            TreeViewDataItem dat = null;
            if (project.GetDatabase() != null)
            {
                dat = new TreeViewDataItem(project.GetDatabase());

                //Determines whether edit mode is enabled or ont.
                if (project.GetDatabase().GetData("defUseEditMode") is bool &&
                    (bool)project.GetDatabase().GetData("defUseEditMode"))
                {
                    isEditing = true;
                }
                else
                {
                    isEditing = false;
                }

                //Sets the image for the toggle icon.
                GuiToggleMode_MouseLeave(this, null);
            }

            //Iterates through each collection and adds it to the database.
            List<DataItem> collections = project.GetItemsByType(DataItemTypes.Collection);
            for (int i = 0; i < collections.Count; i++)
            {
                var col = new TreeViewDataItem(collections[i]);
                col.SetParent(dat);

                //Iterates through each grouping and adds it to the collection.
                List<DataItem> groupings = project.GetCollectionGroupings(collections[i]);
                for (int j = 0; j < groupings.Count; j++)
                {
                    var grp = new TreeViewDataItem(groupings[j]);
                    grp.SetParent(col);

                    //Iterates through each entry ref and adds it to the grouping.
                    List<DataItem> grpRefs = project.GetGroupingEntryRefs(groupings[j]);
                    for (int k = 0; k < grpRefs.Count; k++)
                    {
                        var entryRef = new TreeViewDataItem(grpRefs[k]);
                        entryRef.Header = project.GetEntryRefEntry(grpRefs[k]).GetData("name");
                        entryRef.SetParent(grp);

                        //Filters entries by their field values if applicable.
                        if (treeviewFilterField != null &&
                            !string.IsNullOrEmpty(treeviewFilterText) &&
                            col.GetItem().guid == treeviewFilterItem?.GetItem()?.guid)
                        {
                            //Gets the entry from the reference and the field
                            //that matches the filter field.
                            var entry = project.GetEntryRefEntry(grpRefs[k]);
                            var fields = project.GetEntryFields(entry);
                            var field = fields.FirstOrDefault((item) =>
                            {
                                return project.GetFieldTemplateField(item) ==
                                    treeviewFilterField;
                            });

                            //Gets the type of data to search.
                            object fieldData = field.GetData("data");
                            var templateType = (TemplateFieldType)(int)treeviewFilterField.GetData("dataType");
                            if (templateType == TemplateFieldType.Text ||
                                templateType == TemplateFieldType.Min_Formula ||
                                templateType == TemplateFieldType.Min_Name ||
                                templateType == TemplateFieldType.Min_Group ||
                                templateType == TemplateFieldType.Min_Locality ||
                                templateType == TemplateFieldType.Hyperlink)
                            {
                                if (((string)fieldData).ToLower()
                                    .Contains(treeviewFilterText.ToLower()))
                                {
                                    grp.Items.Add(entryRef);
                                }
                            }
                            else if (templateType == TemplateFieldType.MoneyUSD)
                            {
                                var moneyText = "$" + string.Join(".", fieldData);
                                if (moneyText.ToLower()
                                    .Contains(treeviewFilterText.ToLower()))
                                {
                                    grp.Items.Add(entryRef);
                                }
                            }
                        }
                        else if (treeviewFilterField == null &&
                            !string.IsNullOrEmpty(treeviewFilterText) &&
                            col.GetItem().guid == treeviewFilterItem?.GetItem()?.guid)
                        {
                            var entry = project.GetEntryRefEntry(grpRefs[k]);

                            if (((string)entry.GetData("name")).ToLower()
                                .Contains(treeviewFilterText.ToLower()))
                            {
                                grp.Items.Add(entryRef);
                            }
                        }
                        else
                        {
                            grp.Items.Add(entryRef);
                        }
                    }

                    col.Items.Add(grp);
                }

                dat.Items.Add(col);
            }

            //Sets the treeview.
            if (dat != null)
            {
                dat.IsSelected = true;
                gui.GuiTreeView.Items.Add(dat);
            }

            #region Populates templates
            //Removes all but the "New" entry for templates.
            object firstTemplateItem = gui.GuiMenuTemplates.Items[0];
            gui.GuiMenuTemplates.Items.Clear();
            gui.GuiMenuTemplates.Items.Add(firstTemplateItem);

            List<DataItem> templates = project.GetItemsByType(DataItemTypes.Template);
            if (templates.Count > 0)
            {
                for (int i = 0; i < templates.Count; i++)
                {
                    var itemTemplate = templates[i]; //Captured for lambda.
                    var item = new MenuDataItem(itemTemplate);
                    gui.GuiMenuTemplates.Items.Add(item);
                    item.Click += new RoutedEventHandler((a, b) =>
                    {
                        //Opens the edit template dialog for it.
                        var dlg = new DlgEditTemplate(project, itemTemplate);

                        //Updates the template name to match.
                        dlg.DataNameChanged += new EventHandler((c, d) =>
                        {
                            item.Refresh();
                        });

                        //Updates the template list for a deleted template.
                        if (dlg.ShowDialog() == false &&
                            !project.Items.Contains(itemTemplate))
                        {
                            gui.GuiMenuTemplates.Items.Remove(item);
                        }

                        item.Refresh();

                        //Updates the gui if non-template data changes.
                        if (dlg.ReferencesInvalidated)
                        {
                            ConstructVisuals();
                            SetPage();
                        }
                    });
                }
            }
            #endregion

            //Expands the full tree.
            dat?.ExpandSubtree();
        }

        /// <summary>
        /// Returns the dataitem of the auto-added group for the given
        /// item, navigating up to the nearest collection. Returns
        /// ulong.MaxValue if not found.
        /// </summary>
        private ulong GetAutoAddedGroup(TreeViewDataItem item)
        {
            var col = item;

            //Traverses up to the collection.
            while (col.GetItem().type != DataItemTypes.Collection)
            {
                if (col.GetItem().type == DataItemTypes.Database)
                {
                    return ulong.MaxValue;
                }

                col = col?.GetParent();
            }

            List<DataItem> grps =
                project.GetCollectionGroupings(col.GetItem());

            //Finds the group named 'all' with the smallest
            //guid since it will be the auto-added one.
            ulong allGrp = ulong.MaxValue;

            if (grps != null && grps.Count > 0)
            {
                for (int i = 0; i < grps.Count; i++)
                {
                    if ((string)grps[i].GetData("name") == "all")
                    {
                        if (grps[i].guid < allGrp)
                        {
                            allGrp = grps[i].guid;
                        }
                    }
                }

                return allGrp;
            }

            return ulong.MaxValue;
        }

        /// <summary>
        /// Updates all treeview item headers to match their dataitems.
        /// </summary>
        private void RefreshTreeview()
        {
            for (int i = 0; i < gui.GuiTreeView.Items.Count; i++)
            {
                var dat = (TreeViewDataItem)gui.GuiTreeView.Items[i];
                dat.Refresh(project);

                for (int j = 0; j < dat.Items.Count; j++)
                {
                    var col = (TreeViewDataItem)dat.Items[j];
                    col.Refresh(project);

                    for (int k = 0; k < col.Items.Count; k++)
                    {
                        var grp = (TreeViewDataItem)col.Items[k];
                        grp.Refresh(project);

                        for (int m = 0; m < grp.Items.Count; m++)
                        {
                            var ent = (TreeViewDataItem)grp.Items[m];
                            ent.Refresh(project);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the content based on the currently selected item.
        /// </summary>
        private void SetPage()
        {
            DataItem selItem = selection.GetItem();
            gui.GuiContent.Content = null;

            #region Database Pages
            if (selItem.type == DataItemTypes.Database && isEditing)
            {
                PgDatabaseEdit page = new PgDatabaseEdit(project);

                if (!string.IsNullOrWhiteSpace(page.BgImage))
                {
                    ImageBrush br = new ImageBrush(new BitmapImage(
                            new Uri(page.BgImage, UriKind.Absolute)));
                    br.Stretch = Stretch.UniformToFill;

                    gui.GuiContent.Background = br;
                }

                //Ensures when the database name is changed, it's updated
                //instantly in the treeview.
                page.DataNameChanged += new EventHandler((a, b) =>
                {
                    RefreshTreeview();
                });

                //Instantly sets the background image of the page when it
                //changes.
                page.BgImageChanged += new EventHandler((a, b) =>
                {
                    //Sets the background image.
                    if (!string.IsNullOrWhiteSpace(page.BgImage))
                    {
                        ImageBrush br = new ImageBrush(new BitmapImage(
                            new Uri(page.BgImage, UriKind.Absolute)));
                        br.Stretch = Stretch.UniformToFill;

                        gui.GuiContent.Background = br;
                    }
                    else
                    {
                        gui.GuiContent.Background = Brushes.Transparent;
                    }

                });

                gui.GuiContent.Content = page.Gui;
            }
            else if (selItem.type == DataItemTypes.Database)
            {
                PgDatabaseView page = new PgDatabaseView(project);

                if (!string.IsNullOrWhiteSpace(page.BgImage))
                {
                    ImageBrush br = new ImageBrush(new BitmapImage(
                            new Uri(page.BgImage, UriKind.Absolute)));
                    br.Stretch = Stretch.UniformToFill;

                    gui.GuiContent.Background = br;
                }

                //Handles changing the selected treeview item.
                page.SelectedItemChanged += new EventHandler((a, b) =>
                {
                    //Finds the item with the matching guid.
                    if (gui.GuiTreeView.Items.Count > 0 &&
                        gui.GuiTreeView.Items[0] != null)
                    {
                        var newItem = ((TreeViewDataItem)gui.GuiTreeView.Items[0])
                            .GetContainer(page.SelectedItem);

                        //Uses that item if not null.
                        if (newItem != null)
                        {
                            newItem.IsSelected = true;
                        }
                    }
                });

                gui.GuiContent.Content = page.Gui;
            }
            #endregion

            #region Collection Pages
            else if (selItem.type == DataItemTypes.Collection && isEditing)
            {
                PgCollectionEdit page = new PgCollectionEdit(project, selItem);

                //Sets the background of the collection page.
                if (!string.IsNullOrWhiteSpace((string)project
                    .GetDatabase().GetData("imageUrl")))
                {
                    ImageBrush br = new ImageBrush(new BitmapImage(
                            new Uri((string)project.GetDatabase().GetData("imageUrl"),
                                UriKind.Absolute)));
                    br.Stretch = Stretch.UniformToFill;

                    gui.GuiContent.Background = br;
                }

                //Ensures when the database name is changed, it's updated
                //instantly in the treeview.
                page.DataNameChanged += new EventHandler((a, b) =>
                {
                    RefreshTreeview();
                });

                gui.GuiContent.Content = page.Gui;
            }
            else if (selItem.type == DataItemTypes.Collection)
            {
                PgCollectionView page = new PgCollectionView(project, selItem);

                if (!string.IsNullOrWhiteSpace(page.BgImage))
                {
                    ImageBrush br = new ImageBrush(new BitmapImage(
                            new Uri(page.BgImage, UriKind.Absolute)));
                    br.Stretch = Stretch.UniformToFill;

                    gui.GuiContent.Background = br;
                }

                //Handles changing the selected treeview item.
                page.SelectedItemChanged += new EventHandler((a, b) =>
                {
                    //Finds the item with the matching guid.
                    if (gui.GuiTreeView.Items.Count > 0 &&
                        gui.GuiTreeView.Items[0] != null)
                    {
                        var newItem = ((TreeViewDataItem)gui.GuiTreeView.Items[0])
                            .GetContainer(page.SelectedItem);

                        //Uses that item if not null.
                        if (newItem != null)
                        {
                            newItem.IsSelected = true;
                        }
                    }
                });

                gui.GuiContent.Content = page.Gui;
            }
            #endregion

            #region Grouping Pages
            else if (selItem.type == DataItemTypes.Grouping && isEditing)
            {
                //If the group is the 'all' group, it can't be edited.
                DataItem col = project.GetGroupingCollection(selItem);
                List<DataItem> groupings = project.GetCollectionGroupings(col);
                if (groupings.Count > 0 && selItem.guid == groupings.First().guid)
                {
                    //Creates a message explaining the group can't be edited.
                    TextBlock emptyMessage = new TextBlock();
                    emptyMessage.Text = "This group is auto-generated " +
                        "and can't be edited.";
                    emptyMessage.HorizontalAlignment = HorizontalAlignment.Center;
                    emptyMessage.VerticalAlignment = VerticalAlignment.Center;
                    emptyMessage.Foreground = Brushes.LightGray;

                    gui.GuiContent.Content = emptyMessage;
                    return;
                }

                PgGroupingEdit page = new PgGroupingEdit(project, selItem);

                //Adding an element requires refocusing the page.
                page.EntryIncluded += (a, b) =>
                {
                    selection.GetParent().Focus();
                };

                //Sets the background of the grouping page.
                if (!string.IsNullOrWhiteSpace((string)project
                    .GetDatabase().GetData("imageUrl")))
                {
                    ImageBrush br = new ImageBrush(new BitmapImage(
                            new Uri((string)project.GetDatabase().GetData("imageUrl"),
                                UriKind.Absolute)));
                    br.Stretch = Stretch.UniformToFill;

                    gui.GuiContent.Background = br;
                }

                //Ensures when the database name is changed, it's updated
                //instantly in the treeview.
                page.DataNameChanged += new EventHandler((a, b) =>
                {
                    RefreshTreeview();
                });

                gui.GuiContent.Content = page.Gui;
            }
            else if (selItem.type == DataItemTypes.Grouping)
            {
                PgGroupingView page = new PgGroupingView(project, selItem);

                if (!string.IsNullOrWhiteSpace(page.BgImage))
                {
                    ImageBrush br = new ImageBrush(new BitmapImage(
                            new Uri(page.BgImage, UriKind.Absolute)));
                    br.Stretch = Stretch.UniformToFill;

                    gui.GuiContent.Background = br;
                }

                //Handles changing the selected treeview item.
                page.SelectedItemChanged += new EventHandler((a, b) =>
                {
                    //Finds the item with the matching guid.
                    if (gui.GuiTreeView.Items.Count > 0 &&
                        gui.GuiTreeView.Items[0] != null)
                    {
                        var newItem = ((TreeViewDataItem)gui.GuiTreeView.Items[0])
                            .GetContainer(page.SelectedItem);

                        //Uses that item if not null.
                        if (newItem != null)
                        {
                            newItem.IsSelected = true;
                        }
                    }
                });

                gui.GuiContent.Content = page.Gui;
            }
            #endregion

            #region Entry Pages
            else if (selItem.type == DataItemTypes.GroupingEntryRef && !isEditing)
            {
                PgEntryView page = new PgEntryView(project, selItem, saveUrl);

                //Sets the background of the entry page.
                if (!string.IsNullOrWhiteSpace((string)project
                    .GetDatabase().GetData("imageUrl")))
                {
                    ImageBrush br = new ImageBrush(new BitmapImage(
                            new Uri((string)project.GetDatabase().GetData("imageUrl"),
                                UriKind.Absolute)));
                    br.Stretch = Stretch.UniformToFill;

                    gui.GuiContent.Background = br;
                }

                gui.GuiContent.Content = page.Gui;
            }
            else if (selItem.type == DataItemTypes.GroupingEntryRef && isEditing)
            {
                PgEntryEdit page = new PgEntryEdit(project, selItem, saveUrl);

                //Sets the background of the entry page.
                if (!string.IsNullOrWhiteSpace((string)project
                    .GetDatabase().GetData("imageUrl")))
                {
                    ImageBrush br = new ImageBrush(new BitmapImage(
                            new Uri((string)project.GetDatabase().GetData("imageUrl"),
                                UriKind.Absolute)));
                    br.Stretch = Stretch.UniformToFill;

                    gui.GuiContent.Background = br;
                }

                //Ensures when the entry name is changed, it's updated
                //instantly in the treeview.
                page.DataNameChanged += new EventHandler((a, b) =>
                {
                    RefreshTreeview();
                });

                //Images don't refresh dynamically, so rebuild the page.
                //TODO: Stop tearing down the house to kill a spider...
                page.InvalidatePage += new EventHandler((a, b) =>
                {
                    SetPage();
                    return;
                });

                gui.GuiContent.Content = page.Gui;
            }
            #endregion
        }

        /// <summary>
        /// Updates the gui and recent files in response to the save location.
        /// </summary>
        private void UpdateRecentFiles()
        {
            //Adds the url.
            if (saveUrl != String.Empty && saveUrl != null)
            {
                Utils.RegAddRecentlyOpen(saveUrl);
            }

            var urls = Utils.GetRecentlyOpened().Split('|');
            gui.GuiFileRecent.Items.Clear();

            //Shows the url only if recent files are recorded.
            if (urls.Length == 0 || (urls.Length == 1 && urls[0] == String.Empty))
            {
                gui.GuiFileRecent.Visibility = Visibility.Collapsed;
                gui.GuiFileRecent.IsEnabled = false;
            }
            else
            {
                gui.GuiFileRecent.Visibility = Visibility.Visible;
                gui.GuiFileRecent.IsEnabled = true;
            }

            //Adds each recent file.
            for (int i = 0; i < urls.Length; i++)
            {
                MenuItem item = new MenuItem();
                item.Header = Path.GetFileName(urls[i]);
                item.Tag = urls[i];
                item.ToolTip = urls[i];

                //Loads from the recent URL if possible.
                item.Click += (a, b) =>
                {
                    string url = (string)item.Tag;
                    if (File.Exists(url))
                    {
                        Project tempProj = Project.Load(url);
                        if (tempProj != null)
                        {
                            SaveUrl = url;

                            //Sets the project and resets bindings.
                            project = tempProj;
                            project.Items.CollectionChanged += ChangeTreeview;

                            //Reconstructs the treeview and expands it.
                            ConstructVisuals();
                        }
                        else
                        {
                            //Removes the file if it can't be found.
                            MessageBox.Show("The project at " + url +
                                " could not be loaded.");

                            Utils.RegRemoveRecentlyOpen(url);
                            gui.GuiFileRecent.Items.Remove(item);
                        }
                    }
                    else
                    {
                        //Removes the file if it can't be found.
                        MessageBox.Show("The project at " + url +
                            " could not be found.");

                        Utils.RegRemoveRecentlyOpen(url);
                        gui.GuiFileRecent.Items.Remove(item);
                    }
                };

                //Adds the option only if the file exists.
                if (File.Exists((string)item.Tag))
                {
                    gui.GuiFileRecent.Items.Add(item);
                }
            }

            //Hides the recent files if none were added.
            if (gui.GuiFileRecent.Items.Count == 0)
            {
                gui.GuiFileRecent.Visibility = Visibility.Collapsed;
            }
            else
            {
                gui.GuiFileRecent.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Shows a dialog to print the current page view.
        /// </summary>
        private void Print()
        {
            //Prompts the user with printing options.
            PrintDialog dlg = new PrintDialog();
            dlg.UserPageRangeEnabled = false;

            //Prints content area with overlying gui elements hidden.
            if (dlg.ShowDialog() == true)
            {
                bool wasEditing = isEditing;

                //Switches to view mode (editing becomes false).
                if (isEditing == true)
                {
                    GuiToggleMode_MouseDown(null, null);
                }

                //Hides surrounding elements.
                gui.GuiPrint.Visibility = Visibility.Hidden;
                gui.GuiToggleMode.Visibility = Visibility.Hidden;
                gui.GuiContent.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

                //Prints.
                VisualPrinter.PrintAcrossPages(dlg, gui.GuiContent);

                //Reveals surrounding elements.
                gui.GuiPrint.Visibility = Visibility.Visible;
                gui.GuiToggleMode.Visibility = Visibility.Visible;
                gui.GuiContent.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

                //Switches back to edit mode (editing becomes true).
                if (wasEditing != isEditing)
                {
                    GuiToggleMode_MouseDown(null, null);
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Shows the main display.
        /// </summary>
        public void Show()
        {
            gui.Show();
        }
        #endregion
    }
}