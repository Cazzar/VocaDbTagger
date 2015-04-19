using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace MusicTagger
{
    class CommandlineOptions
    {
        private static readonly CommandlineOptions OptionsImpl = new CommandlineOptions();

        public static CommandlineOptions Options
        {
            get { return OptionsImpl; }
        }

        private CommandlineOptions() { }

        [Option('d', "database", DefaultValue = "http://vocadb.net", HelpText = "Set the database to retrieve from.")]
        public string Database { get; set; }

        [Option('i', "id", Required = true, HelpText = "The ID for the database to pull, string names are not supported.")]
        public uint DatabaseId { get; set; }

        private bool _verbose;
        [Option('v', "verbose", DefaultValue = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose {
            get
            {
              return Simulate || _verbose;
            }
            set
            {
                _verbose = value;
            }
        }

        [Option('f', "item", Required = true, HelpText = "The file or folder to work on")]
        public string WorkItem { get; set; }

        [Option('n', "no-tag", DefaultValue = false, HelpText = "If set, any API based things are not called, including the retagging.")]
        public bool NoTag { get; set; }

        [Option('s', "simulate", DefaultValue = false, HelpText = "If set, no files will be changed. It will also implictly enable verbose mode")]
        public bool Simulate { get; set; }

        [Option('o', "output", DefaultValue = ".", HelpText = "Set the output directory to the folder, this will be the folder ABOVE the album.")]
        public string OutputDir { get; set; }

        [Option('l', "lyrics", DefaultValue = "English", HelpText = "Set the preferred language for the lyrics to add if an ID3V2 tag is present.")]
        public string PreferLyrics { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
