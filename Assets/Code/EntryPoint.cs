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
    class EntryPoint : MonoBehaviour
    {
        [SerializeField] GameObject _diskPrefab;
        [SerializeField] GameObject _diskGhostPrefab;

        [SerializeField] Transform _player1Spawn;
        [SerializeField] Transform _player2Spawn;

        [SerializeField] Text _currentPlayerDisplay;
        [SerializeField] Button _endTurnButton;

        List<Disk> _disks = new List<Disk>();
        Dictionary<int, GameObject> _actorByID = new Dictionary<int, GameObject>();
        Dictionary<GameObject, int> _idByActor = new Dictionary<GameObject, int>();

        FlapAnimation _currentFlap;
        Queue<FlapAnimation> _flapQueue = new Queue<FlapAnimation>();

        int _nextDiskID;
        int _selectedDiskID;

        int _currentPlayer;
        int _playerID;

        GameObject _diskGhost;
        Camera _camera;

        TcpListener _server;
        TcpClient _connectedClient;
        NetworkStream _networkStream;

        TcpClient _client;
        DiskJson[] _diskJsons;
        Dictionary<string, Texture2D> _textureLookup;

        bool _doUnityUpdate;
        bool _networkListening;

        IEnumerator Start()
        {
            _selectedDiskID = -1;
            _camera = Camera.main;

            _diskGhost = Instantiate(_diskGhostPrefab);
            _diskGhost.transform.localScale = Vector3.zero;
            _diskGhost.SetActive(false);

            _diskJsons = AssetLoading.LoadDisks();
            _textureLookup = AssetLoading.LoadTextures();

            switch (MainMenu.NetworkMode)
            {
                case NetworkMode.None:
                {
                    StartSingleplayer();
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

            _doUnityUpdate = true;
        }

        void Update()
        {
            if (_doUnityUpdate == false)
            {
                return;
            }

            switch (MainMenu.NetworkMode)
            {
                case NetworkMode.None:
                {
                    SingleplayerUpdate();
                } break;
                case NetworkMode.Host:
                {
                    ServerUpdate();
                } break;
                case NetworkMode.Client:
                {
                    ClientUpdate();
                } break;
            }
        }

        void OnDestroy()
        {
            _doUnityUpdate = false;
            _networkListening = false;
        }

        IEnumerator StartServer()
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

            _networkListening = true;
            ReadClientMessagesAsync();

            NetworkMessage message = new NetworkMessage();

            message.type = NetworkMessage.Type.Chat;
            message.chat.message = "hello!";
            SendNetworkMessage(message);

            message.type = NetworkMessage.Type.InitializeClient;
            message.initializeClient.playerID = 2;
            _playerID = 1;

            SendNetworkMessage(message);

            message.type = NetworkMessage.Type.DiskSpawn;

            for (int i = 0; i < _diskJsons.Length; i++)
            {
                int player = (i % 2) + 1;
                DiskJson disk = _diskJsons[i];
                SpawnDisk(disk.name, player);
                message.diskSpawn.player = player;
                message.diskSpawn.diskName = disk.name;
                SendNetworkMessage(message);
            }

            _currentPlayer = 1;
            _currentPlayerDisplay.text = _currentPlayer == _playerID
                ? "It's your turn!"
                : $"Player {_currentPlayer}'s turn";

            if (_currentPlayer == _playerID)
            {
                _diskGhost.SetActive(true);
            }

            message.type = NetworkMessage.Type.PlayerTurnUpdateMessage;
            message.playerTurnUpdate.currentPlayer = _currentPlayer;
            SendNetworkMessage(message);

            _endTurnButton.onClick.AddListener(EndTurnServer);
        }

        void UpdateDiskGhost(bool hitSomething, Vector3 hitPoint)
        {
            Disk selectedDisk = null;

            if (_selectedDiskID >= 0)
            {
                selectedDisk = _disks[_selectedDiskID];
            }

            if (hitSomething && selectedDisk != null)
            {
                Vector3 mousePosition = hitPoint;
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
        }

        Disk HandleSelectionInput(GameObject hitDiskActor)
        {
            if (Input.GetMouseButton(0))
            {
                if (ReferenceEquals(hitDiskActor, null) == false)
                {
                    int diskID = _idByActor[hitDiskActor];
                    Disk disk = _disks[diskID];

                    if (disk.Player == _currentPlayer)
                    {
                        _selectedDiskID = diskID;
                        _diskGhost.transform.localScale = hitDiskActor.transform.localScale;

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

            return _selectedDiskID >= 0 ? _disks[_selectedDiskID] : null;
        }

        void MouseRaycast(
            out bool hitAnything,
            out Vector3 hitPosition,
            out GameObject hitDiskActor)
        {
            Ray mouseRay = _camera.ScreenPointToRay(Input.mousePosition);
            hitAnything = Physics.Raycast(mouseRay, out RaycastHit hit);
            hitPosition = hit.point;
            bool hitDisk = hitAnything && hit.collider.CompareTag("Disk");
            hitDiskActor = hitDisk ? hit.collider.gameObject : null;
        }

        void ServerUpdate()
        {
            MouseRaycast(out bool hitAnything, out Vector3 hitPosition, out GameObject hitDiskActor);
            UpdateDiskGhost(hitAnything, hitPosition);
            HandleSelectionInput(hitDiskActor);
        }

        void ClientUpdate()
        {
            MouseRaycast(out bool hitAnything, out Vector3 hitPosition, out GameObject hitDiskActor);
            UpdateDiskGhost(hitAnything, hitPosition);
            HandleSelectionInput(hitDiskActor);
        }

        void SingleplayerUpdate()
        {
            MouseRaycast(out bool hitAnything, out Vector3 hitPosition, out GameObject hitDiskActor);
            UpdateDiskGhost(hitAnything, hitPosition);
            Disk selectedDisk = HandleSelectionInput(hitDiskActor);

            if (Input.GetMouseButtonDown(1))
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
        }

        void StartClient()
        {
            _client = new TcpClient();
            _client.Connect(IPAddress.Loopback, 7777);
            _networkStream = _client.GetStream();
            Debug.Log("connected");

            _networkListening = true;
            ReadServerMessagesAsync();

            _endTurnButton.onClick.AddListener(EndTurnClient);
        }

        void SendNetworkMessage(NetworkMessage message)
        {
            StreamWriter writer = new StreamWriter(_networkStream);
            string json = JsonUtility.ToJson(message);
            writer.WriteLine(json);
            writer.Flush();
        }

        async void ReadClientMessagesAsync()
        {
            StreamReader reader = new StreamReader(_networkStream);

            while (_networkListening)
            {
                Task<string> readTask = reader.ReadLineAsync();
                await readTask;

                string json = readTask.Result;
                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }

                Debug.Log(json);

                NetworkMessage message = JsonUtility.FromJson<NetworkMessage>(json);

                switch (message.type)
                {
                    case NetworkMessage.Type.Chat:
                        Debug.Log(message.chat.message);
                        break;
                    case NetworkMessage.Type.DiskSpawn:
                        break;
                    case NetworkMessage.Type.PlayerTurnUpdateMessage:
                        EndTurnServer();
                        break;
                    default:
                        Debug.LogError($"{message.type} is not a valid value for {typeof(NetworkMessage.Type)}.");
                        break;
                }
            }
        }

        async void ReadServerMessagesAsync()
        {
            StreamReader reader = new StreamReader(_networkStream);

            while (_networkListening)
            {
                Task<string> readTask = reader.ReadLineAsync();
                await readTask;

                string json = readTask.Result;
                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }

                Debug.Log(json);

                NetworkMessage message = JsonUtility.FromJson<NetworkMessage>(json);

                switch (message.type)
                {
                    case NetworkMessage.Type.Chat:
                        Debug.Log(message.chat.message);
                        break;
                    case NetworkMessage.Type.DiskSpawn:
                        SpawnDisk(message.diskSpawn.diskName, message.diskSpawn.player);
                        break;
                    case NetworkMessage.Type.InitializeClient:
                        _playerID = message.initializeClient.playerID;
                        break;
                    case NetworkMessage.Type.PlayerTurnUpdateMessage:
                        _currentPlayer = message.playerTurnUpdate.currentPlayer;
                        _currentPlayerDisplay.text = _currentPlayer == _playerID
                            ? "It's your turn!"
                            : $"Player {_currentPlayer}'s turn";
                        if (_currentPlayer == _playerID)
                        {
                            _diskGhost.SetActive(true);
                        }
                        break;
                    default:
                        Debug.LogError($"{message.type} is not a valid value for {typeof(NetworkMessage.Type)}.");
                        break;
                }
            }
        }

        void StartSingleplayer()
        {
            bool player1 = true;
            foreach (DiskJson diskJson in _diskJsons)
            {
                SpawnDisk(diskJson.name, player1 ? 1 : 2);
                player1 = player1 == false;
            }

            _currentPlayer = 1;
            _currentPlayerDisplay.text = $"Player {_currentPlayer}'s turn";
            _endTurnButton.onClick.AddListener(EndTurnSingleplayer);
        }

        void EndTurnSingleplayer()
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

        void EndTurnClient()
        {
            if (_currentPlayer != _playerID)
            {
                return;
            }

            NetworkMessage message = new NetworkMessage();
            message.type = NetworkMessage.Type.PlayerTurnUpdateMessage;
            SendNetworkMessage(message);
        }

        void EndTurnServer()
        {
            _currentPlayer++;

            if (_currentPlayer > 2)
            {
                _currentPlayer = 1;
            }

            _currentPlayerDisplay.text = _currentPlayer == _playerID
                ? "It's your turn!"
                : $"Player {_currentPlayer}'s turn";

            foreach (Disk disk in _disks)
            {
                disk.RemainingMoves = disk.MaxMoves;
            }

            NetworkMessage message = new NetworkMessage();
            message.type = NetworkMessage.Type.PlayerTurnUpdateMessage;
            message.playerTurnUpdate.currentPlayer = _currentPlayer;
            SendNetworkMessage(message);
        }

        IEnumerator PerformFlap(FlapAnimation flap)
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

        void SpawnDisk(string name, int player)
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

            DiskJson json = _diskJsons.First(d => d.name == name);

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

            Texture2D texture = _textureLookup[json.texture];
            actor.GetComponent<Renderer>().material.mainTexture = texture;

            _disks.Add(disk);
            _idByActor.Add(actor, disk.ID);
            _actorByID.Add(disk.ID, actor);
        }

        bool HasCollisions(Disk disk, Vector3 overridePosition)
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

        class FlapAnimation
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

    [Serializable]
    struct ChatMessage
    {
        public string message;
    }

    [Serializable]
    struct DiskSpawnMessage
    {
        public int player;
        public string diskName;
    }

    [Serializable]
    struct InitializeClientMessage
    {
        public int playerID;
    }

    [Serializable]
    struct PlayerTurnUpdateMessage
    {
        public int currentPlayer;
    }

    [Serializable]
    struct NetworkMessage
    {
        public enum Type
        {
            Unknown,
            Chat,
            DiskSpawn,
            InitializeClient,
            PlayerTurnUpdateMessage
        }

        public Type type;
        public ChatMessage chat;
        public DiskSpawnMessage diskSpawn;
        public InitializeClientMessage initializeClient;
        public PlayerTurnUpdateMessage playerTurnUpdate;
    }
}
