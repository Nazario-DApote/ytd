using System.Globalization;

namespace System.IO.Extensions
{
    public enum FileSizeUnit
    {
        B = 0,
        KB,
        MB,
        GB,
        TB,
        PB,
        EB,
        ZB,
        YB,
    }

    public class FileSize
    {
        long _size;

        public FileSize(string fName)
        {
            var fInfo = new FileInfo(fName);
            _size = fInfo.Length;
        }

        public FileSize(FileInfo fInfo)
        {
            _size = fInfo.Length;
        }

        public FileSize(long fSize)
        {
            _size = fSize;
        }

        private static string ReadableFileSize(double size, int unit = 0, bool printUnit = true)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            while (size >= 1024)
            {
                size /= 1024;
                ++unit;
            }
            string outputFmt = null;

            if ( printUnit )
                outputFmt = "{0:0.###}{1}";
            else
                outputFmt = "{0:0.###}";

            return String.Format(CultureInfo.CurrentCulture, outputFmt, size, units[unit]);
        }

        public override string ToString()
        {
            return ReadableFileSize(_size);
        }

        public string ToString(FileSizeUnit unit, bool printUnit = true)
        {
            return ReadableFileSize(_size, (int) unit, printUnit);
        }
    }
}
