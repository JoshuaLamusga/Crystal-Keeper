using CrystalKeeper.Core;
using CrystalKeeper.GuiCore;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Represents the page associated with a grouping.
    /// </summary>
    class PgGroupingEdit
    {
        #region Members
        /// <summary>
        /// The database item associated with the page.
        /// </summary>
        private Project project;

        /// <summary>
        /// The grouping to be used.
        /// </summary>
        private DataItem grouping;

        /// <summary>
        /// The underlying gui.
        /// </summary>
        private PgGroupingGuiEdit gui;

        /// <summary>
        /// Stores the currently active field.
        /// </summary>
        private LstbxDataItem activeEntry;

        /// <summary>
        /// Fires when project data is changed.
        /// </summary>
        public event EventHandler DataNameChanged;

        /// <summary>
        /// Fires when an entry is included in the group.
        /// </summary>
        public event EventHandler EntryIncluded;
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
        public PgGroupingGuiEdit Gui
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
        /// The collection to be used.
        /// </summary>
        public DataItem Collection
        {
            set
            {
                grouping = value;
            }
            get
            {
                return grouping;
            }
        }

        /// <summary>
        /// Stores the currently active entry reference.
        /// </summary>
        private LstbxDataItem ActiveEntry
        {
            set
            {
                activeEntry = value;
            }
            get
            {
                return activeEntry;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a grouping page from the given project.
        /// </summary>
        /// <param name="project">
        /// The project given.
        /// </param>
        /// <param name="grouping">
        /// The grouping being edited.
        /// </param>
        public PgGroupingEdit(Project project, DataItem grouping)
        {
            this.project = project;
            this.grouping = grouping;
            activeEntry = null;
            ConstructPage();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the page content based on existing material.
        /// </summary>
        public void ConstructPage()
        {
            gui = new PgGroupingGuiEdit();

            #region Grouping name
            //Sets the grouping name.
            if (string.IsNullOrWhiteSpace((string)grouping.GetData("name")))
            {
                gui.TxtblkGroupingName.Text = "Untitled";
            }
            else
            {
                gui.TxtblkGroupingName.Text = (string)grouping.GetData("name");
            }

            //Handles changes to the grouping name.
            gui.TxtblkGroupingName.TextChanged += new TextChangedEventHandler((a, b) =>
            {
                if (!string.IsNullOrWhiteSpace(gui.TxtblkGroupingName.Text))
                {
                    grouping.SetData("name", gui.TxtblkGroupingName.Text);
                }

                //If the textbox is empty, it will keep the last character.
                gui.TxtblkGroupingName.Text = (string)grouping.GetData("name");

                DataNameChanged?.Invoke(this, null);
            });
            #endregion

            //All menu items are represented as entries for consistency.
            #region Menu Item Columns
            #region Menu item, move left/right
            var funcMenuItemMove = new Action(() =>
            {
                if (ActiveEntry == null)
                {
                    return;
                }

                //Moves the item in or out of the group (deletion/addition).
                if (gui.LstbxInGroup.Items.Contains(ActiveEntry))
                {
                    //Finds the entry reference for the current group that
                    //represents the entry, then removes the reference.
                    var refs = project.GetGroupingEntryRefs(grouping);
                    for (int i = 0; i < refs.Count; i++)
                    {
                        if ((ulong)refs[i].GetData("entryGuid") ==
                            ActiveEntry.GetItem().guid)
                        {
                            project.DeleteItemByGuid(refs[i].guid);
                        }
                    }

                    gui.LstbxInGroup.Items.Remove(ActiveEntry);
                    gui.LstbxOutGroup.Items.Add(ActiveEntry);
                }
                else
                {
                    project.AddGroupingEntryRef(
                        grouping.guid,
                        ActiveEntry.GetItem().guid);

                    gui.LstbxOutGroup.Items.Remove(ActiveEntry);
                    gui.LstbxInGroup.Items.Add(ActiveEntry);

                    EntryIncluded?.Invoke(this, null);
                }
            });
            #endregion

            #region Menuitem, selected
            var funcMenuItemSelected = new Action<LstbxDataItem>((newItem) =>
            {
                ActiveEntry = newItem;

                //Ensures only one item is selected at once.
                if (gui.LstbxInGroup.Items.Contains(ActiveEntry))
                {
                    gui.LstbxOutGroup.SelectedItem = null;
                }
                else
                {
                    gui.LstbxInGroup.SelectedItem = null;
                }
            });
            #endregion

            #region Populate menu items
            //Adds every menu item in its original order.
            var col = project.GetGroupingCollection(grouping);
            var entries = project.GetGroupingEntries(grouping);
            var colEntries = project.GetCollectionEntries(col);

            //Adds entries to their respective lists via group inclusiveness.
            for (int i = 0; i < colEntries.Count; i++)
            {
                var item = new LstbxDataItem(colEntries[i]);

                //Looks to see if this entry is in the group.
                var result = entries.Find(new Predicate<DataItem>((a) =>
                {
                    return a.guid.Equals(colEntries[i].guid);
                }));

                //Adds the entry based on whether it's in the group.
                if (result != null)
                {
                    gui.LstbxInGroup.Items.Add(item);
                }
                else
                {
                    gui.LstbxOutGroup.Items.Add(item);
                }

                //Handles item selection.
                item.Selected += new RoutedEventHandler((a, b) =>
                {
                    funcMenuItemSelected(item);
                });
            }
            #endregion

            #region Keyboard event handling
            gui.LstbxInGroup.KeyDown += new KeyEventHandler((a, b) =>
            {
                //Right key pressed: Move to 2nd column
                if (b.Key == Key.Right && b.IsDown && ActiveEntry != null)
                {
                    funcMenuItemMove();
                }
            });

            gui.LstbxOutGroup.KeyDown += new KeyEventHandler((a, b) =>
            {
                //Left key pressed: Move to 1st column
                if (b.Key == Key.Left && b.IsDown &&
                    gui.LstbxOutGroup.SelectedItem != null)
                {
                    funcMenuItemMove();
                }
                #endregion
            });

            #endregion

            #region Left arrow key pressed
            gui.BttnMoveLeft.MouseDown += new MouseButtonEventHandler((a, b) =>
            {
                if (gui.LstbxOutGroup.SelectedItem != null)
                {
                    funcMenuItemMove();
                }
            });
            #endregion

            #region Right arrow key pressed
            gui.BttnMoveRight.MouseDown += new MouseButtonEventHandler((a, b) =>
            {
                if (gui.LstbxInGroup.SelectedItem != null)
                {
                    funcMenuItemMove();
                }
            });
            #endregion
        }
        #endregion
    }
}