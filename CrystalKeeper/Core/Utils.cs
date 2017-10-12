using System;
using System.IO;
using CrystalKeeper.GuiCore;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace CrystalKeeper.Core
{
    /// <summary>
    /// Contains a miscellaneous collection of useful helper methods.
    /// </summary>
    static class Utils
    {
        #region Members
        /// <summary>
        /// Stores paths to recently opened files.
        /// </summary>
        private static string regOpenRecent;
        #endregion

        #region Static Constructor
        /// <summary>
        /// Initializes static members.
        /// </summary>
        static Utils()
        {
            regOpenRecent = String.Empty;
            RegistryKey key;

            //Opens or creates the main crystal keeper entry.
            key = Registry.CurrentUser
                .CreateSubKey("software",
                RegistryKeyPermissionCheck.ReadWriteSubTree)
                .CreateSubKey("Crystal Keeper",
                RegistryKeyPermissionCheck.ReadWriteSubTree);

            //Gets recent files.
            object recentFiles = key.GetValue("recentFiles");
            if (recentFiles != null)
            {
                regOpenRecent = (string)recentFiles;
            }

            key.Close();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns a valid url in the appdata folder using the
        /// path information of the given url. The resulting file
        /// path is guaranteed to be unique from any existing files
        /// in the appdata folder.
        /// </summary>
        /// <param name="relUrl">
        /// A relative url to append to the base appdata url.
        /// </param>
        public static string GetAppdataFolder(string relUrl)
        {
            string dir = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData)
                + "\\Crystal Keeper\\";

            if (relUrl == String.Empty)
            {
                return dir;
            }

            try
            {
                //Removes the root and appends it to the appdata folder.
                string newUrl = relUrl.Substring(Path.GetPathRoot(relUrl).Length);
                string newUrlExt = Path.GetExtension(newUrl);
                newUrl = MakeAbsoluteUrl(dir, newUrl);
                newUrl = newUrl.Substring(0, newUrl.Length - newUrlExt.Length);

                //Creates the directories if they don't exist.
                Directory.CreateDirectory(Path.GetDirectoryName(newUrl));

                //Ensures a unique filepath is generated.
                Random rng = new Random();
                int uniqueNum = rng.Next();

                while (File.Exists(newUrl + uniqueNum + newUrlExt))
                {
                    uniqueNum = rng.Next();
                }

                return newUrl + uniqueNum + newUrlExt;
            }
            catch (ArgumentException e)
            {
                Log("Bad url with GetAppdataFolder(): " + e.StackTrace);
                return dir;
            }
        }

        /// <summary>
        /// Returns a list of recently opened urls, if any.
        /// </summary>
        public static string GetRecentlyOpened()
        {
            return regOpenRecent;
        }

        /// <summary>
        /// Stores up to 10 unique urls as recently opened files.
        /// </summary>
        public static void RegAddRecentlyOpen(string url)
        {
            List<string> urls = new List<string>();
            if (regOpenRecent != String.Empty)
            {
                urls = regOpenRecent.Split('|').ToList();
            }

            //Inserts the url at the start of the pipe-separated list.
            urls.Remove(url);
            urls.Insert(0, url);
            if (urls.Count > 3)
            {
                urls.RemoveAt(urls.Count - 1);
            }

            //Merges all urls into one string.
            url = String.Empty;
            for (int i = 0; i < urls.Count; i++)
            {
                url += urls[i];
                if (i != urls.Count - 1)
                {
                    url += "|";
                }
            }

            regOpenRecent = url;
            SaveRegistryValues();
        }

        /// <summary>
        /// Removes the given url from the recently open files list,
        /// returning success.
        /// </summary>
        public static bool RegRemoveRecentlyOpen(string url)
        {
            List<string> urls = regOpenRecent.Split('|').ToList();

            //Removes the url.
            if (urls.Remove(url))
            {
                //Merges all urls into one string.
                url = String.Empty;
                for (int i = 0; i < urls.Count; i++)
                {
                    url += urls[i];
                    if (i != urls.Count - 1)
                    {
                        url += "|";
                    }
                }

                regOpenRecent = url;
                SaveRegistryValues();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Logs a message, pre-formatted with the date, to logs.txt.
        /// </summary>
        /// <param name="message">
        /// The message to write.
        /// </param>
        public static void Log(string message)
        {
            DateTime now = DateTime.Now;

            message =
                now.Month + " / " +
                now.Day + " / " +
                now.Year + " at " +
                now.Hour + ":" +
                now.Minute + ":" +
                now.Second + " | " + message;

            File.AppendAllText("logs.txt", message + "\n");
        }

        /// <summary>
        /// Iterates through every item up to a depth of 5 and returns the
        /// item matching the given condition, or null if not found.
        /// </summary>
        public static TreeViewDataItem Find(
            this TreeViewDataItem item,
            Func<TreeViewDataItem, bool> condition)
        {
            //Tests the current item if it matches.
            if (condition(item))
            {
                return item;
            }

            //Iterates through each database, collection, grouping, and entry
            //to find the desired item.
            for (int col = 0; col < item.Items.Count; col++)
            {
                var colItem = (TreeViewDataItem)item.Items[col];

                if (colItem == null)
                {
                    continue;
                }
                else if (condition(colItem))
                {
                    return colItem;
                }

                for (int grp = 0; grp < colItem.Items.Count; grp++)
                {
                    var grpItem = (TreeViewDataItem)colItem.Items[grp];

                    if (grpItem == null)
                    {
                        continue;
                    }
                    else if (condition(grpItem))
                    {
                        return grpItem;
                    }

                    for (int ent = 0; ent < grpItem.Items.Count; ent++)
                    {
                        var entItem = (TreeViewDataItem)grpItem.Items[ent];

                        if (entItem == null)
                        {
                            continue;
                        }
                        else if (condition(entItem))
                        {
                            return entItem;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Converts an object to an array of bytes representing the object.
        /// </summary>
        /// <param name="obj">
        /// The object to convert into a binary array.
        /// </param>
        public static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            else if (obj is byte[])
            {
                return obj as byte[];
            }

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                bf.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Converts an array of bytes representing an object to the object.
        /// Cast it to the appropriate instance as necessary. May throw
        /// SerializableException.
        /// </summary>
        /// <param name="byteArray">
        /// The array of bytes representing the object.
        /// </param>
        /// <exception cref="System.Runtime.Serialization.SerializationException">
        /// Thrown when the byte array to decode isn't recognized.
        /// </exception>
        public static Object ByteArrayToObject(byte[] byteArray)
        {
            Object obj;

            using (MemoryStream memStream = new MemoryStream())
            {
                BinaryFormatter binForm = new BinaryFormatter();
                memStream.Write(byteArray, 0, byteArray.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                obj = binForm.Deserialize(memStream);
            }

            return obj;
        }

        /// <summary>
        /// Makes the second url relative to the first, meaning it's only a
        /// correct url when the first is prepended to it. Returns the second
        /// url if it's already relative. Returns an empty string if an error
        /// occurs.
        /// </summary>
        /// <param name="baseUrl">
        /// The url that the second url string should be relative to.
        /// </param>
        /// <param name="otherUrl">
        /// The url string to make relative to the first string.
        /// </param>
        public static string MakeRelativeUrl(string fromUrl, string toUrl)
        {
            //If there is nothing to make relative, returns nothing.
            if (toUrl.Trim() == String.Empty)
            {
                return String.Empty;
            }

            //If path is absolute.
            if (Path.IsPathRooted(toUrl))
            {
                try
                {
                    return Uri.UnescapeDataString(new Uri(fromUrl)
                        .MakeRelativeUri(new Uri(toUrl)).ToString());
                }
                catch (ArgumentNullException e)
                {
                    Log("Null argument in MakeRelativeUrl: " + e.Message);
                    return String.Empty;
                }
                catch (UriFormatException e)
                {
                    Log("Format exception in MakeRelativeUrl: " + e.Message);
                    return String.Empty;
                }
            }

            //If path is already relative.
            else
            {
                return toUrl;
            }
        }

        /// <summary>
        /// Appends relative second urls to the first. Returns the second
        /// url if it's already absolute.
        /// </summary>
        /// <param name="baseUrl">
        /// The absolute url prepended to the relative component.
        /// </param>
        /// <param name="absUrl">
        /// The relative url to be made absolute.
        /// </param>
        public static string MakeAbsoluteUrl(string baseUrl, string absUrl)
        {
            //If there is no base url, returns the rest.
            if (baseUrl == String.Empty)
            {
                return absUrl;
            }

            //Ensures the url strings are correct filepaths.
            baseUrl = Uri.UnescapeDataString(Path.GetDirectoryName(baseUrl).Replace('/', '\\'));
            absUrl = Uri.UnescapeDataString(absUrl.Replace('/', '\\'));

            //If there is nothing to make absolute, returns nothing.
            if (absUrl.Trim() == String.Empty)
            {
                return String.Empty;
            }

            if (!Path.IsPathRooted(absUrl))
            {
                return baseUrl + "\\" + absUrl;
            }
            else
            {
                return absUrl;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Saves all values for the registry in their current state.
        /// </summary>
        private static void SaveRegistryValues()
        {
            RegistryKey key;

            //Opens or creates the main crystal keeper entry.
            key = Registry.CurrentUser
                .CreateSubKey("software",
                RegistryKeyPermissionCheck.ReadWriteSubTree)
                .CreateSubKey("Crystal Keeper",
                RegistryKeyPermissionCheck.ReadWriteSubTree);

            //Stores recent file URLs.
            key.SetValue("recentFiles", regOpenRecent);
            key.Close();
        }
        #endregion
    }
}
