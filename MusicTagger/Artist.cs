using System.Collections.Generic;

namespace MusicTagger
{
    public class Artist
    {
        private static readonly IEqualityComparer<Artist> NameTypeCategoriesComparerInstance =
            new NameTypeCategoriesEqualityComparer();

        public Artist(string name, string type, string categories)
        {
            Name = name;
            Type = type;
            Categories = categories;
        }

        public static IEqualityComparer<Artist> NameTypeCategoriesComparer
        {
            get { return NameTypeCategoriesComparerInstance; }
        }

        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Categories { get; private set; }

        protected bool Equals(Artist other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Type, other.Type) &&
                   string.Equals(Categories, other.Categories);
        }

        public static bool operator ==(Artist a, Artist b)
        {
            return NameTypeCategoriesComparer.Equals(a, b);
        }

        public static bool operator !=(Artist a, Artist b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Artist) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Categories != null ? Categories.GetHashCode() : 0);
                return hashCode;
            }
        }

        private sealed class NameTypeCategoriesEqualityComparer : IEqualityComparer<Artist>
        {
            public bool Equals(Artist x, Artist y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.Name, y.Name) && string.Equals(x.Type, y.Type) &&
                       string.Equals(x.Categories, y.Categories);
            }

            public int GetHashCode(Artist obj)
            {
                unchecked
                {
                    var hashCode = (obj.Name != null ? obj.Name.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (obj.Type != null ? obj.Type.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (obj.Categories != null ? obj.Categories.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}