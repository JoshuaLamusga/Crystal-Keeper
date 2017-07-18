using CrystalKeeper.Core;
using System;
using System.Windows.Controls;

namespace CrystalKeeper.GuiCore
{
    /// <summary>
    /// Tethers a data item to a menu item.
    /// </summary>
    class MenuDataItem : MenuItem
    {
        #region Fields
        /// <summary>
        /// Contains the associated dataitem.
        /// </summary>
        private DataItem _data;
        #endregion

        #region Constructors
        public MenuDataItem(DataItem item)
            : base()
        {
            this._data = item;

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
        /// Two comboboxdataitems are equal if the underlying data's
        /// guids are equal.
        /// </summary>
        public bool Equals(MenuDataItem other)
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
                Header = (string)_data.GetData("name");
            }
            else
            {
                Header = String.Empty;
            }
        }
        #endregion
    }
}
