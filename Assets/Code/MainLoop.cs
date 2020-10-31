using System.Collections.Generic;
using UnityEngine;

namespace DiskWars
{
    public class MainLoop : MonoBehaviour
    {
        [SerializeField] private GameObject _diskPrefab;

        private readonly List<Disk> _disks = new List<Disk>();

        private void Start()
        {
            Disk[] diskTemplates = DiskLoader.LoadAll();
            foreach (Disk diskTemplate in diskTemplates)
            {
                Disk disk = diskTemplate;

                disk.GameObject = Instantiate(_diskPrefab);
                disk.GameObject.name = disk.Name;

                disk.GameObject.transform.localScale = new Vector3(
                    disk.Diameter,
                    disk.GameObject.transform.localScale.y,
                    disk.Diameter);

                disk.GameObject.GetComponent<Renderer>().material.mainTexture = disk.Texture;

                _disks.Add(disk);
            }
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
                        _disks[0].GameObject.transform.position.y,
                        clickPoint.z);

                    _disks[0].MoveToward(randomPointOnFloor);
                }
            }
        }
    }
}
