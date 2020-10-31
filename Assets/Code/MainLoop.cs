using System.Collections.Generic;
using UnityEngine;

namespace DiskWars
{
    public class MainLoop : MonoBehaviour
    {
        [SerializeField] private GameObject _diskPrefab;

        private readonly Dictionary<int, Disk> _diskByIndex = new Dictionary<int, Disk>();
        private readonly Dictionary<int, GameObject> _actorByIndex = new Dictionary<int, GameObject>();
        private readonly Dictionary<GameObject, int> _indexByActor = new Dictionary<GameObject, int>();

        private int _nextDiskIndex;
        private int _selectedDiskIndex;

        private void Start()
        {
            DiskJson[] diskJsons = AssetLoading.LoadDisks();
            Dictionary<string, Texture2D> textureLookup = AssetLoading.LoadTextures();
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
                        _selectedDiskIndex = _indexByActor[hit.collider.gameObject];
                    }
                    else
                    {
                        Disk selectedDisk = _diskByIndex[_selectedDiskIndex];
                        Vector3 clickPoint = hit.point;
                        MoveDisk(selectedDisk, clickPoint);
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
            };

            GameObject actor = Instantiate(_diskPrefab);

            actor.name = disk.Name;

            actor.transform.localScale = new Vector3(
                disk.Diameter,
                actor.transform.localScale.y,
                disk.Diameter);

            Texture2D texture = textureLookup[json.texture];
            actor.GetComponent<Renderer>().material.mainTexture = texture;

            _diskByIndex.Add(disk.Index, disk);
            _indexByActor.Add(actor, disk.Index);
            _actorByIndex.Add(disk.Index, actor);
        }

        private void MoveDisk(Disk disk, Vector3 targetPoint)
        {
            GameObject actor = _actorByIndex[disk.Index];
            targetPoint.y = actor.transform.position.y;

            Vector3 diskLocation = actor.transform.position;
            Vector3 direction = targetPoint - diskLocation;
            Vector3 movement = direction.normalized * disk.Diameter;
            Vector3 targetLocation = diskLocation + movement;
            actor.transform.position = targetLocation;
        }
    }
}
