namespace MusicTagger
{
    internal class TrackInfo
    {
        public TrackInfo(string title, string[] artists, uint disc, uint track, uint length)
        {
            Title = title;
            Artists = artists;
            Track = track;
            Disc = disc;
            Length = length;
        }

        public string[] Artists { get; private set; }
        public string Title { get; private set; }
        public uint Track { get; private set; }
        public uint Disc { get; private set; }
        public uint Length { get; private set; }
    }
}