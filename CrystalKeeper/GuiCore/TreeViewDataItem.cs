using CrystalKeeper.Core;
using System;
using System.Windows.Controls;

namespace CrystalKeeper.GuiCore
{
    /// <summary>
    /// Tethers a data item to a treeview item.
    /// </summary>
    class TreeViewDataItem : TreeViewItem
    {
        #region Members
        /// <summary>
        /// The underlying data associated with this item.
        /// </summary>
        private DataItem _data;

        /// <summary>
        /// The treeview item that contains this one.
        /// </summary>
        private TreeViewDataItem _parentView;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new treeview item with underlying data.
        /// </summary>
        /// <param name="item">
        /// The data to provide.
        /// </param>
        public TreeViewDataItem(DataItem item)
            : base()
        {
            this._data = item;
            _parentView = null;

            if (item != null)
            {
                Header = (string)item.GetData("name");
            }
            else
            {
                Header = String.Empty;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Crawls the node structure in a depth-first search of the item.
        /// </summary>
        private TreeViewDataItem TraverseTree(TreeViewDataItem node, DataItem item)
        {
            TreeViewDataItem result = null;

            //Checks if root node matches the given item.
            if (node == null)
            {
                return null;
            }
            else if (node._data == item)
            {
                return node;
            }

            //Iterates through each node recursively.
            for (int i = 0; i < node.Items.Count; i++)
            {
                result = TraverseTree(
                    (TreeViewDataItem)node.Items[i],
                    item);

                if (result != null)
                {
                    return result;
                }
            }

            return result;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the DataItem associated with the treeview item.
        /// </summary>
        public DataItem GetItem()
        {
            return _data;
        }

        /// <summary>
        /// Traverses the child tree and returns the treeview containing the
        /// given data, or null if not found.
        /// </summary>
        public TreeViewDataItem GetContainer(DataItem item)
        {
            return TraverseTree(this, item);
        }

        /// <summary>
        /// Returns the parent item of this item, or null if not found.
        /// </summary>
        public TreeViewDataItem GetParent()
        {
            return _parentView;
        }

        /// <summary>
        /// Sets the parent item of this item so traversal is easier later.
        /// </summary>
        /// <param name="item">
        /// The containing parent of this item.
        /// </param>
        public void SetParent(TreeViewDataItem item)
        {
            _parentView = item;
        }

        /// <summary>
        /// Two treeviewdataitems are equal if the underlying data's
        /// guids are equal.
        /// </summary>
        public bool Equals(TreeViewDataItem other)
        {
            if (other == null)
            {
                return false;
            }

            return (_data.guid == other._data.guid);
        }

        /// <summary>
        /// A treeviewdataitem is equal to a dataitem when both share the
        /// same guid.
        /// </summary>
        public bool Equals(DataItem other)
        {
            if (other == null)
            {
                return false;
            }

            return (_data.guid == other.guid);
        }

        /// <summary>
        /// Causes the header to update to match the dataitem.
        /// </summary>
        public void Refresh(Project project)
        {
            if (_data != null)
            {
                if (_data.type == DataItemTypes.GroupingEntryRef)
                {
                    Header = (string)project.GetEntryRefEntry(_data).GetData("name");
                }
                else
                {
                    Header = (string)_data.GetData("name");
                }
            }
            else
            {
                Header = String.Empty;
            }
        }
        #endregion
    }
}
