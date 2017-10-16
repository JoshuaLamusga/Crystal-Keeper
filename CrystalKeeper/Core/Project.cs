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
    /// that correspond to controls on the screen.
    /// </summary>
    class Project
    {
        #region Members
        /// <summary>
        /// Stores the next GUID to be created.
        /// </summary>
        private ulong guidCounter;

        /// <summary>
        /// Stores all data items.
        /// </summary>
        private ObservableCollection<DataItem> items;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the collection of all data items.
        /// </summary>
        public ObservableCollection<DataItem> Items
        {
            private set
            {
                items = value;
            }
            get
            {
                return items;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Starts a new project to store all data.
        /// </summary>
        public Project()
        {
            guidCounter = 0;
            items = new ObservableCollection<DataItem>();

            //Automatically adds the database item.
            DataItem item = new DataItem(NewGuid(), DataItemTypes.Database);
            item.SetData("name", GlobalStrings.NameUntitled);
            item.SetData("defUseEditMode", false);
            item.SetData("description", String.Empty);
            item.SetData("imageBackgroundEnabled", false);
            item.SetData("imageUrl", String.Empty);

            items.Add(item);
        }

        /// <summary>
        /// Creates a project from a list of data. Avoid adding DataItem
        /// objects created outside of Project.Add* methods this way.
        /// Tampering with the data directly will quickly lead to issues.
        /// </summary>
        public Project(ObservableCollection<DataItem> items)
        {
            this.items = items;
            ulong largestGuid = 0;

            //Loops through each item to find the largest guid.
            for (int i = 0; i < this.items.Count; i++)
            {
                if (this.items[i].guid > largestGuid)
                {
                    largestGuid = this.items[i].guid;
                }
            }

            //The next guid is the largest one + 1.
            guidCounter = largestGuid + 1;
        }

        /// <summary>
        /// Creates a deep copy of the given project.
        /// </summary>
        public Project(Project project)
        {
            //Populates the project with a deep copy of each item.
            items = new ObservableCollection<DataItem>();
            for (int i = 0; i < project.Items.Count; i++)
            {
                items.Add(new DataItem(project.Items[i]));
            }

            ulong largestGuid = 0;

            //Loops through each item to find the largest guid.
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].guid > largestGuid)
                {
                    largestGuid = items[i].guid;
                }
            }

            //The next guid is the largest one + 1.
            guidCounter = largestGuid + 1;
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Returns a new project loaded from the given file path. Catches
        /// I/O errors and displays messages about them.
        /// </summary>
        /// <param name="url">
        /// The file path with the filename and extension.
        /// </param>
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

                //Tells the user the file did not load and cancel it.
                MessageBox.Show(GlobalStrings.DlgCannotOpenForRead);

                return null;
            }
            catch (PathTooLongException)
            {
                Utils.Log("Could not open " + url + " because it's too long.");

                //Tells the user the file did not load and cancel it.
                MessageBox.Show(GlobalStrings.DlgCannotOpenTooLong);

                return null;
            }
            catch (DirectoryNotFoundException)
            {
                Utils.Log("Could not open " + url + " because the directory wasn't found.");

                //Tells the user the file did not load and cancel it.
                MessageBox.Show(GlobalStrings.DlgCannotOpenNotFound);

                return null;
            }
            catch (Exception e)
            {
                Utils.Log("Could not open " + url + ". Exception: " +
                    e.GetBaseException().StackTrace);

                //Tells the user the file did not load and cancel it.
                MessageBox.Show(GlobalStrings.DlgCannotOpenUnknown);

                return null;
            }

            try
            {
                BinaryReader reader = new BinaryReader(new MemoryStream(data));

                //Reads the version and gets it in major.minor format.
                string appVersionStr = reader.ReadString();
                float appVersion = 0;

                int dec1 = appVersionStr.IndexOf('.');
                int dec2 = appVersionStr.Substring(dec1 + 1).IndexOf('.');
                appVersionStr = appVersionStr.Substring(0, dec1 + dec2 + 1);

                Single.TryParse(appVersionStr, out appVersion);

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
                            item.SetData("description", reader.ReadString());
                            item.SetData("imageBackgroundEnabled", reader.ReadBoolean());

                            //Reads the image data to a file and points to it.
                            int numBytesInImage = reader.ReadInt32();

                            if (numBytesInImage > 0)
                            {
                                byte[] fileData = reader.ReadBytes(numBytesInImage);
                                string newUrl = Utils.GetAppdataFolder("Background.png");
                                File.WriteAllBytes(newUrl, fileData);
                                item.SetData("imageUrl", newUrl);
                            }

                            break;
                        case DataItemTypes.Entry:
                            item.SetData("name", reader.ReadString());
                            item.SetData("refGuid", reader.ReadUInt64());
                            break;
                        case DataItemTypes.EntryField:
                            item.SetData("refGuid", reader.ReadUInt64());
                            item.SetData("templateFieldGuid", reader.ReadUInt64());

                            //Gets the size of the data chunk, then loads it.
                            int numBytes = reader.ReadInt32();
                            item.SetData("data", reader.ReadBytes(numBytes));
                            break;
                        case DataItemTypes.Grouping:
                            item.SetData("name", reader.ReadString());
                            item.SetData("refGuid", reader.ReadUInt64());

                            //Loads group conditions.
                            uint numConditions = reader.ReadUInt32();
                            item.SetData("numConditions", numConditions);
                            for (int i = 0; i < numConditions; i++)
                            {
                                item.SetData("conditionType" + i, (GroupingCondType)reader.ReadInt32());
                                item.SetData("condAddFromLetter" + i, reader.ReadString());
                                item.SetData("condAddToLetter" + i, reader.ReadString());
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
                            item.SetData("columnOrder", reader.ReadInt32());
                            item.SetData("numExtraImages", reader.ReadByte());
                            item.SetData("extraImagePos", reader.ReadInt32());
                            if (appVersion <= 1.0)
                            {
                                item.SetData("displayAsCarousel", false);
                            }
                            else
                            {
                                item.SetData("displayAsCarousel", reader.ReadBoolean());
                            }
                            break;
                    }

                    newItems.Add(item);
                }

                //Parses non-binary entry field data. Text fields are XamlPackage.
                var fields = newItems.Where(o => o.type == DataItemTypes.EntryField).ToList();
                for (int i = 0; i < fields.Count; i++)
                {
                    var tField = newItems.FirstOrDefault(
                        o => o.guid == (ulong)fields[i].GetData("templateFieldGuid"));

                    var ttype = (TemplateFieldType)tField?.GetData("dataType");

                    //Parses large binary-based data.
                    if (ttype == TemplateFieldType.EntryImages ||
                        ttype == TemplateFieldType.Images)
                    {
                        var binData = (byte[])fields[i].GetData("data");

                        //Reads data to files.
                        using (MemoryStream ms = new MemoryStream(binData))
                        {
                            using (BinaryReader br = new BinaryReader(ms))
                            {
                                //Gets the metadata and iterates through urls.
                                var metadata = br.ReadString();
                                var metaUrls = metadata.Split('|');

                                for (int j = 2; j < metaUrls.Length; j++)
                                {
                                    //Skips empty urls of default image fields.
                                    if (metaUrls[j] == String.Empty)
                                    {
                                        continue;
                                    }

                                    int numBytes = br.ReadInt32();
                                    byte[] fileData = br.ReadBytes(numBytes);
                                    string newUrl = Utils.GetAppdataFolder(metaUrls[j]);
                                    metaUrls[j] = newUrl;

                                    //Writes data to the absolute url filepath.
                                    using (FileStream fs = new FileStream(
                                        newUrl, FileMode.Create))
                                    {
                                        using (BinaryWriter writer = new BinaryWriter(fs))
                                        {
                                            writer.Write(fileData);
                                        }
                                    }
                                }

                                //Replaces loaded binary data with metadata.
                                fields[i].SetData("data", String.Join("|", metaUrls));
                            }
                        }
                    }

                    //Preserves all binary-based data.
                    else if (ttype == TemplateFieldType.Text) { }

                    //Parses all object-based data.
                    else
                    {
                        try
                        {
                            fields[i].SetData("data", Utils.ByteArrayToObject(
                                (byte[])fields[i].GetData("data")));
                        }
                        catch (System.Runtime.Serialization.SerializationException)
                        {
                            Utils.Log("Cannot serialize " + url + " to load.");

                            //Tells the user the file did not load and cancel it.
                            MessageBox.Show(GlobalStrings.DlgCannotOpenUnrecognized);

                            return null;
                        }
                    }
                }

                //Constructs the new project instance.
                return new Project(newItems);
            }
            catch (EndOfStreamException)
            {
                Utils.Log("Unexpected end-of-stream in " + url + ".");

                //Tells the user the file did not load and cancel it.
                MessageBox.Show(GlobalStrings.DlgCannotOpenCorrupt);

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
            return guidCounter++;
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

                //Tells the user the file did not save and cancel it.
                MessageBox.Show(GlobalStrings.DlgCannotOpenForRead);

                return;
            }
            catch (PathTooLongException)
            {
                Utils.Log("Could not open " + url + " because it's too long.");

                //Tells the user the file did not save and cancel it.
                MessageBox.Show(GlobalStrings.DlgCannotOpenTooLong);

                return;
            }
            catch (DirectoryNotFoundException)
            {
                Utils.Log("Could not open " + url + " because the directory wasn't found.");

                //Tells the user the file did not save and cancel it.
                MessageBox.Show(GlobalStrings.DlgCannotOpenNotFound);

                return;
            }
            catch (Exception e)
            {
                Utils.Log("Could not open " + url + ". Exception: " + e.GetBaseException().Message);

                //Tells the user the file did not save and cancel it.
                MessageBox.Show(GlobalStrings.DlgCannotOpenUnknown);

                return;
            }

            //Retrieves and writes the application version number.
            writer.Write(FileVersionInfo.GetVersionInfo
                (Assembly.GetExecutingAssembly().Location)
                .ProductVersion);

            //Writes all data items in arbitrary order.
            for (int i = 0; i < items.Count; i++)
            {
                item = items[i];

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
                            writer.Write((string)item.GetData("description"));
                            writer.Write((bool)item.GetData("imageBackgroundEnabled"));

                            //Reads the data from the file.
                            string imageUrl = (string)item.GetData("imageUrl");
                            if (File.Exists(imageUrl))
                            {
                                writer.Write((int)(new FileInfo(imageUrl).Length));
                                writer.Write(File.ReadAllBytes(imageUrl));
                            }
                            else
                            {
                                writer.Write(0);
                            }
                            break;
                        case DataItemTypes.Entry:
                            writer.Write((string)item.GetData("name"));
                            writer.Write((ulong)item.GetData("refGuid"));
                            break;
                        case DataItemTypes.EntryField:
                            writer.Write((ulong)item.GetData("refGuid"));
                            writer.Write((ulong)item.GetData("templateFieldGuid"));

                            DataItem templateField = GetItemByGuid((ulong)item.GetData("templateFieldGuid"));
                            var fieldType = (TemplateFieldType)(int)templateField.GetData("dataType");

                            //Parses large binary-based data.
                            if (fieldType == TemplateFieldType.Images ||
                                fieldType == TemplateFieldType.EntryImages)
                            {
                                //Gets the metadata and reads file data of each url.                        
                                var metadata = ((string)item.GetData("data"));
                                var metaUrls = metadata.Split('|');

                                using (MemoryStream ms = new MemoryStream())
                                {
                                    using (BinaryWriter br = new BinaryWriter(ms))
                                    {
                                        br.Write(metadata);

                                        //Appends bytes and size of each url to data.
                                        for (int k = 2; k < metaUrls.Length; k++)
                                        {
                                            if (File.Exists(metaUrls[k]))
                                            {
                                                var fileData = File.ReadAllBytes(metaUrls[k]);

                                                br.Write(fileData.Length);
                                                br.Write(fileData);
                                            }
                                        }

                                        metadata = String.Join("|", metaUrls);
                                    }

                                    if (metaUrls.Length > 0)
                                    {
                                        //Writes the size of the data chunk, then saves it.
                                        var rawData = ms.ToArray();
                                        writer.Write(rawData.Length);
                                        writer.Write(rawData);
                                    }
                                    else
                                    {
                                        //Writes the size of the data chunk, then saves it.
                                        var rawData = Utils.ObjectToByteArray(item.GetData("data"));
                                        writer.Write(rawData.Length);
                                        writer.Write(rawData);
                                    }
                                }
                            }
                            else
                            {
                                //Writes the size of the data chunk, then saves it.
                                var rawData = Utils.ObjectToByteArray(item.GetData("data"));
                                writer.Write(rawData.Length);
                                writer.Write(rawData);
                            }
                            break;
                        case DataItemTypes.Grouping:
                            writer.Write((string)item.GetData("name"));
                            writer.Write((ulong)item.GetData("refGuid"));
                            writer.Write((uint)item.GetData("numConditions"));

                            //Sets group conditions.
                            for (int j = 0; j < (uint)item.GetData("numConditions"); j++)
                            {
                                writer.Write((int)(GroupingCondType)item.GetData("conditionType" + j));
                                writer.Write((string)item.GetData("condAddFromLetter" + j));
                                writer.Write((string)item.GetData("condAddToLetter" + j));
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
                            writer.Write((int)item.GetData("columnOrder"));
                            writer.Write((byte)item.GetData("numExtraImages"));
                            writer.Write((int)item.GetData("extraImagePos"));
                            writer.Write((bool)item.GetData("displayAsCarousel"));
                            break;
                    }
                }
                catch (InvalidCastException e)
                {
                    Utils.Log("Invalid cast exception: " + e.GetBaseException().Message);

                    //Tell the user the file did not save and cancel it.
                    MessageBox.Show(GlobalStrings.DlgCannotSaveCorrupt);
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
            item.SetData("fontFamilies", fontFamilies);
            item.SetData("headerColorR", headerColorR);
            item.SetData("headerColorG", headerColorG);
            item.SetData("headerColorB", headerColorB);
            item.SetData("contentColorR", contentColorR);
            item.SetData("contentColorG", contentColorG);
            item.SetData("contentColorB", contentColorB);

            items.Add(item);
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
        /// <param name="dataType">
        /// Represents the type of data the field can contain.
        /// </param>
        /// <param name="isVisible">
        /// True if the field should be visible to the user.
        /// </param>
        /// <param name="isTitleVisible">
        /// True if the title above the field should be visible to the user.
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
            item.SetData("columnOrder", columnOrder);
            item.SetData("numExtraImages", (byte)99);
            item.SetData("extraImagePos", TemplateImagePos.Under);
            item.SetData("displayAsCarousel", false);

            items.Add(item);
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
        /// <param name="dataType">
        /// Represents the type of data the field can contain.
        /// </param>
        /// <param name="isVisible">
        /// True if the field should be visible to the user.
        /// </param>
        /// <param name="isTitleVisible">
        /// True if the title above the field should be visible to the user.
        /// </param>
        /// <param name="columnOrder">
        /// Represents the position of the field in the column.
        /// </param>
        /// <param name="numExtraImages">
        /// The number of images to display (for image-related fields).
        /// </param>
        /// <param name="extraImagePos">
        /// The orientation to display extra images (for image-related fields).
        /// </param>
        public ulong AddTemplateField(
            string name,
            ulong templateColumnGuid,
            TemplateFieldType dataType,
            bool isVisible,
            bool isTitleVisible,
            int columnOrder,
            byte numExtraImages,
            TemplateImagePos extraImagePos,
            bool displayAsCarousel)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.TemplateField);

            item.SetData("name", name);
            item.SetData("refGuid", templateColumnGuid);
            item.SetData("dataType", (int)dataType);
            item.SetData("isVisible", isVisible);
            item.SetData("isTitleVisible", isTitleVisible);
            item.SetData("columnOrder", columnOrder);
            item.SetData("numExtraImages", numExtraImages);
            item.SetData("extraImagePos", extraImagePos);
            item.SetData("displayAsCarousel", displayAsCarousel);

            items.Add(item);
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
        public ulong AddTemplateColumnData(
            bool isFirstColumn,
            ulong templateGuid)
        {
            DataItem item = new DataItem(
                NewGuid(),
                DataItemTypes.TemplateColumnData);

            item.SetData("isFirstColumn", isFirstColumn);
            item.SetData("refGuid", templateGuid);

            items.Add(item);
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

            items.Add(item);
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

            items.Add(item);
            return item.guid;
        }

        /// <summary>
        /// Adds a condition to a grouping for automatically
        /// including entries. Returns true if successful and false
        /// otherwise.
        /// </summary>
        /// <param name="groupGuid">
        /// The guid of the group to add a condition to.
        /// </param>
        /// <param name="startingString">
        /// All entries that are within a range of words will be automatically
        /// included. For example, Me to Mu includes "melt" and "Mt.", but not
        /// "much". The starting string is the upper bracket of this range,
        /// similar to Me.
        /// </param>
        /// <param name="endingString">
        /// All entries that are within a range of words will be automatically
        /// included. For example, Me to Mu includes "melt" and "Mt.", but not
        /// "much". The starting string is the lower bracket of this range,
        /// similar to Mu.
        /// </param>
        public bool AddGroupingCondition(
            ulong groupGuid,
            string startingString,
            string endingString)
        {
            DataItem item = GetItemByGuid(groupGuid);

            if (item.type != DataItemTypes.Grouping)
            {
                return false;
            }

            //Gets the number of conditions and adds 1.
            uint numConditions = (uint)item.GetData("numConditions") + 1;
            item.SetData("numConditions", numConditions);

            //Sets condition data.
            item.SetData("conditionType" + numConditions, GroupingCondType.ByLetter);
            item.SetData("condAddFromLetter" + numConditions, startingString);
            item.SetData("condAddToLetter" + numConditions, endingString);
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

            items.Add(item);
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

            items.Add(item);
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

            items.Add(item);
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
                return items.Remove(item);
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
            return items.ToList().Find(new Predicate<DataItem>((item) =>
            {
                return (item.guid == guid);
            }));
        }

        /// <summary>
        /// Returns all items with the given type.
        /// </summary>
        /// <param name="type">
        /// The type of data to return items of.
        /// </param>
        public List<DataItem> GetItemsByType(DataItemTypes type)
        {
            return items.ToList().FindAll(new Predicate<DataItem>((item) =>
            {
                return (item.type == type);
            }));
        }

        /// <summary>
        /// Returns the grouping dataitems of the collection dataitem, or null
        /// if collection is invalid.
        /// </summary>
        /// <param name="collection">
        /// A data item of type collection.
        /// </param>
        public List<DataItem> GetCollectionGroupings(DataItem collection)
        {
            //If the data item is a collection.
            if (collection != null &&
                collection.type == DataItemTypes.Collection)
            {
                //Gets all groupings that link to the collection.
                return items.ToList().FindAll(new Predicate<DataItem>((item) =>
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
        /// Returns the template of a collection, or null if not found.
        /// </summary>
        /// <param name="collection">
        /// A data item of type collection.
        /// </param>
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
        /// Returns all entries for a collection, or null if collection is
        /// invalid.
        /// </summary>
        /// <param name="collection">
        /// A data item of type collection.
        /// </param>
        public List<DataItem> GetCollectionEntries(DataItem collection)
        {
            //If the data item is a collection.
            if (collection != null &&
                collection.type == DataItemTypes.Collection)
            {
                //Gets all entries that link to the collection.
                return items.ToList().FindAll(new Predicate<DataItem>((item) =>
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
        /// Returns the collection of an entry dataitem, or null if not found.
        /// </summary>
        /// <param name="entry">
        /// A data item of type entry.
        /// </param>
        public DataItem GetEntryCollection(DataItem entry)
        {
            if (entry != null && entry.type == DataItemTypes.Entry)
            {
                return GetItemByGuid((ulong)entry.GetData("refGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns all fields for an entry, or an empty list if none found.
        /// </summary>
        /// <param name="entry">
        /// A data item of type entry.
        /// </param>
        public List<DataItem> GetEntryFields(DataItem entry)
        {
            return items.ToList().FindAll(new Predicate<DataItem>((item) =>
            {
                return (item.type == DataItemTypes.EntryField &&
                    GetItemByGuid((ulong)item.GetData("refGuid"))
                    == entry);
            }));
        }

        /// <summary>
        /// Returns all references to the given entry item, or an empty list
        /// if none found.
        /// </summary>
        /// <param name="entry">
        /// A data item of type Entry.
        /// </param>
        public List<DataItem> GetEntryEntryRefs(DataItem entry)
        {
            var results = new List<DataItem>();

            if (entry.type != DataItemTypes.Entry)
            {
                return results;
            }

            var itemsByType = GetItemsByType(DataItemTypes.GroupingEntryRef);

            //Finds all references to delete with the entry.
            foreach (DataItem item in itemsByType)
            {
                if ((ulong)item.GetData("entryGuid") == entry.guid)
                {
                    results.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns the entry being referenced by the entry ref dataitem, or
        /// null if not found.
        /// </summary>
        /// <param name="entryRef">
        /// A data item of type grouping entry reference.
        /// </param>
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
        /// Returns the grouping container of the entry ref dataitem, or null
        /// if not found.
        /// </summary>
        /// <param name="entryRef">
        /// A data item of type grouping entry reference.
        /// </param>
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
        /// Returns the entry of a field dataitem, or null if field is invalid.
        /// </summary>
        /// <param name="field">
        /// A data item of type entry field.
        /// </param>
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
        /// dataitem is "filling in" the data for, or null if template field
        /// is invalid.
        /// </summary>
        /// <param name="field">
        /// A data item of type entry field.
        /// </param>
        public DataItem GetFieldTemplateField(DataItem field)
        {
            if (field != null && field.type == DataItemTypes.EntryField)
            {
                return GetItemByGuid((ulong)field.GetData("templateFieldGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns the collection of a grouping dataitem, or null if
        /// collection is invalid.
        /// </summary>
        /// <param name="grouping">
        /// A data item of type grouping.
        /// </param>
        public DataItem GetGroupingCollection(DataItem grouping)
        {
            if (grouping != null && grouping.type == DataItemTypes.Grouping)
            {
                return GetItemByGuid((ulong)grouping.GetData("refGuid"));
            }

            return null;
        }

        /// <summary>
        /// Returns all entry references for a grouping, or null if grouping
        /// is invalid.
        /// </summary>
        /// <param name="grouping">
        /// A data item of type grouping.
        /// </param>
        public List<DataItem> GetGroupingEntryRefs(DataItem grouping)
        {
            //If the data item is a grouping.
            if (grouping != null && grouping.type == DataItemTypes.Grouping)
            {
                //Gets all entry references that link to its collection.
                return items.ToList().FindAll(new Predicate<DataItem>((item) =>
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
        /// Returns all entries for a grouping, or null if grouping is invalid.
        /// </summary>
        /// <param name="grouping">
        /// A data item of type grouping.
        /// </param>
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

            return entries;
        }

        /// <summary>
        /// Returns the template referenced by the given template field or
        /// template column data dataitem, or null if field is invalid.
        /// </summary>
        /// <param name="field">
        /// A data item of type template column data or template field.
        /// </param>
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
        /// Returns a list of all collection items referencing the template,
        /// or an empty list if invalid or none found.
        /// </summary>
        /// <param name="template">
        /// A data item of type template.
        /// </param>
        public List<DataItem> GetTemplateCollections(DataItem template)
        {
            //If the data item is a template.
            if (template != null &&
                template.type == DataItemTypes.Template)
            {
                //Finds all items that are collections with the template.
                return items.ToList().FindAll(new Predicate<DataItem>((item) =>
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
        /// Returns the fields from the template column, or an empty list if
        /// invalid or none found.
        /// </summary>
        /// <param name="templateColumn">
        /// A data item of type template column.
        /// </param>
        public List<DataItem> GetTemplateColumnFields(DataItem templateColumn)
        {
            //If the data item is a template column.
            if (templateColumn != null &&
                templateColumn.type == DataItemTypes.TemplateColumnData)
            {
                //Gets all template fields that link to the template.
                return items.ToList().FindAll(new Predicate<DataItem>((item) =>
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
        /// <param name="template">
        /// A data item of type template.
        /// </param>
        public List<DataItem> GetTemplateColumns(DataItem template)
        {
            //If the data item is a template.
            if (template != null &&
                template.type == DataItemTypes.Template)
            {
                //Gets all linked template column order objects.
                return items.ToList().FindAll(new Predicate<DataItem>((item) =>
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
        /// Returns the database dataitem, or null if not found.
        /// </summary>
        public DataItem GetDatabase()
        {
            List<DataItem> itemsByType = GetItemsByType(DataItemTypes.Database);
            if (itemsByType != null &&
                itemsByType.Count > 0)
            {
                return itemsByType.First();
            }

            return null;
        }

        /// <summary>
        /// Clears all data and resets the guid.
        /// </summary>
        public void Reset()
        {
            guidCounter = 0;
            items.Clear();

            //Automatically adds the database item.
            DataItem item = new DataItem(NewGuid(), DataItemTypes.Database);
            item.SetData("name", "Untitled");
            item.SetData("defUseEditMode", false);
            item.SetData("description", String.Empty);
            item.SetData("imageBackgroundEnabled", false);
            item.SetData("imageUrl", String.Empty);

            items.Add(item);
        }
        #endregion
    }
}