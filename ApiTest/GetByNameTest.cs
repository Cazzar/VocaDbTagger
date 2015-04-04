using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicTagger;

namespace ApiTest
{
    [TestClass]
    public class GetByNameTest
    {
        private AlbumInfo _iSayLove;

        [TestInitialize]
        public void Setup()
        {
            _iSayLove = AlbumInfo.GetFromName("8HIT");
            Assert.IsNotNull(_iSayLove, "API Get Failed.");
        }

        [TestMethod]
        public void TestMeta()
        {
            Assert.AreEqual("8HIT", _iSayLove.Name, "Incorrect album name");
            Assert.AreEqual("マイナスP, じーざすP feat. 鏡音リン, 鏡音レン", _iSayLove.ArtistString(), "Incorrect artist");
            Assert.AreEqual((uint)2011, _iSayLove.ReleaseYear, "Incorrect name");
        }

        [TestMethod]
        public void TestTracks()
        {
            Assert.AreEqual((uint)1, _iSayLove.Discs, "Incorrect disc count");
            Assert.AreEqual((uint)3, _iSayLove.TotalTrackCount, "Incorrect track count");

            var info = _iSayLove.Tracks[0][0];
            Assert.AreEqual("8HIT", info.Title, "Disc 1, Track 1 is wrongly titled");
            Assert.AreEqual("じーざすP feat. 鏡音リン, 鏡音レン", info.ArtistString, "Disc 1, Track 1's artist string is not as expected");
        }
    }
}
