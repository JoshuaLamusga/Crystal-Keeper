using CrystalKeeper.Core;
using CrystalKeeper.GuiCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace CrystalKeeper.Gui
{
    class PgEntryEdit
    {
        #region Members
        /// <summary>
        /// The project associated with the page.
        /// </summary>
        private Project project;

        /// <summary>
        /// The entry to be used.
        /// </summary>
        private DataItem entry;

        /// <summary>
        /// The underlying gui.
        /// </summary>
        private PgEntryGuiEdit gui;

        /// <summary>
        /// Stores the currently active field.
        /// </summary>
        private LstbxDataItem activeEntry;

        /// <summary>
        /// Stores the location that the project is saved at.
        /// </summary>
        private string projectUrl;

        /// <summary>
        /// Fires when project data is changed.
        /// </summary>
        public event EventHandler DataNameChanged;

        /// <summary>
        /// Fires to reconstruct the view from the parent control.
        /// </summary>
        public event EventHandler InvalidatePage;
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
        public PgEntryGuiEdit Gui
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
        /// The collection to be used.
        /// </summary>
        public DataItem Collection
        {
            set
            {
                entry = value;
            }
            get
            {
                return entry;
            }
        }

        /// <summary>
        /// Stores the currently active entry reference.
        /// </summary>
        private LstbxDataItem ActiveEntry
        {
            set
            {
                activeEntry = value;
            }
            get
            {
                return activeEntry;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a grouping page from the given project.
        /// </summary>
        /// <param name="project">
        /// The project given.
        /// </param>
        /// <param name="entryRef">
        /// A reference to the entry to be edited.
        /// </param>
        /// <param name="projectUrl">
        /// The saved location so relative urls can be appended. Absolute urls
        /// will be loaded without being appended.
        /// </param>
        public PgEntryEdit(Project project, DataItem entryRef, string projectUrl)
        {
            this.project = project;
            this.projectUrl = projectUrl;
            entry = project.GetEntryRefEntry(entryRef);
            activeEntry = null;
            ConstructPage();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the page content based on existing material.
        /// </summary>
        public void ConstructPage()
        {
            gui = new PgEntryGuiEdit();

            #region Entry name
            //Sets the entry name.
            if (string.IsNullOrWhiteSpace((string)entry.GetData("name")))
            {
                gui.TxtbxEntryName.Text = GlobalStrings.NameUntitled;
            }
            else
            {
                gui.TxtbxEntryName.Text = (string)entry.GetData("name");
            }

            //Handles changes to the entry name.
            gui.TxtbxEntryName.TextChanged += new TextChangedEventHandler((a, b) =>
            {
                if (!string.IsNullOrWhiteSpace(gui.TxtbxEntryName.Text))
                {
                    entry.SetData("name", gui.TxtbxEntryName.Text);
                }

                //If the textbox is empty, it will keep the last character.
                gui.TxtbxEntryName.Text = (string)entry.GetData("name");

                DataNameChanged?.Invoke(this, null);
            });
            #endregion

            #region Mineral Suggestion Lists
            //Reads mineral names, group classifications, formulas, localities,
            //and whether the mineral is an official IMA mineral from the
            //mineral list. Splits into tuples for the searchbox.
            var minInfo = Properties.Resources.Minerals.Split('\n').ToList();
            var minNames = new List<Tuple<string, string>>();
            var minGroups = new List<Tuple<string, string>>();
            var minFormulas = new List<Tuple<string, string>>();
            var minLocalities = new List<Tuple<string, string>>();
            var minIsReal = new List<bool>();
            for (int i = 0; i < minInfo.Count; i++)
            {
                string[] mineralParts = minInfo[i].Split('|');
                if (mineralParts.Length >= 5)
                {
                    minNames.Add(new Tuple<string, string>
                        (mineralParts[0], null));
                    if (mineralParts[1] != String.Empty)
                    {
                        minGroups.Add(new Tuple<string, string>
                            (mineralParts[0], mineralParts[1]));
                    }
                    if (mineralParts[2] != String.Empty)
                    {
                        minFormulas.Add(new Tuple<string, string>
                            (mineralParts[0], mineralParts[2]));
                    }
                    if (mineralParts[3] != String.Empty)
                    {
                        minLocalities.Add(new Tuple<string, string>
                            (mineralParts[0], mineralParts[3]));
                    }

                    minIsReal.Add(mineralParts[4].StartsWith("1"));
                }
            }
            #endregion

            //Generates the view based on the collection template.
            var template = project.GetCollectionTemplate(project.GetEntryCollection(entry));
            var tCenterImages = (bool)template.GetData("centerImages");
            var tTwoColumns = (bool)template.GetData("twoColumns");

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

            //Displays each field for editing.
            for (int i = 0; i < entryFields.Count; i++)
            {
                //Gets the field and its data.
                var field = entryFields[i];
                object fieldData = field.GetData("data");

                //Gets the type of data and associated grid position.
                var currTemplateField = project.GetFieldTemplateField(field);
                var fieldName = (string)currTemplateField.GetData("name");
                var templateType = (TemplateFieldType)(int)currTemplateField.GetData("dataType");
                var isFirstColumn = (bool)project.GetItemByGuid(
                    (ulong)currTemplateField.GetData("refGuid")).GetData("isFirstColumn");

                //Skips all 2nd-column fields in a single-column layout.
                if (!tTwoColumns && !isFirstColumn)
                {
                    continue;
                }

                //Sets the name of the field.
                TextBlock fieldNameGui = new TextBlock(new Run(fieldName));
                fieldNameGui.FontWeight = FontWeights.Bold;

                //Centers the field name to match centered image fields.
                if (templateType == TemplateFieldType.EntryImages && tCenterImages)
                {
                    fieldNameGui.HorizontalAlignment = HorizontalAlignment.Center;
                }

                //Sets up the container for the whole field.
                StackPanel elementsContainer = new StackPanel();

                //Displays text fields.
                //Text is stored in the binary XamlPackage format.
                if (templateType == TemplateFieldType.Text)
                {
                    RichTextEditor textEditor = new RichTextEditor();
                    textEditor.Gui.Margin = new Thickness(0, 4, 0, 12);
                    textEditor.Gui.MinWidth = 32;
                    if (fieldData is byte[])
                    {
                        textEditor.LoadData((byte[])fieldData);
                    }

                    //Enables the data to be changed.
                    textEditor.Gui.Textbox.TextChanged += (a, b) =>
                    {
                        field.SetData("data", textEditor.SaveData());
                    };

                    elementsContainer.Children.Add(fieldNameGui);
                    elementsContainer.Children.Add(textEditor.Gui);
                }

                //Displays hyperlinks.
                //Text is stored as a string.
                 if (templateType == TemplateFieldType.Hyperlink)
                {
                    TextBox fieldDataGui = new TextBox();
                    fieldDataGui.AcceptsReturn = true;
                    fieldDataGui.Margin = new Thickness(2, 4, 2, 0);
                    fieldDataGui.MinWidth = 32;
                    fieldDataGui.Text = (string)fieldData;
                    fieldDataGui.TextWrapping = TextWrapping.Wrap;

                    //Enables the data to be changed.
                    fieldDataGui.TextChanged += (a, b) =>
                    {
                        field.SetData("data", fieldDataGui.Text);
                    };

                    elementsContainer.Children.Add(fieldNameGui);
                    elementsContainer.Children.Add(fieldDataGui);
                }

                //Displays text with mineral suggestions.
                //Text is stored as a string.
                else if (templateType == TemplateFieldType.Min_Name ||
                    templateType == TemplateFieldType.Min_Formula ||
                    templateType == TemplateFieldType.Min_Group ||
                    templateType == TemplateFieldType.Min_Locality)
                {
                    SearchBox fieldDataGui = null;
                    if (templateType == TemplateFieldType.Min_Name)
                    {
                        fieldDataGui = new SearchBox(minNames);
                    }
                    if (templateType == TemplateFieldType.Min_Formula)
                    {
                        fieldDataGui = new SearchBox(minFormulas);
                    }
                    if (templateType == TemplateFieldType.Min_Group)
                    {
                        fieldDataGui = new SearchBox(minGroups);
                    }
                    if (templateType == TemplateFieldType.Min_Locality)
                    {
                        fieldDataGui = new SearchBox(minLocalities);
                    }

                    fieldDataGui.Gui.Margin = new Thickness(2, 4, 2, 0);
                    fieldDataGui.Gui.textbox.MinWidth = 32;
                    fieldDataGui.Gui.textbox.Text = (string)fieldData;
                    fieldDataGui.Gui.textbox.TextWrapping = TextWrapping.Wrap;
                    fieldDataGui.SearchByWord = true;
                    fieldDataGui.HideSuggestions();

                    //Enables the data to be changed.
                    fieldDataGui.Gui.textbox.TextChanged += (a, b) =>
                    {
                        field.SetData("data", fieldDataGui.Gui.textbox.Text);
                    };

                    //Decorates real minerals in italic.
                    fieldDataGui.MenuItemAdded += (a) =>
                    {
                        int pos = minNames.FindIndex(
                            (b) => b.Item1 == (string)a.Content);

                        if (pos != -1 && minIsReal[pos])
                        {
                            a.FontStyle = FontStyles.Italic;
                        }
                    };

                    elementsContainer.Children.Add(fieldNameGui);
                    elementsContainer.Children.Add(fieldDataGui.Gui);
                }

                //Displays money in the USD format.
                //Data is stored as a 2-element string array.
                else if (templateType == TemplateFieldType.MoneyUSD)
                {
                    var data = (string[])fieldData;

                    //Sets up the first textbox.
                    TextBox fieldData1Gui = new TextBox();
                    fieldData1Gui.MinWidth = 32;
                    fieldData1Gui.Text = data[0];
                    fieldData1Gui.TextWrapping = TextWrapping.Wrap;

                    //Enables the data to be changed.
                    fieldData1Gui.TextChanged += (a, b) =>
                    {
                        //Filters non-digit characters.
                        string replacement = Regex.Replace(fieldData1Gui.Text, @"\D*", String.Empty);
                        if (!fieldData1Gui.Text.Equals(replacement))
                        {
                            fieldData1Gui.Text = replacement;
                        }

                        var strings = (string[])field.GetData("data");
                        strings[0] = fieldData1Gui.Text;
                    };

                    //Sets up the second textbox.
                    TextBox fieldData2Gui = new TextBox();
                    fieldData2Gui.MinWidth = 32;
                    fieldData2Gui.Text = data[1];
                    fieldData2Gui.TextWrapping = TextWrapping.Wrap;

                    //Enables the data to be changed.
                    fieldData2Gui.TextChanged += (a, b) =>
                    {
                        //Filters non-digit characters.
                        string replacement = Regex.Replace(fieldData2Gui.Text, @"\D*", String.Empty);
                        if (!fieldData2Gui.Text.Equals(replacement))
                        {
                            fieldData2Gui.Text = replacement;
                        }

                        var strings = (string[])field.GetData("data");
                        strings[1] = fieldData2Gui.Text;
                    };

                    WrapPanel fieldsContainer = new WrapPanel();
                    fieldsContainer.Margin = new Thickness(2, 4, 2, 0);
                    fieldsContainer.Orientation = Orientation.Horizontal;
                    fieldsContainer.Children.Add(new TextBlock(new Run("$")));
                    fieldsContainer.Children.Add(fieldData1Gui);
                    fieldsContainer.Children.Add(new TextBlock(new Run(" . ")));
                    fieldsContainer.Children.Add(fieldData2Gui);
                    elementsContainer.Children.Add(fieldNameGui);
                    elementsContainer.Children.Add(fieldsContainer);
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
                        allData = new List<string>() { "False", String.Empty };
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

                    //Adds an extra image slot if one doesn't exist.
                    if (loadedUrls.LastOrDefault().Trim() != String.Empty)
                    {
                        loadedUrls.Add(String.Empty);
                    }

                    //Sets up a container for all elements.
                    elementsContainer.Children.Add(fieldNameGui);

                    //Sets the orientation to be horizontal if chosen.
                    if (extraImagePos == TemplateImagePos.Left ||
                        extraImagePos == TemplateImagePos.Right)
                    {
                        elementsContainer.Orientation = Orientation.Horizontal;
                    }

                    if (!isAnimated)
                    {
                        Grid imagesContainer = new Grid();

                        //Creates an image for each url.
                        for (int j = 0; j < loadedUrls.Count; j++)
                        {
                            ImgThumbnail thumbnail = new ImgThumbnail(loadedUrls[j], false);
                            bool isUrlValid = true;

                            //Sets margins based on orientation.
                            if (extraImagePos == TemplateImagePos.Left ||
                                extraImagePos == TemplateImagePos.Right)
                            {
                                thumbnail.Margin = new Thickness(4, 2, 12, 2);
                            }
                            else
                            {
                                thumbnail.Margin = new Thickness(2, 4, 2, 12);
                            }

                            int index = j; //For lambda capture.

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
                                    isUrlValid = (thumbnail.ImgUrl != String.Empty);
                                }
                            };

                            StackPanel contentControls = CreateImageControls(
                                field, loadedUrls, index, isAnimated, isMuted);

                            //Centers the content controls to match centered image fields.
                            if (templateType == TemplateFieldType.EntryImages && tCenterImages)
                            {
                                contentControls.HorizontalAlignment = HorizontalAlignment.Center;
                            }

                            StackPanel imageControls = new StackPanel();
                            imageControls.Children.Add(contentControls);
                            imageControls.Children.Add(thumbnail);

                            imagesContainer.Children.Add(imageControls);

                            //Waits until the image is fully loaded.
                            thumbnail.Loaded += (a, b) =>
                            {

                                if (thumbnail.ActualWidth <= 0 &&
                                    thumbnail.ImgUrl != String.Empty)
                                {
                                    //Sets up a broken image button.
                                    var bttnBrokenImage = new Image();
                                    var newImg = new BitmapImage(new Uri(Assets.BrokenImage));
                                    bttnBrokenImage.Source = newImg;
                                    bttnBrokenImage.MaxWidth = newImg.Width;
                                    bttnBrokenImage.MaxHeight = newImg.Height;
                                    bttnBrokenImage.ToolTip = GlobalStrings.TipBrokenImage;

                                    //Allows user to select a new image url.
                                    bttnBrokenImage.MouseDown += (c, d) =>
                                    {
                                        OpenFileDialog dlg = new OpenFileDialog();

                                        //Opens to the location of the missing image.
                                        string dirName = Path.GetDirectoryName(
                                            Path.GetFullPath(thumbnail.ImgUrl));
                                        if (Directory.Exists(dirName))
                                        {
                                            dlg.InitialDirectory = dirName;
                                        }
                                        dlg.FileName = Path.GetFileName(thumbnail.ImgUrl);

                                        dlg.CheckPathExists = true;
                                        dlg.Filter = GlobalStrings.FilterImages;
                                        dlg.FilterIndex = 0;
                                        dlg.Title = GlobalStrings.CaptionLoadImage;

                                        if (dlg.ShowDialog() == true)
                                        {
                                            loadedUrls[index] = dlg.FileName;
                                            string options = (isAnimated) ? "True" : "False";
                                            options += "|" + ((isMuted) ? "True" : "False");
                                            string newData = string.Join("|", loadedUrls);
                                            newData = options + "|" + newData;
                                            field.SetData("data", newData);

                                            //Invalidates the page to update.
                                            InvalidatePage?.Invoke(this, null);
                                        }
                                    };

                                    imageControls.Children.Remove(thumbnail);
                                    imageControls.Children.Add(bttnBrokenImage);
                                }
                            };
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
                                //Centers controls that support it.
                                if (typeof(FrameworkElement).IsAssignableFrom(item.GetType()))
                                {
                                    ((FrameworkElement)item).VerticalAlignment = VerticalAlignment.Center;
                                }

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
                        horzScroller.Focusable = false;
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
                    else
                    {
                        MediaElement media = null;
                        ImgAnimated thumbnail = null;

                        if (loadedUrls.Count >= 1 &&
                            (loadedUrls[0].ToLower().EndsWith(".wmv") ||
                            loadedUrls[0].ToLower().EndsWith(".mp4")))
                        {
                            media = new MediaElement();
                            media.IsMuted = isMuted;

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
                            catch (Exception)
                            {
                                MessageBox.Show(GlobalStrings.DlgMediaNotLoadedWarning);

                                loadedUrls.RemoveAt(0);

                                string options = (isAnimated) ? "True" : "False";
                                options += "|" + ((isMuted) ? "True" : "False");
                                string newData = string.Join("|", loadedUrls);
                                newData = options + "|" + newData;
                                field.SetData("data", newData);

                                InvalidatePage?.Invoke(this, null);
                                return;
                            }
                        }
                        else
                        {
                            thumbnail = new ImgAnimated(loadedUrls, true);
                            thumbnail.SetPlaybackDelay(1000);
                            thumbnail.MaxWidth = thumbnail.GetSourceWidth();
                            thumbnail.MaxHeight = thumbnail.GetSourceHeight();
                        }

                        StackPanel contentControls = CreateImageControls(
                            field, loadedUrls, 0, isAnimated, isMuted);

                        //Centers the content controls to match centered image fields.
                        if (templateType == TemplateFieldType.EntryImages && tCenterImages)
                        {
                            contentControls.HorizontalAlignment = HorizontalAlignment.Center;
                        }

                        StackPanel imageControls = new StackPanel();
                        imageControls.Children.Add(contentControls);
                        if (media != null)
                        {
                            imageControls.Children.Add(media);
                        }
                        else
                        {
                            imageControls.Children.Add(thumbnail);
                        }

                        elementsContainer.Children.Add(imageControls);
                    }
                }

                //Sets the width and columns of the element container.
                AdjustWidths(elementsContainer, tTwoColumns);
                if ((templateType == TemplateFieldType.EntryImages) &&
                    tCenterImages)
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
        }

        /// <summary>
        /// Creates and returns an image controls bar for an image.
        /// </summary>
        private StackPanel CreateImageControls(
            DataItem field,
            List<string> urls,
            int urlIndex,
            bool isAnimatedMedia,
            bool isMuted)
        {
            //Sets up an upload image button.
            var bttnUpload = new Image();
            BitmapImage newImg = new BitmapImage(new Uri(Assets.BttnAddStill));
            bttnUpload.Source = newImg;
            bttnUpload.ToolTip = GlobalStrings.TipBttnUpload;

            //Sets up an upload movie button.
            var bttnUploadMovie = new Image();
            newImg = new BitmapImage(new Uri(Assets.BttnAddMovie));
            bttnUploadMovie.Source = newImg;
            bttnUploadMovie.ToolTip = GlobalStrings.TipBttnUploadMovie;

            //Sets up a delete image button.
            var bttnDelete = new Image();
            newImg = new BitmapImage(new Uri(Assets.BttnDelete));
            bttnDelete.Source = newImg;
            bttnDelete.ToolTip = GlobalStrings.TipBttnDelete;

            //Sets up a mute button.
            var bttnVolume = new Image();
            newImg = (isMuted) ?
                new BitmapImage(new Uri(Assets.BttnVolumeOff)) :
                new BitmapImage(new Uri(Assets.BttnVolumeOn));
            bttnVolume.Source = newImg;
            bttnVolume.ToolTip = GlobalStrings.TipBttnMute;
            
            //Disables the delete button if there's nothing to delete.
            if (!File.Exists(urls[urlIndex]))
            {
                bttnDelete.IsEnabled = false;
                bttnDelete.Opacity = 0.5;
            }

            //Highlights the upload button when the mouse enters image bounds.
            bttnUpload.MouseEnter += (a, b) =>
            {
                newImg = new BitmapImage();
                newImg.BeginInit();
                newImg.UriSource = new Uri(Assets.BttnAddHover);
                newImg.EndInit();
                bttnUpload.Source = newImg;
            };

            //Un-highlights the upload button when the mouse leaves image bounds.
            bttnUpload.MouseLeave += (a, b) =>
            {
                newImg = new BitmapImage();
                newImg.BeginInit();
                newImg.UriSource = new Uri(Assets.BttnAddStill);
                newImg.EndInit();
                bttnUpload.Source = newImg;
            };

            //Handles uploading images.
            bttnUpload.MouseDown += (a, b) =>
            {
                OpenFileDialog dlg = new OpenFileDialog();

                //Opens the directory of the first url.
                if (File.Exists(urls.First()))
                {
                    dlg.InitialDirectory = urls.First();
                }

                dlg.Multiselect = true;
                dlg.CheckPathExists = true;
                dlg.Filter = GlobalStrings.FilterImages;
                dlg.FilterIndex = 0;
                dlg.Title = GlobalStrings.CaptionLoadImage;

                if (dlg.ShowDialog() == true)
                {
                    //Ensures no file is larger than 2GB.
                    for (int k = 0; k < dlg.FileNames.Length; k++)
                    {
                        if (new FileInfo(dlg.FileNames[k]).Length >= 2147000000)
                        {
                            MessageBox.Show(
                                GlobalStrings.DlgImageTooBigWarning,
                                GlobalStrings.DlgImageTooBigCaption);

                            return;
                        }
                    }

                    //Replaces all images for multi-image uploads.
                    if (dlg.FileNames.Length > 1)
                    {
                        urls.Clear();
                    }

                    //Sets or adds images to the list.
                    for (int k = 0; k < dlg.FileNames.Length; k++)
                    {
                        if (k == 0 && dlg.FileNames.Length == 1)
                        {
                            urls[urlIndex] = dlg.FileNames[k];
                        }
                        else
                        {
                            urls.Add(dlg.FileNames[k]);
                        }
                    }

                    string newData = string.Join("|", urls);
                    newData = "False|False|" + newData;
                    field.SetData("data", newData);

                    //Redraws the page.
                    InvalidatePage?.Invoke(this, null);
                }
            };

            //Highlights the upload movie button when the mouse enters image bounds.
            bttnUploadMovie.MouseEnter += (a, b) =>
            {
                newImg = new BitmapImage();
                newImg.BeginInit();
                newImg.UriSource = new Uri(Assets.BttnAddMovieHover);
                newImg.EndInit();
                bttnUploadMovie.Source = newImg;
            };

            //Un-highlights the upload button when the mouse leaves image bounds.
            bttnUploadMovie.MouseLeave += (a, b) =>
            {
                newImg = new BitmapImage();
                newImg.BeginInit();
                newImg.UriSource = new Uri(Assets.BttnAddMovie);
                newImg.EndInit();
                bttnUploadMovie.Source = newImg;
            };

            //Handles uploading movies and multiple images.
            bttnUploadMovie.MouseDown += (a, b) =>
            {
                OpenFileDialog dlg = new OpenFileDialog();

                //Opens the directory of the first url.
                if (File.Exists(urls.First()))
                {
                    dlg.InitialDirectory = urls.First();
                }

                dlg.Multiselect = true;
                dlg.CheckPathExists = true;
                dlg.Filter = GlobalStrings.FilterImagesOrMovie;
                dlg.FilterIndex = 0;
                dlg.Title = GlobalStrings.CaptionLoadImagesOrMovie;

                if (dlg.ShowDialog() == true)
                {
                    //Ensures no file is larger than 2GB.
                    for (int k = 0; k < dlg.FileNames.Length; k++)
                    {
                        if (new FileInfo(dlg.FileNames[k]).Length >= 2147000000)
                        {
                            MessageBox.Show(
                                GlobalStrings.DlgImageTooBigWarning,
                                GlobalStrings.DlgImageTooBigCaption);

                            return;
                        }
                    }

                    string newData = string.Join("|", dlg.FileNames);
                    newData = "True|False|" + newData;
                    field.SetData("data", newData);

                    //Redraws the page.
                    InvalidatePage?.Invoke(this, null);
                }
            };

            //Highlights the delete button when the mouse enters image bounds.
            bttnDelete.MouseEnter += (a, b) =>
            {
                newImg = new BitmapImage();
                newImg.BeginInit();
                newImg.UriSource = new Uri(Assets.BttnDeleteHover);
                newImg.EndInit();
                bttnDelete.Source = newImg;
            };

            //Un-highlights the delete button when the mouse leaves image bounds.
            bttnDelete.MouseLeave += (a, b) =>
            {
                newImg = new BitmapImage();
                newImg.BeginInit();
                newImg.UriSource = new Uri(Assets.BttnDelete);
                newImg.EndInit();
                bttnDelete.Source = newImg;
            };

            //Prompts to delete the existing image.
            bttnDelete.MouseDown += (a, b) =>
            {
                var mssgResult = MessageBox.Show(
                    GlobalStrings.DlgDeleteImageWarning,
                    GlobalStrings.DlgDeleteImageCaption,
                    MessageBoxButton.YesNo);

                if (mssgResult == MessageBoxResult.Yes)
                {
                    //Clears the url(s) of an animation or movie.
                    if (isAnimatedMedia)
                    {
                        urls.Clear();
                    }

                    //Removes a url for an image.
                    else
                    {
                        urls.RemoveAt(urlIndex);
                    }

                    string newData = string.Join("|", urls);
                    newData = "False|False|" + newData;
                    field.SetData("data", newData);

                    //Redraws the page.
                    InvalidatePage?.Invoke(this, null);
                }
            };

            //Highlights the delete button when the mouse enters image bounds.
            bttnVolume.MouseEnter += (a, b) =>
            {
                newImg = new BitmapImage();
                newImg.BeginInit();
                newImg.UriSource = (isMuted) ?
                    new Uri(Assets.BttnVolumeOffHover) :
                    new Uri(Assets.BttnVolumeOnHover);
                newImg.EndInit();
                bttnVolume.Source = newImg;
            };

            //Un-highlights the delete button when the mouse leaves image bounds.
            bttnVolume.MouseLeave += (a, b) =>
            {
                newImg = new BitmapImage();
                newImg.BeginInit();
                newImg.UriSource = (isMuted) ?
                    new Uri(Assets.BttnVolumeOff) :
                    new Uri(Assets.BttnVolumeOn);
                newImg.EndInit();
                bttnVolume.Source = newImg;
            };

            //Prompts to delete the existing image.
            bttnVolume.MouseDown += (a, b) =>
            {
                string newData = string.Join("|", urls);
                newData = isAnimatedMedia + "|" + !isMuted + "|" + newData;
                field.SetData("data", newData);

                //Redraws the page.
                InvalidatePage?.Invoke(this, null);
            };

            StackPanel contentControls = new StackPanel();
            contentControls.Orientation = Orientation.Horizontal;
            contentControls.Children.Add(bttnDelete);
            contentControls.Children.Add(bttnUpload);
            contentControls.Children.Add(bttnUploadMovie);
            if (isAnimatedMedia)
            {
                contentControls.Children.Add(bttnVolume);
            }
            return contentControls;
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
            gui.TxtbxEntryName.LayoutUpdated += (a, b) =>
            {
                if (useTwoColumns)
                {
                    element.MaxWidth = gui.TxtbxEntryName.ActualWidth / 2;
                }
                else
                {
                    element.MaxWidth = gui.TxtbxEntryName.ActualWidth;
                }
            };

            //Updates the layout immediately.
            if (useTwoColumns)
            {
                element.MaxWidth = gui.TxtbxEntryName.ActualWidth / 2;
            }
            else
            {
                element.MaxWidth = gui.TxtbxEntryName.ActualWidth;
            }
        }
        #endregion
    }
}