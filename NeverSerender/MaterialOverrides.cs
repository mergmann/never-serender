using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRageMath;
using VRageRender.Messages;

namespace NeverSerender
{
    public class MaterialOverrides
    {
        private readonly Dictionary<string, Vector3> colorOverrides;
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> modifiers;

        public MaterialOverrides()
        {
            modifiers = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
            colorOverrides = new Dictionary<string, Vector3>();

            foreach (var modifier in MyDefinitionManager.Static.GetAssetModifierDefinitions())
            {
                if (modifier.DefaultColor.HasValue)
                    colorOverrides.Add(modifier.Id.SubtypeName, modifier.DefaultColor.Value.ColorToHSVDX11());

                var skinModifiers = new Dictionary<string, Dictionary<string, string>>();
                foreach (var texture in modifier.Textures)
                {
                    if (!skinModifiers.TryGetValue(texture.Location, out var value))
                    {
                        value = new Dictionary<string, string>();
                        skinModifiers.Add(texture.Location, value);
                    }

                    switch (texture.Type)
                    {
                        case MyTextureType.ColorMetal: value["ColorMetalTexture"] = texture.Filepath; break;
                        case MyTextureType.NormalGloss: value["NormalGlossTexture"] = texture.Filepath; break;
                        case MyTextureType.Extensions: value["AddMapsTexture"] = texture.Filepath; break;
                        case MyTextureType.Alphamask: value["AlphamaskTexture"] = texture.Filepath; break;
                        case MyTextureType.Unspecified: break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                modifiers.Add(modifier.Id.SubtypeName, skinModifiers);
            }
        }

        public Dictionary<string, string> GetModifier(string skin, string material)
        {
            Dictionary<string, string> modifier = null;
            modifiers.TryGetValue(skin, out var skinModifiers);
            skinModifiers?.TryGetValue(material, out modifier);
            return modifier;
        }

        public Vector3? GetColorOverride(string skin)
        {
            return colorOverrides.TryGetValue(skin, out var color) ? color : (Vector3?)null;
        }
    }
}