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
            DiskJson[] diskJsons = JsonLoader.LoadDisks();
            Dictionary<string, Texture2D> textureLookup = JsonLoader.LoadTextures();
            foreach (DiskJson diskJson in diskJsons)
            {
                SpawnDisk(diskJson, textureLookup);
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

        private void SpawnDisk(DiskJson json, Dictionary<string, Texture2D> textureLookup)
        {
            Disk disk = new Disk
            {
                Index = _nextDiskIndex++,
                Name = json.name,
                Diameter = json.diameter,
                GameObject = Instantiate(_diskPrefab)
            };

            disk.GameObject.name = disk.Name;

            disk.GameObject.transform.localScale = new Vector3(
                disk.Diameter,
                disk.GameObject.transform.localScale.y,
                disk.Diameter);

            Texture2D texture = textureLookup[json.texture];
            disk.GameObject.GetComponent<Renderer>().material.mainTexture = texture;

            _disks.Add(disk.Index, disk);
            _indexLookup.Add(disk.GameObject, disk.Index);
        }
    }
}
