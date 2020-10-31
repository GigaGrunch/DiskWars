using UnityEngine;

namespace DiskWars
{
    public class MainLoop : MonoBehaviour
    {
        private Disk _disk;

        private void Start()
        {
            _disk.Diameter = 1f;
            _disk.GameObject = GameObject.Find("Disk");
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray clickRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(clickRay, out RaycastHit hit))
                {
                    Vector3 clickPoint = hit.point;
                    Vector3 randomPointOnFloor = new Vector3(
                        clickPoint.x,
                        _disk.GameObject.transform.position.y,
                        clickPoint.z);

                    _disk.MoveToward(randomPointOnFloor);
                }
            }
        }
    }
}
