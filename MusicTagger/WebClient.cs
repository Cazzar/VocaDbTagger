using System;
using System.Net;
using System.Reflection;

namespace MusicTagger
{
    class WebClient : System.Net.WebClient
    {

        protected override WebRequest GetWebRequest(Uri address)
        {
            var req = base.GetWebRequest(address) as HttpWebRequest;
            if (req == null) return null;

            req.Accept = "application/xml";
            req.UserAgent = "VocaDB Tagger (" + Program.Version + ") By Cazzar";

            return req;
        }
    }
}
