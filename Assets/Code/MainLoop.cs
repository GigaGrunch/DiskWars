using System.Collections.Generic;
using UnityEngine;

namespace DiskWars
{
    public class MainLoop : MonoBehaviour
    {
        [SerializeField] private GameObject _diskPrefab;
        [SerializeField] private GameObject _diskGhostPrefab;

        private readonly Dictionary<int, Disk> _diskByID = new Dictionary<int, Disk>();
        private readonly Dictionary<int, GameObject> _actorByID = new Dictionary<int, GameObject>();
        private readonly Dictionary<GameObject, int> _idByActor = new Dictionary<GameObject, int>();

        private int _nextDiskID;
        private int _selectedDiskID;

        private GameObject _diskGhost;

        private void Start()
        {
            DiskJson[] diskJsons = AssetLoading.LoadDisks();
            Dictionary<string, Texture2D> textureLookup = AssetLoading.LoadTextures();
            foreach (DiskJson diskJson in diskJsons)
            {
                SpawnDisk(diskJson, textureLookup);
            }

            _diskGhost = Instantiate(_diskGhostPrefab);
            _diskGhost.transform.localScale *= 0f;
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
                        GameObject diskActor = hit.collider.gameObject;
                        _selectedDiskID = _idByActor[diskActor];
                        _diskGhost.transform.localScale = diskActor.transform.localScale;
                    }
                    else
                    {
                        Disk selectedDisk = _diskByID[_selectedDiskID];
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
                ID = _nextDiskID++,
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

            _diskByID.Add(disk.ID, disk);
            _idByActor.Add(actor, disk.ID);
            _actorByID.Add(disk.ID, actor);
        }

        private void MoveDisk(Disk disk, Vector3 targetPoint)
        {
            GameObject actor = _actorByID[disk.ID];
            targetPoint.y = actor.transform.position.y;

            Vector3 diskLocation = actor.transform.position;
            Vector3 direction = targetPoint - diskLocation;
            Vector3 movement = direction.normalized * disk.Diameter;
            Vector3 targetLocation = diskLocation + movement;
            actor.transform.position = targetLocation;
        }
    }
}
