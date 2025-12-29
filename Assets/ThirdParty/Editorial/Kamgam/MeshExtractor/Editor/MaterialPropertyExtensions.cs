using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.MeshExtractor
{
    public static class MaterialPropertyExtensions
    {
        public static List<string> MainTextureNames = new List<string> { "_BaseMap", "_MainTex", "_AlbedoMap", "_AlbedoTex", "_Main", "_Albedo", "_BaseTexture", "_BaseTex", "_Base_Texture" };
        public static List<string> NormalMapNames = new List<string> { "_BumpMap", "_NormalMap", "_Bump", "_Normal", "_MainNormalMap", "_ParallaxMap" };
        public static List<string> SpecularMapNames = new List<string> { "_SpecGlossMap", "_SpecularColorMap", "_SpecularMap", "_Specular", "_MainSpecularMap" };
        public static List<string> MetallicMapNames = new List<string> { "_MetallicGlossMap", "_MetallicColorMap", "_MetallicMap", "_Metallic", "_MainMetallicMap" };
        public static List<string> EmissionMapNames = new List<string> { "_EmissionMap", "_EmissiveColorMap", "_Emission", "_EmissiveMap", "_Emissive", "_MainEmissiveMap" };
        public static List<string> OcclusionMapNames = new List<string> { "_OcclusionMap", "_OcclusionColorMap", "_Occlusion", "_MainOcclusionMap" };

        public static bool HasTextureProperty(this Material material, string propertyName)
        {
            if (material == null || material.shader == null)
                return false;

            int count = material.shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                string name = material.shader.GetPropertyName(i);
                if (name == propertyName)
                {
                    var type = material.shader.GetPropertyType(i);
                    if (type == UnityEngine.Rendering.ShaderPropertyType.Texture)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries each property name in order and returns the result for the first property that is found.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        public static Texture GetTextureOrNull(this Material material, params string[] propertyNames)
        {
            return GetTextureOrNullByName(material, propertyNames);
        }

        /// <summary>
        /// Tries each property name in order and returns the result for the first property that is found.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        public static Texture GetTextureOrNullByName(this Material material, IList<string> propertyNames)
        {
            if (material == null || material.shader == null)
                return null;

            for (int i = 0; i < propertyNames.Count; i++)
            {
                if (material.HasTextureProperty(propertyNames[i]))
                {
                    return material.GetTexture(propertyNames[i]);
                }
            }

            return null;
        }

        /// <summary>
        /// Tries each property name in order and sets the first property that is found.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="texture"></param>
        /// <param name="propertyNames"></param>
        public static void SetTexture(this Material material, Texture texture, params string[] propertyNames)
        {
            SetTextureByName(material, texture, propertyNames);
        }

        /// <summary>
        /// Tries each property name in order and sets the first property that is found.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="texture"></param>
        /// <param name="propertyNames"></param>
        public static void SetTextureByName(this Material material, Texture texture, IList<string> propertyNames)
        {
            if (material == null || material.shader == null || texture == null)
                return;

            for (int i = 0; i < propertyNames.Count; i++)
            {
                if (material.HasTextureProperty(propertyNames[i]))
                {
                    material.SetTexture(propertyNames[i], texture);
                    return;
                }
            }
        }

        public static Texture GetMainTexture(this Material material)
        {
            if (material == null || material.shader == null)
                return null;

            Texture result = material.mainTexture;
            
            if (result == null)
            {
                // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
                result = material.GetTextureOrNullByName(MainTextureNames);
            }

            return result;
        }

        public static void SetMainTexture(this Material material, Texture texture)
        {
            if (material == null || material.shader == null || texture == null)
                return;

            // Try to guess the texture property name.
            material.SetTextureByName(texture, MainTextureNames);

            // Try the unity method of setting too.
            material.mainTexture = texture;
        }

        public static Texture GetNormalMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(NormalMapNames);
        }

        public static void SetNormalMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, NormalMapNames);
        }


        public static Texture GetSpecularMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(SpecularMapNames);
        }

        public static void SetSpecularMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, SpecularMapNames);
        }


        public static Texture GetMetallicMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(MetallicMapNames);
        }

        public static void SetMetallicMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, MetallicMapNames);
        }


        public static Texture GetEmissionMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(EmissionMapNames);
        }

        public static void SetEmissionMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, EmissionMapNames);
        }


        public static Texture GetOcclusionMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(OcclusionMapNames);
        }

        public static void SetOcclusionMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, OcclusionMapNames);
        }

        public static bool IsMainName(this Material material, string propertyName)
        {
            return IsAlbedoName(material, propertyName);
        }

        public static bool IsAlbedoName(this Material material, string propertyName)
        {
            return MainTextureNames.Contains(propertyName);
        }

        public static bool IsNormalMapName(this Material material, string propertyName)
        {
            return NormalMapNames.Contains(propertyName);
        }

        public static bool IsSpecularMapName(this Material material, string propertyName)
        {
            return SpecularMapNames.Contains(propertyName);
        }

        public static bool IsMetallicMapName(this Material material, string propertyName)
        {
            return MetallicMapNames.Contains(propertyName);
        }

        public static bool IsEmissionMapName(this Material material, string propertyName)
        {
            return EmissionMapNames.Contains(propertyName);
        }

        public static bool IsOcclusionMapName(this Material material, string propertyName)
        {
            return OcclusionMapNames.Contains(propertyName);
        }

        // public static bool IsAutoDetectedTextureName(this Material material, string propertyName)
        // {
        //     return IsAutoDetectedTextureName(propertyName);
        // }

        // public static bool IsAutoDetectedTextureName(string propertyName)
        // {
        //     foreach (var name in allAutoDetectedTextureNames())
        //     {
        //         if (propertyName == name)
        //             return true;
        //     }
        // 
        //     return false;
        // }

        // public static IEnumerable<string> AllAutoDetectedTextureNames => allAutoDetectedTextureNames();
        // 
        // static IEnumerable<string> allAutoDetectedTextureNames()
        // {
        //     foreach (var name in MainTextureNames)
        //         yield return name;
        // 
        //     foreach (var name in NormalMapNames)
        //         yield return name;
        // 
        //     foreach (var name in SpecularMapNames)
        //         yield return name;
        // 
        //     foreach (var name in MetallicMapNames)
        //         yield return name;
        // 
        //     foreach (var name in EmissionMapNames)
        //         yield return name;
        // 
        //     foreach (var name in OcclusionMapNames)
        //         yield return name;
        // }
    }
}
