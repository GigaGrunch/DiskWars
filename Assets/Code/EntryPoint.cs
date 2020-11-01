using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace DiskWars
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private GameObject _diskPrefab;
        [SerializeField] private GameObject _diskGhostPrefab;

        private readonly List<Disk> _disks = new List<Disk>();
        private readonly Dictionary<int, GameObject> _actorByID = new Dictionary<int, GameObject>();
        private readonly Dictionary<GameObject, int> _idByActor = new Dictionary<GameObject, int>();

        private FlapAnimation _currentFlap;
        private readonly Queue<FlapAnimation> _flapQueue = new Queue<FlapAnimation>();

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

            _selectedDiskID = -1;

            _camera = Camera.main;
            _diskGhost = Instantiate(_diskGhostPrefab);
            _diskGhost.transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            Disk selectedDisk = null;

            if (_selectedDiskID >= 0)
            {
                selectedDisk = _disks.First(d => d.ID == _selectedDiskID);
            }

            Ray mouseRay = _camera.ScreenPointToRay(Input.mousePosition);
            bool hitSomething = Physics.Raycast(mouseRay, out RaycastHit hit);

            if (hitSomething && selectedDisk != null)
            {
                Vector3 mousePosition = hit.point;
                Vector3 targetPoint = mousePosition;
                targetPoint.y = selectedDisk.Position.y;
                Vector3 diskLocation = selectedDisk.Position;
                Vector3 direction = targetPoint - diskLocation;
                Vector3 movement = direction.normalized * selectedDisk.Diameter;
                Vector3 targetLocation = diskLocation + movement;
                targetLocation.y = Disk.THICKNESS / 2f;

                while (HasCollisions(selectedDisk, targetLocation))
                {
                    targetLocation.y += Disk.THICKNESS;
                }

                _diskGhost.transform.position = targetLocation;
            }

            if (Input.GetMouseButton(0))
            {
                if (hitSomething && hit.collider.CompareTag("Disk"))
                {
                    GameObject diskActor = hit.collider.gameObject;
                    _selectedDiskID = _idByActor[diskActor];
                    _diskGhost.transform.localScale = diskActor.transform.localScale;
                }
                else
                {
                    _selectedDiskID = -1;
                    _diskGhost.transform.localScale = Vector3.zero;
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (selectedDisk != null)
                {
                    selectedDisk.Position = _diskGhost.transform.position;

                    GameObject actor = _actorByID[selectedDisk.ID];
                    FlapAnimation flapAnimation = new FlapAnimation
                    {
                        Actor = actor,
                        TargetLocation = selectedDisk.Position
                    };

                    _flapQueue.Enqueue(flapAnimation);
                }
            }

            if (_flapQueue.Any() && _currentFlap == null)
            {
                FlapAnimation flap = _flapQueue.Dequeue();
                StartCoroutine(PerformFlap(flap));
            }
        }

        private IEnumerator PerformFlap(FlapAnimation flap)
        {
            _currentFlap = flap;

            GameObject actor = flap.Actor;
            Vector3 targetLocation = flap.TargetLocation;

            Vector3 rotationPoint = Math.Between(actor.transform.position, targetLocation);
            Vector3 direction = (targetLocation - actor.transform.position).normalized;
            Vector3 rotationAxis = new Vector3(direction.z, 0f, -direction.x);

            for (int i = 0; i < 90; i++)
            {
                actor.transform.RotateAround(point: rotationPoint, axis: rotationAxis, 2f);
                yield return null;
            }

            actor.transform.position = targetLocation;

            _currentFlap = null;
        }

        private void SpawnDisk(DiskJson json, Dictionary<string, Texture2D> textureLookup)
        {
            Disk disk = new Disk
            {
                ID = _nextDiskID++,
                Name = json.name,
                Diameter = json.diameter,
                Position = new Vector3(0f, Disk.THICKNESS / 2f, 0f)
            };

            while (HasCollisions(disk, disk.Position))
            {
                disk.Position.y += Disk.THICKNESS;
            }

            GameObject actor = Instantiate(_diskPrefab);

            actor.name = disk.Name;
            actor.transform.position = disk.Position;

            actor.transform.localScale = new Vector3(
                disk.Diameter,
                Disk.THICKNESS / 2f,
                disk.Diameter);

            Texture2D texture = textureLookup[json.texture];
            actor.GetComponent<Renderer>().material.mainTexture = texture;

            _disks.Add(disk);
            _idByActor.Add(actor, disk.ID);
            _actorByID.Add(disk.ID, actor);
        }

        private bool HasCollisions(Disk disk, Vector3 overridePosition)
        {
            foreach (Disk other in _disks.Where(other => disk.ID != other.ID))
            {
                {
                    float sqrDistance = Math.SquaredHorizontalDistance(overridePosition, other.Position);
                    float minSqrDistance = Math.Square(disk.Diameter / 2f + other.Diameter / 2f);
                    bool horizontalOverlap = sqrDistance < minSqrDistance;
                    if (horizontalOverlap == false)
                    {
                        continue;
                    }
                }
                {
                    float sqrDistance = Math.SquaredVerticalDistance(overridePosition, other.Position);
                    float minSqrDistance = Math.Square(Disk.THICKNESS);

                    bool verticalOverlap = sqrDistance < minSqrDistance;
                    if (verticalOverlap == false)
                    {
                        continue;
                    }
                }

                return true;
            }

            return false;
        }

        private class FlapAnimation
        {
            public GameObject Actor;
            public Vector3 TargetLocation;
        }
    }
}
