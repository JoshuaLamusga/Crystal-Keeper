using CrystalKeeper.Core;
using CrystalKeeper.GuiCore;
using System;
using System.Linq;
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

            #region Grouping conditions
            gui.BttnCondAdd.Click += BttnCondAdd_Click;
            gui.BttnCondApply.Click += BttnCondApply_Click;

            //Populates all grouping conditions.
            uint numConditions = (uint)grouping.GetData("numConditions");
            for (int i = 0; i < numConditions; i++)
            {
                var condType = (GroupingCondType)
                    grouping.GetData("conditionType" + i);

                switch (condType)
                {
                    case (GroupingCondType.ByLetter):
                        string condFromLetter = (string)
                            grouping.GetData("condAddFromLetter" + i);
                        string condToLetter = (string)
                            grouping.GetData("condAddToLetter" + i);

                        gui.GroupConditions.Children.Add(
                            AddConditionByRange(condFromLetter, condToLetter, (uint)i));
                        break;
                }
            }
            #endregion
        }

        /// <summary>
        /// Adds new matches amongst all entries not included in the group.
        /// </summary>
        private void BttnCondApply_Click(object sender, RoutedEventArgs e)
        {
            uint numConditions = (uint)grouping.GetData("numConditions");
            bool wasEntryAdded = false;

            //Iterates through each entry that's not in the group.
            var entries = project.GetCollectionEntries(
                project.GetGroupingCollection(grouping))
                .Except(project.GetGroupingEntries(grouping)).ToList();

            for (int i = 0; i < entries.Count; i++)
            {
                string name = ((string)entries[i].GetData("name")).ToLower();

                //Iterates through each condition.
                for (int j = 0; j < numConditions; j++)
                {
                    var condType = (GroupingCondType)
                    grouping.GetData("conditionType" + j);

                    if (condType == GroupingCondType.ByLetter)
                    {
                        //Gets condition data.
                        var condFromLetter = ((string)
                            grouping.GetData("condAddFromLetter" + j))
                            .ToLower();
                        var condToLetter = ((string)
                            grouping.GetData("condAddToLetter" + j))
                            .ToLower();

                        //Skips invalid conditions.
                        if (condFromLetter?.Length == 0 ||
                            condToLetter?.Length == 0)
                        {
                            continue;
                        }

                        //Adds if the character range matches.
                        if (condFromLetter.CompareTo(name) <= 0 &&
                            condToLetter.CompareTo(name) >= 0)
                        {
                            wasEntryAdded = true;

                            project.AddGroupingEntryRef(
                                grouping.guid,
                                entries[i].guid);

                            //Removes the first match from the out group.
                            for (int k = 0; k < gui.LstbxOutGroup.Items.Count; k++)
                            {
                                if (((LstbxDataItem)gui.LstbxOutGroup.Items[k]).GetItem().guid ==
                                    entries[i].guid)
                                {
                                    gui.LstbxOutGroup.Items.Remove(entries[i]);
                                    break;
                                }
                            }

                            //Creates a new item for the entry.
                            var item = new LstbxDataItem(entries[i]);
                            item.Selected += new RoutedEventHandler((a, b) =>
                            {
                                ActiveEntry = item;

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

                            //Adds to the in group.
                            gui.LstbxInGroup.Items.Add(item);                            

                            break;
                        }
                    }
                }
            }

            //Grouping can only regain focus after adding entries if
            //any were actually added, so this ensures grouping has focus.
            if (wasEntryAdded)
            {
                EntryIncluded?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Adds a new condition entry when clicked.
        /// </summary>
        private void BttnCondAdd_Click(object sender, RoutedEventArgs e)
        {
            uint condNum = (uint)grouping.GetData("numConditions");
            grouping.SetData("numConditions", condNum + 1);

            //Adds condition data.
            grouping.SetData("conditionType" + condNum, GroupingCondType.ByLetter);
            grouping.SetData("condAddFromLetter" + condNum, String.Empty);
            grouping.SetData("condAddToLetter" + condNum, String.Empty);

            gui.GroupConditions.Children.Add(
                AddConditionByRange(String.Empty, String.Empty,condNum));
        }

        /// <summary>
        /// Returns the wrap panel containing the gui for a condition.
        /// </summary>
        /// <param name="fromStr">
        /// The starting range that entries must compare greater or equal to.
        /// </param>
        /// <param name="toStr">
        /// The ending range that entries must compare less or equal to.
        /// </param>
        /// <param name="condNum">
        /// The index specifying which condition this is.
        /// </param>
        private WrapPanel AddConditionByRange(string fromStr, string toStr, uint condNum)
        {
            //Sets up the condition GUI.
            WrapPanel pnlCondition = new WrapPanel();

            TextBlock txtblkPhrase1 = new TextBlock();
            txtblkPhrase1.Margin = new Thickness(4);
            txtblkPhrase1.Text = "Add entries that start with " +
                "any of the letters from ";

            TextBox txtbxFromLetter = new TextBox();
            txtbxFromLetter.Margin = new Thickness(4);
            txtbxFromLetter.MinWidth = 16;
            txtbxFromLetter.Text = fromStr;

            TextBlock txtblkPhrase2 = new TextBlock();
            txtblkPhrase2.Margin = new Thickness(4);
            txtblkPhrase2.Text = " to ";

            TextBox txtbxToLetter = new TextBox();
            txtbxToLetter.Margin = new Thickness(4);
            txtbxToLetter.MinWidth = 16;
            txtbxToLetter.Text = toStr;

            Button bttnRemoveCond = new Button();
            bttnRemoveCond.Margin = new Thickness(4);
            bttnRemoveCond.Content = "Delete";

            pnlCondition.Children.Add(txtblkPhrase1);
            pnlCondition.Children.Add(txtbxFromLetter);
            pnlCondition.Children.Add(txtblkPhrase2);
            pnlCondition.Children.Add(txtbxToLetter);
            pnlCondition.Children.Add(bttnRemoveCond);

            //Binds Gui events with data.
            txtbxFromLetter.TextChanged += (a, b) =>
            {
                grouping.SetData("condAddFromLetter" + condNum,
                    txtbxFromLetter.Text);
            };

            txtbxToLetter.TextChanged += (a, b) =>
            {
                grouping.SetData("condAddToLetter" + condNum,
                    txtbxToLetter.Text);
            };

            //Overwrites with last cond and decrements number.
            bttnRemoveCond.Click += (a, b) =>
            {
                uint index = (uint)grouping.GetData("numConditions") - 1;
                var type = (GroupingCondType)grouping.GetData("conditionType" + index);

                //Decrements to no conditions.
                if (index == 0)
                {
                    grouping.SetData("numConditions", index);
                    gui.GroupConditions.Children.Remove(pnlCondition);
                    return;
                }

                //Overwrites the condition with the last condition.
                grouping.SetData("conditionType" + condNum,
                    grouping.GetData("conditionType" + index));

                switch (type)
                {
                    case (GroupingCondType.ByLetter):
                        grouping.SetData("condAddFromLetter" + condNum,
                            grouping.GetData("condAddFromLetter" + index));

                        grouping.SetData("condAddToLetter" + condNum,
                            grouping.GetData("condAddToLetter" + index));
                        break;
                }

                //Decrements the number of conditions.
                grouping.SetData("numConditions", index);
                gui.GroupConditions.Children.Remove(pnlCondition);
            };

            return pnlCondition;
        }
        #endregion
    }
}