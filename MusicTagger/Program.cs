using System;
using System.IO;
using System.Linq;
using System.Net;
using CommandLine;
using MusicTool;
using VocaDb.Model.DataContracts.Albums;

namespace MusicTagger
{
    class Program
    {
        private static AlbumForApiContract _album;
        private static byte[] _image;
        static void Main(string[] args)
        {
            var options = CommandlineOptions.Options;
            if (!Parser.Default.ParseArguments(args, options))            
                return;

            if (!options.NoTag)
            {
                _album = AlbumUtil.GetAlbumById(options.DatabaseId, options.Database, options.Verbose);
                if (options.Verbose) Console.WriteLine("Downloading URI: {0}", String.Format("{0}/Album/CoverPicture/{1}", options.Database, _album.Id));
                _image =
                    new WebClient().DownloadData(String.Format("{0}/Album/CoverPicture/{1}", options.Database, _album.Id));
            }
            var item = options.WorkItem;

            if (File.Exists(item))
                ProcessFile(new FileInfo(item));
            else if (Directory.Exists(item))
                IterateDirectory(new DirectoryInfo(item), ProcessFile);

#if DEBUG
            Console.ReadLine();
#endif
        }

        private static void ProcessFile(FileInfo file)
        {
            if (!file.CanFileBeProcessed()) return;

            var simulate = CommandlineOptions.Options.Simulate;
            var util = new FileUtil(file, _album, _image);
            if (!CommandlineOptions.Options.NoTag)
                util.RetagFile(CommandlineOptions.Options.PreferLyrics, simulate);
            
            util.RenameFile(CommandlineOptions.Options.OutputDir, simulate);
        }

        private static void IterateDirectory(DirectoryInfo di, Action<FileInfo> processAction)
        {
            if (CommandlineOptions.Options.Verbose) Console.WriteLine("Processing Folder: {0}", di.Name);
            Console.Title = di.Name;

            di.GetDirectories().ToList().ForEach(d => IterateDirectory(d, processAction));
            di.GetFiles().ToList().ForEach(processAction);
        }
    }
}
