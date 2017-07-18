namespace CrystalKeeper.Core
{
    /// <summary>
    /// Represents the type of data used in a template field.
    /// </summary>
    enum TemplateFieldType
    {
        /// <summary>
        /// Required for displaying images in the treeview.
        /// </summary>
        EntryImages = 0,

        /// <summary>
        /// Text will appear as content.
        /// </summary>
        Text = 1,

        /// <summary>
        /// Two separate text fields.
        /// </summary>
        MoneyUSD = 2,

        /// <summary>
        /// Stores a string url to the image resource.
        /// </summary>
        Images = 3,
        
        /// <summary>
        /// Stores a string url to the webpage.
        /// </summary>
        Hyperlink = 4,

        /// <summary>
        /// Used for mineral formula auto-fill.
        /// </summary>
        Text_Formula = 5,

        /// <summary>
        /// Used for auto-suggesting mineral names word-by-word.
        /// </summary>
        Text_Minerals = 6
    }
}