using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace CrystalKeeper.Core
{
    /// <summary>
    /// Contains functionality to save, load, and construct data hierarchies
    /// that correspond to widgets on the screen.
    /// </summary>
    class Project
    {
        #region Members
        /// <summary>
        /// Stores the next GUID to be created.
        /// </summary>
        private ulong _guidCounter;

        /// <summary>
        /// Stores all data items.
        /// </summary>
        private ObservableCollection<DataItem> _items;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the collection of all data items.
        /// </summary>
        public ObservableCollection<DataItem> Items
        {
            private set
            {
                _items = value;
            }
            get
            {
                return _items;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Starts a new project to store all data.
        /// </summary>
        public Project()
        {
            _guidCounter = 0;
            _items = new ObservableCollection<DataItem>();

            //Automatically adds the database item.
            DataItem item = new DataItem(NewGuid(), DataItemTypes.Database);
            item.SetData("name", "Untitled");
            item.SetData("defUseEditMode", false);
            item.SetData("defSearchByText", true);
            item.SetData("defCacheData", true);
            item.SetData("description", String.Empty);
            item.SetData("imageBackgroundEnabled", false);
            item.SetData("imageUrl", String.Empty);

            _items.Add(item);
        }

        /// <summary>
        /// Starts a new project from the given set of data. Do not set the
        /// data outside of Project.Add* methods. Tampering with the data
        /// directly will quickly lead to issues.
        /// </summary>
        public Project(ObservableCollection<DataItem> items)
        {
            _items = items;
            ulong largestGuid = 0;

            //Loops through each item to find the largest guid.
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].guid > largestGuid)
                {
                    largestGuid = _items[i].guid;
                }
            }

            //The next guid is the largest one + 1.
            _guidCounter = largestGuid + 1;
        }

        /// <summary>
        /// Creates a deep copy of the given project.
        /// </summary>
        public Project(Project project)
        {
            //Populates the project with a deep copy of each item.
            _items = new ObservableCollection<DataItem>();
            for (int i = 0; i < project.Items.Count; i++)
            {
                _items.Add(new DataItem(project.Items[i]));
            }

            ulong largestGuid = 0;

            //Loops through each item to find the largest guid.
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].guid > largestGuid)
                {
                    largestGuid = _items[i].guid;
                }
            }

            //The next guid is the largest one + 1.
            _guidCounter = largestGuid + 1;
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Generates a new project from the given file path.
        /// </summary>
        /// <param name="url">
        /// The file path with the filename and extension.
        /// </param>
        /// <returns>
        /// A constructed project.
        /// </returns>
        public static Project Load(string url)
        {
            //Stores all bytes in the file.
            byte[] data = null;

            //Stores the item being constructed.
            DataItem item = null;

            //Data is buffered here first and only overwrites on success.
            var newItems = new ObservableCollection<DataItem>();

            try
            {
                data = File.ReadAllBytes(url);
            }
            catch (UnauthorizedAccessException)
            {
                Utils.Log("Could not open " + url + " for read access.");

                //Tell the user the file did not load and cancel it.
                MessageBox.Show("The file was not loaded because it was " +
                    "prevented from being loaded in that location.");

                return null;
            }
            catch (PathTooLongException)
            {
                Utils.Log("Could not open " + url + " because it's too long.");

                //Tell the user the file did not load and cancel it.
                MessageBox.Show("The file was not loaded because the " +
                    "filename is too long to process.");

                return null;
            }
            catch (DirectoryNotFoundException)
            {
                Utils.Log("Could not open " + url + " because the directory wasn't found.");

                //Tell the user the file did not load and cancel it.
                MessageBox.Show("The file was not loaded because the " +
                    "location to load it in was changed while trying to load.");

                return null;
            }
            catch (Exception e)
            {
                Utils.Log("Could not open " + url + ". Exception: " +
                    e.GetBaseException().StackTrace);

                //Tell the user the file did not load and cancel it.
                MessageBox.Show("The file could not be loaded for an " +
                    "unknown reason.");

                return null;
            }

            try
            {
                BinaryReader reader = new BinaryReader(new MemoryStream(data));
                string appVersion = reader.ReadString();

                //As long as it's not the end of the file, read another chunk.
                while (reader.BaseStream.Position < data.Length)
                {
                    //Constructs a new item.
                    byte itemType = reader.ReadByte();
                    ulong itemGuid = reader.ReadUInt64();
                    item = new DataItem(itemGuid, (DataItemTypes)itemType);

                    switch (item.type)
                    {
                        case DataItemTypes.Collection:
                            item.SetData("name", reader.ReadString());
                            item.SetData("description", reader.ReadString());
                            item.SetData("refGuid", reader.ReadUInt64());
                            break;
                        case DataItemTypes.Database:
                            item.SetData("name", reader.ReadString());
                            item.SetData("defUseEditMode", reader.ReadBoolean());
                            item.SetData("defSearchByText", reader.ReadBoolean());
                            item.SetData("defCacheData", reader.ReadBoolean());
                            item.SetData("description", reader.ReadString());
                            item.SetData("imageBackgroundEnabled", reader.ReadBoolean());
                            item.SetData("imageUrl", reader.ReadString());
                            break;
                        case DataItemTypes.Entry:
                            item.SetData("name", reader.ReadString());
                            item.SetData("refGuid", reader.ReadUInt64());
                            break;
                        case DataItemTypes.EntryField:
                            item.SetData("refGuid", reader.ReadUInt64());
                            item.SetData("templateFieldGuid", reader.ReadUInt64());

                            //Declares the size of the data chunk, then loads it.
                            int numBytes = reader.ReadInt32();
                            try
                            {
                                item.SetData("data", Utils.ByteArrayToObject(reader.ReadBytes(numBytes)));
                            }
                            catch (System.Runtime.Serialization.SerializationException)
                            {
                                Utils.Log("Cannot serialize " + url + "to load.");

                                //Tell the user the file did not load and cancel it.
                                MessageBox.Show("The file could not be loaded because " +
                                    "it is not a Crystal Keeper file.");

                                return null;
                            }
                            break;
                        case DataItemTypes.Grouping:
                            item.SetData("name", reader.ReadString());
                            item.SetData("refGuid", reader.ReadUInt64());

                            //Loads group conditions.
                            uint numConditions = reader.ReadUInt32();
                            item.SetData("numConditions", numConditions);
                            for (int i = 0; i < numConditions; i++)
                            {
                                item.SetData("conditionType" + i, reader.ReadByte());
                                item.SetData("name1" + i, reader.ReadString());
                                item.SetData("name2" + i, reader.ReadString());
                                item.SetData("fieldGuid" + i, reader.ReadUInt64());
                            }
                            break;
                        case DataItemTypes.GroupingEntryRef:
                            item.SetData("refGuid", reader.ReadUInt64());
                            item.SetData("entryGuid", reader.ReadUInt64());
                            break;
                        case DataItemTypes.Template:
                            item.SetData("name", reader.ReadString());
                            item.SetData("centerImages", reader.ReadBoolean());
                            item.SetData("twoColumns", reader.ReadBoolean());
                            item.SetData("numExtraImages", reader.ReadByte());
                            item.SetData("extraImagePos", reader.ReadInt32());
                            item.SetData("fontFamilies", reader.ReadString());
                            item.SetData("headerColorR", reader.ReadByte());
                            item.SetData("headerColorG", reader.ReadByte());
                            item.SetData("headerColorB", reader.ReadByte());
                            item.SetData("contentColorR", reader.ReadByte());
                            item.SetData("contentColorG", reader.ReadByte());
                            item.SetData("contentColorB", reader.ReadByte());
                            break;
                        case DataItemTypes.TemplateColumnData:
                            item.SetData("isFirstColumn", reader.ReadBoolean());
                            item.SetData("refGuid", reader.ReadUInt64());
                            break;
                        case DataItemTypes.TemplateField:
                            item.SetData("name", reader.ReadString());
                            item.SetData("refGuid", reader.ReadUInt64());
                            item.SetData("dataType", reader.ReadInt32());
                            item.SetData("isVisible", reader.ReadBoolean());
                            item.SetData("isTitleVisible", reader.ReadBoolean());
                            item.SetData("isTitleInline", reader.ReadBoolean());
                            item.SetData("columnOrder", reader.ReadInt32());
                            break;
                    }

                    newItems.Add(item);
                }

                //Constructs the new project instance.
                return new Project(newItems);
            }
            catch (EndOfStreamException)
            {
                Utils.Log("Unexpected end-of-stream in " + url + ".");

                //Tell the user the file did not load and cancel it.
                MessageBox.Show("The file could not be loaded because " +
                    "it is corrupted.");

                return null;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Returns a new globally unique identifier.
        /// </summary>
        private ulong NewGuid()
        {
            return _guidCounter++;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Attempts to save to the given filename (with no extension),
        /// overwriting any existing file in the same location.
        /// </summary>
        /// <param name="url">
        /// The file path, filename, and extension to save the database to.
        /// </param>
        public void Save(string url)
        {
            BinaryWriter writer = null;
            DataItem item = null;

            try
            {
                if (File.Exists(url))
                {
                    File.Delete(url);
                }

                writer = new BinaryWriter(File.OpenWrite(url));
            }
            catch (UnauthorizedAccessException)
            {
                Utils.Log("Could not open " + url + " for write access.");

                //Tell the user the file did not save and cancel it.
                MessageBox.Show("The file was not saved because it was " +
                    "prevented from being saved in that location.");

                return;
            }
            catch (PathTooLongException)
            {
                Utils.Log("Could not open " + url + " because it's too long.");

                //Tell the user the file did not save and cancel it.
                MessageBox.Show("The file was not saved because the " +
                    "filename is too long. Please use a shorter name.");

                return;
            }
            catch (DirectoryNotFoundException)
            {
                Utils.Log("Could not open " + url + " because the directory wasn't found.");

                //Tell the user the file did not save and cancel it.
                MessageBox.Show("The file was not saved because the " +
                    "location to save it in was changed while trying to save.");

                return;
            }
            catch (Exception e)
            {
                Utils.Log("Could not open " + url + ". Exception: " + e.GetBaseException().Message);

                //Tell the user the file did not save and cancel it.
                MessageBox.Show("The file could not be saved. Try " +
                    "specifying a different location or filename.");

                return;
            }

            //Retrieves and writes the application version number.
            writer.Write(FileVersionInfo.GetVersionInfo
                (Assembly.GetExecutingAssembly().Location)
                .ProductVersion);

            //Ensures the database image path is relative.
            string newPath = Utils.MakeRelativeUrl(url, (string)GetDatabase().GetData("imageUrl"));
            if (newPath != "")
            {
                GetDatabase().SetData("imageUrl", newPath);
            }

            //Ensures all image paths are relative.
            List<DataItem> entries = GetItemsByType(DataItemTypes.Entry);
            for (int i = 0; i < entries.Count; i++)
            {
                List<DataItem> fields = GetEntryFields(entries[i]);
                for (int j = 0; j < fields.Count; j++)
                {
                    DataItem templateField = GetItemByGuid((ulong)fields[j].GetData("templateFieldGuid"));
                    var fieldType = (TemplateFieldType)(int)templateField.GetData("dataType");

                    //Reads the image-formatted fields.
                    if (fieldType == TemplateFieldType.Images ||
                        fieldType == TemplateFieldType.EntryImages)
                    {
                        var data = ((string)fields[j].GetData("data")).Split('|');
                        string newData = data.FirstOrDefault();
                        if (newData.Trim() != String.Empty)
                        {
                            newData += "|";
                        }

                        //Reads all urls and makes them relative if not so.
                        for (int k = 1; k < data.Length; k++)
                        {
                            string newUrl = Utils.MakeRelativeUrl(url, data[k]);
                            if (newUrl != "")
                            {
                                newData += newUrl;
                            }
                            if (k != data.Length - 1)
                            {
                                newData += "|";
                            }
                        }
                        fields[j].SetData("data", newData);
                    }
                }
            }

            //Writes all data items in arbitrary order.
            for (int i = 0; i < _items.Count; i++)
            {
                item = _items[i];

                //Writes header info so items can be read in any order.
                writer.Write((byte)item.type);
                writer.Write(item.guid);

                //Writes specific data.
                try
                {
                    switch (item.type)
                    {
                        case DataItemTypes.Collection:
                            writer.Write((string)item.GetData("name"));
                            writer.Write((string)item.GetData("description"));
                            writer.Write((ulong)item.GetData("refGuid"));
                            break;
                        case DataItemTypes.Database:
                            writer.Write((string)item.GetData("name"));
                            writer.Write((bool)item.GetData("defUseEditMode"));
                            writer.Write((bool)item.GetData("defSearchByText"));
                            writer.Write((bool)item.GetData("defCacheData"));
                            writer.Write((string)item.GetData("description"));
                            writer.Write((bool)item.GetData("imageBackgroundEnabled"));
                            writer.Write((string)item.GetData("imageUrl"));
                            break;
                        case DataItemTypes.Entry:
                            writer.Write((string)item.GetData("name"));
                            writer.Write((ulong)item.GetData("refGuid"));
                            break;
                        case DataItemTypes.EntryField:
                            writer.Write((ulong)item.GetData("refGuid"));
                            writer.Write((ulong)item.GetData("templateFieldGuid"));

                            //Writes the size of the data chunk, then saves it.
                            var rawData = Utils.ObjectToByteArray(item.GetData("data"));
                            writer.Write(rawData.Length);
                            writer.Write(rawData);
                            break;
                        case DataItemTypes.Grouping:
                            writer.Write((string)item.GetData("name"));
                            writer.Write((ulong)item.GetData("refGuid"));
                            writer.Write((uint)item.GetData("numConditions"));

                            //Sets group conditions.
                            for (int j = 0; j < (uint)item.GetData("numConditions"); j++)
                            {
                                writer.Write((byte)item.GetData("conditionType" + j));
                                writer.Write((string)item.GetData("name1" + j));
                                writer.Write((string)item.GetData("name2" + j));
                                writer.Write((ulong)item.GetData("fieldGuid" + j));
                            }
                            break;
                        case DataItemTypes.GroupingEntryRef:
                            writer.Write((ulong)item.GetData("refGuid"));
                            writer.Write((ulong)item.GetData("entryGuid"));
                            break;
                        case DataItemTypes.Template:
                            writer.Write((string)item.GetData("name"));
                            writer.Write((bool)item.GetData("centerImages"));
                            writer.Write((bool)item.GetData("twoColumns"));
                            writer.Write((byte)item.GetData("numExtraImages"));
                            writer.Write((int)item.GetData("extraImagePos"));
                            writer.Write((string)item.GetData("fontFamilies"));
                            writer.Write((byte)item.GetData("headerColorR"));
                            writer.Write((byte)item.GetData("headerColorG"));
                            writer.Write((byte)item.GetData("headerColorB"));
                            writer.Write((byte)item.GetData("contentColorR"));
                            writer.Write((byte)item.GetData("contentColorG"));
                            writer.Write((byte)item.GetData("contentColorB"));
                            break;
                        case DataItemTypes.TemplateColumnData:
                            writer.Write((bool)item.GetData("isFirstColumn"));
                            writer.Write((ulong)item.GetData("refGuid"));
                            break;
                        case DataItemTypes.TemplateField:
                            writer.Write((string)item.GetData("name"));
                            writer.Write((ulong)item.GetData("refGuid"));
                            writer.Write((int)item.GetData("dataType"));
                            writer.Write((bool)item.GetData("isVisible"));
                            writer.Write((bool)item.GetData("isTitleVisible"));
                            writer.Write((bool)item.GetData("isTitleInline"));
                            writer.Write((int)item.GetData("columnOrder"));
                            break;
                    }
                }
                catch (InvalidCastException e)
                {
                    Utils.Log("Invalid cast exception: " + e.GetBaseException().Message);

                    //Tell the user the file did not save and cancel it.
                    MessageBox.Show("The file is corrupt and cannot be saved.");
                }
            }

            writer.Flush();
            writer.Close();
            writer.Dispose();
        }

        /// <summary>
        /// Adds a new template.
        /// </summary>
        /// <param name="name">
        /// The name of the new template.
        /// </param>
        /// <param name="centerImages">
        /// Centers main images at the top of the page, outside of columns.
        /// </param>
        /// <param name="twoColumns">
        /// Uses two columns rather than one column for content alignment.
        /// </param>
        /// <param name="numExtraImages">
        /// The number of extra images to display.
        /// </param>
        /// <param name="extraImagePos">
        /// Where to position extra images. 0 = right, 1 = under, 2 = left,
        /// and 3 = above. All locations in relation to main image display.
        /// </param>
        /// <param name="fontFamilies">
        /// Any number of font families separated by ; for both the title
        /// and content.
        /// </param>
        /// <param name="headerColorR">
        /// The red channel for the color of all data field titles.
        /// </param>
        /// <param name="headerColorG">
        /// The green channel for the color of all data field titles.
        /// </param>
        /// <param name="headerColorB">
        /// The blue channel for the color of all data field titles.
        /// </param>
        /// <param name="contentColorR">
        /// The red channel for the color of all data field content.
        /// </param>
        /// <param name="contentColorG">
        /// The green channel for the color of all data field content.
        /// </param>
        /// <param name="contentColorB">
        /// The blue channel for the color of all data field content.
        /// </param>
        public ulong AddTemplate(
            string name,
            bool centerImages,
            bool twoColumns,
            byte numExtraImages,
            TemplateImagePos extraImagePos,
            string fontFamilies,
            byte headerColorR,
            byte headerColorG,
            byte headerColorB,
            byte contentColorR,
            byte contentColorG,
            byte contentColorB)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.Template);

            item.SetData("name", name);
            item.SetData("centerImages", centerImages);
            item.SetData("twoColumns", twoColumns);
            item.SetData("numExtraImages", numExtraImages);
            item.SetData("extraImagePos", (int)extraImagePos);
            item.SetData("fontFamilies", fontFamilies);
            item.SetData("headerColorR", headerColorR);
            item.SetData("headerColorG", headerColorG);
            item.SetData("headerColorB", headerColorB);
            item.SetData("contentColorR", contentColorR);
            item.SetData("contentColorG", contentColorG);
            item.SetData("contentColorB", contentColorB);

            _items.Add(item);
            return item.guid;
        }

        /// <summary>
        /// Adds a field to a template.
        /// </summary>
        /// <param name="name">
        /// A user-friendly name for the object.
        /// </param>
        /// <param name="templateColumnGuid">
        /// The guid of the containing column this field belongs to.
        /// </param>
        /// <param name="isVisible">
        /// True if the field should be visible to the user.
        /// </param>
        /// <param name="dataType">
        /// Represents the type of data the field can contain.
        /// </param>
        /// <param name="columnOrder">
        /// Represents the position of the field in the column.
        /// </param>
        public ulong AddTemplateField(
            string name,
            ulong templateColumnGuid,
            TemplateFieldType dataType,
            bool isVisible,
            bool isTitleVisible,
            bool isTitleInline,
            int columnOrder)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.TemplateField);

            item.SetData("name", name);
            item.SetData("refGuid", templateColumnGuid);
            item.SetData("dataType", (int)dataType);
            item.SetData("isVisible", isVisible);
            item.SetData("isTitleVisible", isTitleVisible);
            item.SetData("isTitleInline", isTitleInline);
            item.SetData("columnOrder", columnOrder);

            _items.Add(item);
            return item.guid;
        }

        /// <summary>
        /// Adds column order data for fields by guid.
        /// </summary>
        /// <param name="isFirstColumn">
        /// If the column data represents the first column.
        /// </param>
        /// <param name="templateGuid">
        /// The guid of the containing template object.
        /// </param>
        /// <param name="fieldReferences">
        /// References to each template field by guid.
        /// </param>
        public ulong AddTemplateColumnData(
            bool isFirstColumn,
            ulong templateGuid)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.TemplateColumnData);

            item.SetData("isFirstColumn", isFirstColumn);
            item.SetData("refGuid", templateGuid);

            _items.Add(item);
            return item.guid;
        }

        /// <summary>
        /// Adds a collection.
        /// </summary>
        /// <param name="name">
        /// A user-friendly name for the object.
        /// </param>
        /// <param name="description">
        /// A user-friendly description of what the collection is about.
        /// </param>
        /// <param name="templateGuid">
        /// The guid of the template used to determine how entries in this
        /// collection are visually displayed.
        /// </param>
        public ulong AddCollection(
            string name,
            string description,
            ulong templateGuid)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.Collection);

            item.SetData("name", name);
            item.SetData("description", description);
            item.SetData("refGuid", templateGuid);

            _items.Add(item);
            return item.guid;
        }

        /// <summary>
        /// Adds a grouping.
        /// </summary>
        /// <param name="name">
        /// A user-friendly name for the object.
        /// </param>
        /// <param name="collectionGuid">
        /// The guid of the containing collection object.
        /// </param>
        public ulong AddGrouping(
            string name,
            ulong collectionGuid)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.Grouping);

            item.SetData("name", name);
            item.SetData("refGuid", collectionGuid);
            item.SetData("numConditions", (uint)0);

            _items.Add(item);
            return item.guid;
        }

        /// <summary>
        /// Adds a condition to a grouping for automatically
        /// including entries. Returns true if successful and false
        /// otherwise.
        /// 
        /// Adds a condition to alphabetically add entries starting
        /// with the given letter and ending with another.
        /// </summary>
        /// <param name="groupGuid">
        /// The guid of the group to add a condition to.
        /// </param>
        /// <param name="startingLetter">
        /// All entries in the containing collection with a name that
        /// starts with a letter from the range of this letter to the
        /// ending letter will be automatically included.
        /// </param>
        /// <param name="endingLetter">
        /// All entries in the containing collection with a name that
        /// starts with a letter from the range of the starting letter
        /// to this letter will be automatically included.
        /// </param>
        public bool AddGroupingCondition(
            ulong groupGuid,
            string startingLetter,
            string endingLetter)
        {
            DataItem item = GetItemByGuid(groupGuid);

            if (item.type != DataItemTypes.Grouping)
            {
                return false;
            }

            //Gets the number of conditions and adds 1.
            uint numConditions = 0;
            uint.TryParse((string)item.GetData("numConditions"), out numConditions);
            numConditions += 1;
            item.SetData("numConditions", numConditions);

            //Sets condition data.
            item.SetData("conditionType" + numConditions, (byte)0);
            item.SetData("name1" + numConditions, startingLetter);
            item.SetData("name2" + numConditions, endingLetter);
            item.SetData("fieldGuid" + numConditions, (ulong)0);
            return true;
        }

        /// <summary>
        /// Adds an entry reference to a grouping.
        /// </summary>
        /// <param name="groupingGuid">
        /// The guid of the containing group object.
        /// </param>
        /// <param name="entryGuid">
        /// The guid of the entry to point to.
        /// </param>
        public ulong AddGroupingEntryRef(
            ulong groupingGuid,
            ulong entryGuid)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.GroupingEntryRef);

            item.SetData("refGuid", groupingGuid);
            item.SetData("entryGuid", entryGuid);

            _items.Add(item);
            return item.guid;
        }

        /// <summary>
        /// Adds an entry.
        /// </summary>
        /// <param name="name">
        /// A user-friendly name for the object.
        /// </param>
        /// <param name="collectionGuid">
        /// The guid of the containing collection object.
        /// </param>
        public ulong AddEntry(
            string name,
            ulong collectionGuid)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.Entry);

            item.SetData("name", name);
            item.SetData("refGuid", collectionGuid);

            _items.Add(item);
            return item.guid;
        }

        /// <summary>
        /// Adds a data field for an entry.
        /// </summary>
        /// <param name="entryGuid">
        /// The guid of the entry this field belongs to.
        /// </param>
        /// <param name="templateFieldGuid">
        /// The guid of the template field that this field corresponds to.
        /// </param>
        /// <param name="data">
        /// The raw data contained by this field.
        /// </param>
        public ulong AddField(
            ulong entryGuid,
            ulong templateFieldGuid,
            object data)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.EntryField);

            item.SetData("refGuid", entryGuid);
            item.SetData("templateFieldGuid", templateFieldGuid);
            item.SetData("data", data);

            _items.Add(item);
            return item.guid;
        }

        /// <summary>
        /// Deletes the given item. Returns true if successful; false if it
        /// didn't exist. The first item with a matching guid will be deleted.
        /// </summary>
        /// <param name="item">
        /// The item to delete.
        /// </param>
        public bool DeleteItem(DataItem item)
        {
            if (item != null)
            {
                return _items.Remove(item);
            }

            return false;
        }

        /// <summary>
        /// Deletes the item with the given GUID. Returns success.
        /// </summary>
        /// <param name="guid">
        /// The globally unique identifier.
        /// </param>
        /// <returns>
        /// True if successful; false otherwise.
        /// </returns>
        public bool DeleteItemByGuid(ulong guid)
        {
            return DeleteItem(GetItemByGuid(guid));
        }

        /// <summary>
        /// Gets the item with the given guid. It can be changed.
        /// </summary>
        /// <param name="guid">
        /// The globally unique identifier.
        /// </param>
        /// <returns>
        /// The data item with the given guid, or null if not found.
        /// </returns>
        public DataItem GetItemByGuid(ulong guid)
        {
            return _items.ToList().Find(new Predicate<DataItem>((item) =>
            {
                return (item.guid == guid);
            }));
        }

        /// <summary>
        /// Returns all items with the given type.
        /// </summary>
        /// <returns>
        /// All items matching the given type.
        /// </returns>
        public List<DataItem> GetItemsByType(DataItemTypes type)
        {
            return _items.ToList().FindAll(new Predicate<DataItem>((item) =>
            {
                return (item.type == type);
            }));
        }

        /// <summary>
        /// Returns the grouping dataitems of the collection dataitem.
        /// </summary>
        /// <returns>
        /// The grouping dataitems of the collection dataitem specified, or
        /// null if none exist.
        /// </returns>
        public List<DataItem> GetCollectionGroupings(DataItem collection)
        {
            //If the data item is a collection.
            if (collection != null &&
                collection.type == DataItemTypes.Collection)
            {
                //Gets all groupings that link to the collection.
                return _items.ToList().FindAll(new Predicate<DataItem>((item) =>
                {
                    if (item.type == DataItemTypes.Grouping)
                    {
                        return (GetItemByGuid(
                            (ulong)item.GetData("refGuid")) == collection);
                    }
                    else
                    {
                        return false;
                    }
                }));
            }

            //If the data item isn't a collection.
            return null;
        }

        /// <summary>
        /// Returns a template for a collection.
        /// </summary>
        /// <returns>
        /// The template dataitem of a collection dataitem.
        /// </returns>
        public DataItem GetCollectionTemplate(DataItem collection)
        {
            if (collection != null &&
                collection.type == DataItemTypes.Collection)
            {
                return GetItemByGuid((ulong)collection.GetData("refGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns all entries for a collection, or null if there are none.
        /// </summary>
        /// <returns>
        /// The entry dataitems of a collection dataitem. Returns null if no
        /// entries exist.
        /// </returns>
        public List<DataItem> GetCollectionEntries(DataItem collection)
        {
            //If the data item is a collection.
            if (collection != null &&
                collection.type == DataItemTypes.Collection)
            {
                //Gets all entries that link to the collection.
                return _items.ToList().FindAll(new Predicate<DataItem>((item) =>
                {
                    if (item.type == DataItemTypes.Entry)
                    {
                        return (GetItemByGuid(
                            (ulong)item.GetData("refGuid")) == collection);
                    }
                    else
                    {
                        return false;
                    }
                }));
            }

            //If the data item isn't a collection.
            return null;
        }

        /// <summary>
        /// Returns the collection of an entry dataitem.
        /// </summary>
        /// <returns>
        /// The collection dataitem referenced by an entry dataitem.
        /// </returns>
        public DataItem GetEntryCollection(DataItem entry)
        {
            if (entry != null && entry.type == DataItemTypes.Entry)
            {
                return GetItemByGuid((ulong)entry.GetData("refGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns all fields for an entry, or null if there are none.
        /// </summary>
        /// <returns>
        /// The fields dataitems of an entry dataitem. Returns null if no
        /// fields exist.
        /// </returns>
        public List<DataItem> GetEntryFields(DataItem entry)
        {
            return _items.ToList().FindAll(new Predicate<DataItem>((item) =>
            {
                return (item.type == DataItemTypes.EntryField &&
                    GetItemByGuid((ulong)item.GetData("refGuid"))
                    == entry);
            }));
        }

        /// <summary>
        /// Returns all references to the given entry item.
        /// </summary>
        /// <param name="entry">
        /// A dataitem of type Entry.
        /// </param>
        /// <returns>
        /// True if successful; false otherwise.
        /// </returns>
        public List<DataItem> GetEntryEntryRefs(DataItem entry)
        {
            var results = new List<DataItem>();

            if (entry.type != DataItemTypes.Entry)
            {
                return results;
            }

            var items = GetItemsByType(DataItemTypes.GroupingEntryRef);

            //Finds all references to delete with the entry.
            foreach (DataItem item in items)
            {
                if ((ulong)item.GetData("entryGuid") == entry.guid)
                {
                    results.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns the entry being referenced by the entry ref dataitem.
        /// </summary>
        /// <returns>
        /// The entry dataitem referenced by the entry ref dataitem, or null
        /// if an entry ref isn't provided.
        /// </returns>
        public DataItem GetEntryRefEntry(DataItem entryRef)
        {
            if (entryRef != null &&
                entryRef.type == DataItemTypes.GroupingEntryRef)
            {
                return GetItemByGuid((ulong)entryRef.GetData("entryGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns the grouping container of the entry ref dataitem.
        /// </summary>
        /// <returns>
        /// The grouping dataitem that the entry ref dataitem references, or
        /// null if an entry ref isn't provided.
        /// </returns>
        public DataItem GetEntryRefGrouping(DataItem entryRef)
        {
            if (entryRef != null &&
                entryRef.type == DataItemTypes.GroupingEntryRef)
            {
                return GetItemByGuid((ulong)entryRef.GetData("refGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns the entry of a field dataitem.
        /// </summary>
        /// <returns>
        /// The entry dataitem referenced by a field dataitem.
        /// </returns>
        public DataItem GetFieldEntry(DataItem field)
        {
            if (field != null && field.type == DataItemTypes.EntryField)
            {
                return GetItemByGuid((ulong)field.GetData("refGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns the corresponding template field that the given field
        /// dataitem is "filling in" the data for.
        /// </summary>
        /// <returns>
        /// The template field dataitem referenced by the entry field
        /// dataitem, or null if a field is not provided.
        /// </returns>
        public DataItem GetFieldTemplateField(DataItem field)
        {
            if (field != null && field.type == DataItemTypes.EntryField)
            {
                return GetItemByGuid((ulong)field.GetData("templateFieldGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns the collection of a grouping dataitem.
        /// </summary>
        /// <returns>
        /// The collection dataitem referenced by a grouping dataitem.
        /// </returns>
        public DataItem GetGroupingCollection(DataItem grouping)
        {
            if (grouping != null && grouping.type == DataItemTypes.Grouping)
            {
                return GetItemByGuid((ulong)grouping.GetData("refGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns all entry references for a grouping, or null if there are
        /// none.
        /// </summary>
        /// <returns>
        /// The entry reference dataitems of a grouping dataitem. Returns null
        /// if no entries exist.
        /// </returns>
        public List<DataItem> GetGroupingEntryRefs(DataItem grouping)
        {
            //If the data item is a grouping.
            if (grouping != null && grouping.type == DataItemTypes.Grouping)
            {
                //Gets all entry references that link to its collection.
                return _items.ToList().FindAll(new Predicate<DataItem>((item) =>
                {
                    if (item.type == DataItemTypes.GroupingEntryRef)
                    {
                        return (GetItemByGuid(
                            (ulong)item.GetData("refGuid")) == grouping);
                    }
                    else
                    {
                        return false;
                    }
                }));
            }

            //If the data item isn't a grouping.
            return null;
        }

        /// <summary>
        /// Returns all entries for a grouping, or null if there are none.
        /// </summary>
        /// <returns>
        /// The entry dataitems from the references of a grouping dataitem.
        /// Returns null if no entries exist.
        /// </returns>
        public List<DataItem> GetGroupingEntries(DataItem grouping)
        {
            List<DataItem> refs = GetGroupingEntryRefs(grouping);

            if (refs == null)
            {
                return null;
            }

            List<DataItem> entries = new List<DataItem>();
            for (int i = 0; i < refs.Count; i++)
            {
                entries.Add(
                    GetItemByGuid((ulong)refs[i].GetData("entryGuid")));
            }

            //If the data item isn't a grouping.
            return entries;
        }

        /// <summary>
        /// Returns the template referenced by the given template field or
        /// template column data dataitem.
        /// </summary>
        /// <returns>
        /// The dataitem referenced as the template by a template field or
        /// template column data dataitem, or null if it doesn't exist.
        /// </returns>
        public DataItem GetTemplateItemTemplate(DataItem field)
        {
            if (field == null)
            {
                return null;
            }

            if (field.type == DataItemTypes.TemplateColumnData)
            {
                return GetItemByGuid((ulong)field.GetData("refGuid"));
            }
            else if (field.type == DataItemTypes.TemplateField)
            {
                DataItem column = GetItemByGuid((ulong)field.GetData("refGuid"));
                return GetItemByGuid((ulong)column.GetData("refGuid"));
            }

            //If the data item isn't a template field or template column data.
            return null;
        }

        /// <summary>
        /// Returns a list of all collection items referencing the template.
        /// </summary>
        /// <returns>
        /// A List of DataItem objects representing collections that use the
        /// given template item.
        /// </returns>
        public List<DataItem> GetTemplateCollections(DataItem template)
        {
            //If the data item is a template.
            if (template != null &&
                template.type == DataItemTypes.Template)
            {
                //Finds all items that are collections with the template.
                return _items.ToList().FindAll(new Predicate<DataItem>((item) =>
                {
                    if (GetCollectionTemplate(item) != null)
                    {
                        return (GetCollectionTemplate(item).Equals(template));
                    }

                    return false;
                }));
            }

            //If the template has no items or isn't a template.
            return new List<DataItem>();
        }

        /// <summary>
        /// Returns the fields from the template column, or null if not found.
        /// </summary>
        /// <returns>
        /// All fields referencing the given template column data dataitem, or
        /// null if no fields reference it.
        /// </returns>
        public List<DataItem> GetTemplateColumnFields(DataItem templateColumn)
        {
            //If the data item is a template column.
            if (templateColumn != null &&
                templateColumn.type == DataItemTypes.TemplateColumnData)
            {
                //Gets all template fields that link to the template.
                return _items.ToList().FindAll(new Predicate<DataItem>((item) =>
                {
                    if (item.type == DataItemTypes.TemplateField)
                    {
                        return (GetItemByGuid(
                            (ulong)item.GetData("refGuid")) == templateColumn);
                    }
                    else
                    {
                        return false;
                    }
                }));
            }

            //If the template has no items or isn't a template.
            return null;
        }

        /// <summary>
        /// Returns the column order from the template, or null if none exist.
        /// </summary>
        /// <returns>
        /// All column order data referencing the given template dataitem, or
        /// null if none reference it.
        /// </returns>
        public List<DataItem> GetTemplateColumns(DataItem template)
        {
            //If the data item is a template.
            if (template != null &&
                template.type == DataItemTypes.Template)
            {
                //Gets all linked template column order objects.
                return _items.ToList().FindAll(new Predicate<DataItem>((item) =>
                {
                    if (item.type == DataItemTypes.TemplateColumnData)
                    {
                        return (GetItemByGuid(
                            (ulong)item.GetData("refGuid")) == template);
                    }
                    else
                    {
                        return false;
                    }
                }));
            }

            //If the template has no items or isn't a template.
            return null;
        }

        /// <summary>
        /// Returns the database dataitem.
        /// </summary>
        /// <returns>
        /// The database dataitem.
        /// </returns>
        public DataItem GetDatabase()
        {
            List<DataItem> items = GetItemsByType(DataItemTypes.Database);
            if (items != null &&
                items.Count > 0)
            {
                return items.First();
            }

            return null;
        }

        /// <summary>
        /// Clears all data and resets the guid.
        /// </summary>
        public void Reset()
        {
            _guidCounter = 0;
            _items.Clear();

            //Automatically adds the database item.
            DataItem item = new DataItem(NewGuid(), DataItemTypes.Database);
            item.SetData("name", "Untitled");
            item.SetData("defUseEditMode", false);
            item.SetData("defSearchByText", true);
            item.SetData("defCacheData", true);
            item.SetData("description", String.Empty);
            item.SetData("imageBackgroundEnabled", false);
            item.SetData("imageUrl", String.Empty);

            _items.Add(item);
        }
        #endregion
    }
}