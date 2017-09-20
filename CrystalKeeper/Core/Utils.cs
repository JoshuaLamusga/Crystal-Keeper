using System;
using System.IO;
using CrystalKeeper.GuiCore;
using System.Runtime.Serialization.Formatters.Binary;

namespace CrystalKeeper.Core
{
    /// <summary>
    /// Contains a miscellaneous collection of useful helper methods.
    /// </summary>
    static class Utils
    {
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
                    return "";
                }
                catch (UriFormatException e)
                {
                    Log("Format exception in MakeRelativeUrl: " + e.Message);
                    return "";
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
    }
}
