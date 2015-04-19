using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VocaDb.Model.DataContracts.Songs;

namespace MusicTool
{
    internal class ArtistEqualityComparer : IEqualityComparer<ArtistForSongContract>
    {
        private static readonly ArtistEqualityComparer ComparerImpl = new ArtistEqualityComparer();

        internal static ArtistEqualityComparer Comparer { get { return ComparerImpl; } } 
        
        public bool Equals(ArtistForSongContract x, ArtistForSongContract y)
        {
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;

            return x.Name == y.Name;
        }

        public int GetHashCode(ArtistForSongContract obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
