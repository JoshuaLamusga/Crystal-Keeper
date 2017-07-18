using CrystalKeeper.Core;
using System;
using System.Windows.Controls;

namespace CrystalKeeper.GuiCore
{
    /// <summary>
    /// Tethers a data item to a listbox item.
    /// </summary>
    class LstbxDataItem : ListBoxItem
    {
        #region Fields
        /// <summary>
        /// Contains the associated dataitem.
        /// </summary>
        private DataItem _data;
        #endregion

        #region Constructors
        public LstbxDataItem(DataItem item)
            : base()
        {
            this._data = item;

            if (item != null)
            {
                Content = (string)item.GetData("name");
            }
            else
            {
                Content = String.Empty;
            }
        }

        /// <summary>
        /// Clones the given item and resets header to match.
        /// </summary>
        /// <param name="item">The item to copy.</param>
        public LstbxDataItem(LstbxDataItem item)
        {
            _data = item._data;

            //Sets the header for the element according to the name.
            if (item != null && item.GetItem() != null)
            {
                Content = (string)item.GetItem().GetData("name");
            }
            else
            {
                Content = String.Empty;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets the DataItem associated with the combobox item.
        /// </summary>
        /// <returns></returns>
        public DataItem GetItem()
        {
            return _data;
        }

        /// <summary>
        /// Sets the data item.
        /// </summary>
        public void SetItem(DataItem item)
        {
            _data = item;
        }

        /// <summary>
        /// Two comboboxdataitems are equal if the underlying data's
        /// guids are equal.
        /// </summary>
        public bool Equals(LstbxDataItem other)
        {
            if (other == null)
            {
                return false;
            }

            return (_data.guid == other._data.guid);
        }

        /// <summary>
        /// A comboboxdataitem is equal to a dataitem when both share the
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
        public void Refresh()
        {
            if (_data != null)
            {
                Content = (string)_data.GetData("name");
            }
            else
            {
                Content = String.Empty;
            }
        }
        #endregion
    }
}
