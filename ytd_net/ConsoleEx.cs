using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ytd
{
    public static class ConsoleEx
    {
        private delegate string ReadLineDelegate();

        private delegate ConsoleKeyInfo ReadKeyDelegate();

        private static ManualResetEvent rendererEvent = new ManualResetEvent(false);

        public static EventWaitHandle GetSyncObject
        {
            get { return rendererEvent; }
        }

        /// <summary>
        /// http://stackoverflow.com/questions/57615/how-to-add-a-timeout-to-console-readline
        /// </summary>
        /// <param name="secTimeOut"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public static string ReadLine(int secTimeOut, string @default)
        {
            ReadLineDelegate d = Console.ReadLine;
            IAsyncResult result = d.BeginInvoke(null, null);
            result.AsyncWaitHandle.WaitOne(secTimeOut * 1000); // timeout e.g. 15000 for 15 secs

            if ( result.IsCompleted )
            {
                string resultstr = d.EndInvoke(result);
                //Console.WriteLine("Read: " + resultstr);
                return resultstr;
            }
            else
            {
                //Console.WriteLine("Timed out!");
                //throw new TimedoutException("Timed Out!");

                return @default;
            }
        }

        public static char ReadKey(int secTimeOut, char @default)
        {
            rendererEvent.Reset();

            try
            {
                ReadKeyDelegate d = Console.ReadKey;
                IAsyncResult result = d.BeginInvoke(null, null);
                result.AsyncWaitHandle.WaitOne(secTimeOut * 1000); // timeout e.g. 15000 for 15 secs

                if ( result.IsCompleted )
                {
                    ConsoleKeyInfo resultstr = d.EndInvoke(result);
                    //Console.WriteLine("Read: " + resultstr.KeyChar);

                    Console.WriteLine();
                    return resultstr.KeyChar;
                }
                else
                {
                    //Console.WriteLine("Timed out!");
                    //throw new TimedoutException("Timed Out!");

                    Console.WriteLine();
                    return @default;
                }
            }
            finally
            {
                rendererEvent.Set();
            }
        }

        public static void OverwriteConsoleMessage(string message)
        {
            Console.CursorLeft = 0;
            int maxCharacterWidth = Console.WindowWidth - 1;
            if ( message.Length > maxCharacterWidth )
            {
                message = message.Substring(0, maxCharacterWidth - 3) + "...";
            }
            message = message + new string(' ', maxCharacterWidth - message.Length);
            Console.Write(message);
        }

        public static void RenderConsoleProgress(int percentage)
        {
            RenderConsoleProgress(percentage, '\u2590', Console.ForegroundColor, string.Empty);
        }

        public static void RenderConsoleProgress(int percentage, char progressBarCharacter, ConsoleColor color, string message)
        {
            rendererEvent.Reset();

            try
            {
                Console.CursorVisible = false;
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.CursorLeft = 0;
                int width = Console.WindowWidth - 1;
                int newWidth = (int) ((width * percentage) / 100d);
                string progBar = new string(progressBarCharacter, newWidth) + new string(' ', width - newWidth);
                Console.Write(progBar);
                if ( string.IsNullOrEmpty(message) ) message = string.Empty;
                Console.CursorTop++;
                OverwriteConsoleMessage(message);
                Console.CursorTop--;
                Console.ForegroundColor = originalColor;
                Console.CursorVisible = true;
            }
            finally
            {
                rendererEvent.Set();
            }
        }

        private class NativeMethods
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
            public static extern bool PathCompactPathEx([Out] StringBuilder pszOut, string szPath, int cchMax, int dwFlags);
        }

        public static string CompactPath(string longPathName, int wantedLength)
        {
            // NOTE: You need to create the builder with the required capacity before calling function.
            // See http://msdn.microsoft.com/en-us/library/aa446536.aspx
            StringBuilder sb = new StringBuilder(wantedLength + 1);
            NativeMethods.PathCompactPathEx(sb, longPathName, wantedLength + 1, 0);
            return sb.ToString();
        }
    }
}