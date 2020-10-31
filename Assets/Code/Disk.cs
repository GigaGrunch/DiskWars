using UnityEngine;

namespace DiskWars
{
    public struct Disk
    {
        public string Name;
        public float Diameter;
        public Texture2D Texture;
        public GameObject GameObject;
    }

    public struct DiskJson
    {
        public string name;
        public float diameter;
        public string texture;
    }
}
