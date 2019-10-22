using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ShellBoost.Core.Utilities;
using SqlNado;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    // stores cache of files in Google Drive in a local sqlite database.
    // note: we use open source SQLNado project for interop with sqlite
    // https://github.com/smourier/SQLNado
    public sealed class LocalDatabase : IDisposable
    {
        private readonly SQLiteDatabase _db;

        public LocalDatabase(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            _db = new SQLiteDatabase(path);
            _db.BindOptions.DateTimeFormat = SQLiteDateTimeFormat.RoundTrip;
            var options = _db.CreateSaveOptions();
            options.DeleteUnusedColumns = true;

            _db.SynchronizeSchema<DriveFile>(options).SynchronizeIndices(null);
            _db.SynchronizeSchema<DrivePath>(options).SynchronizeIndices(null);
            _db.SynchronizeSchema<KeyValue>(options).SynchronizeIndices(null);
        }

        // saves all files to the database and delete unused files
        public void SynchronizeAll(IReadOnlyDictionary<string, DriveFile> files)
        {
            if (files == null || files.Count == 0)
                return;

            // gather existing ids
            var toBeDeletedIds = new HashSet<string>(_db.LoadObjects("SELECT Id FROM DriveFile").Select(o => (string)o[0]));

            var options = _db.CreateSaveOptions();
            options.SynchronizeSchema = false;
            options.SynchronizeIndices = false;
            options.UseTransaction = true;
            _db.Save(enumerate());

            // delete id that where not saved
            _db.BeginTransaction();
            foreach (var deletedId in toBeDeletedIds)
            {
                _db.ExecuteNonQuery("DELETE FROM DriveFile WHERE Id=?", deletedId);
                _db.ExecuteNonQuery("DELETE FROM DrivePath WHERE Id=?", deletedId);
            }
            _db.Commit();

            IEnumerable enumerate()
            {
                foreach (var kv in files)
                {
                    if (string.IsNullOrEmpty(kv.Value.Id))
                        throw new ArgumentException("File with relative path '" + kv.Key + "' has no id.", nameof(files));

                    yield return kv.Value;
                    var path = new DrivePath();
                    path.RelativePath = kv.Key;
                    path.Id = kv.Value.Id;

                    // remove from list of to be deleted
                    toBeDeletedIds.Remove(path.Id);
                    yield return path;
                }
            }
        }

        // save a file in the database
        public void SynchronizeOne(string relativePath, DriveFile file)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (string.IsNullOrEmpty(file.Id))
                throw new ArgumentException(null, nameof(file));

            var path = new DrivePath();
            path.RelativePath = relativePath;
            path.Id = file.Id;
            var options = _db.CreateSaveOptions();
            options.SynchronizeSchema = false;
            options.SynchronizeIndices = false;
            options.UseTransaction = false;
            _db.BeginTransaction();
            _db.Save(file);
            _db.Save(path);
            _db.Commit();
        }

        // delete a file from the database using its id
        public void DeleteFile(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            _db.BeginTransaction();
            _db.ExecuteNonQuery("DELETE FROM DriveFile WHERE Id=?", id);
            _db.ExecuteNonQuery("DELETE FROM DrivePath WHERE Id=?", id);
            _db.Commit();
        }

        // get a relative path from an id
        public string GetRelativePath(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var dp = _db.Load<DrivePath>("WHERE Id=?", id).FirstOrDefault();
            if (dp == null)
                return null;

            return dp.RelativePath;
        }

        // get an id from a relative path
        public string GetId(string relativePath)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            var dp = _db.LoadByPrimaryKey<DrivePath>(relativePath);
            if (dp == null)
                return null;

            return dp.Id;
        }

        // get a file using its id
        public DriveFile GetFile(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return _db.LoadByPrimaryKey<DriveFile>(id);
        }

        // get files for a folder using the folder/parent id
        public IEnumerable<DriveFile> GetFolderFiles(string parentId)
        {
            if (parentId == null)
                throw new ArgumentNullException(nameof(parentId));

            return _db.Load<DriveFile>("WHERE ParentId=?", parentId);
        }

        public void Dispose() => _db.Dispose();

        // represents a persisted key+value pair
        private class KeyValue
        {
            public KeyValue()
            {
                LastWriteTime = DateTime.Now;
            }

            [SQLiteColumn(IsPrimaryKey = true)]
            public string Key { get; set; }
            public string Value { get; set; }
            public DateTime LastWriteTime { get; set; }

            public override string ToString() => Key + "=" + Value + " (" + LastWriteTime + ")";
        }

        // we add a persistent key value utilities
        public void SetValue(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var kv = new KeyValue();
            kv.Key = key;
            kv.Value = value;
            _db.Save(kv);
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var kv = _db.LoadByPrimaryKey<KeyValue>(key);
            if (kv == null)
            {
                value = default;
                return false;
            }
            return Conversions.TryChangeType(kv.Value, out value);
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!TryGetValue<T>(key, out var value))
                return defaultValue;

            return value;
        }

        public string GetNullifiedValue(string key) => GetValue(key).Nullify();
        public string GetValue(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return _db.LoadByPrimaryKey<KeyValue>(key)?.Value;
        }

        public void RemoveValue(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var kv = new KeyValue();
            kv.Key = key;
            _db.Delete(kv);
        }
    }
}
