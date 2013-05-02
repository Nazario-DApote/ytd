using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ytd.PlayList
{
    internal class PlayListManager
    {
        private List<VideoItem> _playList;

        internal IList<VideoItem> PlayList
        {
            get { return _playList; }
        }

        public PlayListManager()
        {
            _playList = new List<VideoItem>();
        }

        /// <summary>
        ///  Regular expression built for C# on: lun, apr 29, 2013, 05:52:28
        ///  Using Expresso Version: 3.0.3634, http://www.ultrapico.com
        ///
        ///  A description of the regular expression:
        ///
        ///  Match expression but don't capture it. [http://www.youtube.com/playlist\?&?list=]
        ///      http://www.youtube.com/playlist\?&?list=
        ///          http://www
        ///          Any character
        ///          youtube
        ///          Any character
        ///          com/playlist
        ///          Literal ?
        ///          &, zero or one repetitionslist=
        ///  [Pid]: A named capture group. [.+]
        ///      Any character, one or more repetitions
        ///  &, zero or one repetitions
        ///
        ///
        /// </summary>
        private static Regex regexPlayList = new Regex(
              "(?:http://www.youtube.com/playlist\\?&?list=)(?<Pid>.+)&?",
            RegexOptions.CultureInvariant
            | RegexOptions.Compiled
            );

        public void Fetch(string url)
        {
            _playList.Clear();

            Match m = regexPlayList.Match(url);
            if ( m.Success && m.Groups[1].Success )
            {
                string feedUrl = string.Concat("http://gdata.youtube.com/feeds/api/playlists/", m.Groups[1].Value);
                RssManager rm = new RssManager(feedUrl);
                _playList.AddRange(rm.GetFeed());
            }
        }

        public void Save(string fileName, bool full = true)
        {
            using ( StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8) )
            {
                foreach ( var video in _playList )
                {
                    if ( full )
                    {
                        sw.WriteLine("Title: {0}", video.Title);
                        sw.WriteLine("Description: {0}", video.Description);
                        sw.WriteLine("Author: {0}", video.Author);
                        sw.WriteLine("Date: {0}", video.Date);
                        sw.WriteLine("Link: {0}", video.Link);
                        sw.WriteLine();
                    }
                    else
                    {
                        sw.WriteLine(video.Link);
                    }
                }
            }
        }
    }
}