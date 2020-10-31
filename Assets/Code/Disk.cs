using UnityEngine;

namespace DiskWars
{
    public class Disk
    {
        public const float THICKNESS = 0.02f;

        public int ID;
        public string Name;
        public float Diameter;
        public Vector3 Position;
    }

    public struct DiskJson
    {
        public string name;
        public float diameter;
        public string texture;
    }
}
