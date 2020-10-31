using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiskWars
{
    public class MainLoop : MonoBehaviour
    {
        [SerializeField] private GameObject _diskPrefab;
        [SerializeField] private GameObject _diskGhostPrefab;

        private readonly Disk[] _disks = new Disk[1024];
        private readonly Dictionary<int, GameObject> _actorByID = new Dictionary<int, GameObject>();
        private readonly Dictionary<GameObject, int> _idByActor = new Dictionary<GameObject, int>();

        private int _nextDiskID;
        private int _selectedDiskID;

        private GameObject _diskGhost;
        private Camera _camera;

        private void Start()
        {
            DiskJson[] diskJsons = AssetLoading.LoadDisks();
            Dictionary<string, Texture2D> textureLookup = AssetLoading.LoadTextures();
            foreach (DiskJson diskJson in diskJsons)
            {
                SpawnDisk(diskJson, textureLookup);
            }

            _camera = Camera.main;
            _diskGhost = Instantiate(_diskGhostPrefab);
            _diskGhost.transform.localScale = _actorByID[0].transform.localScale;
        }

        private void Update()
        {
            ref Disk selectedDisk = ref DiskByID(_selectedDiskID);

            Ray mouseRay = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit hit) == false)
            {
                return;
            }

            Vector3 mousePosition = hit.point;
            Vector3 targetPoint = mousePosition;
            GameObject actor = _actorByID[selectedDisk.ID];
            targetPoint.y = actor.transform.position.y;
            Vector3 diskLocation = actor.transform.position;
            Vector3 direction = targetPoint - diskLocation;
            Vector3 movement = direction.normalized * selectedDisk.Diameter;
            Vector3 targetLocation = diskLocation + movement;

            _diskGhost.transform.position = targetLocation;

            if (Input.GetMouseButtonDown(0))
            {
                if (hit.collider.CompareTag("Disk"))
                {
                    GameObject diskActor = hit.collider.gameObject;
                    _selectedDiskID = _idByActor[diskActor];
                    _diskGhost.transform.localScale = diskActor.transform.localScale;
                }
                else
                {
                    actor.transform.position = targetLocation;
                    selectedDisk.Position.X = targetLocation.x;
                    selectedDisk.Position.Z = targetLocation.z;
                    Debug.Log($"overlaps: {OverlapsAny(selectedDisk)}");

                    for (int i = 0; i < _nextDiskID; i++)
                    {
                        Debug.Log($"disk at {_disks[i].Position.X}, {_disks[i].Position.Z}");
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

            _disks[disk.ID] = disk;
            _idByActor.Add(actor, disk.ID);
            _actorByID.Add(disk.ID, actor);
        }

        private bool OverlapsAny(Disk disk)
        {
            foreach (Disk other in _disks)
            {
                if (disk.ID == other.ID)
                {
                    continue;
                }

                float sqrDistance = Position.SquareDistance(disk.Position, other.Position);
                float minSqrDistance = Mathf.Pow(disk.Diameter / 2f + other.Diameter / 2f, 2f);
                bool horizontalOverlap = sqrDistance < minSqrDistance;

                if (horizontalOverlap)
                {
                    return true;
                }
            }

            return false;
        }

        private ref Disk DiskByID(int id)
        {
            for (int i = 0; i < _disks.Length; i++)
            {
                ref Disk disk = ref _disks[i];

                if (disk.ID == id)
                {
                    return ref disk;
                }
            }

            throw new InvalidOperationException();
        }
    }
}
