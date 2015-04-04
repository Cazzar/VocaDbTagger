using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicTagger;

namespace ApiTest
{
    [TestClass]
    public class GetByNameTest
    {
        private AlbumInfo _album;

        [TestInitialize]
        public void Setup()
        {
            _album = AlbumInfo.GetFromName("8HIT");
            Assert.IsNotNull(_album, "API Get Failed.");
        }

        [TestMethod]
        public void TestMeta()
        {
            Assert.AreEqual("8HIT", _album.Name, "Incorrect album name");
            Assert.AreEqual("マイナスP, じーざすP feat. 鏡音リン, 鏡音レン", _album.ArtistString(), "Incorrect artist");
            Assert.AreEqual((uint)2011, _album.ReleaseYear, "Incorrect name");
        }

        [TestMethod]
        public void TestTracks()
        {
            Assert.AreEqual((uint)1, _album.Discs, "Incorrect disc count");
            Assert.AreEqual((uint)3, _album.TotalTrackCount, "Incorrect track count");

            var info = _album.Tracks[0][0];
            Assert.AreEqual("8HIT", info.Title, "Disc 1, Track 1 is wrongly titled");
            Assert.AreEqual("じーざすP feat. 鏡音リン, 鏡音レン", info.ArtistString, "Disc 1, Track 1's artist string is not as expected");
        }
    }
}
