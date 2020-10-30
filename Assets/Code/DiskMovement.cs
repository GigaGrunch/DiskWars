using UnityEngine;

namespace DiskWars
{
    public static class DiskMovement
    {
        public static void MoveToward(this Disk disk, Vector3 point)
        {
            Vector3 diskLocation = disk.GameObject.transform.position;
            Vector3 direction = point - diskLocation;
            Vector3 movement = direction.normalized * disk.Diameter;
            Vector3 targetLocation = diskLocation + movement;
            disk.GameObject.transform.position = targetLocation;
        }
    }
}
