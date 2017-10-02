using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CrystalKeeper.Gui
{
    class RichTextEditor
    {
        #region Members
        /// <summary>
        /// The gui is made available for binding.
        /// </summary>
        public RichTextEditorGui Gui { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new styled textbox.
        /// </summary>
        public RichTextEditor()
        {
            Gui = new RichTextEditorGui();
            SetHandlers();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets all content in a binary format (XamlPackage).
        /// </summary>
        public byte[] SaveData()
        {
            byte[] result;

            TextRange t = new TextRange(
                Gui.Textbox.Document.ContentStart,
                Gui.Textbox.Document.ContentEnd);

            using (MemoryStream ms = new MemoryStream())
            {
                t.Save(ms, DataFormats.XamlPackage);
                result = ms.ToArray();
            }

            return result;
        }

        /// <summary>
        /// Loads content from a binary format (XamlPackage).
        /// </summary>
        public void LoadData(byte[] data)
        {
            TextRange t = new TextRange(
                Gui.Textbox.Document.ContentStart,
                Gui.Textbox.Document.ContentEnd);

            using (MemoryStream ms = new MemoryStream(data))
            {
                try
                {
                    t.Load(ms, DataFormats.XamlPackage);
                }
                catch (ArgumentException) { }
            }
        }

        /// <summary>
        /// Hooks event handlers to the textbox.
        /// </summary>
        private void SetHandlers()
        {
            Gui.Textbox.PreviewKeyDown += Textbox_PreviewKeyDown;

            //Generates font sizes and sets the default.
            List<double> fontSizes = new List<double>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
            for (int i = 0; i < fontSizes.Count; i++)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = fontSizes[i];

                if (fontSizes[i] == 12d)
                {
                    item.IsSelected = true;
                }

                Gui.TxtFontSize.Items.Add(item);
            }

            //Generates and sets the default font.
            List<FontFamily> fonts = Fonts.SystemFontFamilies.ToList();
            for (int i = 0; i < fonts.Count; i++)
            {
                var itemFont = new ComboBoxItem();
                FontFamily itemFontFamily = fonts[i];
                itemFont.FontFamily = itemFontFamily;
                itemFont.Tag = itemFontFamily;
                itemFont.Content = itemFontFamily.Source;

                if (fonts[i].Source == "Arial" ||
                    fonts[i].Source == "Courier New" ||
                    fonts[i].Source == "Sans Serif")
                {
                    itemFont.IsSelected = true;
                }

                Gui.CmbxFontFamily.Items.Add(itemFont);
            }

            //Hooks event handlers.
            Gui.BttnBold.Click += BttnBold_Click;
            Gui.BttnFontColor.Click += BttnFontColor_Click;
            Gui.BttnItalic.Click += BttnItalic_Click;
            Gui.BttnStrikethrough.Click += BttnStrikethrough_Click;
            Gui.BttnUnderline.Click += BttnUnderline_Click;
            Gui.CmbxFontFamily.SelectionChanged += CmbxFontFamily_SelectionChanged;
            Gui.TxtFontSize.KeyDown += TxtFontSize_KeyDown;
            Gui.TxtFontSize.SelectionChanged += TxtFontSize_SelectionChanged;
            Gui.Textbox.SelectionChanged += Textbox_SelectionChanged;
            Gui.Textbox.LostFocus += Textbox_LostFocus;
        }

        /// <summary>
        /// Sets the styles of selected text.
        /// </summary>
        private void Textbox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //If Control is held.
            if (e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.LeftCtrl) ||
                e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.RightCtrl))
            {
                //Toggles bold text.
                if (e.Key == System.Windows.Input.Key.B)
                {
                    Gui.BttnBold.IsChecked = !Gui.BttnBold.IsChecked;
                }

                //Toggles italic text.
                else if (e.Key == System.Windows.Input.Key.I)
                {
                    Gui.BttnItalic.IsChecked = !Gui.BttnItalic.IsChecked;
                }

                //Toggles underlined text.
                else if (e.Key == System.Windows.Input.Key.U)
                {
                    Gui.BttnUnderline.IsChecked = !Gui.BttnUnderline.IsChecked;
                }

                //Prevents RichTextBox's faulty sub/superscript shortcut.
                else if (e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.OemPlus))
                {
                    e.Handled = true;
                }
                else if (e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.OemMinus))
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Sets the font size for the selected text.
        /// </summary>
        private void TxtFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Gui.TxtFontSize.SelectedItem != null)
            {
                Gui.Textbox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty,
                    ((ComboBoxItem)Gui.TxtFontSize.SelectedItem).Content);
            }

            Gui.Textbox.Focus();
        }

        /// <summary>
        /// Filters non-digits, then sets the font size for the selection.
        /// </summary>
        private void TxtFontSize_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            string newString = String.Empty;

            //Strips all non-digit characters.
            for (int i = 0; i < Gui.TxtFontSize.Text.Length; i++)
            {
                if (Char.IsDigit(Gui.TxtFontSize.Text[i]))
                {
                    newString += Gui.TxtFontSize.Text[i];
                }
            }

            Gui.TxtFontSize.Text = newString;
            if (Double.TryParse(Gui.TxtFontSize.Text, out double size))
            {
                if (size > 0 && size < 500)
                {
                    Gui.Textbox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
                }
            }
        }
        
        /// <summary>
        /// Sets the font from the list of font families for the selected text.
        /// </summary>
        private void CmbxFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Gui.CmbxFontFamily.SelectedItem != null)
            {
                Gui.Textbox.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty,
                    ((ComboBoxItem)Gui.CmbxFontFamily.SelectedItem).Tag);
            }

            Gui.Textbox.Focus();
        }

        /// <summary>
        /// Underlines the selected text.
        /// </summary>
        private void BttnUnderline_Click(object sender, RoutedEventArgs e)
        {
            if (Gui.BttnUnderline?.IsChecked ?? false)
            {
                Gui.BttnStrikethrough.IsChecked = false;
                Gui.Textbox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty,
                    TextDecorations.Underline);
            }
            else
            {
                Gui.Textbox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty,
                    null);
            }

            Gui.Textbox.Focus();
        }

        /// <summary>
        /// Draws a line through the selected text.
        /// </summary>
        private void BttnStrikethrough_Click(object sender, RoutedEventArgs e)
        {
            if (Gui.BttnStrikethrough?.IsChecked ?? false)
            {
                Gui.BttnUnderline.IsChecked = false;
                Gui.Textbox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty,
                    TextDecorations.Strikethrough);
            }
            else
            {
                Gui.Textbox.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty,
                    null);
            }

            Gui.Textbox.Focus();
        }

        /// <summary>
        /// Makes the selected text italic.
        /// </summary>
        private void BttnItalic_Click(object sender, RoutedEventArgs e)
        {
            if (Gui.BttnItalic?.IsChecked ?? false)
            {
                Gui.Textbox.Selection.ApplyPropertyValue(TextElement.FontStyleProperty,
                    FontStyles.Italic);
            }
            else
            {
                Gui.Textbox.Selection.ApplyPropertyValue(TextElement.FontStyleProperty,
                    FontStyles.Normal);
            }

            Gui.Textbox.Focus();
        }

        /// <summary>
        /// Sets the color of the selected text.
        /// </summary>
        private void BttnFontColor_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.ColorDialog();
            dlg.FullOpen = true;
            dlg.SolidColorOnly = true;
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Yes ||
                result == System.Windows.Forms.DialogResult.OK)
            {
                SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(dlg.Color.R, dlg.Color.G, dlg.Color.B));
                Gui.FontColorBg.Background = brush;
                Gui.Textbox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty,
                    brush);
            }

            Gui.Textbox.Focus();
        }

        /// <summary>
        /// Makes the selected text bold.
        /// </summary>
        private void BttnBold_Click(object sender, RoutedEventArgs e)
        {
            if (Gui.BttnBold?.IsChecked ?? false)
            {
                Gui.Textbox.Selection.ApplyPropertyValue(TextElement.FontWeightProperty,
                    FontWeights.Bold);
            }
            else
            {
                Gui.Textbox.Selection.ApplyPropertyValue(TextElement.FontWeightProperty,
                    FontWeights.Normal);
            }

            Gui.Textbox.Focus();
        }

        /// <summary>
        /// Prevents a loss of focus from forgetting the user selection.
        /// </summary>
        private void Textbox_LostFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Updates controls to indicate the state of the selected text.
        /// </summary>
        private void Textbox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //Updates existing properties.
            //Bold
            object temp = Gui.Textbox.Selection.GetPropertyValue(TextElement.FontWeightProperty);
            Gui.BttnBold.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(FontWeights.Bold));

            //Italic
            temp = Gui.Textbox.Selection.GetPropertyValue(TextElement.FontStyleProperty);
            Gui.BttnItalic.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(FontStyles.Italic));

            //Underline
            temp = Gui.Textbox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            Gui.BttnUnderline.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(TextDecorations.Underline));

            //Strikethrough
            temp = Gui.Textbox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            Gui.BttnStrikethrough.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(TextDecorations.Strikethrough));

            //Font color
            temp = Gui.Textbox.Selection.GetPropertyValue(TextElement.ForegroundProperty);
            if (temp != DependencyProperty.UnsetValue)
            {
                Gui.FontColorBg.Background = (Brush)temp;
            }

            //Font family
            temp = Gui.Textbox.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
            if (temp != DependencyProperty.UnsetValue)
            {
                for (int i = 0; i < Gui.CmbxFontFamily.Items.Count; i++)
                {
                    if ((((Gui.CmbxFontFamily.Items[i]) as ComboBoxItem)
                        .Tag as FontFamily).Source == ((FontFamily)temp).Source)
                    {
                        Gui.CmbxFontFamily.SelectedIndex = i;
                    }
                }
            }

            //Font size
            temp = Gui.Textbox.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            if (temp != DependencyProperty.UnsetValue)
            {
                Gui.TxtFontSize.Text = temp.ToString();
            }
        }
        #endregion
    }
}