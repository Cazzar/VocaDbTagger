using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VocaDb.Model.DataContracts.Albums;

namespace MusicTool
{
    public class AlbumUtil
    {
        public static AlbumForApiContract GetAlbumById(uint id, string api = "http://vocadb.net", bool verbose = false)
        {
            var client = new WebClient(verbose);
            return
                Deserialize<AlbumForApiContract>(
                    Encoding.UTF8.GetString(
                        client.DownloadData(
                            String.Format("{1}/api/albums/{0}?fields=tracks&songFields=artists,lyrics", id, api))));

        }

        public static T Deserialize<T>(string rawXml)
        {
            using (var reader = XmlReader.Create(new StringReader(rawXml) ))
            {
                var formatter0 = new DataContractSerializer(typeof(T));
                return (T)formatter0.ReadObject(reader);
            }
        }
    }
}
