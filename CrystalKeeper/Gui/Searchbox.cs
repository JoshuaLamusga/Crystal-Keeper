﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Represents a searchable textbox that allows the user to type each
    /// word with autosuggest capabilities (per word or string).
    /// </summary>
    class SearchBox
    {
        #region Members
        /// <summary>
        /// Contains the underlying user control.
        /// </summary>
        private SearchboxGui gui;

        /// <summary>
        /// If false, suggestions take into account and replace the entire
        /// string. If true, they affect only the current word.
        /// </summary>
        private bool searchByWord;

        /// <summary>
        /// A list of all suggestions and replacements.
        /// </summary>
        private List<Tuple<string, string>> suggestions;
        #endregion

        #region Properties
        /// <summary>
        /// If false, suggestions take into account and replace the entire
        /// string. If true, they affect only the current word.
        /// </summary>
        public bool SearchByWord
        {
            get
            {
                return searchByWord;
            }
            set
            {
                searchByWord = value;
            }
        }

        /// <summary>
        /// Gets or sets the suggestions for the listbox. Suggestions are
        /// automatically sorted with duplicates removed.
        /// </summary>
        public List<Tuple<string, string>> Suggestions
        {
            get
            {
                return suggestions;
            }
            set
            {
                suggestions = value
                    .Distinct()
                    .OrderBy((n) => n.Item1)
                    .ToList();
                RefreshSuggestions();
            }
        }

        /// <summary>
        /// Gets the underlying gui for purposes such as linking to a parent.
        /// </summary>
        public SearchboxGui Gui
        {
            get
            {
                return gui;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised after menu items for the dropdown are created.
        /// </summary>
        public event Action<ListBoxItem> MenuItemAdded;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs an empty searchbox.
        /// </summary>
        public SearchBox()
        {
            gui = new SearchboxGui();
            searchByWord = false;
            suggestions = new List<Tuple<string, string>>();

            SetHandlers();
        }

        /// <summary>
        /// Constructs a searchbox with suggestions.
        /// </summary>
        public SearchBox(List<Tuple<string, string>> suggestions)
        {
            gui = new SearchboxGui();
            searchByWord = false;
            this.suggestions = suggestions;

            SetHandlers();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Hides all suggestions to display only the text field.
        /// </summary>
        public void HideSuggestions()
        {
            //Clears all old suggestions.
            gui.suggestions.Items.Clear();

            //Hides the suggestions and adds the text.
            gui.suggestions.Visibility = System.Windows.Visibility.Collapsed;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets handlers for all default behavior.
        /// </summary>
        private void SetHandlers()
        {
            gui.suggestions.Visibility = System.Windows.Visibility.Collapsed;
            gui.textbox.TextChanged += HandlerRefreshSuggestions;
            gui.textbox.PreviewKeyDown += Textbox_TextChanged;
            gui.suggestions.PreviewKeyDown += Suggestions_PreviewKeyDown;
            gui.container.LostFocus += Container_LostFocus;
        }

        /// <summary>
        /// Hides suggestions when mouse focus is clearly not in bounds.
        /// Keyboard focus is handled explicitly in Suggestions_PreviewKeyDown.
        /// </summary>
        private void Container_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!gui.container.IsMouseOver)
            {
                HideSuggestions();
            }
        }

        /// <summary>
        /// Hides the suggestions when they lose focus.
        /// </summary>
        private void Suggestions_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Tab)
            {
                HideSuggestions();
            }
        }

        /// <summary>
        /// Simulates functionality of searchboxes from other gui systems.
        /// </summary>
        private void Textbox_TextChanged(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //Switches from textbox to suggestions box.
            if (e.Key == System.Windows.Input.Key.Down)
            {
                gui.suggestions.Focus();
            }

            //Closes suggestions when escape is pressed.
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                HideSuggestions();
            }
        }

        /// <summary>
        /// Calls the refresh suggestions function.
        /// </summary>
        private void HandlerRefreshSuggestions(object sender, TextChangedEventArgs e)
        {
            RefreshSuggestions();
        }

        /// <summary>
        /// Selects a suggestion when the menu item is clicked and adds it
        /// to the current text. Closes the suggestions.
        /// </summary>
        /// <param name="headerName">
        /// The name of the menu item (to be appended).
        /// </param>
        private void SelectSuggestion(string headerName)
        {
            //Disconnects refresh suggestions to prevent circular calls.
            gui.textbox.TextChanged -= HandlerRefreshSuggestions;

            HideSuggestions();

            if (!searchByWord)
            {
                //Uses the replacement if specified, else suggestion.
                var match = suggestions.FindIndex((a) => a.Item1 == headerName);
                if (match != -1 && suggestions[match].Item2 != null)
                {
                    gui.textbox.Text = suggestions[match].Item2;
                }
                else
                {
                    gui.textbox.Text = headerName;
                }
            }
            else
            {
                var words = gui.textbox.Text.Split(' ');

                if (words.Length > 0)
                {
                    //Uses the replacement if specified, else suggestion.
                    var match = suggestions.FindIndex((a) => a.Item1 == headerName);
                    if (match != -1 && suggestions[match].Item2 != null)
                    {
                        words[words.Length - 1] = suggestions[match].Item2;
                        gui.textbox.Text = string.Join(" ", words);
                    }
                    else
                    {
                        words[words.Length - 1] = headerName;
                        gui.textbox.Text = string.Join(" ", words);
                    }
                }
            }

            //Reconnects the refresh suggestions functionality.
            gui.textbox.TextChanged += HandlerRefreshSuggestions;
        }

        /// <summary>
        /// Updates the suggestions for the current word or all text whenever
        /// the text changes. Opens the suggestions if they don't exist.
        /// </summary>
        private void RefreshSuggestions()
        {
            //Clears all old suggestions.
            gui.suggestions.Items.Clear();

            //Builds a list of all suggested items.
            var filteredSuggestions = new List<Tuple<string, string>>();
            var otherSuggestions = new List<Tuple<string, string>>();

            int suggestionsAllowed = 10;

            //Adds each suggestion up to the max allowed.
            string userText = String.Empty;
            if ((!searchByWord || gui.textbox.Text == string.Empty))
            {
                userText = RemoveDiacritics(gui.textbox.Text.ToLower());
            }
            else
            {
                var words = gui.textbox.Text.Split(' ');
                userText = RemoveDiacritics(words[words.Length - 1].ToLower());
            }

            //Records suggestions for StartsWith and Contains separately.
            for (int i = 0; i < suggestions.Count; i++)
            {
                string searchText =
                    RemoveDiacritics(suggestions[i].Item1.ToLower());

                if (searchText.StartsWith(userText))
                {
                    filteredSuggestions.Add(suggestions[i]);
                }
                else if (searchText.Contains(userText) &&
                    filteredSuggestions.Count + otherSuggestions.Count
                    < suggestionsAllowed)
                {
                    otherSuggestions.Add(suggestions[i]);
                }

                if (filteredSuggestions.Count == suggestionsAllowed)
                {
                    break;
                }
            }

            //Adds to the filtered suggestions up to the max count.
            if (filteredSuggestions.Count < suggestionsAllowed)
            {
                for (int i = 0; i < otherSuggestions.Count; i++)
                {
                    filteredSuggestions.Add(otherSuggestions[i]);
                    if (filteredSuggestions.Count == suggestionsAllowed)
                    {
                        break;
                    }
                }
            }

            //Creates a gui item for each match. Matches will
            //appear in sorted order since original file is sorted.
            for (int i = 0; i < filteredSuggestions.Count; i++)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = filteredSuggestions[i].Item1;

                //Clicking selects the item.
                item.PreviewMouseLeftButtonDown += (sender, e) =>
                {
                    SelectSuggestion((string)item.Content);
                };

                //Pressing enter selects the item.
                item.KeyDown += (sender, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                        SelectSuggestion((string)item.Content);
                    }
                };

                gui.suggestions.Items.Add(item);

                MenuItemAdded?.Invoke(item);
            }

            //Hides or un-hides the suggestions as appropriate.
            if (filteredSuggestions.Count != 0 &&
                !string.IsNullOrWhiteSpace(gui.textbox.Text))
            {
                gui.suggestions.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                gui.suggestions.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Returns an un-accented copy of the given string.
        /// </summary>
        private static string RemoveDiacritics(string str)
        {
            /*
            Code copied from Dana on SE, licensed under CC-By-SA
            https://stackoverflow.com/questions/5459641/replacing-characters-in-c-sharp-ascii/13154805#13154805
            */

            string strNorm = str.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < strNorm.Length; i++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(strNorm[i]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(strNorm[i]);
                }
            }

            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }
        #endregion
    }
}