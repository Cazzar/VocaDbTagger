using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TagLib.Mpeg;
using VocaDb.Model.DataContracts.Albums;
using VocaDb.Model.DataContracts.Songs;
using VocaDb.Model.Domain.Artists;
using File = TagLib.File;

namespace MusicTool
{
    [DebuggerStepThrough]
    public static class Extensions
    {
        public static SongInAlbumForApiContract GetTrack(this AlbumForApiContract album, uint disc, uint track)
        {
            return album.Tracks.FirstOrDefault(t => t.DiscNumber == disc && t.TrackNumber == track);
        }

        public static IEnumerable<SongInAlbumForApiContract> GetDiscTracks(this AlbumForApiContract album, uint disc)
        {
            return album.Tracks.Where(track => track.DiscNumber == disc);
        }

        public static string GetArtistString(this SongInAlbumForApiContract song)
        {
            var artists = song.Song.Artists.Where(artist => artist.Categories.HasFlag(ArtistCategories.Producer)).Select(artist => artist.Name).ToList();
            var vocals  = song.Song.Artists.Where(artist => artist.Categories.HasFlag(ArtistCategories.Vocalist)).Select(artist => artist.Name).ToList();
            var vocalString = String.Join(", ", vocals);
            var artistString = String.Join(", ", artists);

            return String.IsNullOrWhiteSpace(vocalString) ? artistString : String.Format("{0} feat. {1}", artistString, vocalString);
        }

        public static bool CanFileBeProcessed(this FileInfo file)
        {
            try
            {
                return (File.Create(file.FullName) as AudioFile) != null;
            }
            catch
            {
                return false;
            }
        }

        public static DirectoryInfo MakeIfNotExisting(this DirectoryInfo info, bool simulate)
        {
            if (info.Exists) return info;

            if (simulate)
            {
                Console.WriteLine("Create directory {0}", info.FullName);
                return info;
            }
            info.Create();
            return info;
        }

        public static string CleanFileName(this string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
    }
}
