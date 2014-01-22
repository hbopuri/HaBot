using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace haBotLibrary
{
    public class SiteMap
    {
        public Uri Url { get; set; }

        public static IEnumerable<SiteMap> GetAllPagesUnder(Uri urlRoot)
        {
            var queue = new Queue<Uri>();
            var allSiteUrls = new HashSet<Uri>();

            queue.Enqueue(urlRoot);
            allSiteUrls.Add(urlRoot);

            while (queue.Count > 0)
            {
                Uri url = queue.Dequeue();

                HttpWebRequest oReq = (HttpWebRequest)WebRequest.Create(url);
                oReq.UserAgent = @"Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.1.5) HarshaBopuri.com/20130406 Firefox/3.5.5";

                HttpWebResponse resp = (HttpWebResponse)oReq.GetResponse();

                SiteMap result;

                if (resp.ContentType.StartsWith("text/html", StringComparison.InvariantCultureIgnoreCase))
                {
                    HtmlDocument doc = new HtmlDocument();
                    try
                    {
                        var resultStream = resp.GetResponseStream();
                        doc.Load(resultStream); // The HtmlAgilityPack
                        result = new Internal() { Url = url, HtmlDocument = doc };
                    }
                    catch (System.Net.WebException ex)
                    {
                        result = new SiteMap.Error() { Url = url, Exception = ex };
                    }
                    catch (Exception ex)
                    {
                        ex.Data.Add("Url", url);    // Annotate the exception with the Url
                        throw;
                    }

                    // Success, hand off the page
                    yield return new SiteMap.Internal() { Url = url, HtmlDocument = doc };

                    // And and now queue up all the links on this page
                    foreach (HtmlNode link in doc.DocumentNode.SelectNodes(@"//a[@href]"))
                    {
                        HtmlAttribute att = link.Attributes["href"];
                        if (att == null) continue;
                        string href = att.Value;
                        if (href.StartsWith("javascript", StringComparison.InvariantCultureIgnoreCase)) continue;      // ignore javascript on buttons using a tags

                        Uri urlNext = new Uri(href, UriKind.RelativeOrAbsolute);

                        // Make it absolute if it's relative
                        if (!urlNext.IsAbsoluteUri)
                        {
                            urlNext = new Uri(urlRoot, urlNext);
                        }

                        if (!allSiteUrls.Contains(urlNext))
                        {
                            allSiteUrls.Add(urlNext);               // keep track of every page we've handed off

                            if (urlRoot.IsBaseOf(urlNext))
                            {
                                queue.Enqueue(urlNext);
                            }
                            else
                            {
                                yield return new SiteMap.External() { Url = urlNext };
                            }
                        }

                    }
                }
                if (allSiteUrls.Count() > 500)
                    break;
            }
        }


        ///// <summary>
        ///// In the future might provide all the images too??
        ///// </summary>
        //public class Image : WebPage
        //{
        //}

        /// <summary>
        /// Error loading page
        /// </summary>
        public class Error : SiteMap
        {
            public int HttpResult { get; set; }
            public Exception Exception { get; set; }
        }


        /// <summary>
        /// External page - not followed
        /// </summary>
        /// <remarks>
        /// No body - go load it yourself
        /// </remarks>
        public class External : SiteMap
        {
        }

        /// <summary>
        /// Internal page
        /// </summary>
        public class Internal : SiteMap
        {
            /// <summary>
            /// For internal pages we load the document for you
            /// </summary>
            public virtual HtmlDocument HtmlDocument { get; internal set; }
        }

        public static XDocument BuildXML(IEnumerable<SiteMap> mypagelist, string freequency = "weekly", double priority = 0.5)
        {
            const string ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            const string w3 = "http://www.w3.org/2001/XMLSchema-instance";
            const string schema_location = "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd";

            XNamespace xmlns = XNamespace.Get(ns);
            XNamespace xsi = XNamespace.Get(w3);
            XNamespace schemaLocation = XNamespace.Get(schema_location);

            XDocument xdoc = new XDocument(
                new XElement(xmlns + "urlset",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(xsi + "schemaLocation", schemaLocation)
                ));
            foreach (var item in mypagelist)
            {
                xdoc.Root.Add(GetXMLElement(xmlns, item, freequency, priority));
            }
            return xdoc;
        }

        private static XElement GetXMLElement(XNamespace xnsp, SiteMap item, string freequency, double priority)
        {
            var newElement = new XElement(xnsp + "url",
       new XElement(xnsp + "loc", item.Url),
           new XElement(xnsp + "lastmod", DateTime.Now.Date.ToShortDateString()),
           new XElement(xnsp + "changefreq", freequency),
           new XElement(xnsp + "priority", priority));
            return newElement;
        }
    }
}
