using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;

namespace ytd.PlayList
{
    /// <summary>
    /// Class to parse and display RSS Feeds
    /// </summary>
    internal class RssManager : IDisposable
    {
        #region Variables

        private string _feedTitle;
        private List<VideoItem> _rssItems = new List<VideoItem>();
        private bool _IsDisposed;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Empty constructor, allowing us to
        /// instantiate our class and set our
        /// _url variable to an empty string
        /// </summary>
        public RssManager()
        {
            Url = string.Empty;
        }

        /// <summary>
        /// Constructor allowing us to instantiate our class
        /// and set the _url variable to a value
        /// </summary>
        /// <param name="feedUrl">The URL of the Rss feed</param>
        public RssManager(string feedUrl)
        {
            Url = feedUrl;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the URL of the RSS feed to parse.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets all the items in the RSS feed.
        /// </summary>
        public IList<VideoItem> RssItems
        {
            get { return _rssItems; }
        }

        /// <summary>
        /// Gets the title of the RSS feed.
        /// </summary>
        public string Title
        {
            get { return _feedTitle; }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Retrieves the remote RSS feed and parses it.
        /// </summary>
        public IList<VideoItem> GetFeed()
        {
            //check to see if the FeedURL is empty
            if ( String.IsNullOrEmpty(Url) )
                //throw an exception if not provided
                throw new ArgumentException("You must provide a feed URL");
            //start the parsing process
#if TEST_RSS
            using ( StreamReader sr = new StreamReader("debug_feed.xml"))
            using ( XmlReader reader = XmlReader.Create(sr) )
#else
            using ( XmlReader reader = XmlReader.Create(Url) )
#endif
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(reader);

                //#if !TEST_RSS
                //                xmlDoc.Save("debug_feed.xml");
                //#endif

                //parse the items of the feed
                SetNameSpaceMngr(xmlDoc);
                GetFeedTitle(xmlDoc);
                _rssItems.AddRange(ParseRssItems(xmlDoc));

                string rel = GetRelatedFeed(xmlDoc);
                while ( !string.IsNullOrEmpty(rel) )
                {
                    using ( XmlReader relReader = XmlReader.Create(rel) )
                    {
                        XmlDocument xmlRelDoc = new XmlDocument();
                        xmlRelDoc.Load(relReader);
                        _rssItems.AddRange(ParseRssItems(xmlRelDoc));
                        rel = GetRelatedFeed(xmlRelDoc);
                    }
                }

                //return the feed items
                return _rssItems;
            }
        }

        private string GetRelatedFeed(XmlDocument xmlDoc)
        {
            string feedRelation = null;

            XmlNode relationNode = xmlDoc.SelectSingleNode("/default:feed/default:link[@rel='next']", _nmsmngr);
            if ( relationNode != null )
            {
                XmlAttribute hrefAttrib = relationNode.Attributes["href"];
                if ( hrefAttrib != null )
                {
                    feedRelation = hrefAttrib.InnerText;
                }
            }

            return feedRelation;
        }

        private void GetFeedTitle(XmlDocument xmlDoc)
        {
            XmlNode feedNode = xmlDoc.SelectSingleNode("/default:feed", _nmsmngr);
            ParseDocElements(feedNode, "default:title", ref _feedTitle);
        }

        /// <summary>
        /// Parses the xml document in order to retrieve the RSS items.
        /// </summary>
        private List<VideoItem> ParseRssItems(XmlDocument xmlDoc)
        {
            List<VideoItem> rssItems = new List<VideoItem>();

            XmlNodeList nodes = xmlDoc.SelectNodes("/default:feed/default:entry", _nmsmngr);

            foreach ( XmlNode node in nodes )
            {
                VideoItem item = new VideoItem();
                ParseDocElements(node, "default:title", ref item.Title);
                ParseDocElements(node, "default:content", ref item.Description);
                ParseDocElements(node, "default:author/default:name", ref item.Author);

                XmlNode videoLinkNode = node.SelectSingleNode("default:link[@rel='alternate']", _nmsmngr);
                if ( videoLinkNode != null )
                {
                    XmlAttribute hrefAttrib = videoLinkNode.Attributes["href"];
                    if ( hrefAttrib != null )
                    {
                        // Clean the video link from other parameters
                        var urlBuilder = new UriBuilder(hrefAttrib.InnerText);
                        var values = HttpUtility.ParseQueryString(urlBuilder.Query);
                        string videoParam = values["v"];
                        item.Link = string.Concat(urlBuilder.Scheme, "://", urlBuilder.Host, urlBuilder.Path, "?v=", videoParam);
                    }
                }

                string date = null;
                ParseDocElements(node, "default:published", ref date);
                DateTime.TryParse(date, out item.Date);

                rssItems.Add(item);
            }

            return rssItems;
        }

        private XmlNamespaceManager _nmsmngr;

        private void SetNameSpaceMngr(XmlDocument xmlDoc)
        {
            _nmsmngr = new XmlNamespaceManager(xmlDoc.NameTable);
            _nmsmngr.AddNamespace(string.Empty, "http://www.w3.org/2005/Atom");
            _nmsmngr.AddNamespace("media", "http://search.yahoo.com/mrss/");
            _nmsmngr.AddNamespace("openSearch", "http://a9.com/-/spec/opensearchrss/1.0/");
            _nmsmngr.AddNamespace("gd", "http://schemas.google.com/g/2005");
            _nmsmngr.AddNamespace("yt", "http://gdata.youtube.com/schemas/2007");
            _nmsmngr.AddNamespace("default", "http://www.w3.org/2005/Atom");
        }

        /// <summary>
        /// Parses the XmlNode with the specified XPath query
        /// and assigns the value to the property parameter.
        /// </summary>
        private void ParseDocElements(XmlNode parent, string xPath, ref string property)
        {
            if ( parent != null )
            {
                XmlNode node = parent.SelectSingleNode(xPath, _nmsmngr);
                if ( node != null )
                    property = node.InnerText;
            }
        }

        #endregion Methods

        #region IDisposable Members

        /// <summary>
        /// Performs the disposal.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if ( disposing && !_IsDisposed )
            {
                _rssItems.Clear();
                Url = null;
                _feedTitle = null;
            }

            _IsDisposed = true;
        }

        /// <summary>
        /// Releases the object to the garbage collector
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Members
    }
}