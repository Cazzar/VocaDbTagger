using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using TagLib;
using TagLib.Id3v2;
using TagLib.Mpeg;
using VocaDb.Model.DataContracts.Albums;
using VocaDb.Model.DataContracts.Songs;
using VocaDb.Model.Domain.Artists;
using VocaDb.Model.Domain.Globalization;
using VocaDb.Model.Service.BBCode;
using Tag = TagLib.Id3v2.Tag;

namespace MusicTool
{
    public class FileUtil
    {
        private readonly FileInfo _operatingFile;
        private readonly SongInAlbumForApiContract _trackInformation;
        private readonly AlbumForApiContract _albumInfomation;
        private readonly AudioFile _taggedFile;
        private readonly IPicture _picture;
        private bool _retagged;

        public FileUtil(FileInfo operatingFile, AlbumForApiContract albumInfomation, byte[] pictureBytes)
        {
            _operatingFile = operatingFile;
            _albumInfomation = albumInfomation;
            _taggedFile = new AudioFile(operatingFile.FullName);
            if (albumInfomation != null)
                _trackInformation = albumInfomation.GetTrack(_taggedFile.Tag.Disc, _taggedFile.Tag.Track);

            _picture = new AttachedPictureFrame(new Picture(new ByteVector(pictureBytes, pictureBytes.Length)){Type = PictureType.FrontCover});
            if (_trackInformation == null && albumInfomation != null) throw new ArgumentException("The track information was somehow null for file: " + operatingFile.FullName);
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public bool RenameFile(string destDir, bool simulate = false)
        {
            if (_operatingFile.IsReadOnly && !simulate) _operatingFile.Attributes = Util.RemoveAttribute(_operatingFile.Attributes, FileAttributes.ReadOnly);

            var file1 = _taggedFile;
            if (file1 != null && !simulate)
            {
                if (_taggedFile.TagTypes.HasFlag(TagTypes.Ape)) _taggedFile.RemoveTags(TagTypes.Ape);

                _taggedFile.Save();
            }

            var tag = _taggedFile.Tag;

            //%album%/$if($gt(%totaldiscs%,1),Disc %discnumber%-,)$num(%tracknumber%,2)$if(%compilation%, %artist% -,) %title%
            var album = tag.Album;
            if (String.IsNullOrWhiteSpace(album)) album = "No Album";
            album = album.Normalize().CleanFileName();
            if (tag.Title == null)
            {
                return false;
            }

            var totalDiscs = tag.DiscCount;
            var disc = tag.Disc;
            var track = tag.Track;
            var artist = (tag.Performers.Length == 1) ? tag.Performers[0] : "Various Artists";
            artist = artist.Normalize().CleanFileName();
            var title = tag.Title.Normalize().CleanFileName();

            var albumDir = new DirectoryInfo(Path.Combine(destDir, album)).MakeIfNotExisting(simulate);
            var saveDir = albumDir;
            if (totalDiscs > 1)
            {
                saveDir =
                    new DirectoryInfo(Path.Combine(albumDir.FullName, String.Format("Disc {0}", disc)))
                        .MakeIfNotExisting(simulate);
            }

            Debug.Assert(_operatingFile.Directory != null, "file.Directory != null");
            string[] types = { "*.jpg", "*.png", "scans.*", "vocadb.txt" };
            foreach (var type in types.Where(type => _operatingFile.Directory != null))
            {
                _operatingFile.Directory.GetFiles(type).ToList().ForEach(fi =>
                {
                    var path = Path.Combine(albumDir.FullName, fi.Name);
                    if (System.IO.File.Exists(path)) return;

                    if (simulate)
                    {
                        Console.WriteLine("Copy {0} to {1}", fi.FullName, path);
                        return;
                    }
                    
                    fi.CopyTo(path);
                    fi.Delete();
                });
            }
            var scansFolder = Path.Combine(_operatingFile.Directory.FullName, "Scans");

            if (Directory.Exists(scansFolder) && !System.IO.Directory.Exists(Path.Combine(albumDir.FullName, "Scans")))
            {
                if (simulate)
                    Console.WriteLine("Copy {0} to {1}", scansFolder, Path.Combine(albumDir.FullName, "Scans"));
                else
                    Util.DirectoryCopy(scansFolder, Path.Combine(albumDir.FullName, "Scans"));
            }

            var fileName = String.Format("{0:D2}. {1} - {2}{3}", track, artist, title, _operatingFile.Extension).CleanFileName().Normalize();
            if (System.IO.File.Exists(Path.Combine(saveDir.FullName, fileName)))
            {
                return false;
            }
            if (simulate)
            {
                Console.WriteLine("Move file {0} to {1}", _operatingFile.FullName, Path.Combine(saveDir.FullName, fileName));
                return true;
            }
            _operatingFile.MoveTo(Path.Combine(saveDir.FullName, fileName));
            return true;
        }

        public void RetagFile(string preferLyrics, bool simulate = false, bool force = false)
        {
            if (!force && _retagged) return;
            var tag = _taggedFile.Tag;
            var id3Tag = _taggedFile.GetTag(TagTypes.Id3v2) as Tag;
            var artists = new List<ArtistForSongContract>();

            foreach (var track in _albumInfomation.Tracks)
            {
                artists.AddRange(track.Song.Artists.Where(artist => artist.Categories.HasFlag(ArtistCategories.Producer)));
            }

            var uniqueArtists = artists.Distinct(ArtistEqualityComparer.Comparer).ToList();
            uniqueArtists.Sort((a, b) => artists.Count(v => v.Name == a.Name) - artists.Count(v => v.Name == b.Name));

            if (simulate)
            {
                Console.WriteLine("tag.Disc {0} => {1}", tag.Disc, _trackInformation.DiscNumber);
                Console.WriteLine("tag.DiscCount {0} => {1}", tag.DiscCount,
                    _albumInfomation.Tracks.Max((t) => t.DiscNumber));

                Console.WriteLine("tag.Year {0} => {1}", tag.Year, _trackInformation.Song.CreateDate.Year);
                Console.WriteLine("tag.AlbumArtistsSort {0} => {1}", String.Join(",", tag.AlbumArtistsSort),
                    String.Join(",", uniqueArtists.Select(a => a.Name).ToArray()));

                Console.WriteLine("tag.AlbumArtists {0} => {1}", String.Join(",", tag.AlbumArtists),
                    _albumInfomation.ArtistString);

                Console.WriteLine("tag.Performers {0} => {1}", String.Join(",", tag.Performers),
                    _trackInformation.GetArtistString());

                Console.WriteLine("tag.PerformersSort {0} => {1}", String.Join(",", tag.PerformersSort),
                    _trackInformation.GetArtistString());

                Console.WriteLine("tag.Track {0} => {1}", tag.Track, _trackInformation.TrackNumber);

                Console.WriteLine("tag.TrackCount {0} => {1}", tag.TrackCount,
                    _albumInfomation.Tracks.Count(t => t.DiscNumber == _trackInformation.DiscNumber));

                Console.WriteLine("tag.Title {0} => {1}", tag.Title, _trackInformation.Song.DefaultName);
                Console.WriteLine("tag.Album {0} => {1}", tag.Album, _albumInfomation.DefaultName);
                if (id3Tag != null)
                {
                    Console.WriteLine("id3Tag.Comment {0} => {1}", id3Tag.Comment, String.Format("VocaDB: {0}\nAlbum Artists{1}", _albumInfomation.Id, String.Join(", ", uniqueArtists.Select(a => a.Name))));
                    Console.WriteLine("id3Tag.IsCompilation {0} => {1}", id3Tag.IsCompilation, uniqueArtists.Count > 1);
                    var lyrics = (_trackInformation.Song.Lyrics == null) ? null :
                        _trackInformation.Song.Lyrics.FirstOrDefault(l => l.Language == (ContentLanguageSelection)Enum.Parse(typeof(ContentLanguageSelection), preferLyrics)) ??
                        _trackInformation.Song.Lyrics.FirstOrDefault(l => l.Language == ContentLanguageSelection.Japanese) ??
                        _trackInformation.Song.Lyrics.FirstOrDefault(l => l.Language == ContentLanguageSelection.Romaji);
                    if (lyrics != null)
                        Console.WriteLine("id3Tag.Lyrics: added, language: {0}", lyrics.Language.ToString());
                }


                var sFound = false;
                for (var i = 0; i < tag.Pictures.Length; i++)
                {
                    if (tag.Pictures[i].Type != PictureType.FrontCover) continue;
                    Console.WriteLine("Rewriting Front cover image {0}", i);
                    sFound = true;
                    break;
                }
                if (sFound)
                {
                    return;
                }

                Console.WriteLine("Prepending album image.");
                return;
            }

            tag.Disc = (uint)_trackInformation.DiscNumber;
            tag.DiscCount = (uint) _albumInfomation.Tracks.Max((t) => t.DiscNumber);
            tag.Year = (uint) _trackInformation.Song.CreateDate.Year;

            tag.AlbumArtistsSort = uniqueArtists.Select(a => a.Name).ToArray();
            tag.AlbumArtists = new[] { _albumInfomation.ArtistString };
            tag.Performers = new[] {_trackInformation.GetArtistString()};
            tag.PerformersSort = new[] {_trackInformation.GetArtistString()};

            tag.Track = (uint) _trackInformation.TrackNumber;
            tag.TrackCount = (uint) _albumInfomation.Tracks.Count(t => t.DiscNumber == _trackInformation.DiscNumber);
            tag.Title = _trackInformation.Song.DefaultName;
            tag.Album = _albumInfomation.DefaultName;

            if (id3Tag != null)
            {
                id3Tag.IsCompilation = uniqueArtists.Count > 1;

                id3Tag.Comment = String.Format("VocaDB: {0}\nAlbum Artists{1}", _albumInfomation.Id, String.Join(", ", uniqueArtists.Select(a => a.Name)));
                var lyrics = (_trackInformation.Song.Lyrics == null) ? null :
                    _trackInformation.Song.Lyrics.FirstOrDefault(l => l.Language == (ContentLanguageSelection)Enum.Parse(typeof(ContentLanguageSelection), preferLyrics)) ??
                    _trackInformation.Song.Lyrics.FirstOrDefault(l => l.Language == ContentLanguageSelection.Japanese) ??
                    _trackInformation.Song.Lyrics.FirstOrDefault(l => l.Language == ContentLanguageSelection.Romaji);
                id3Tag.Lyrics = (lyrics != null) ? lyrics.Value : "";
            }

            var found = false;
            for (var i = 0; i < tag.Pictures.Length; i++)
            {
                if (tag.Pictures[i].Type != PictureType.FrontCover) continue;
                tag.Pictures[i] = new AttachedPictureFrame(_picture);
                Console.WriteLine("Rewriting Front cover image {0}", i);
                found = true;
                break;
            }
            if (found)
            {
                _retagged = true;
                _taggedFile.Save();
                return;
            }

            var newPictures = new IPicture[tag.Pictures.Length + 1];
            Array.Copy(tag.Pictures, 0, newPictures, 1, tag.Pictures.Length);
            newPictures[0] = new AttachedPictureFrame(_picture);
            tag.Pictures = newPictures;

            _retagged = true;
            _taggedFile.Save();
        }
    }
}
