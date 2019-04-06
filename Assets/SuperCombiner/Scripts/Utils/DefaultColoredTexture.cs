using UnityEngine;
using System.Collections;

namespace LunarCatsStudio.SuperCombiner
{
    /// <summary>
    /// Returns the default color for each texture property
    /// </summary>
    public class DefaultColoredTexture
    {
        public static Color GetDefaultTextureColor(string textureProperty)
        {
            if (textureProperty.Equals("_BumpMap"))
            {
                return new Color(0.5f, 0.5f, 1f);
            }
            if (textureProperty.Equals("_MetallicGlossMap"))
            {
                return new Color(0f, 0f, 0f, 1f);
            }
            if (textureProperty.Equals("_ParallaxMap"))
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            if (textureProperty.Equals("_OcclusionMap"))
            {
                return new Color(1f, 1f, 1f, 1f);
            }
            if (textureProperty.Equals("_EmissionMap"))
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            if (textureProperty.Equals("_DetailMask"))
            {
                return new Color(0f, 0f, 0f, 0f);
            }
            return Color.white;
        }
    }
}
