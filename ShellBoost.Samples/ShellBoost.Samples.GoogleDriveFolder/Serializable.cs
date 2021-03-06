﻿using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    // a utility class to save persist an object into an Xml file
    public abstract class Serializable<T> where T : new()
    {
        public static void RemoveAll(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                return;

            Directory.Delete(dir, true);
        }

        public static T Deserialize(string filePath) => Deserialize(filePath, new T());
        public static T Deserialize(string filePath, T defaultValue)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                return defaultValue;

            try
            {
                using (var reader = new XmlTextReader(filePath))
                {
                    return Deserialize(reader, defaultValue);
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        public static T Deserialize(Stream stream) => Deserialize(stream, new T());
        public static T Deserialize(Stream stream, T defaultValue)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            try
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(stream);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static T Deserialize(TextReader reader) => Deserialize(reader, new T());
        public static T Deserialize(TextReader reader, T defaultValue)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            try
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(reader);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static T Deserialize(XmlReader reader) => Deserialize(reader, new T());
        public static T Deserialize(XmlReader reader, T defaultValue)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            try
            {
                var deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(reader);
            }
            catch
            {
                return defaultValue;
            }
        }

        public void Serialize(XmlWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var serializer = new XmlSerializer(GetType());
            serializer.Serialize(writer, this);
        }

        public void Serialize(TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var serializer = new XmlSerializer(GetType());
            serializer.Serialize(writer, this);
        }

        public void Serialize(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var serializer = new XmlSerializer(GetType());
            serializer.Serialize(stream, this);
        }

        public void Serialize(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var writer = new XmlTextWriter(filePath, Encoding.UTF8))
            {
                Serialize(writer);
            }
        }

        public static string BackupDirectoryName => "bak";
        public static void Backup(string filePath, TimeSpan? maxDuration = null)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            if (!File.Exists(filePath))
                return;

            var bakPath = Path.Combine(Path.GetDirectoryName(filePath), BackupDirectoryName, string.Format("{0:yyyy}_{0:MM}_{0:dd}.{1}.xml", DateTime.Now, Environment.TickCount));
            var dir = Path.GetDirectoryName(bakPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.Copy(filePath, bakPath, true);

            if (maxDuration.HasValue)
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    if (string.Compare(file, bakPath, StringComparison.OrdinalIgnoreCase) == 0)
                        continue;

                    var name = Path.GetFileNameWithoutExtension(file);
                    var tick = name.IndexOf('.');
                    if (tick < 0)
                        continue;

                    var dateName = name.Substring(0, tick).Split('_');
                    if (dateName.Length != 3)
                        continue;

                    var month = 0;
                    var day = 0;
                    if (!int.TryParse(dateName[0], out var year) &&
                        !int.TryParse(dateName[1], out month) &&
                        !int.TryParse(dateName[2], out day))
                        continue;

                    DateTime dt;
                    try
                    {
                        dt = new DateTime(year, month, day);
                    }
                    catch
                    {
                        continue;
                    }

                    if ((DateTime.Now - dt) > maxDuration.Value)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // continue
                        }
                    }
                }
            }
        }
    }
}
