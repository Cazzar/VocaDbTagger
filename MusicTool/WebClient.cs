using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusicTool
{
    internal class WebClient : System.Net.WebClient
    {
        private readonly bool _verbose;
        public WebClient(bool verbose)
        {
            _verbose = verbose;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            if (_verbose) Console.WriteLine("Downloading URI: {0}", address.ToString());

            var req = base.GetWebRequest(address) as HttpWebRequest;
            if (req == null) return null;

            req.Accept = "application/xml";
            req.UserAgent = "VocaDB Tagger (2.0.0) By Cazzar";

            return req;
        }
    }
}
