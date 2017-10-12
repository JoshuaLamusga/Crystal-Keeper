using CrystalKeeper.Core;
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
    /// <summary>
    /// Represents the page associated with an entry.
    /// </summary>
    class PgEntryView
    {
        #region Members
        /// <summary>
        /// The project associated with the page.
        /// </summary>
        private Project project;

        /// <summary>
        /// The underlying gui.
        /// </summary>
        private PgEntryGuiView gui;

        /// <summary>
        /// The guid of the item selected.
        /// </summary>
        private DataItem entry;

        /// <summary>
        /// Fires when the treeview item should change.
        /// </summary>
        public event EventHandler SelectedItemChanged;

        /// <summary>
        /// Stores the location that the project is saved at.
        /// </summary>
        private string projectUrl;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the data used to construct the page.
        /// </summary>
        public Project Project
        {
            set
            {
                project = value;
                ConstructPage();
            }
            get
            {
                return project;
            }
        }

        /// <summary>
        /// Gets or sets the gui that controls the page appearance.
        /// </summary>
        public PgEntryGuiView Gui
        {
            private set
            {
                gui = value;
            }
            get
            {
                return gui;
            }
        }

        /// <summary>
        /// The guid of the item selected.
        /// </summary>
        public DataItem Entry
        {
            get
            {
                return entry;
            }
            private set
            {
                entry = value;
                SelectedItemChanged(this, null);
            }
        }

        /// <summary>
        /// Returns the current background image url.
        /// </summary>
        public string BgImage
        {
            get
            {
                return (string)Project.GetDatabase().GetData("imageUrl");
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an entry page from the given project.
        /// </summary>
        /// <param name="project">
        /// The project associated with the page.
        /// </param>
        /// <param name="entryRef">
        /// A reference to the entry to be edited.
        /// </param>
        public PgEntryView(Project project, DataItem entryRef, string projectUrl)
        {
            this.project = project;
            this.projectUrl = projectUrl;
            entry = project.GetEntryRefEntry(entryRef);
            ConstructPage();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the page content based on existing material.
        /// </summary>
        public void ConstructPage()
        {
            gui = new PgEntryGuiView();

            #region Entry name
            //Sets the entry name.
            if (string.IsNullOrWhiteSpace((string)Entry.GetData("name")))
            {
                gui.TxtblkEntryName.Text = GlobalStrings.NameUntitled;
            }
            else
            {
                gui.TxtblkEntryName.Text = (string)Entry.GetData("name");
            }
            #endregion

            //Gets template details.
            var template = project.GetCollectionTemplate(project.GetEntryCollection(entry));
            bool tCenterImages = (bool)template.GetData("centerImages");
            var tTwoColumns = (bool)template.GetData("twoColumns");
            string tFontFamilies = (string)template.GetData("fontFamilies");

            gui.TxtblkEntryName.FontFamily = new FontFamily(tFontFamilies);

            var tContentColor = new SolidColorBrush(Color.FromRgb(
                (byte)template.GetData("contentColorR"),
                (byte)template.GetData("contentColorG"),
                (byte)template.GetData("contentColorB")));

            var tTitleColor = new SolidColorBrush(Color.FromRgb(
                (byte)template.GetData("headerColorR"),
                (byte)template.GetData("headerColorG"),
                (byte)template.GetData("headerColorB")));

            gui.TxtblkEntryName.Foreground = tTitleColor;

            //Adjusts the column widths if two columns are used.
            if (!tTwoColumns)
            {
                gui.RightCol.MaxWidth = 0;
            }

            //Gets entry fields, then sorts them by column order.
            var entryFields = project.GetEntryFields(entry);
            entryFields.OrderBy(new Func<DataItem, int>((a) =>
            {
                return (int)project.GetFieldTemplateField(a).GetData("columnOrder");
            }));

            //Displays each field for viewing.
            for (int i = 0; i < entryFields.Count; i++)
            {
                //Gets the field and its data.
                var field = entryFields[i];
                object fieldData = field.GetData("data");
                var templateField = project.GetFieldTemplateField(entryFields[i]);
                byte tNumExtraImages = (byte)templateField.GetData("numExtraImages");
                var tExtraImagePos = (TemplateImagePos)(int)templateField.GetData("extraImagePos");

                //Gets various data regarding the field.
                var currField = entryFields[i];
                var currTemplateField = project.GetFieldTemplateField(currField);
                var templateType = (TemplateFieldType)(int)currTemplateField.GetData("dataType");
                var fieldName = (string)currTemplateField.GetData("name");
                var tfIsVisible = (bool)currTemplateField.GetData("isVisible");
                var tfTitleIsVisible = (bool)currTemplateField.GetData("isTitleVisible");

                //Gets the type of data and associated grid position.
                var isFirstColumn = (bool)project.GetItemByGuid(
                    (ulong)currTemplateField.GetData("refGuid")).GetData("isFirstColumn");

                //Skips all 2nd-column fields in a single-column layout.
                if (!tTwoColumns && !isFirstColumn)
                {
                    continue;
                }

                //Skips fields not to be rendered.
                if (!tfIsVisible)
                {
                    continue;
                }

                //Sets the name of the field.
                TextBlock fieldNameGui = new TextBlock(new Run(fieldName));
                fieldNameGui.FontFamily = new FontFamily(tFontFamilies);
                fieldNameGui.FontWeight = FontWeights.Bold;
                fieldNameGui.Foreground = tContentColor;

                //Centers the field name to match centered image fields.
                if (templateType == TemplateFieldType.EntryImages && tCenterImages)
                {
                    fieldNameGui.HorizontalAlignment = HorizontalAlignment.Center;
                }

                //Creates a container for the whole field.
                StackPanel elementsContainer = new StackPanel();

                //Displays text fields.
                //Text is stored in the binary XamlPackage format.
                if (templateType == TemplateFieldType.Text)
                {
                    RichTextBoxNoMargins fieldDataGui = new RichTextBoxNoMargins();
                    fieldDataGui.Textbox.FontFamily = new FontFamily(tFontFamilies);
                    fieldDataGui.Textbox.Foreground = tContentColor;
                    fieldDataGui.Textbox.Margin = new Thickness(0, 4, 0, 12);
                    fieldDataGui.Textbox.IsReadOnly = true;

                    //Set thickness since border color changes dynamically.
                    fieldDataGui.Textbox.BorderThickness = new Thickness(0);

                    //Loads the XamlPackage if possible.
                    if (fieldData is byte[])
                    {
                        TextRange txt = new TextRange(
                            fieldDataGui.Textbox.Document.ContentStart,
                            fieldDataGui.Textbox.Document.ContentEnd);

                        using (MemoryStream ms = new MemoryStream((byte[])fieldData))
                        {
                            try
                            {
                                txt.Load(ms, DataFormats.XamlPackage);
                            }
                            catch (ArgumentException) { }
                        }

                        //Does not display empty fields.
                        if (String.IsNullOrWhiteSpace(txt.Text))
                        {
                            continue;
                        }
                    }

                    //Skips rendering for empty strings (given if 0 bytes).
                    else
                    {
                        continue;
                    }

                    //Gets whether the title is visible or not.
                    if (tfTitleIsVisible)
                    {
                        elementsContainer.Children.Add(fieldNameGui);
                        elementsContainer.Children.Add(fieldDataGui);                        
                    }

                    //Adds the field.
                    else
                    {
                        elementsContainer.Children.Add(fieldDataGui);
                    }
                }

                //Displays text fields.
                //Data is stored as a string.
                if (templateType == TemplateFieldType.Min_Formula ||
                    templateType == TemplateFieldType.Min_Name ||
                    templateType == TemplateFieldType.Min_Group ||
                    templateType == TemplateFieldType.Min_Locality)
                {
                    //Does not display empty text fields.
                    if (String.IsNullOrWhiteSpace((string)fieldData))
                    {
                        continue;
                    }

                    TextBlock fieldDataGui = new TextBlock();
                    fieldDataGui.FontFamily = new FontFamily(tFontFamilies);
                    fieldDataGui.Foreground = tContentColor;
                    fieldDataGui.Margin = new Thickness(2, 4, 2, 12);
                    fieldDataGui.Text = (string)fieldData;
                    fieldDataGui.TextWrapping = TextWrapping.Wrap;

                    //Parses the appearance of mineral formulas.
                    if (templateType == TemplateFieldType.Min_Formula)
                    {
                        //Gets text, tracks alignment, and makes a run.
                        string text = fieldDataGui.Text;
                        BaselineAlignment align = BaselineAlignment.Baseline;
                        Run run = new Run();

                        //Clears text from field.
                        fieldDataGui.Inlines.Clear();

                        //Toggles align to subscript on _ and superscript on ^.
                        for (int j = 0; j < text.Length; j++)
                        {
                            if (text[j] == '_' ||
                                text[j] == '^')
                            {
                                fieldDataGui.Inlines.Add(run);
                                run = new Run();

                                if (text[j] == '_')
                                {
                                    if (align != BaselineAlignment.Subscript)
                                    {
                                        align = BaselineAlignment.Subscript;
                                    }
                                    else
                                    {
                                        align = BaselineAlignment.Baseline;
                                    }
                                }
                                else if (text[j] == '^')
                                {
                                    if (align != BaselineAlignment.Superscript)
                                    {
                                        align = BaselineAlignment.Superscript;
                                    }
                                    else
                                    {
                                        align = BaselineAlignment.Baseline;
                                    }
                                }

                                run.BaselineAlignment = align;
                            }
                            else
                            {
                                run.Text += text[j];
                            }
                        }
                        fieldDataGui.Inlines.Add(run);
                    }

                    //Gets whether the title is visible or not.
                    if (tfTitleIsVisible)
                    {
                        elementsContainer.Children.Add(fieldNameGui);
                    }
                    elementsContainer.Children.Add(fieldDataGui);
                }

                //Displays webpages.
                //Data is stored as a string.
                else if (templateType == TemplateFieldType.Hyperlink)
                {
                    string url = (string)fieldData;

                    //Prepends the scheme to the beginning if necessary.
                    if (!url.StartsWith("http"))
                    {
                        url = "http://" + url;
                    }

                    //Creates the hyperlink only if it's a valid internet url.
                    Uri uriResult;
                    if (Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                        (uriResult.Scheme == Uri.UriSchemeHttp ||
                        uriResult.Scheme == Uri.UriSchemeHttps))
                    {
                        //Sets the hyperlink.
                        Hyperlink webpage = new Hyperlink();
                        webpage.Inlines.Add(url);

                        try
                        {
                            webpage.NavigateUri = new Uri((string)fieldData);
                            webpage.Click += (a, b) =>
                            {
                                System.Diagnostics.Process.Start(webpage.NavigateUri.ToString());
                            };
                        }
                        catch (UriFormatException)
                        {
                            //Don't log navigation errors.
                        }
                        catch (Exception e)
                        {
                            Utils.Log("Hyperlink error: " + e.Message);
                        }

                        //Sets a containing textblock.
                        TextBlock fieldDataGui = new TextBlock(webpage);
                        fieldDataGui.FontFamily = new FontFamily(tFontFamilies);
                        fieldDataGui.Foreground = tContentColor;
                        fieldDataGui.Margin = new Thickness(2, 4, 2, 12);
                        fieldDataGui.MinWidth = 32;
                        fieldDataGui.TextWrapping = TextWrapping.Wrap;

                        if (tfTitleIsVisible)
                        {
                            elementsContainer.Children.Add(fieldNameGui);
                        }
                        elementsContainer.Children.Add(fieldDataGui);
                    }

                    //Does not display empty or malformed hyperlinks.
                    else
                    {
                        continue;
                    }
                }

                //Displays as a single text field.
                //Data is stored as a 2-element string array.
                else if (templateType == TemplateFieldType.MoneyUSD)
                {
                    //Gets the values of the currency.
                    string[] moneyData = (string[])fieldData;
                    string dollarAmount = moneyData[0];
                    string centsAmount = moneyData[1];

                    //If no dollars are given, displays a zero.
                    if (String.IsNullOrWhiteSpace(dollarAmount))
                    {
                        dollarAmount = "0";
                    }

                    //If no cents are given, displays a zero.
                    else if (String.IsNullOrWhiteSpace(centsAmount))
                    {
                        centsAmount = "00";
                    }

                    //Sets the textblock to display the money.
                    TextBlock fieldDataGui = new TextBlock();
                    fieldDataGui.FontFamily = new FontFamily(tFontFamilies);
                    fieldDataGui.Foreground = tContentColor;
                    fieldDataGui.Margin = new Thickness(2, 4, 2, 12);
                    fieldDataGui.MinWidth = 32;
                    fieldDataGui.Text = "$" + dollarAmount + "." + centsAmount;
                    fieldDataGui.TextWrapping = TextWrapping.Wrap;

                    //Does not display empty currency fields.
                    if (String.IsNullOrWhiteSpace(moneyData[0]) &&
                        String.IsNullOrWhiteSpace(moneyData[1]))
                    {
                        continue;
                    }

                    //Displays the title if intended.
                    if (tfTitleIsVisible)
                    {
                        elementsContainer.Children.Add(fieldNameGui);
                    }

                    //Hides the field if no information was originally given.
                    elementsContainer.Children.Add(fieldDataGui);
                }

                //Displays image-type fields.
                //Data is stored as a string of urls delimited by |.
                else if (templateType == TemplateFieldType.EntryImages ||
                    templateType == TemplateFieldType.Images)
                {
                    List<string> allData = new List<string>();
                    List<string> loadedUrls = new List<string>();
                    bool isAnimated = false;
                    bool isMuted = false;
                    var extraImagePos = (TemplateImagePos)(int)currTemplateField.GetData("extraImagePos");

                    //Loads the data if it exists, or sets it if empty.
                    if (((string)fieldData) == String.Empty)
                    {
                        allData = new List<string>() { "False", "False", String.Empty };
                    }
                    else
                    {
                        allData = ((string)fieldData).Split('|').ToList();
                    }

                    //Gets non-url data.
                    isAnimated = (allData[0] == "True");
                    isMuted = (allData[1] == "True");

                    //Gets url data.
                    loadedUrls = allData.GetRange(2, allData.Count - 2);

                    //Turns each relative url into an absolute one.
                    for (int j = 0; j < loadedUrls.Count; j++)
                    {
                        loadedUrls[j] = Utils.MakeAbsoluteUrl(projectUrl, loadedUrls[j]);
                    }

                    //If no urls are valid, skips the field.
                    if (!loadedUrls.Any((a) => { return File.Exists(a); }))
                    {
                        continue;
                    }

                    //Sets up a container for all elements.
                    if (tfTitleIsVisible)
                    {
                        elementsContainer.Children.Add(fieldNameGui);
                    }

                    //Still images that do not rotate and are not movies.
                    if (!isAnimated)
                    {
                        Grid imagesContainer = new Grid();

                        //Creates an image for each url.
                        for (int j = 0; j < loadedUrls.Count; j++)
                        {
                            ImgThumbnail thumbnail = new ImgThumbnail(loadedUrls[j]);

                            //Sets margins based on orientation.
                            if (tExtraImagePos == TemplateImagePos.Left ||
                                tExtraImagePos == TemplateImagePos.Right)
                            {
                                thumbnail.Margin = new Thickness(4, 2, 12, 2);
                            }
                            else
                            {
                                thumbnail.Margin = new Thickness(2, 4, 2, 12);
                            }

                            //Resizes the image.
                            thumbnail.Loaded += (a, b) =>
                            {
                                if (thumbnail.ActualWidth > 0)
                                {
                                    thumbnail.MaxWidth = thumbnail.GetSourceWidth();
                                    thumbnail.MaxHeight = thumbnail.GetSourceHeight();

                                    //Limits to 500px. Sets only one dimension
                                    //so the other can adapt automatically.
                                    if (thumbnail.MaxHeight > 500)
                                    {
                                        thumbnail.MaxHeight = 500;
                                    }
                                    else if (thumbnail.MaxWidth > 500)
                                    {
                                        thumbnail.MaxWidth = 500;
                                    }
                                }
                                else
                                {
                                    thumbnail.SetSize(0);
                                }
                            };

                            imagesContainer.Children.Add(thumbnail);

                            //Exits when 1 + number of extra images are displayed.
                            if (j == tNumExtraImages && tNumExtraImages > 0)
                            {
                                break;
                            }
                        }

                        //Reverses element order.
                        if (extraImagePos == TemplateImagePos.Above ||
                            extraImagePos == TemplateImagePos.Left)
                        {
                            List<UIElement> elements = new List<UIElement>();
                            for (int k = 0; k < imagesContainer.Children.Count; k++)
                            {
                                elements.Add(imagesContainer.Children[k]);
                            }
                            elements.Reverse();
                            imagesContainer.Children.Clear();
                            for (int k = 0; k < elements.Count; k++)
                            {
                                imagesContainer.Children.Add(elements[k]);
                            }
                        }

                        //Sets position of elements.
                        for (int k = 0; k < imagesContainer.Children.Count; k++)
                        {
                            var item = imagesContainer.Children[k];

                            if (extraImagePos == TemplateImagePos.Left ||
                                extraImagePos == TemplateImagePos.Right)
                            {
                                Grid.SetColumn(item, imagesContainer.ColumnDefinitions.Count);
                                imagesContainer.ColumnDefinitions.Add(new ColumnDefinition());
                            }
                            else
                            {
                                Grid.SetRow(item, imagesContainer.RowDefinitions.Count);
                                imagesContainer.RowDefinitions.Add(new RowDefinition());
                            }
                        }

                        ScrollViewer horzScroller = new ScrollViewer();
                        horzScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                        horzScroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                        horzScroller.Content = imagesContainer;

                        //Centers the scrollbar for new items.
                        horzScroller.Loaded += (a, b) =>
                        {
                            horzScroller.ScrollToHorizontalOffset(horzScroller.ScrollableWidth / 2);
                        };

                        //Sets the width and columns of the images container.
                        if ((templateType == TemplateFieldType.EntryImages) &&
                            tCenterImages)
                        {
                            AdjustWidths(horzScroller, false);
                            Grid.SetRow(horzScroller, 1);
                            gui.GuiItems.Children.Add(horzScroller);
                        }
                        else if (isFirstColumn)
                        {
                            AdjustWidths(horzScroller, tTwoColumns);
                            gui.LeftColItems.Children.Add(horzScroller);
                        }
                        else
                        {
                            AdjustWidths(horzScroller, tTwoColumns);
                            gui.RightColItems.Children.Add(horzScroller);
                        }

                        continue;
                    }

                    //Images that rotate or are movies.
                    else
                    {
                        MediaElement media = null;
                        ImgAnimated thumbnail = null;

                        //Loads movies.
                        if (loadedUrls.Count >= 1 &&
                            (loadedUrls[0].ToLower().EndsWith(".wmv") ||
                            loadedUrls[0].ToLower().EndsWith(".mp4")))
                        {
                            media = new MediaElement();
                            media.IsMuted = isMuted;
                            media.Margin = new Thickness(2, 4, 2, 12);

                            try
                            {
                                media.Source = new Uri(loadedUrls[0]);
                                media.MediaOpened += (a, b) =>
                                {
                                    media.MaxWidth = media.NaturalVideoWidth;
                                    media.MaxHeight = media.NaturalVideoHeight;
                                };
                                media.MediaEnded += (a, b) =>
                                {
                                    media.Position = new TimeSpan(0, 0, 1);
                                };
                            }
                            catch (InvalidOperationException) { } //Ignores loading errors.
                            catch (ArgumentNullException) { } //Ignores loading errors.
                            catch (UriFormatException) { } //Ignores loading errors.
                            catch (Exception e) //Logs unknown errors.
                            {
                                Utils.Log("While loading media: " + e.Message);
                            }
                        }

                        //Loads rotating images.
                        else
                        {
                            thumbnail = new ImgAnimated(loadedUrls, true);
                            thumbnail.SetPlaybackDelay(1000);
                            thumbnail.Margin = new Thickness(2, 4, 2, 12);
                            thumbnail.MaxWidth = thumbnail.GetSourceWidth();
                            thumbnail.MaxHeight = thumbnail.GetSourceHeight();
                        }

                        if (media != null)
                        {
                            elementsContainer.Children.Add(media);
                        }
                        else
                        {
                            elementsContainer.Children.Add(thumbnail);
                        }
                    }
                }

                //Sets the width and columns of the element container.
                AdjustWidths(elementsContainer, tTwoColumns);
                if (templateType == TemplateFieldType.EntryImages && tCenterImages)
                {
                    Grid.SetRow(elementsContainer, 1);
                    gui.GuiItems.Children.Add(elementsContainer);
                }
                else if (isFirstColumn)
                {
                    gui.LeftColItems.Children.Add(elementsContainer);
                }
                else
                {
                    gui.RightColItems.Children.Add(elementsContainer);
                }
            }

            //If there is nothing to display, shows a message saying so.
            if (gui.GuiItems.Children.Count == 2 &&
                gui.LeftColItems.Children.Count == 0 &&
                gui.RightColItems.Children.Count == 0)
            {
                //Creates the message as a textblock.
                TextBlock emptyMessage = new TextBlock();
                emptyMessage.Text = GlobalStrings.HintNoContent;
                emptyMessage.HorizontalAlignment = HorizontalAlignment.Center;
                emptyMessage.VerticalAlignment = VerticalAlignment.Center;
                emptyMessage.Foreground = Brushes.LightGray;

                //Adds the message to the center of the grid.
                Grid.SetRow(emptyMessage, 1);
                gui.GuiItems.Children.Add(emptyMessage);
            }
        }

        /// <summary>
        /// Sets the width of the given element to adjust automatically with
        /// respect to the template layout.
        /// </summary>
        /// <param name="element">
        /// An element to bind the width of.
        /// </param>
        /// <param name="useTwoColumns">
        /// Whether a two-column layout is used or not.
        /// </param>
        private void AdjustWidths(FrameworkElement element, bool useTwoColumns)
        {
            //Evaluates the max width when the layout updates.
            gui.TxtblkEntryName.LayoutUpdated += (a, b) =>
            {
                if (useTwoColumns)
                {
                    element.MaxWidth = gui.TxtblkEntryName.ActualWidth / 2;
                }
                else
                {
                    element.MaxWidth = gui.TxtblkEntryName.ActualWidth;
                }
            };

            //Updates the layout immediately.
            if (useTwoColumns)
            {
                element.MaxWidth = gui.TxtblkEntryName.ActualWidth / 2;
            }
            else
            {
                element.MaxWidth = gui.TxtblkEntryName.ActualWidth;
            }
        }
        #endregion
    }
}