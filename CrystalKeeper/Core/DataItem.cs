using System;
using System.Collections.Generic;

namespace CrystalKeeper.Core
{
    /// <summary>
    /// Represents an item with a GUID and a set of properties.
    /// </summary>
    class DataItem
    {
        #region Members
        /// <summary>
        /// The globally unique identifier of the item.
        /// </summary>
        public readonly ulong guid;

        /// <summary>
        /// The type of data contained.
        /// </summary>
        public readonly DataItemTypes type;

        /// <summary>
        /// A list of named properties with primitive values.
        /// </summary>
        private Dictionary<string, object> _properties;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new data item with a globally-unique identifier. If a
        /// unique identifier is not provided, it could result in collisions
        /// while retrieving instances.
        /// </summary>
        public DataItem(
            ulong guid,
            DataItemTypes type)
        {
            this.guid = guid;
            this.type = type;
            _properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates a deep copy of another data item.
        /// </summary>
        public DataItem(DataItem item)
        {
            guid = item.guid;
            type = item.type;
            _properties = new Dictionary<string, object>(item._properties);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Clears all properties.
        /// </summary>
        public void ClearData()
        {
            _properties.Clear();
        }

        /// <summary>
        /// If guids are equal, the objects are considered equal.
        /// </summary>
        public bool Equals(DataItem item)
        {
            return (guid == item.guid);
        }

        /// <summary>
        /// Returns a copy of all data.
        /// </summary>
        /// <returns>
        ///  A Dictionary object shallow copy of all data.
        /// </returns>
        public Dictionary<string, object> GetData()
        {
            return new Dictionary<string, object>(_properties);
        }

        /// <summary>
        /// Attempts to get a property by name, returning an empty string if
        /// not found. Warning: object returned is mutable.
        /// </summary>
        /// <param name="key">
        /// The property name to search for.
        /// </param>
        /// <returns>
        /// The property's value if it exists, or an emtpy string otherwise.
        /// </returns>
        public object GetData(string key)
        {
            if (_properties.ContainsKey(key) &&
                _properties[key] != null)
            {
                return _properties[key];
            }

            return String.Empty;
        }

        /// <summary>
        /// Adds a value to the data, overwriting it if the key exists.
        /// </summary>
        /// <param name="key">
        /// The name of the property to add.
        /// </param>
        /// <param name="value">
        /// The value associated with the property.
        /// </param>
        public void SetData(string key, object value)
        {
            if (_properties.ContainsKey(key))
            {
                _properties[key] = value;
            }
            else
            {
                _properties.Add(key, value);
            }
        }
        #endregion
    }
}
