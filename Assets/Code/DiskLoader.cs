using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DiskWars
{
    public static class DiskLoader
    {
        public static Disk[] LoadAll()
        {
            string disksPath = Path.Combine(Application.streamingAssetsPath, "Disks");

            string[] textureFiles = Directory.GetFiles(disksPath, "*.png");
            Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>(textureFiles.Length);

            foreach (string textureFile in textureFiles)
            {
                byte[] data = File.ReadAllBytes(textureFile);
                Texture2D texture = new Texture2D(0, 0);
                texture.LoadImage(data);
                textures[Path.GetFileName(textureFile)] = texture;
            }

            string[] jsonFiles = Directory.GetFiles(disksPath, "*.json");
            Disk[] disks = new Disk[jsonFiles.Length];

            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string file = jsonFiles[i];
                string text = File.ReadAllText(file);
                DiskJson json = JsonUtility.FromJson<DiskJson>(text);

                Disk disk = new Disk
                {
                    Name = json.name,
                    Diameter = json.diameter,
                    Texture = textures[json.texture]
                };

                disks[i] = disk;
            }

            return disks;
        }
    }
}
