using System;
using VRageMath;

namespace NeverSerender
{
    public class AssetIdentifier : IEquatable<AssetIdentifier>
    {
        public readonly string asset;
        public readonly string skin;
        public readonly Vector3 color;

        public AssetIdentifier(string asset, string skin, Vector3 color)
        {
            this.asset = asset;
            this.skin = skin;
            this.color = color;
        }

        // https://stackoverflow.com/a/263416
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ asset.GetHashCode();
                hash = (hash * 16777619) ^ skin?.GetHashCode() ?? 0x484EACB5;
                return (hash * 16777619) ^ color.GetHashCode();
            }
        }

        public bool Equals(AssetIdentifier other)
        {
            return asset == other.asset
                && skin == other.skin
                && color == other.color;
        }
    }
}
