using System;
using System.Collections.Generic;
using System.Linq;
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
        /// A list of all suggestions.
        /// </summary>
        private List<string> suggestions;
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
        public List<string> Suggestions
        {
            get
            {
                return suggestions;
            }
            set
            {
                suggestions = value.Distinct().ToList();
                suggestions.Sort();
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

        #region Constructors
        /// <summary>
        /// Constructs an empty searchbox.
        /// </summary>
        public SearchBox()
        {
            gui = new SearchboxGui();
            searchByWord = false;
            suggestions = new List<string>();

            SetHandlers();
        }

        /// <summary>
        /// Constructs a searchbox with suggestions.
        /// </summary>
        public SearchBox(List<string> suggestions)
        {
            gui = new SearchboxGui();
            searchByWord = false;
            this.suggestions = suggestions;

            SetHandlers();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Sets handlers for all default behavior.
        /// </summary>
        private void SetHandlers()
        {
            gui.suggestions.Visibility = System.Windows.Visibility.Collapsed;

            //Simulates filtering.
            gui.textbox.TextChanged += HandlerRefreshSuggestions;
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
            //Disconnects the refresh suggestions functionality.
            //FIXME: This avoids a threading race condition. Sloppy.
            gui.textbox.TextChanged -= HandlerRefreshSuggestions;

            HideSuggestions();

            if (!searchByWord)
            {
                gui.textbox.Text = headerName;
            }
            else
            {
                var words = gui.textbox.Text.Split(' ');

                if (words.Length > 0)
                {
                    words[words.Length - 1] = headerName;
                    gui.textbox.Text = string.Join(" ", words);
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
            List<string> filteredSuggestions = new List<string>();
            filteredSuggestions = suggestions.Where((suggestion) =>
            {
                if (!searchByWord || gui.textbox.Text == string.Empty)
                {
                    return suggestion.ToLower()
                    .Contains(gui.textbox.Text.ToLower());
                }
                else
                {
                    var words = gui.textbox.Text.Split(' ');
                    return suggestion.ToLower()
                    .Contains(words[words.Length - 1].ToLower());
                }
            }).ToList();

            //Creates a gui item (up to 50) for each match. Matches will
            //appear in sorted order since original file is sorted.
            for (int i = 0; i < filteredSuggestions.Count && i < 50; i++)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = filteredSuggestions[i];
                item.Selected += (a, b) =>
                {
                    SelectSuggestion((string)item.Content);
                };
                gui.suggestions.Items.Add(item);
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
        /// Hides all suggestions to display only the text field.
        /// </summary>
        private void HideSuggestions()
        {
            //Clears all old suggestions.
            gui.suggestions.Items.Clear();

            //Hides the suggestions and adds the text.
            gui.suggestions.Visibility = System.Windows.Visibility.Collapsed;
        }
        #endregion
    }
}