using System;
using MusicTagger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApiTest
{
    [TestClass]
    public class GetAlbumByIdTest
    {
        private AlbumInfo _iSayLove;

        [TestInitialize]
        public void Setup()
        {
            _iSayLove = AlbumInfo.GetFromId(9692);
            Assert.IsNotNull(_iSayLove, "API Get Failed.");
        }

        [TestMethod]
        public void TestMeta()
        {
            Assert.AreEqual("I say love", _iSayLove.Name, "Incorrect album name");
            Assert.AreEqual("ラマーズP feat. Various", _iSayLove.ArtistString(), "Incorrect artist");
            Assert.AreEqual((uint) 2014, _iSayLove.ReleaseYear, "Incorrect name");
        }

        [TestMethod]
        public void TestTracks()
        {
            Assert.AreEqual((uint) 1, _iSayLove.Discs, "Incorrect disc count");
            Assert.AreEqual((uint) 10, _iSayLove.TotalTrackCount, "Incorrect track count");

            var info = _iSayLove.Tracks[0][1];
            Assert.AreEqual("デレ化現象100％", info.Title, "Disc 1, Track 2 is wrongly titled");
            Assert.AreEqual("ラマーズP feat. 初音ミク V3 (Light)", info.ArtistString, "Disc 1, Track 2's artist string is not as expected");
        }
    }
}
