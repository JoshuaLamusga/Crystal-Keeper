namespace CrystalKeeper.Core
{
    /// <summary>
    /// Represents the position of additional images relative to the main one.
    /// </summary>
    enum TemplateImagePos
    {
        /// <summary>
        /// Images are displayed horizontally in order.
        /// </summary>
        Right = 0,

        /// <summary>
        /// Images are displayed vertically in order.
        /// </summary>
        Under = 1,
        
        /// <summary>
        /// Images are displayed horizontally in reverse order.
        /// </summary>
        Left = 2,

        /// <summary>
        /// Images are displayed vertically in reverse order.
        /// </summary>
        Above = 3
    }
}
