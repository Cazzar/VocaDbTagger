using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using NDesk.Options;
using TagLib;
using TagLib.Mpeg;
using File = System.IO.File;

namespace MusicTagger
{
    public class Program
    {
        public const string Version = "1.0";
        public static readonly WebHeaderCollection Headers = new WebHeaderCollection();
        public static string ApiEndpoint { get; set; }

        private static readonly List<string> Nulls = new List<string>();
        private static readonly HashSet<AlbumInfo> Albums = new HashSet<AlbumInfo>(AlbumInfo.AlbumInfoComparer);

        private static void Main(string[] args)
        {
            ApiEndpoint = "http://vocadb.net";
            Console.WriteLine(AlbumInfo.GetFromId(2001).Tracks[0][0].ArtistString);
            Console.ReadKey();

            return;
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: [folder/file] [id] [vocadb/utaudb]");
                return;
            }

            switch (args[2].ToLower())
            {
                case "utaudb":
                case "utau":
                case "u":
                    ApiEndpoint = "http://utaitedb.net/";
                    break;
                case "vocadb":
                case "vocaloid":
                case "voca":
                case "v":
                    ApiEndpoint = "http://vocadb.net";
                    break;
                default:
                    Console.WriteLine("Unrecognised database: {0}", args[2]);
                    return;
            }

            AlbumInfo album = null;
            try
            {
                var id = Convert.ToUInt32(args[1]);
                album = AlbumInfo.GetFromId(id);
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid ID {0}, or possible API error", args[1]);
            }

            IterateDirectory(new DirectoryInfo(args[0]), f => ProcessFileWithAlbum(f, album));
        }

        private static void ProcessFileWithAlbum(FileSystemInfo file, AlbumInfo album)
        {
            Console.WriteLine("Processing file: {0}", file.Name);
            try
            {
                var audio = TagLib.File.Create(file.FullName) as AudioFile;
                if (audio == null) return;

                album.WriteToFile(audio);
            }
            catch (Exception)
            {
                //ignored
            }
        }

        private static void IterateDirectory(DirectoryInfo di, Action<FileInfo> processAction)
        {
            Console.WriteLine("Processing Folder: {0}", di.Name);
            Console.Title = di.Name;

            di.GetDirectories().ToList().ForEach(d => IterateDirectory(d, processAction));
            di.GetFiles().ToList().ForEach(processAction);
        }

        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        private static void ProcessFile(FileInfo file)
        {
            Console.Write("Processing file: {0}:\t", file.Name);
            try
            {
                var music = TagLib.File.Create(file.FullName) as AudioFile;
                if (music == null || Nulls.Contains(music.Tag.Album))
                {
                    Console.WriteLine("Skipping...");
                    return;
                }

                AlbumInfo album = null;
                if (File.Exists(Path.Combine(file.DirectoryName, "vocadb.txt")))
                {
                    var id = Convert.ToUInt32(File.ReadAllLines(Path.Combine(file.DirectoryName, "vocadb.txt"))[0]);
                    album = Albums.FirstOrDefault(a => a.VocaDbId == id);
                    if (album == null)
                    {
                        album = AlbumInfo.GetFromId(id);
                        Albums.Add(album);
                    }
                }
                if (album == null) album = GetForAlbum(music.Tag.Album);

                if (album == null)
                {
                    Nulls.Add(music.Tag.Album);
                    return;
                }

                album.WriteToFile(music);
                Console.WriteLine("Done!");
                Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                //ignored
                if (!(ex is UnsupportedFormatException))
                {
                    Console.WriteLine("Exception! of type: {0}", ex);
                }
            }
        }

        private static AlbumInfo GetForAlbum(string name)
        {
            var album = Albums.FirstOrDefault(a => a.Name == name);

            if (album == null)
            {
                album = AlbumInfo.GetFromName(name);
                if (album != null) Albums.Add(album);
            }
            if (album != null) return album;

            Nulls.Add(name);
            return null;
        }
    }
}