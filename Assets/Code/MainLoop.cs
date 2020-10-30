using System.Collections;
using UnityEngine;

namespace DiskWars
{
    public class MainLoop : MonoBehaviour
    {
        private IEnumerator Start()
        {
            Disk disk;
            disk.Diameter = 1f;
            disk.GameObject = GameObject.Find("Disk");

            while (true)
            {
                yield return new WaitForSeconds(1f);

                Vector2 randomPoint = Random.insideUnitCircle;
                randomPoint *= 10f;
                Vector3 randomPointOnFloor = new Vector3(
                    randomPoint.x,
                    disk.GameObject.transform.position.y,
                    randomPoint.y);

                disk.MoveToward(randomPointOnFloor);
            }
        }
    }
}
