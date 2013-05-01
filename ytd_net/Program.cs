using System;
using System.Collections.Generic;
using NDesk.Options;
using ytd.PlayList;
using ytd.Downloader;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Collections.Specialized;

namespace ytd
{
    internal class Program
    {
        private static bool help;
        private static int verbose = 0;
        private static string playlist;
        private static bool full;
        private static bool download;
        private static bool filelist;
        private static ManualResetEvent downloadDone;
        private static string fileName;
        private static string size;

        private static void Main(string[] args)
        {
            var p = new OptionSet() {
                { "p|playlist=", "get all video data from a playlist link and save them to VALUE", v => { playlist = v; full = true;} },
                { "p1", "get only video urls from a playlist link and save them to VALUE", v => { playlist = v; full = false; } },
                { "d|download", "download the video url and save to output.", v => { download = (v != null); } },
                { "l|list", "consider the <url> parameter as a list of links.", v => { filelist = (v != null); } },
                { "o|output=", "specify the output file name.", v => { fileName = v; } },
                { "s|size=", "specify the preferred video size.", v => { size = v; } },
   	            { "v|verbose", "show verbose message.",  v => { ++verbose; } },
   	            { "h|?|help", "print the help.",  v => { help = (v != null); } },
               };

            string url = null;

            try
            {
                List<string> extra = p.Parse(args);

                if ( !help && extra.Count < 1 )
                {
                    ShowAppInfo();
                    ShowHelp(p);
                    Environment.Exit(-1);
                }

                url = extra[0];
            }
            catch ( OptionException e )
            {
                Console.Write("greet: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `greet --help' for more information.");
                Environment.Exit(-1);
            }

            // Execution
            try
            {
                ShowAppInfo();

                if ( !string.IsNullOrEmpty(playlist) )
                {
                    ExportPlayList(url, playlist);
                }
                else if ( download )
                {
                    if ( filelist )
                        DownloadVideoList(url);
                    else
                        DownloadVideo(url, fileName);
                }
                else
                    ShowHelp(p);
            }
            catch ( Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                #if DEBUG
                WaitKeyPress();
                #endif
            }
        }

        private static void DownloadVideoList(string listPath)
        {
            using (StreamReader listReader = new StreamReader(listPath))
            {
                List<string> urlList = new List<string>();
                string line = listReader.ReadLine();

                bool isUri = Uri.IsWellFormedUriString(line, UriKind.RelativeOrAbsolute);
                if ( isUri )
                    urlList.Add(line);

                foreach ( var url in urlList )
                {
                    DownloadVideo(url, null);
                }
            }
        }

        private static void DownloadVideo(string url, string fileName)
        {
            Console.WriteLine("* Download YourTuve video");
          
            Console.WriteLine("Fetching video informations ...");
            List<YouTubeVideoQuality> videoList = YouTubeDownloader.GetYouTubeVideoUrls(url);
            if ( videoList.Count > 0 )
            {
                downloadDone = new ManualResetEvent(false);

                foreach ( var video in videoList )
                {
                    if ( string.Compare(video.Extention, "mp4", true) == 0 )
                    {
                        string fn = fileName;

                        if ( string.IsNullOrEmpty(fn) )
                            fn = string.Concat(video.VideoTitle, ".mp4");

                        Console.WriteLine("url: {0}", url);
                        Console.WriteLine("fileName: {0}", fn);

                        if ( File.Exists(fn) )
                        {
                            Console.Write("{0} already exists! Overwrite? (Y/n) ", fn);
                            char res = ConsoleEx.ReadKey(5, 'y');
                            if ( res.Equals('y') )
                                File.Delete(fn);
                            else
                                return;
                        }

                        downloadDone.Reset();
                        Console.WriteLine("* Downloading {0} => {1}", ConsoleEx.CompactPath(video.DownloadUrl, 40), ConsoleEx.CompactPath(fileName, 40));
                        DownloadFile(video.DownloadUrl, fn);
                        downloadDone.WaitOne();
                    }
                }
            }
            else
            {
                Console.WriteLine("The <url> specified doesn't contain videos");
            }
        }

        private static void DownloadFile(string url, string fileName)
        {
            var downloader = new FileDownloader(url, Path.GetDirectoryName(fileName), Path.GetFileName(fileName));
            downloader.ProgressChanged += downloader_ProgressChanged;
            downloader.RunWorkerCompleted += downloader_RunWorkerCompleted;
            downloader.RunWorkerAsync();
        }

        private static void downloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.ResetColor();

            if(Console.CursorLeft != 0)
                Console.WriteLine();

            FileDownloader downloader = sender as FileDownloader;
            if ( downloader != null )
            {
                Console.WriteLine("Download status: {0}", downloader.DownloadStatus);
            }

            downloadDone.Set();
        }

        private static void downloader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                FileDownloader downloader = sender as FileDownloader;
                if ( downloader != null )
                {
                    int percent = e.ProgressPercentage > 100 ? 100 : e.ProgressPercentage;
                    string speed = String.Format(new FileSizeFormatProvider(), "{0:fs}", downloader.DownloadSpeed);
                    string ETA = downloader.ETA == 0 ? "" : "  [ " + FormatLeftTime.Format(((long) downloader.ETA) * 1000) + " ]";
                    ConsoleEx.RenderConsoleProgress(percent, '*', ConsoleColor.Green, string.Format("Downloaded {0}% {1}", percent, speed + ETA));
                }
            }
            catch { }
        }

        private static void WaitKeyPress()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void ExportPlayList(string url, string fileName)
        {
            Console.WriteLine("* Playlist export");
            Console.WriteLine("url: {0}", url);
            Console.WriteLine("fileName: {0}", fileName);

            PlayListManager pl = new PlayListManager();

            Console.WriteLine("Playlist fetching ...");
            pl.Fetch(url);
            Console.WriteLine("Playlist fetched.");

            if ( pl.PlayList.Count > 0 )
            {
                string fn = fileName;

                if ( string.IsNullOrEmpty(fn) )
                    fn = "playlist.txt";

                Console.WriteLine("Playlist saving ...");
                pl.Save(fn, full);
                Console.WriteLine("Playlist saved.");
            }
            else
            {
                Console.WriteLine("Playlist does not contain video.");
            }
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: {0} [OPTIONS]+ <url> <filename>", AppInfo.ProductName);
            Console.WriteLine();
            Console.WriteLine("  {0,-26} {1}", "<url>", "the YouTube playlist url or video url.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        private static void ShowAppInfo()
        {
            Console.WriteLine();
            Console.WriteLine("{0} version {1}", AppInfo.Title, AppInfo.Version);
            Console.WriteLine("{0}", AppInfo.Description);
            Console.WriteLine("{0}", AppInfo.CopyrightHolder);
            Console.WriteLine("===========================================================");
            Console.WriteLine();
        }

        private static void Debug(string format, params object[] args)
        {
            if ( verbose > 0 )
            {
                Console.Write("# ");
                Console.WriteLine(format, args);
            }
        }
    }
}