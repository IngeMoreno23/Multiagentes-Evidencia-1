using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

[Serializable]
public class BirdState
{
    public float[] pos;
    public float[] vel;
}

public class FlockClient : MonoBehaviour
{
    [Header("Socket TCP")]
    [SerializeField] string _ip = "127.0.0.1";
    [SerializeField] int _port = 1110;

    Socket _socket;
    Thread _thread;
    volatile bool _running;

    [Header("Birds Instances")]
    [SerializeField] GameObject birdPrefab;
    readonly Dictionary<string, Bird> _birds = new();
    Dictionary<string, BirdState> _lastTick = new();

    void Start()
    {
        Application.runInBackground = true;
        StartClient();
    }

    void Update()
    {
        if (_lastTick == null)
            return;

        foreach (var kv in _lastTick)
        {
            string id = kv.Key;
            BirdState state = kv.Value;

            Vector2 pos = new Vector2(state.pos[0], state.pos[1]);
            Vector2 vel = new Vector2(state.vel[0], state.vel[1]);

            if (!_birds.ContainsKey(id))
            {
                GameObject go = Instantiate(birdPrefab);
                Bird bird = go.GetComponent<Bird>();
                _birds[id] = bird;

                go.transform.position = new Vector3(pos.x, 0, pos.y);
            }

            _birds[id].UpdateFromServer(pos, vel);
        }
    }

    void StartClient()
    {
        _running = true;
        _thread = new Thread(ClientThread);
        _thread.IsBackground = true;
        _thread.Start();
    }

    void ClientThread()
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(_ip, _port);

            byte[] buffer = new byte[8192];

            while (_running)
            {
                if (_socket.Available == 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                int bytes = _socket.Receive(buffer);
                if (bytes <= 0)
                    continue;

                string json = Encoding.UTF8.GetString(buffer, 0, bytes);

                Debug.Log(json);

                if (json.StartsWith("Tick:"))
                    json = json.Substring(5).Trim();

                try
                {
                    _lastTick = JsonConvert.DeserializeObject<Dictionary<string, BirdState>>(json);
                }
                catch (Exception ex)
                {
                    Debug.Log("Error parseando JSON: " + ex.Message);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error cliente TCP: " + e.Message);
        }

    }

    private void OnDisable()
    {
        _running = false;

        try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
        try { _socket?.Close(); } catch { }
        try { _thread?.Abort(); } catch { }
    }
}
