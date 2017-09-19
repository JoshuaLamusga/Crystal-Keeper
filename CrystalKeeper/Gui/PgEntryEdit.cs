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
        /// TODO: Find a better way to get an image to refresh dynamically...
        /// </summary>
        public event EventHandler InvalidateEntirePage;
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
                gui.TxtbxEntryName.Text = "Untitled";
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
                    if (mineralParts[1] != "")
                    {
                        minGroups.Add(new Tuple<string, string>
                            (mineralParts[0], mineralParts[1]));
                    }
                    if (mineralParts[2] != "")
                    {
                        minFormulas.Add(new Tuple<string, string>
                            (mineralParts[0], mineralParts[2]));
                    }
                    if (mineralParts[3] != "")
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
            var tExtraImagePos = (TemplateImagePos)(int)template.GetData("extraImagePos");

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
                //Data is stored as a string.
                if (templateType == TemplateFieldType.Text ||
                    templateType == TemplateFieldType.Hyperlink)
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

                //Displays text with mineral name suggestions.
                //Text is stored as a string.
                else if (templateType == TemplateFieldType.Text_Minerals)
                {
                    SearchBox fieldDataGui = new SearchBox(minNames);
                    fieldDataGui.Gui.Margin = new Thickness(2, 4, 2, 0);
                    fieldDataGui.Gui.textbox.MinWidth = 32;
                    fieldDataGui.Gui.textbox.Text = (string)fieldData;
                    fieldDataGui.Gui.textbox.TextWrapping = TextWrapping.Wrap;
                    fieldDataGui.SearchByWord = true;

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

                //Displays text with mineral formula suggestions.
                //Text is stored as a string.
                else if (templateType == TemplateFieldType.Text_Formula)
                {
                    SearchBox fieldDataGui = new SearchBox(minFormulas);
                    fieldDataGui.Gui.Margin = new Thickness(2, 4, 2, 0);
                    fieldDataGui.Gui.textbox.MinWidth = 32;
                    fieldDataGui.Gui.textbox.Text = (string)fieldData;
                    fieldDataGui.Gui.textbox.TextWrapping = TextWrapping.Wrap;
                    fieldDataGui.SearchByWord = true;

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
                        string replacement = Regex.Replace(fieldData1Gui.Text, @"\D*", "");
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
                        string replacement = Regex.Replace(fieldData2Gui.Text, @"\D*", "");
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

                    //Loads the data if it exists, or sets it if empty.
                    if (((string)fieldData) == "")
                    {
                        allData = new List<string>() { "False", "" };
                    }
                    else
                    {
                        allData = ((string)fieldData).Split('|').ToList();
                    }

                    //Gets whether the image is animated or not.
                    string isAnimatedStr = allData[0];
                    if (isAnimatedStr == "True")
                    {
                        isAnimated = true;
                    }

                    //Gets url data.
                    loadedUrls = allData.GetRange(1, allData.Count - 1);

                    //Turns each relative url into an absolute one.
                    for (int j = 0; j < loadedUrls.Count; j++)
                    {
                        loadedUrls[j] = Utils.MakeAbsoluteUrl(projectUrl, loadedUrls[j]);
                    }

                    //Adds an extra image slot if one doesn't exist.
                    if (loadedUrls.LastOrDefault().Trim() != "")
                    {
                        loadedUrls.Add("");
                    }

                    //Sets up a container for all elements.
                    elementsContainer.Children.Add(fieldNameGui);

                    //Sets the orientation to be horizontal if chosen.
                    if (tExtraImagePos == TemplateImagePos.Left ||
                        tExtraImagePos == TemplateImagePos.Right)
                    {
                        elementsContainer.Orientation = Orientation.Horizontal;
                    }

                    //Sets up the context menu for the image options.
                    ContextMenu menu = new ContextMenu();
                    MenuItem animatable = new MenuItem();
                    animatable.Header = "Rotate through images?";
                    animatable.IsCheckable = true;
                    animatable.IsChecked = isAnimated;
                    menu.Items.Add(animatable);
                    elementsContainer.ContextMenu = menu;

                    //Clicking the animated option toggles it and refreshes.
                    animatable.Click += (a, b) =>
                    {
                        string options = (!isAnimated) ? "True" : "False";
                        string newData = options + "|" + string.Join("|", loadedUrls);
                        field.SetData("data", newData);

                        InvalidateEntirePage?.Invoke(this, null);
                    };

                    if (!isAnimated)
                    {
                        //Creates an image for each url.
                        for (int j = 0; j < loadedUrls.Count; j++)
                        {
                            ImgThumbnail thumbnail = new ImgThumbnail(loadedUrls[j], false);

                            //Resizes the image.
                            thumbnail.Loaded += (a, b) =>
                            {
                                if (thumbnail.ActualWidth > 0)
                                {
                                    thumbnail.SetSize(150);
                                }
                                else
                                {
                                    thumbnail.SetSize(0);
                                }
                            };

                            int index = j; //For lambda capture.

                            //Sets up an upload image button.
                            var bttnUpload = new Image();
                            BitmapImage newImg = new BitmapImage();
                            newImg.BeginInit();
                            newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnAdd.png");
                            newImg.EndInit();
                            bttnUpload.Source = newImg;
                            bttnUpload.ToolTip = "Click to select images.";

                            //Sets up a delete image button.
                            var bttnDelete = new Image();
                            newImg = new BitmapImage();
                            newImg.BeginInit();
                            newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnDelete.png");
                            newImg.EndInit();
                            bttnDelete.Source = newImg;
                            bttnDelete.ToolTip = "Click to delete all images.";

                            //Disables the delete button if there's nothing to delete.
                            if (!File.Exists(loadedUrls[j]))
                            {
                                bttnDelete.IsEnabled = false;
                                bttnDelete.Opacity = 0.5;
                            }

                            //Highlights the upload button when the mouse enters image bounds.
                            bttnUpload.MouseEnter += (a, b) =>
                            {
                                newImg = new BitmapImage();
                                newImg.BeginInit();
                                newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnAddHover.png");
                                newImg.EndInit();
                                bttnUpload.Source = newImg;
                            };

                            //Un-highlights the upload button when the mouse leaves image bounds.
                            bttnUpload.MouseLeave += (a, b) =>
                            {
                                newImg = new BitmapImage();
                                newImg.BeginInit();
                                newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnAdd.png");
                                newImg.EndInit();
                                bttnUpload.Source = newImg;
                            };

                            //Handles uploading an image to change it.
                            bttnUpload.MouseDown += (a, b) =>
                            {
                                OpenFileDialog dlg = new OpenFileDialog();

                                //Opens the directory of the first url.
                                if (File.Exists(loadedUrls.First()))
                                {
                                    dlg.InitialDirectory = loadedUrls.First();
                                }

                                dlg.CheckPathExists = true;
                                dlg.Filter = "images|*.bmp;*.jpg;*.jpeg;*.gif;*.tif;*.tiff;*.png";
                                dlg.FilterIndex = 0;
                                dlg.Title = "Load image";

                                if (dlg.ShowDialog() == true)
                                {
                                    loadedUrls[index] = dlg.FileName;
                                    string options = (isAnimated) ? "True" : "False";
                                    string newData = string.Join("|", loadedUrls);
                                    newData = options + "|" + newData;
                                    field.SetData("data", newData);

                                    //TODO: Invalidates the page to update.
                                    InvalidateEntirePage?.Invoke(this, null);
                                }
                            };

                            //Highlights the delete button when the mouse enters image bounds.
                            bttnDelete.MouseEnter += (a, b) =>
                            {
                                newImg = new BitmapImage();
                                newImg.BeginInit();
                                newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnDeleteHover.png");
                                newImg.EndInit();
                                bttnDelete.Source = newImg;
                            };

                            //Un-highlights the delete button when the mouse leaves image bounds.
                            bttnDelete.MouseLeave += (a, b) =>
                            {
                                newImg = new BitmapImage();
                                newImg.BeginInit();
                                newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnDelete.png");
                                newImg.EndInit();
                                bttnDelete.Source = newImg;
                            };

                            //Prompts to delete the existing image.
                            bttnDelete.MouseDown += (a, b) =>
                            {
                                var mssgResult = MessageBox.Show("Are you sure you want to delete the image?",
                                    "Confirm deletion",
                                    MessageBoxButton.YesNo);

                                if (mssgResult == MessageBoxResult.Yes)
                                {
                                    loadedUrls.RemoveAt(index);

                                    string options = (isAnimated) ? "True" : "False";
                                    string newData = string.Join("|", loadedUrls);
                                    newData = options + "|" + newData;
                                    field.SetData("data", newData);

                                    //TODO: Invalidates the page to update.
                                    InvalidateEntirePage?.Invoke(this, null);
                                }
                            };

                            StackPanel contentControls = new StackPanel();
                            contentControls.Orientation = Orientation.Horizontal;
                            contentControls.Children.Add(bttnDelete);
                            contentControls.Children.Add(bttnUpload);

                            //Centers the content controls to match centered image fields.
                            if (templateType == TemplateFieldType.EntryImages && tCenterImages)
                            {
                                contentControls.HorizontalAlignment = HorizontalAlignment.Center;
                            }

                            StackPanel imageControls = new StackPanel();
                            imageControls.Children.Add(contentControls);
                            imageControls.Children.Add(thumbnail);

                            elementsContainer.Children.Add(imageControls);
                        }
                    }
                    else
                    {
                        MediaElement media = null;
                        ImgAnimated thumbnail = null;

                        if (loadedUrls.Count == 2 &&
                            (loadedUrls[0].ToLower().EndsWith(".wmv") ||
                            loadedUrls[0].ToLower().EndsWith(".mp4")))
                        {
                            media = new MediaElement();

                            try
                            {
                                media.Source = new Uri(loadedUrls[0]);
                            }
                            catch (Exception)
                            {
                                MessageBox.Show("The file couldn't be loaded" +
                                    "or played correctly.");

                                loadedUrls.RemoveAt(0);

                                string options = (isAnimated) ? "True" : "False";
                                string newData = string.Join("|", loadedUrls);
                                newData = options + "|" + newData;
                                field.SetData("data", newData);

                                InvalidateEntirePage?.Invoke(this, null);
                                return;
                            }
                        }
                        else
                        {
                            thumbnail = new ImgAnimated(loadedUrls, true);
                        }

                        //Sets up an upload image button.
                        var bttnUpload = new Image();
                        BitmapImage newImg = new BitmapImage();
                        newImg.BeginInit();
                        newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnAdd.png");
                        newImg.EndInit();
                        bttnUpload.Source = newImg;
                        bttnUpload.ToolTip = "Click to select images.";

                        //Sets up a delete image button.
                        var bttnDelete = new Image();
                        newImg = new BitmapImage();
                        newImg.BeginInit();
                        newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnDelete.png");
                        newImg.EndInit();
                        bttnDelete.Source = newImg;
                        bttnDelete.ToolTip = "Click to delete all images.";

                        //Disables the delete button if there's nothing to delete.
                        if (!File.Exists(loadedUrls.FirstOrDefault()))
                        {
                            bttnDelete.IsEnabled = false;
                            bttnDelete.Opacity = 0.5;
                        }

                        //Highlights the upload button when the mouse enters image bounds.
                        bttnUpload.MouseEnter += (a, b) =>
                        {
                            newImg = new BitmapImage();
                            newImg.BeginInit();
                            newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnAddHover.png");
                            newImg.EndInit();
                            bttnUpload.Source = newImg;
                        };

                        //Un-highlights the upload button when the mouse leaves image bounds.
                        bttnUpload.MouseLeave += (a, b) =>
                        {
                            newImg = new BitmapImage();
                            newImg.BeginInit();
                            newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnAdd.png");
                            newImg.EndInit();
                            bttnUpload.Source = newImg;
                        };

                        //Handles uploading an image to change it.
                        bttnUpload.MouseDown += (a, b) =>
                        {
                            OpenFileDialog dlg = new OpenFileDialog();

                            //Opens the directory of the first url.
                            if (File.Exists(loadedUrls.First()))
                            {
                                dlg.InitialDirectory = loadedUrls.First();
                            }

                            dlg.CheckPathExists = true;
                            dlg.Filter = "images and movies|*.bmp;*.jpg;*.jpeg;*.gif;*.tif;*.tiff;*.png;*.wmv;*.mp4";
                            dlg.FilterIndex = 0;
                            dlg.Multiselect = true;
                            dlg.Title = "Load image";

                            if (dlg.ShowDialog() == true)
                            {
                                string options = (isAnimated) ? "True" : "False";
                                string newData = string.Join("|", dlg.FileNames);
                                newData = options + "|" + newData;
                                field.SetData("data", newData);

                                //TODO: Invalidates the page to update.
                                InvalidateEntirePage?.Invoke(this, null);
                            }
                        };

                        //Highlights the delete button when the mouse enters image bounds.
                        bttnDelete.MouseEnter += (a, b) =>
                        {
                            newImg = new BitmapImage();
                            newImg.BeginInit();
                            newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnDeleteHover.png");
                            newImg.EndInit();
                            bttnDelete.Source = newImg;
                        };

                        //Un-highlights the delete button when the mouse leaves image bounds.
                        bttnDelete.MouseLeave += (a, b) =>
                        {
                            newImg = new BitmapImage();
                            newImg.BeginInit();
                            newImg.UriSource = new Uri("pack://application:,,,/Assets/BttnDelete.png");
                            newImg.EndInit();
                            bttnDelete.Source = newImg;
                        };

                        //Prompts to delete the existing image.
                        bttnDelete.MouseDown += (a, b) =>
                        {
                            var mssgResult = MessageBox.Show("Are you sure you want to delete the image?",
                                "Confirm deletion",
                                MessageBoxButton.YesNo);

                            if (mssgResult == MessageBoxResult.Yes)
                            {
                                string options = (isAnimated) ? "True|" : "False|";
                                field.SetData("data", options);

                                //TODO: Invalidates the page to update.
                                InvalidateEntirePage?.Invoke(this, null);
                            }
                        };

                        StackPanel contentControls = new StackPanel();
                        contentControls.Orientation = Orientation.Horizontal;
                        contentControls.Children.Add(bttnDelete);
                        contentControls.Children.Add(bttnUpload);

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
            //TODO: elements don't recess after expanding.

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