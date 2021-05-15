using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolderSite.Utilities
{
    public static class IOExtensions
    {
        private static readonly char[] _invalidChars = Path.GetInvalidFileNameChars();
        private static readonly string[] _reservedFileNames = new[]
        {
            "con", "prn", "aux", "nul",
            "com0", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
            "lpt0", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9",
        };

        private static string EscapeReservedName(string reservedName) => "｟" + reservedName + "｠";
        private static string EscapeInvalidChar(char invalidChar) => "｟u" + ((short)invalidChar).ToString("X4") + "｠"; // note: it's ok, no reserved name starts with 'u'
        private static bool IsAllDots(string fileName) => !fileName.Any(c => c != '.');

        public static string EscapeNameToFileName(string text, int maxLength = 255)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (Array.IndexOf(_reservedFileNames, text.ToLowerInvariant()) >= 0 || IsAllDots(text))
                return EscapeReservedName(text);

            var sb = new StringBuilder(Math.Min(text.Length, maxLength - 1));
            foreach (var c in text)
            {
                if (Array.IndexOf(_invalidChars, c) >= 0)
                {
                    sb.Append(EscapeInvalidChar(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            var s = sb.ToString();
            if (s.Length >= maxLength) // a segment is always 255 max even with long file names
            {
                s = s.Substring(0, maxLength - 1);
            }

            if (s.EqualsIgnoreCase(text))
                return text;

            return s;
        }

        public static string UnescapeFileNameToName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return fileName;

            var sb = new StringBuilder(fileName.Length);
            for (var i = 0; i < fileName.Length; i++)
            {
                var c = fileName[i];
                if (c != '｟')
                {
                    sb.Append(c);
                    continue;
                }

                //+ minimum len between parens is 3
                var pos = fileName.IndexOf('｠', Math.Min(fileName.Length, i + 4));
                if (pos < 0)
                {
                    sb.Append(c);
                    continue;
                }

                if (fileName[i + 1] == 'u')
                {
                    var text = fileName.Substring(i + 2, pos - i - 2);
                    if (short.TryParse(text, NumberStyles.HexNumber, null, out var s))
                    {
                        sb.Append((char)s);
                    }
                    else
                    {
                        sb.Append(c);
                        sb.Append('u');
                        sb.Append(text);
                        sb.Append('｠');
                    }
                    i += pos - i;
                }
                else
                {
                    for (var j = i + 1; j < pos; j++)
                    {
                        sb.Append(fileName[j]);
                        i++;
                    }
                    i++;
                }
            }
            return sb.ToString();
        }

        public static string NormalizePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (!Path.IsPathRooted(path))
                throw new ArgumentException(null, nameof(path));

            const string longFileNamePrefix = @"\\?\";
            if (!path.StartsWith(longFileNamePrefix))
                return longFileNamePrefix + path;

            return path;
        }

        public static ConsoleKeyInfo ConsoleReadKey(int timeout, bool intercept = true, int slice = 50)
        {
            var waited = 0;
            slice = Math.Max(15, slice);
            while (waited < timeout)
            {
                if (Console.KeyAvailable)
                    return Console.ReadKey(intercept);

                Thread.Sleep(slice);
                waited += slice;
            }
            return new ConsoleKeyInfo((char)0, 0, false, false, false);
        }

        public static void DirectoryCopy(string sourcePath, string targetPath, bool throwOnError = true)
        {
            if (sourcePath == null)
                throw new ArgumentNullException(nameof(sourcePath));

            if (targetPath == null)
                throw new ArgumentNullException(nameof(targetPath));

            DirectoryCopy(new DirectoryInfo(sourcePath), new DirectoryInfo(targetPath), throwOnError);
        }

        public static void DirectoryCopy(DirectoryInfo source, DirectoryInfo target, bool throwOnError = true)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (!target.Exists)
            {
                if (throwOnError)
                {
                    target.Create();
                }
                else
                {
                    try
                    {
                        target.Create();
                    }
                    catch
                    {
                        return;
                    }
                }
            }

            foreach (var file in source.EnumerateFiles())
            {
                if (throwOnError)
                {
                    file.CopyTo(Path.Combine(target.FullName, file.Name), true);
                }
                else
                {
                    try
                    {
                        file.CopyTo(Path.Combine(target.FullName, file.Name), true);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            foreach (var dir in source.EnumerateDirectories())
            {
                DirectoryInfo subDir;
                if (throwOnError)
                {
                    subDir = target.CreateSubdirectory(dir.Name);
                }
                else
                {
                    try
                    {
                        subDir = target.CreateSubdirectory(dir.Name);
                    }
                    catch
                    {
                        continue;
                    }
                }

                DirectoryCopy(dir, subDir, throwOnError);
            }
        }
    }
}
