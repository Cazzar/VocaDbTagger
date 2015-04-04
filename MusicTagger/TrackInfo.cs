using System;
using System.Linq;

namespace MusicTagger
{
    public class TrackInfo
    {
        public TrackInfo(string title, Artist[] artists, uint disc, uint track, uint length)
        {
            Title = title;
            Artists = artists;
            Track = track;
            Disc = disc;
            Length = length;
        }

        public Artist[] Artists { get; private set; }

        public string ArtistString
        {
            get
            {
                var artists   = String.Join(", ", from artist in Artists where artist.Categories == "Producer" select artist.Name);
                var vocaloids = String.Join(", ", from artist in Artists where artist.Categories == "Vocalist" select artist.Name);

                return String.IsNullOrWhiteSpace(vocaloids) ? artists : String.Format("{0} feat. {1}", artists, vocaloids);
            }
        }

        public string Title { get; private set; }
        public uint Track { get; private set; }
        public uint Disc { get; private set; }
        public uint Length { get; private set; }
    }
}