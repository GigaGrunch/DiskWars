using System.IO;
using UnityEngine;

namespace DiskWars
{
    public static class DiskLoader
    {
        public static Disk[] LoadAll()
        {
            string disksPath = Path.Combine(Application.streamingAssetsPath, "Disks");
            string[] jsonFiles = Directory.GetFiles(disksPath, "*.json");
            Disk[] disks = new Disk[jsonFiles.Length];

            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string file = jsonFiles[i];
                string json = File.ReadAllText(file);
                Disk disk = JsonUtility.FromJson<Disk>(json);
                disks[i] = disk;
            }

            return disks;
        }
    }
}
