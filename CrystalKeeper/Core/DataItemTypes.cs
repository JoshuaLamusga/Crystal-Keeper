namespace CrystalKeeper.Core
{
    /// <summary>
    /// Represents a type of data to store.
    /// </summary>
    enum DataItemTypes
    {
        /// <summary>
        /// The database stores project-wide preferences.
        /// </summary>
        Database = 0,

        /// <summary>
        /// Collections use templates to define the data fields and visual
        /// layout.
        /// </summary>
        Template = 1,

        /// <summary>
        /// A template field represents a type of data field for a template.
        /// </summary>
        TemplateField = 2,

        /// <summary>
        /// Template columns store the order of template fields and help
        /// indicate the number of columns a template uses.
        /// </summary>
        TemplateColumnData = 3,

        /// <summary>
        /// Collections use a template to define fields and visual layout for
        /// their associated entries, which are organized in groupings.
        /// </summary>
        Collection = 4,

        /// <summary>
        /// Groupings help organize entries in a collection. Entries are
        /// associated with the collection directly, and groupings use
        /// entry references.
        /// </summary>
        Grouping = 5,

        /// <summary>
        /// A reference to an entry, used for groupings, so that there can be
        /// multiple pointers to the same entry.
        /// </summary>
        GroupingEntryRef = 6,

        /// <summary>
        /// An entry stores the actual data as entry fields matching the
        /// parent collection's template fields.
        /// </summary>
        Entry = 7,

        /// <summary>
        /// Entry fields store user data based on the template associated with
        /// the collection in which the parent entry resides.
        /// </summary>
        EntryField = 8
    }
}
