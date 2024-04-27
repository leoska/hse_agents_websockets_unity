using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

using NativeWebSocket;

public class GameController : MonoBehaviour
{
    public GameObject leftPlayer;
    public GameObject rightPlayer;
    public GameObject ball;

    public int _tickRate = 60;
    public float _speed = 2.0f;
    public float lagTimer = 0f;
    
    private WebSocket _socket;
    private GameObject _current;
    
    private float _timer = 0f;
    private float _timerLimit = 0f;

    public bool _lag = false;
    
    // Start is called before the first frame update
    async void Start()
    {
        _timerLimit = 1f / _tickRate;
        
        _socket = new WebSocket("ws://127.0.0.1:8080/ws");

        _lag = false;

        _socket.OnOpen += () =>
        {
            Debug.Log("Successfully connected!");
        };
        
        _socket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        _socket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };
        
        _socket.OnMessage += (bytes) =>
        {
            StartCoroutine(StartWait(lagTimer, () =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);

                var packet = JsonUtility.FromJson<Packet>(message);

                switch (packet.event_type)
                {
                    case 1:
                        if (packet.mode == "red")
                        {
                            _current = leftPlayer;
                        } 
                        else if (packet.mode == "blue")
                        {
                            _current = rightPlayer;
                        }
                        break;
                
                    case 10:
                        float position_red = packet.position_red;
                        float position_blue = packet.position_blue;

                        if (_current != leftPlayer)
                        {
                            leftPlayer.transform.position = new Vector3(-7f, position_red, 0f);
                        }

                        if (_current != rightPlayer)
                        {
                            rightPlayer.transform.position = new Vector3(7f, position_blue, 0f);
                        }

                        ball.transform.position = new Vector3(packet.ball_x, packet.ball_y, 0f);
                    
                        break;
                
                    default:
                        throw new Exception($"Undefined event type: {message}");
                }
            }));
        };

        await _socket.Connect();
    }

    private IEnumerator StartWait(float time, Action callback)
    {
        yield return new WaitForSeconds(time);
        callback();
    }

    void DispatchMessageQueue()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _socket.DispatchMessageQueue();
#endif
    }

    void Move()
    {
        var up = Input.GetKey(KeyCode.W);
        var down =  Input.GetKey(KeyCode.S);

        var vertical = Convert.ToInt32(up) - Convert.ToInt32(down);

        if (_current == null)
        {
            return;
        }
        
        var newPosition = new Vector3(_current.transform.position.x, _current.transform.position.y + vertical * _speed * Time.deltaTime, 0f);

        _current.transform.position = newPosition;
    }

    async Task SendPosition()
    {
        var packet = new Packet();
        packet.event_type = 10;
        packet.position = _current.transform.position.y;

        await _socket.SendText(JsonUtility.ToJson(packet));
    }

    // Update is called once per frame
    async void Update()
    {
        if (_socket.State != WebSocketState.Open)
            return;
        
        DispatchMessageQueue();

        Move();

        if (Input.GetKeyUp(KeyCode.F))
        {
            _lag = !_lag;
        }

        if (_current == null)
        {
            return;
        }

        _timer += Time.deltaTime;
        if (_timer < _timerLimit || _lag)
        {
            return;
        }

        _timer = 0f;
        await SendPosition();
    }
}
