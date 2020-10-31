using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DiskWars
{
    public static class AssetLoading
    {
        public static DiskJson[] LoadDisks()
        {
            string[] jsonFiles = Directory.GetFiles(DisksPath, "*.json");
            DiskJson[] disks = new DiskJson[jsonFiles.Length];

            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string file = jsonFiles[i];
                string text = File.ReadAllText(file);
                DiskJson disk = JsonUtility.FromJson<DiskJson>(text);
                disks[i] = disk;
            }

            return disks;
        }

        public static Dictionary<string, Texture2D> LoadTextures()
        {
            string[] textureFiles = Directory.GetFiles(DisksPath, "*.png");
            Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>(textureFiles.Length);

            foreach (string textureFile in textureFiles)
            {
                byte[] data = File.ReadAllBytes(textureFile);
                Texture2D texture = new Texture2D(0, 0);
                texture.LoadImage(data);
                textures[Path.GetFileName(textureFile)] = texture;
            }

            return textures;
        }

        private static string DisksPath => Path.Combine(Application.streamingAssetsPath, "Disks");
    }
}
