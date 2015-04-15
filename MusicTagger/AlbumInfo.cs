using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml;
using TagLib;
using TagLib.Id3v2;
using Tag = TagLib.Id3v2.Tag;

namespace MusicTagger
{
    public class AlbumInfo
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
        public uint ReleaseYear { get; private set; }

        public uint TotalTrackCount
        {
            get { return Tracks.Aggregate<TrackInfo[], uint>(0, (current, t) => current + Convert.ToUInt32(t.Length)); }
        }

        public IPicture Picture { get; private set; }
        public uint VocaDbId { get; private set; }

        public void WriteToFile(File taggedFile)
        {
            var tag = taggedFile.Tag;
            var id3Tag = taggedFile.GetTag(TagTypes.Id3v2) as Tag;
            var disc = Tracks[Math.Min(tag.Disc - 1, 0)];
            if (tag.Track > disc.Length) return;
            if (tag.Track == 0 || tag.Track - 1 >= disc.Length)
            {
                return;
//                ((Action) (() => { }))();
            }
            var info = disc[tag.Track - 1];
            var artists = new List<Artist>();

            for (var i = 0; i < Discs; i++)
            {
                var discArtists = (from track in Tracks[i] select track.Artists);

                foreach (var discArtist in discArtists)
                {
                    artists.AddRange(discArtist.Where(a => a.Categories == "Producer"));
                }
            }

            var uniqueArtists = artists.Distinct().ToList();
            uniqueArtists.Sort((a, b) => artists.Count(v => v == a) - artists.Count(v => v == b));

            tag.AlbumArtists = new[] {ArtistString()};
            //uniqueArtists.Count() > 1 ? new []{"Various Artists"} : uniqueArtists.Select(a => a.Name).ToArray();
            tag.AlbumArtistsSort = uniqueArtists.Select(a => a.Name).ToArray();

            if (id3Tag != null)
            {
                id3Tag.IsCompilation = uniqueArtists.Count > 1;
                id3Tag.Comment = String.Format("VocaDB: {0}\nAlbum Artists{1}", VocaDbId,
                    String.Join(", ", uniqueArtists.Select(a => a.Name)));
            }

            tag.Album = Name;

            tag.Performers = new[] {info.ArtistString};
            tag.PerformersSort = new[] {info.ArtistString};
            tag.TrackCount = (uint) disc.Length;
            tag.Track = info.Track;
            tag.Title = info.Title;
            tag.Disc = info.Disc;
            tag.DiscCount = Discs;
            tag.Year = ReleaseYear;

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
//            Console.WriteLine("API Query! (Album Info by ID)");
            apiResponse.LoadXml(
                Web.DownloadString(String.Format("{0}/api/albums/{1}?fields=tracks&songFields=artists",
                    Program.ApiEndpoint, id)));
            return LoadFromXml(apiResponse.DocumentElement);
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static AlbumInfo LoadFromXml(XmlNode doc, bool useTracks = true)
        {
            var album = new AlbumInfo
            {
                Name = doc["DefaultName"].InnerText,
                Artists = new[] {doc["ArtistString"].InnerText},
                VocaDbId = Convert.ToUInt32(doc["Id"].InnerText)
            };


            if (useTracks)
                LoadTracks(doc["Tracks"], album);
            else
                LoadTracks(Convert.ToUInt32(doc["Id"].InnerText), album);

//            Console.WriteLine("API Query! (Picture)");
            album.Picture =
                new Picture(
                    Web.DownloadData(String.Format("{0}/Album/CoverPicture/{1}", Program.ApiEndpoint,
                        doc["Id"].InnerText))) {Type = PictureType.FrontCover};
            album.ReleaseYear = (doc["ReleaseDate"]["IsEmpty"].InnerText == "true")
                ? 0
                : Convert.ToUInt32(doc["ReleaseDate"]["Year"].InnerText);
            //, Convert.ToInt32(doc["ReleaseDate"]["Month"].InnerText), Convert.ToInt32(doc["ReleaseDate"]["Day"].InnerText));

            return album;
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static void LoadTracks(uint id, AlbumInfo album)
        {
            var apiResponse = new XmlDocument();
//            Console.WriteLine("API Query! (Tracks)");
            apiResponse.LoadXml(
                Web.DownloadString(String.Format("{0}/api/albums/{1}/tracks?fields=artists", Program.ApiEndpoint, id)));
            var doc = apiResponse.DocumentElement;
            var tracks = (from node in doc.ChildNodes.Cast<XmlNode>()
                let title = node["Name"].InnerText
                let artists = (from artist in node["Song"]["Artists"].ChildNodes.Cast<XmlNode>()
                    select 
                        new Artist(artist["Name"].InnerText,
                            !IsEmpty(artist["Artist"]) ? artist["Artist"]["ArtistType"].InnerText : "",
                            artist["Categories"].InnerText)).ToArray()
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
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private static void LoadTracks(XmlNode tracksNode, AlbumInfo album)
        {
            var tracks = (from node in tracksNode.ChildNodes.Cast<XmlNode>()
                let title = node["Name"].InnerText
                let artists = (from artist in node["Song"]["Artists"].ChildNodes.Cast<XmlNode>()
                    select
                        new Artist(artist["Name"].InnerText,
                            !IsEmpty(artist["Artist"]) ? artist["Artist"]["ArtistType"].InnerText : "",
                            artist["Categories"].InnerText)).ToArray()
                let disc = Convert.ToUInt32(node["DiscNumber"].InnerText)
                let track = Convert.ToUInt32(node["TrackNumber"].InnerText)
                let length = Convert.ToUInt32(node["Song"]["LengthSeconds"].InnerText)
                select new TrackInfo(title, artists, disc, track, length)).ToList();
            album.Discs = tracks.Max(t => t.Disc);
            album.Tracks = new TrackInfo[album.Discs][];

            for (var i = 1; i <= album.Discs; i++)
            {
                var discTracks = (from track in tracks where track.Disc == i select track).ToList();
                discTracks.Sort((t1, t2) => t1.Track.CompareTo(t2.Track));
                album.Tracks[i - 1] = discTracks.ToArray();
            }
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static AlbumInfo GetFromName(string name)
        {
            var apiResponse = new XmlDocument();
//            Console.WriteLine("API Query! (Album Search)");
            apiResponse.LoadXml(
                Web.DownloadString(String.Format("{0}/api/albums?query={1}&fields=tracks&songFields=artists",
                    Program.ApiEndpoint,
                    Uri.EscapeDataString(name))));

            var items = apiResponse.DocumentElement["Items"];
            return items.FirstChild == null ? null : LoadFromXml(items.FirstChild, false);
        }

        private static bool IsEmpty(XmlNode node)
        {
            if (node.Attributes == null || node.Attributes["i:nil"] == null) return false;
            return (node.Attributes["i:nil"].Value == "true");
        }

        public string ArtistString(string delim = ", ", uint leniancy = 3, uint vocalLeniancy = 3)
        {
            var allArtists = new List<Artist>();
            foreach (var track in Tracks.SelectMany(disc => disc)) allArtists.AddRange(track.Artists);

            var artists = allArtists.Distinct().Where(a => a.Categories == "Producer").ToList();
            var vocals =
                allArtists.Distinct()
                    .Where(a => a.Categories == "Vocalist" && artists.Count(b => b.Name == a.Name) == 0)
                    .ToList();

            artists.Sort((a, b) => allArtists.Count(t => t.Name == a.Name) - allArtists.Count(t => t.Name == b.Name));
            vocals.Sort((a, b) => allArtists.Count(t => t.Name == a.Name) - allArtists.Count(t => t.Name == b.Name));

            if (artists.Count() > leniancy) return "Various Artists";

            var vocalists = (vocals.Count >= vocalLeniancy) ? "Various" : String.Join(", ", vocals.Select(a => a.Name));

            if (String.IsNullOrWhiteSpace(vocalists))
                return String.Join(delim, artists);
            return String.Format("{0} feat. {1}", String.Join(delim, artists.Select(a => a.Name)), vocalists);
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