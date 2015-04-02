using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using TagLib;
using TagLib.Id3v2;

namespace MusicTagger
{
    internal class AlbumInfo
    {
        private static readonly WebClient Web = new WebClient {Encoding = Encoding.UTF8};
        private static readonly IEqualityComparer<AlbumInfo> AlbumInfoComparerInstance = new AlbumInfoEqualityComparer();

        private AlbumInfo() : this(null, null, new TrackInfo[0][], null)
        {
        }

        private AlbumInfo(string[] artists, string name, TrackInfo[][] tracks, IPicture picture)
        {
            Name = name;
            Tracks = tracks;
            Picture = picture;
            Artists = artists;
        }

        public static IEqualityComparer<AlbumInfo> AlbumInfoComparer
        {
            get { return AlbumInfoComparerInstance; }
        }

        public string[] Artists { get; private set; }
        public string Name { get; private set; }
        public TrackInfo[][] Tracks { get; private set; }
        public uint Discs { get; private set; }
        public DateTime ReleaseDate { get; private set; }

        public uint TrackCount
        {
            get { return (uint) Tracks.Length; }
        }

        public IPicture Picture { get; private set; }
        public uint VocaDbId { get; private set; }

        public void WriteToFile(File taggedFile)
        {
            var tag = taggedFile.Tag;
            var id3tag = taggedFile.GetTag(TagTypes.Id3v2) as TagLib.Id3v2.Tag;
            var disc = Tracks[Math.Min(tag.Disc - 1, 0)];
            var info = disc[tag.Track - 1];
            var artists = new List<string>();

            for (var i = 0; i < Discs; i++)
            {
                var discArtists = from track in Tracks[i] select track.Artists;
                foreach (var discArtist in discArtists)
                {
                    artists.AddRange(discArtist);
                }
            }

            var uniqueArtists = artists.Distinct().ToList();
            uniqueArtists.Sort((a, b) => artists.Count(v => v == a) - Artists.Count(v => v == b));

            tag.AlbumArtists = artists.Distinct().Count() > 1 ? new []{"Various Artists"} : artists.Distinct().ToArray();
            tag.AlbumArtistsSort = uniqueArtists.ToArray();

            if (id3tag != null) id3tag.IsCompilation = artists.Count > 1;

            tag.Album = Name;
            tag.Performers = info.Artists;
            tag.PerformersSort = info.Artists;
            tag.TrackCount = (uint) disc.Length;
            tag.Track = info.Track;
            tag.Title = info.Title;
            tag.Disc = info.Disc;
            tag.DiscCount = Discs;
            tag.Year = (uint) ReleaseDate.Year;

            var found = false;
            for (var i = 0; i < tag.Pictures.Length; i++)
            {
                if (tag.Pictures[i].Type != PictureType.FrontCover) continue;
                tag.Pictures[i] = new AttachedPictureFrame(Picture);
                found = true;
                break;
            }
            if (found)
            {
                taggedFile.Save();
                return;
            }

            var newPictures = new IPicture[tag.Pictures.Length + 1];
            Array.Copy(tag.Pictures, 0, newPictures, 1, tag.Pictures.Length);
            newPictures[0] = new AttachedPictureFrame(Picture);
            tag.Pictures = newPictures;

            taggedFile.Save();
        }

        public static AlbumInfo GetFromId(uint id)
        {
            var apiResponse = new XmlDocument();
            Console.WriteLine("API Query! (Album Info by ID)");
            apiResponse.LoadXml(Web.DownloadString(String.Format("{0}/api/albums/{1}", Program.ApiEndpoint, id)));
            return LoadFromXml(apiResponse.DocumentElement);
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static AlbumInfo LoadFromXml(XmlNode doc)
        {
            var album = new AlbumInfo
            {
                Name = doc["DefaultName"].InnerText,
                Artists = doc["ArtistString"].InnerText.Split(','),
                VocaDbId = Convert.ToUInt32(doc["Id"].InnerText)
            };

            LoadTracks(Convert.ToUInt32(doc["Id"].InnerText), album);

            Console.WriteLine("API Query! (Picture)");
            album.Picture =
                new Picture(
                    Web.DownloadData(String.Format("{0}/Album/CoverPicture/{1}", Program.ApiEndpoint,
                        doc["Id"].InnerText))) {Type = PictureType.FrontCover};
            album.ReleaseDate = new DateTime(Convert.ToInt32(doc["ReleaseDate"]["Year"].InnerText), Convert.ToInt32(doc["ReleaseDate"]["Month"].InnerText), Convert.ToInt32(doc["ReleaseDate"]["Day"].InnerText));

            return album;
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static void LoadTracks(uint id, AlbumInfo album)
        {
            var apiResponse = new XmlDocument();
            Console.WriteLine("API Query! (Tracks)");
            apiResponse.LoadXml(Web.DownloadString(String.Format("{0}/api/albums/{1}/tracks", Program.ApiEndpoint, id)));
            var doc = apiResponse.DocumentElement;
            var tracks = (from node in doc.ChildNodes.Cast<XmlNode>().Where(node => node.Name == "SongInAlbumContract")
                let title = node["Name"].InnerText
                let artists = new[] {node["Song"]["ArtistString"].InnerText}
                let disc = Convert.ToUInt32(node["DiscNumber"].InnerText)
                let track = Convert.ToUInt32(node["TrackNumber"].InnerText)
                let length = Convert.ToUInt32(node["Song"]["LengthSeconds"].InnerText)
                select new TrackInfo(title, artists, disc, track, length)).ToList();

            album.Discs = tracks.Max(t => t.Disc);
            album.Tracks = new TrackInfo[album.Discs][];
            for (var i = 1; i <= album.Discs; i++)
            {
                var discTracks = (from track in tracks
                    where track.Disc == i
                    select track).ToList();

                discTracks.Sort((t1, t2) => t1.Track.CompareTo(t2.Track));
                album.Tracks[i - 1] = discTracks.ToArray();
            }
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static AlbumInfo GetFromName(string name)
        {
            var apiResponse = new XmlDocument();
            Console.WriteLine("API Query! (Album Search)");
            apiResponse.LoadXml(
                Web.DownloadString(String.Format("{0}/api/albums?query={1}", Program.ApiEndpoint,
                    Uri.EscapeDataString(name))));

            var items = apiResponse.DocumentElement["Items"];
            return items.FirstChild == null ? null : LoadFromXml(items.FirstChild);
        }

        private sealed class AlbumInfoEqualityComparer : IEqualityComparer<AlbumInfo>
        {
            public bool Equals(AlbumInfo x, AlbumInfo y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return Equals(x.Artists, y.Artists) && string.Equals(x.Name, y.Name) && Equals(x.Tracks, y.Tracks) &&
                       x.Discs == y.Discs;
            }

            public int GetHashCode(AlbumInfo obj)
            {
                unchecked
                {
                    var hashCode = (obj.Artists != null ? obj.Artists.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (obj.Name != null ? obj.Name.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (obj.Tracks != null ? obj.Tracks.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (int) obj.Discs;
                    return hashCode;
                }
            }
        }
    }
}