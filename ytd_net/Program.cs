using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Extensions;
using System.Threading;
using NDesk.Options;
using ytd.Downloader;
using ytd.PlayList;
using System.Reflection;

namespace ytd
{
    internal class Program
    {
        private static bool help;
        private static int verbose;
        private static string playlist;
        private static bool full;
        private static bool download;
        private static bool filelist;
        private static ManualResetEvent downloadDone;
        private static string fileName;
        private static int videoWidth;
        private static double videoSize;
        private static string videoExtension;

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);
            var p = new OptionSet() {
                { "p|playlist=", "get all video data from a playlist link and save them to VALUE", v => { playlist = v; full = true;} },
                { "p1", "get only video urls from a playlist link and save them to VALUE", v => { playlist = v; full = false; } },
                { "d|download", "download the video url and save to output.", v => { download = (v != null); } },
                { "l|list", "consider the <url> parameter as a list of links.", v => { filelist = (v != null); } },
                { "o|output=", "specify the output file name.", v => { fileName = v; } },
                { "w|width=", "specify the preferred video width.", v => { Int32.TryParse(v, out videoWidth); } },
                { "s|size=", "specify the preferred video file size.", v => { Double.TryParse(v, out videoSize); } },
                { "e|ext=", "specify the preferred video file extension.", v => { videoExtension = v; } },
   	            { "v|verbose", "show verbose message.",  v => { ++verbose; } },
   	            { "h|?|help", "print the help.",  v => { help = (v != null); } },
               };

            string url = null;

            try
            {
                List<string> extra = p.Parse(args);

                if ( !help )
                {
                    if ( (extra.Count == 0) )
                        throw new ArgumentException("Mandatory parameter missing!");

                    url = extra[0];

                    if ( string.IsNullOrEmpty(videoExtension) )
                        videoExtension = "flv";
                }
            }
            catch ( OptionException e )
            {
                ShowError(e);
            }
            catch ( ArgumentException e )
            {
                ShowError(e);
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
                    {
                        if ( File.Exists(url) )
                            DownloadVideoList(url);
                        else
                            throw new FileNotFoundException("File list not found!", url);
                    }
                    else
                        DownloadVideo(url, fileName);
                }
                else if ( help )
                    ShowHelp(p);
                else
                    throw new ArgumentException("Invalid parameters!");
            }
            catch ( Exception e )
            {
                ShowError(e);
            }
#if DEBUG
            finally
            {
                WaitKeyPress();
            }
#endif
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled exception has occured!");
            Console.WriteLine("Please report to author: {0}", AppInfo.AuthorEmail);
            Type exceptionType = e.ExceptionObject.GetType();
            Exception ex = e.ExceptionObject as Exception;
            Debug("{0}: {1}", exceptionType.ToString(), ex.Message);
            Environment.Exit(-1);
        }

        private static void ShowError(Exception e)
        {
            Console.WriteLine(e.Message);
            Debug("StackTrace:\r\n{0}", e.StackTrace);

            if ( e.InnerException != null )
                Console.WriteLine(e.InnerException.Message);

            Console.WriteLine("Try `{0} --help' for more information.", AppInfo.ExecutableName);
            Environment.Exit(-1);
        }

        private static void DownloadVideoList(string listPath)
        {
            using ( StreamReader listReader = new StreamReader(listPath) )
            {
                List<string> urlList = new List<string>();
                string line = null;
                while ( (line = listReader.ReadLine()) != null)
                {
                    bool isUri = Uri.IsWellFormedUriString(line, UriKind.RelativeOrAbsolute);
                    if ( isUri )
                        urlList.Add(line);
                }


                foreach ( var url in urlList )
                {
                    DownloadVideo(url, null);
                }
            }
        }

        private static void DownloadVideo(string url, string fileName)
        {
            Console.WriteLine("* Download YourTube video");

            Console.WriteLine("Fetching video informations ...");
            List<YouTubeVideoQuality> videoList = YouTubeDownloader.GetYouTubeVideoUrls(url);
            if ( videoList.Count > 0 )
            {
                downloadDone = new ManualResetEvent(false);

                YouTubeVideoQuality video = GetPreferred(videoList);

                if ( video != null )
                {
                    string fn = fileName;

                    if ( string.IsNullOrEmpty(fn) )
                        fn = string.Concat(video.VideoTitle, ".", video.Extention);

                    Console.WriteLine("url: {0}", url);
                    Console.WriteLine("fileName: \"{0}\"", fn);

                    if ( File.Exists(fn) )
                    {
                        Console.Write("\"{0}\" already exists! Overwrite? (y/N) ", fn);
                        char res = ConsoleEx.ReadKey(5, 'n');
                        if ( res.Equals('n') )
                            File.Delete(fn);
                        else
                            return;
                    }

                    downloadDone.Reset();
                    Console.WriteLine("Video Title: \"{0}\"", video.VideoTitle);
                    Console.WriteLine("File lenght: {0}", new FileSize(video.VideoSize).ToString(FileSizeUnit.B));
                    Console.WriteLine("* Downloading {0} => \"{1}\"", ConsoleEx.CompactPath(video.DownloadUrl, 40), ConsoleEx.CompactPath(fileName, 40));
                    DownloadFile(video.DownloadUrl, fn);
                    downloadDone.WaitOne();
                }
                else
                    Console.WriteLine("No video match conditions!");
            }
            else
            {
                Console.WriteLine("The <url> specified doesn't contain videos");
            }
        }

        private static YouTubeVideoQuality GetPreferred(List<YouTubeVideoQuality> videoList)
        {
            YouTubeVideoQuality userPreferredVideo = null;

            double videoDist = Double.MaxValue;
            foreach ( var video in videoList )
            {
                if ( userPreferredVideo == null )
                    userPreferredVideo = video;

                if ( videoWidth > 0 || videoSize > 0 )
                {
                    double curVideoDist = GetVideoDistance(video);
                    if ( videoDist > curVideoDist )
                    {
                        videoDist = curVideoDist;
                        userPreferredVideo = video;
                    }

                    if ( videoDist == curVideoDist && string.Compare(video.Extention, videoExtension, true) == 0 )
                        userPreferredVideo = video;
                }
                else if ( string.Compare(video.Extention, videoExtension, true) == 0 )
                {
                    userPreferredVideo = video;
                }
            }

            return userPreferredVideo;
        }

        private static double GetVideoDistance(YouTubeVideoQuality video)
        {
            double f1 = 0.0;
            if ( videoWidth > 0 )
                f1 = Math.Pow(Math.Abs(video.Dimension.Width - videoWidth), 2);

            double f2 = 0.0;
            if ( videoSize > 0 )
                f2 = Math.Pow(Math.Abs(video.VideoSize - videoSize), 2);

            return Math.Sqrt(f1 + f2);
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
            ConsoleEx.GetSyncObject.WaitOne();

            Console.ResetColor();

            if ( Console.CursorLeft != 0 )
                Console.WriteLine();

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
                    string ETA = downloader.ETA == 0 ? "" : " [" + FormatLeftTime.Format(((long) downloader.ETA) * 1000) + "]";
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
            Console.WriteLine("Usage: {0} [OPTIONS]+ <url>", AppInfo.ExecutableName);
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