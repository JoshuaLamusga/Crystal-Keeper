using CrystalKeeper.Core;
using System;
using System.Windows.Controls;

namespace CrystalKeeper.GuiCore
{
    /// <summary>
    /// Tethers a data item to a combobox item.
    /// </summary>
    class CmbxDataItem : ComboBoxItem
    {
        #region Fields
        /// <summary>
        /// Contains the associated dataitem.
        /// </summary>
        private DataItem _data;
        #endregion

        #region Constructors
        public CmbxDataItem(DataItem item)
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
        public bool Equals(CmbxDataItem other)
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
