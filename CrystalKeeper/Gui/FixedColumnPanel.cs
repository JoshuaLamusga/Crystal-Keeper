using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Displays elements in horizontally-fixed columns with expandable
    /// vertical sizes.
    /// </summary>
    class FixedColumnPanel
    {
        #region Members
        /// <summary>
        /// The grid and panels. Modifying columns or the first item in each
        /// column may result in unintended behavior.
        /// </summary>
        public Grid Gui { get; set; }

        /// <summary>
        /// Stores a reference to each column stack.
        /// </summary>
        public List<StackPanel> columns;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new horizontally-fixed column panel.
        /// </summary>
        public FixedColumnPanel(int numCols)
        {
            Gui = new Grid();
            columns = new List<StackPanel>();

            //Adds each column and populates it with a stackpanel.
            for (int i = 0; i < numCols; i++)
            {
                Gui.ColumnDefinitions.Add(new ColumnDefinition());
                StackPanel columnStack = new StackPanel();
                Grid.SetColumn(columnStack, i);

                Gui.Children.Add(columnStack);
                columns.Add(columnStack);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds items to columns from left to right, filling in by item
        /// count. If items are significantly different in size, columns may
        /// not be visually balanced in length.
        /// </summary>
        public void AddItem(UIElement element)
        {
            //Exits if there are no columns.
            if (columns.Count == 0)
            {
                return;
            }

            //Inserts to the first column with a smaller size than the first.
            int size = columns[0].Children.Count;
            for (int i = 1; i < columns.Count; i++)
            {
                if (columns[i].Children.Count < size)
                {
                    columns[i].Children.Add(element);
                    return;
                }
            }

            //All have same size, so insert to left column.
            columns[0].Children.Add(element);
        }
        #endregion
    }
}
