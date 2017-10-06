using System.Windows.Controls;
using System.Windows.Input;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Encapsulates and provides functionality for a textbox dialog. Used to
    /// allow the user to provide some text.
    /// </summary>
    class DlgTextbox
    {
        #region Members
        /// <summary>
        /// Stores the gui instance.
        /// </summary>
        private DlgTextboxGui gui;

        /// <summary>
        /// Stores the result of the dialog that the user submitted.
        /// </summary>
        private string textResult;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an empty textbox popup.
        /// </summary>
        public DlgTextbox()
        {
            gui = new DlgTextboxGui();

            ConstructPage();
        }

        /// <summary>
        /// Creates a new textboz popup with a settable caption.
        /// </summary>
        /// <param name="windowTitle">
        /// The window caption.
        /// </param>
        public DlgTextbox(string windowTitle)
        {
            gui = new DlgTextboxGui();
            gui.wndMain.Title = windowTitle;

            ConstructPage();
        }
        #endregion

        #region Private Methods
        private void ConstructPage()
        {
            gui.GuiGrid.KeyDown += GuiGrid_KeyDown;

            //Updates the gui text whenever it changes.
            gui.GuiTextbox.TextChanged +=
                new TextChangedEventHandler((a, b) =>
                {
                    textResult = gui.GuiTextbox.Text;
                });

            gui.GuiTextbox.KeyDown += new KeyEventHandler((a, b) =>
            {
                //When enter is pressed.
                if (b.Key == Key.Enter && b.IsDown)
                {
                    gui.DialogResult = true;
                    gui.Close();
                }
            });

            //Closes when submit is clicked.
            gui.GuiSubmit.Click +=
                new System.Windows.RoutedEventHandler((a, b) =>
                {
                    gui.DialogResult = true;
                    gui.Close();
                });
        }

        /// <summary>
        /// Closes the gui if escape is pressed.
        /// </summary>
        private void GuiGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                gui.Close();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Shows the textbox as a dialog.
        /// </summary>
        public bool? ShowDialog()
        {
            gui.GuiTextbox.Focus();
            return gui.ShowDialog();
        }

        /// <summary>
        /// Returns the current text provided by the user.
        /// </summary>
        public string GetText()
        {
            return textResult;
        }
        #endregion
    }
}
