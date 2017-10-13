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
        private Dictionary<string, object> properties;
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
            properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates a deep copy of another data item.
        /// </summary>
        public DataItem(DataItem item)
        {
            guid = item.guid;
            type = item.type;
            properties = new Dictionary<string, object>(item.properties);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Clears all properties.
        /// </summary>
        public void ClearData()
        {
            properties.Clear();
        }

        /// <summary>
        /// Returns true if the guids are equal; false otherwise.
        /// </summary>
        /// <param name="item">
        /// The item to compare to this item for equality.
        /// </param>
        public bool Equals(DataItem item)
        {
            return (guid == item?.guid);
        }

        /// <summary>
        /// Returns a shallow copy of all data.
        /// </summary>
        public Dictionary<string, object> GetData()
        {
            return new Dictionary<string, object>(properties);
        }

        /// <summary>
        /// Returns the value associated with the named property, or an empty
        /// string if not found. The returned value is mutable.
        /// </summary>
        /// <param name="key">
        /// The property name to search for.
        /// </param>
        public object GetData(string key)
        {
            if (properties.ContainsKey(key) &&
                properties[key] != null)
            {
                return properties[key];
            }

            return String.Empty;
        }

        /// <summary>
        /// Copies the data from the given dictionary.
        /// </summary>
        /// <param name="dict">
        /// The dictionary from which to copy all entries.
        /// </param>
        public void SetData(Dictionary<string, object> dict)
        {
            properties = new Dictionary<string, object>(dict);
        }

        /// <summary>
        /// Associates a value with a key, overwriting any existing value.
        /// </summary>
        /// <param name="key">
        /// The name of the property to add (or overwrite).
        /// </param>
        /// <param name="value">
        /// The value associated with the property.
        /// </param>
        public void SetData(string key, object value)
        {
            if (properties.ContainsKey(key))
            {
                properties[key] = value;
            }
            else
            {
                properties.Add(key, value);
            }
        }
        #endregion
    }
}
