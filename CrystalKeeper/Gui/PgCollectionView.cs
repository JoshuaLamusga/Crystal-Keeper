﻿using CrystalKeeper.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CrystalKeeper.Gui
{
    /// <summary>
    /// Represents the page associated with a collection.
    /// </summary>
    class PgCollectionView
    {
        #region Members
        /// <summary>
        /// The project data associated with the page.
        /// </summary>
        private Project project;

        /// <summary>
        /// The underlying gui.
        /// </summary>
        private PgCollectionGuiView gui;

        /// <summary>
        /// The collection to use.
        /// </summary>
        private DataItem collection;

        /// <summary>
        /// The dataitem to navigate to when selected.
        /// </summary>
        private DataItem selectedItem;

        /// <summary>
        /// The url where the project is stored.
        /// </summary>
        private string projectUrl;

        /// <summary>
        /// Fires when the treeview item should change.
        /// </summary>
        public event EventHandler SelectedItemChanged;
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
        public PgCollectionGuiView Gui
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
        public DataItem SelectedItem
        {
            get
            {
                return selectedItem;
            }
            private set
            {
                selectedItem = value;
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
        /// Creates a collection page from the given project.
        /// </summary>
        /// <param name="project">
        /// The current project to retrieve item data from.
        /// </param>
        /// <param name="collection">
        /// The collection item used to construct the page.
        /// </param>
        public PgCollectionView(
            Project project,
            DataItem collection,
            string projectUrl)
        {
            this.project = project;
            this.collection = collection;
            this.projectUrl = projectUrl;
            ConstructPage();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the page content based on existing material.
        /// </summary>
        public void ConstructPage()
        {
            gui = new PgCollectionGuiView();

            #region Collection name
            //Sets the collection name.
            if (string.IsNullOrWhiteSpace((string)collection.GetData("name")))
            {
                gui.TxtblkCollectionName.Text = GlobalStrings.NameUntitled;
            }
            else
            {
                gui.TxtblkCollectionName.Text = (string)collection.GetData("name");
            }
            #endregion

            #region Collection description
            //Sets the collection description.
            if (!string.IsNullOrWhiteSpace((string)collection.GetData("description")))
            {
                gui.TxtbxDescription.Text = (string)collection.GetData("description");
            }
            #endregion

            #region Groupings
            //Creates an element to stack images in vertical columns.
            FixedColumnPanel imageStack = new FixedColumnPanel(3);
            Gui.GuiItems.Children.Add(imageStack.Gui);

            //Adds each group.
            List<DataItem> grps =
                Project.GetCollectionGroupings(collection);
            
            for (int i = 0; i < grps.Count; i++)
            {
                bool isGroupEntryFound = false;
                var entryRefs = project.GetGroupingEntryRefs(grps[i]);

                Grid groupObject = new Grid();

                //Adds the first successful entry.
                for (int j = 0; j < entryRefs.Count; j++)
                {
                    //Gets the entry's fields.
                    var fields = project.GetEntryFields(
                        project.GetEntryRefEntry(entryRefs[j]));

                    //Finds the field index with an EntryImages type.
                    int fieldId = -1;
                    for (int k = 0; k < fields.Count; k++)
                    {
                        var tempField = project.GetFieldTemplateField(fields[k]);
                        if ((TemplateFieldType)(int)tempField.GetData("dataType") ==
                            TemplateFieldType.EntryImages)
                        {
                            fieldId = k;
                            break;
                        }
                    }

                    //Attempts to load the entryimages media.
                    if (fieldId != -1)
                    {
                        string imgUrl = (string)fields[fieldId].GetData("data");
                        List<string> loadedData = new List<string>();
                        List<string> urls = new List<string>();
                        bool isAnimated = false;
                        bool isMuted = false;

                        //Loads existing data.
                        if (imgUrl != String.Empty)
                        {
                            loadedData = imgUrl.Split('|').ToList();
                            isAnimated = (loadedData[0] == "True");
                            isMuted = (loadedData[1] == "True");
                            urls = loadedData.GetRange(2, loadedData.Count - 2);
                            urls = urls.Where(o => File.Exists(o)).ToList();
                        }

                        //Loads a visual if possible.
                        if (urls.Count > 0)
                        {
                            //Loads the first still image.
                            if (!isAnimated)
                            {
                                ImgThumbnail img = new ImgThumbnail(urls[0]);
                                img.Margin = new Thickness(4);
                                img.HorizontalAlignment = HorizontalAlignment.Center;

                                //Resizes the image.
                                img.Loaded += (a, b) =>
                                {
                                    img.IsEnabled = false;
                                    if (img.ActualWidth > 0)
                                    {
                                        img.MaxWidth = img.GetSourceWidth();
                                        img.MaxHeight = img.GetSourceHeight();
                                    }
                                    else
                                    {
                                        img.SetSize(0);
                                    }
                                };

                                //Prevents clicking to open a larger window.
                                img.PreviewMouseUp +=
                                    new System.Windows.Input.MouseButtonEventHandler((a, b) =>
                                    {
                                        b.Handled = true;
                                    });

                                isGroupEntryFound = true;
                                groupObject.Children.Add(img);
                            }

                            //Loads movies and animations.
                            else
                            {
                                MediaElement media = null;
                                ImgAnimated img = null;

                                //Loads the first movie.
                                if (urls[0].ToLower().EndsWith(".wmv") ||
                                    urls[0].ToLower().EndsWith(".mp4"))
                                {
                                    media = new MediaElement();
                                    media.IsMuted = isMuted;
                                    media.Margin = new Thickness(4);
                                    media.HorizontalAlignment = HorizontalAlignment.Center;

                                    try
                                    {
                                        media.Volume = 0;
                                        media.Source = new Uri(urls[0]);

                                        //Resizes the image.
                                        media.MediaOpened += (a, b) =>
                                        {
                                            media.MaxWidth = media.NaturalVideoWidth;
                                            media.MaxHeight = media.NaturalVideoHeight;
                                            media.LoadedBehavior = MediaState.Pause;
                                            media.Volume = 1;
                                        };

                                        //Pauses and resumes playback on hover.
                                        media.MouseEnter += (a, b) =>
                                        {
                                            media.LoadedBehavior = MediaState.Play;
                                        };

                                        media.MouseLeave += (a, b) =>
                                        {
                                            media.LoadedBehavior = MediaState.Pause;
                                        };

                                        //Loops the movie.
                                        media.MediaEnded += (a, b) =>
                                        {
                                            media.Position = new TimeSpan(0, 0, 1);
                                        };

                                        isGroupEntryFound = true;
                                        groupObject.Children.Add(media);
                                    }
                                    catch (InvalidOperationException) { } //Ignores loading errors.
                                    catch (ArgumentNullException) { } //Ignores loading errors.
                                    catch (UriFormatException) { } //Ignores loading errors.
                                    catch (Exception e) //Logs unknown errors.
                                    {
                                        Utils.Log("While loading media in grouping view: " + e.Message);
                                    }
                                }

                                //Loads rotating images.
                                else
                                {
                                    img = new ImgAnimated(urls, false);
                                    img.SetPlaybackDelay(1000);
                                    img.Margin = new Thickness(4);
                                    img.HorizontalAlignment = HorizontalAlignment.Center;

                                    //Resizes the image.
                                    img.Loaded += (a, b) =>
                                    {
                                        if (img.ActualWidth > 0)
                                        {
                                            img.MaxWidth = img.GetSourceWidth();
                                            img.MaxHeight = img.GetSourceHeight();
                                        }
                                        else
                                        {
                                            img.MaxHeight = 0;
                                            img.MaxWidth = 0;
                                        }
                                    };

                                    //Prevents clicking to open a larger window.
                                    img.PreviewMouseUp +=
                                        new System.Windows.Input.MouseButtonEventHandler((a, b) =>
                                        {
                                            b.Handled = true;
                                        });

                                    isGroupEntryFound = true;
                                    groupObject.Children.Add(img);
                                }
                            }
                        }
                    }

                    //Breaks when an entry has been added.
                    if (isGroupEntryFound)
                    {
                        break;
                    }
                }

                //Adds an overlapping caption to each group
                TextBlock blk = new TextBlock();
                blk.Background = new SolidColorBrush(Color.FromArgb(196, 255, 255, 255));
                blk.Text = (string)grps[i].GetData("name");
                blk.Padding = new Thickness(4);
                blk.TextAlignment = TextAlignment.Center;
                blk.TextWrapping = TextWrapping.Wrap;
                blk.HorizontalAlignment = HorizontalAlignment.Stretch;
                blk.VerticalAlignment = VerticalAlignment.Bottom;
                blk.FontSize = 14;

                //Bold when hovered.
                blk.MouseEnter +=
                    new System.Windows.Input.MouseEventHandler((a, b) =>
                    {
                        blk.FontWeight = FontWeights.Bold;
                    });

                //Normal when not hovered.
                blk.MouseLeave +=
                    new System.Windows.Input.MouseEventHandler((a, b) =>
                    {
                        blk.FontWeight = FontWeights.Normal;
                    });

                groupObject.Children.Add(blk);

                //Navigates to the grouping when clicked.
                int pos = i; //Captured for the lambda.

                groupObject.MouseUp +=
                    new System.Windows.Input.MouseButtonEventHandler((a, b) =>
                    {
                        SelectedItem = grps[pos];
                    });

                //Adds the group to the image stack.
                imageStack.AddItem(groupObject);
            }
            #endregion
        }
        #endregion
    }
}