using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DiskWars
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private GameObject _diskPrefab;
        [SerializeField] private GameObject _diskGhostPrefab;

        [SerializeField] private Transform _player1Spawn;
        [SerializeField] private Transform _player2Spawn;

        [SerializeField] private Text _currentPlayerDisplay;
        [SerializeField] private Button _endTurnButton;

        private readonly List<Disk> _disks = new List<Disk>();
        private readonly Dictionary<int, GameObject> _actorByID = new Dictionary<int, GameObject>();
        private readonly Dictionary<GameObject, int> _idByActor = new Dictionary<GameObject, int>();

        private FlapAnimation _currentFlap;
        private readonly Queue<FlapAnimation> _flapQueue = new Queue<FlapAnimation>();

        private int _nextDiskID;
        private int _selectedDiskID;

        private int _currentPlayer;

        private GameObject _diskGhost;
        private Camera _camera;

        private TcpListener _server;
        private TcpClient _connectedClient;
        private NetworkStream _networkStream;

        private TcpClient _client;

        private IEnumerator Start()
        {
            switch (MainMenu.NetworkMode)
            {
                case NetworkMode.None:
                {
                    yield return StartSingleplayer();
                } break;
                case NetworkMode.Host:
                {
                    yield return StartServer();
                } break;
                case NetworkMode.Client:
                {
                    StartClient();
                } break;
            }
        }

        private void OnDestroy()
        {
            _server = null;
            _connectedClient = null;
            _client = null;
        }

        private IEnumerator StartServer()
        {
            _server = new TcpListener(IPAddress.Loopback, 7777);

            _server.Start();

            bool connected = false;
            Thread connectThread = new Thread(() =>
            {
                _connectedClient = _server.AcceptTcpClient();
                _networkStream = _connectedClient.GetStream();
                connected = true;
            });
            connectThread.Start();

            while (connected == false)
            {
                yield return null;
            }

            Debug.Log("connected!");

            NetworkMessage message = new NetworkMessage
            {
                type = NetworkMessage.Type.chat,
                chat = new ChatMessage { message = "hello!" }
            };

            SendToClient(message);

            message.chat.message = "bye!";

            SendToClient(message);
        }

        private void SendToClient(NetworkMessage message)
        {
            StreamWriter writer = new StreamWriter(_networkStream);
            string json = JsonUtility.ToJson(message);
            writer.WriteLine(json);
            writer.Flush();
        }

        [Serializable]
        private struct ChatMessage
        {
            public string message;
        }

        [Serializable]
        private struct NetworkMessage
        {
            public enum Type
            {
                unknown,
                chat,
                diskSpawn
            }

            public Type type;
            public ChatMessage chat;
        }

        private void StartClient()
        {
            _client = new TcpClient();
            _client.Connect(IPAddress.Loopback, 7777);
            Debug.Log("connected");

            ReadServerMessagesAsync();
        }

        private async void ReadServerMessagesAsync()
        {
            StreamReader reader = new StreamReader(_client.GetStream());

            while (_client != null)
            {
                Task<string> readTask = reader.ReadLineAsync();
                await readTask;
                Debug.Log(readTask.Result);
            }
        }

        private IEnumerator StartSingleplayer()
        {
            DiskJson[] diskJsons = AssetLoading.LoadDisks();
            Dictionary<string, Texture2D> textureLookup = AssetLoading.LoadTextures();

            bool player1 = true;
            foreach (DiskJson diskJson in diskJsons)
            {
                SpawnDisk(diskJson, textureLookup, player1 ? 1 : 2);
                player1 = player1 == false;
            }

            _selectedDiskID = -1;

            _camera = Camera.main;
            _diskGhost = Instantiate(_diskGhostPrefab);
            _diskGhost.transform.localScale = Vector3.zero;

            _currentPlayer = 1;
            _currentPlayerDisplay.text = $"Player {_currentPlayer}'s turn";
            _endTurnButton.onClick.AddListener(EndTurn);

            while (true)
            {
                Disk selectedDisk = null;

                if (_selectedDiskID >= 0)
                {
                    selectedDisk = _disks[_selectedDiskID];
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
                        int diskID = _idByActor[diskActor];
                        Disk disk = _disks[diskID];

                        if (disk.Player == _currentPlayer)
                        {
                            _selectedDiskID = diskID;
                            _diskGhost.transform.localScale = diskActor.transform.localScale;

                            Material ghostMaterial = _diskGhost.GetComponent<Renderer>().material;
                            Color ghostColor = disk.RemainingMoves > 0 ? Color.blue : Color.red;
                            ghostColor.a = ghostMaterial.color.a;
                            ghostMaterial.color = ghostColor;
                        }
                    }
                    else
                    {
                        _selectedDiskID = -1;
                        _diskGhost.transform.localScale = Vector3.zero;
                    }
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    if (selectedDisk != null && selectedDisk.RemainingMoves > 0)
                    {
                        selectedDisk.RemainingMoves -= 1;
                        selectedDisk.Position = _diskGhost.transform.position;

                        Material ghostMaterial = _diskGhost.GetComponent<Renderer>().material;
                        Color ghostColor = selectedDisk.RemainingMoves > 0 ? Color.blue : Color.red;
                        ghostColor.a = ghostMaterial.color.a;
                        ghostMaterial.color = ghostColor;

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

                yield return null;
            }
        }

        private void EndTurn()
        {
            _currentPlayer++;

            if (_currentPlayer > 2)
            {
                _currentPlayer = 1;
            }

            _currentPlayerDisplay.text = $"Player {_currentPlayer}'s turn";

            foreach (Disk disk in _disks)
            {
                disk.RemainingMoves = disk.MaxMoves;
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

        private void SpawnDisk(DiskJson json, Dictionary<string, Texture2D> textureLookup, int player)
        {
            Vector3 position = Vector3.zero;

            switch (player)
            {
                case 1:
                    position = _player1Spawn.position;
                    break;
                case 2:
                    position = _player2Spawn.position;
                    break;
            }

            position.y = Disk.THICKNESS;

            Disk disk = new Disk
            {
                ID = _nextDiskID++,
                Player = player,
                Name = json.name,
                Diameter = json.diameter,
                Position = position,
                MaxMoves = json.moves,
                RemainingMoves = json.moves
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

    public enum NetworkMode
    {
        None,
        Host,
        Client
    }
}
