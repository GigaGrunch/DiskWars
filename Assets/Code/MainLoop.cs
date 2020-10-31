using System.Collections.Generic;
using UnityEngine;

namespace DiskWars
{
    public class MainLoop : MonoBehaviour
    {
        [SerializeField] private GameObject _diskPrefab;

        private readonly Dictionary<int, Disk> _disks = new Dictionary<int, Disk>();
        private readonly Dictionary<GameObject, int> _indexLookup = new Dictionary<GameObject, int>();
        private int _nextDiskIndex;
        private int _selectedDiskIndex;

        private void Start()
        {
            Disk[] diskTemplates = DiskLoader.LoadAll();
            foreach (Disk diskTemplate in diskTemplates)
            {
                SpawnDisk(diskTemplate);
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray clickRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(clickRay, out RaycastHit hit))
                {
                    if (hit.collider.CompareTag("Disk"))
                    {
                        _selectedDiskIndex = _indexLookup[hit.collider.gameObject];
                    }
                    else
                    {
                        Vector3 clickPoint = hit.point;
                        Disk selectedDisk = _disks[_selectedDiskIndex];
                        Vector3 randomPointOnFloor = new Vector3(
                            clickPoint.x,
                            selectedDisk.GameObject.transform.position.y,
                            clickPoint.z);

                        selectedDisk.MoveToward(randomPointOnFloor);
                    }
                }
            }
        }

        private void SpawnDisk(Disk template)
        {
            Disk disk = template;

            disk.Index = _nextDiskIndex++;

            disk.GameObject = Instantiate(_diskPrefab);
            disk.GameObject.name = disk.Name;

            disk.GameObject.transform.localScale = new Vector3(
                disk.Diameter,
                disk.GameObject.transform.localScale.y,
                disk.Diameter);

            disk.GameObject.GetComponent<Renderer>().material.mainTexture = disk.Texture;

            _disks.Add(disk.Index, disk);
            _indexLookup.Add(disk.GameObject, disk.Index);
        }
    }
}
