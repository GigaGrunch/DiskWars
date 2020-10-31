using UnityEngine;

namespace DiskWars
{
    public struct Disk
    {
        public int ID;
        public string Name;
        public float Diameter;
        public Vector2 Position;
    }

    public struct DiskJson
    {
        public string name;
        public float diameter;
        public string texture;
    }
}
