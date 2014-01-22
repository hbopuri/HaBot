using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace haBot.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult SiteMap()
        {
            return View();
        }

        [HttpPost]
        public FileResult SiteMap(string siteUrl = "http://www.harshabopuri.com")
        {
            var mypagelist = haBotLibrary.SiteMap.GetAllPagesUnder(new Uri(siteUrl));
            XDocument xDoc = haBotLibrary.SiteMap.BuildXML(mypagelist);
            ViewBag.Title = siteUrl;
            string myHost = new Uri(siteUrl).Host;
            if (xDoc != null)
            {
                string xml = xDoc.ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(xml);
                return File(bytes, "text/xml", myHost + ".xml");

            }
            return File("", "text/html");
        }
    }
}
