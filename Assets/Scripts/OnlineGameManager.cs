using UnityEngine;
using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Protobuf;
using System.Collections.Concurrent;

public class OnlineGameManager : MonoBehaviour
{
    [Header("Game References")]
    public Board board;
    public GameManager gameManager;

    private TcpClient client;
    private NetworkStream stream;
    private string playerId;
    private Thread receiveThread;
    private bool isConnected = false;
    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    
    // 게임 상태 관련 변수
    private bool canPlay = false;
    private string currentColor; // "black" or "white"
    
    public string PlayerId => playerId;
    public bool CanPlay => canPlay;
    public StoneColor CurrentPlayerColor => currentColor == "black" ? StoneColor.Black : StoneColor.White;
    
    public event Action<bool> OnConnectionStateChanged;
    public event Action<string, string> OnGameStarted;
    public event Action<int[]> OnGameStateUpdated;
    public event Action<bool, int> OnGameOver;

    private void Start()
    {
        // 유니티 시작시 서버 연결
        ConnectToServer();
    }

    private void Update()
    {
        // 메인 스레드에서 실행해야 하는 작업 처리
        while (mainThreadActions.TryDequeue(out Action action))
        {
            action.Invoke();
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    public void TryPlaceStone(int x, int y)
    {
        if (!isConnected || !canPlay) return;
        
        // 현재 내 차례인지 확인
        int currentTurn = currentColor == "black" ? 1 : 2;
        if (board.GetCurrentTurn() != currentTurn) return;
        
        var placeStoneMsg = new Gomoku.GomokuMessage
        {
            PlaceStone = new Gomoku.PlaceStone
            {
                PlayerId = playerId,
                Position = new Gomoku.Position
                {
                    X = x,
                    Y = y
                }
            }
        };
        SendMessage(placeStoneMsg);
    }
    
    private async void ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync("localhost", 9090);
            stream = client.GetStream();
            isConnected = true;
            
            // 플레이어 ID 생성 및 로그인
            playerId = System.Guid.NewGuid().ToString();
            SendLoginMessage();
            
            // 수신 스레드 시작
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.Start();
            
            OnConnectionStateChanged?.Invoke(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect: {e.Message}");
            OnConnectionStateChanged?.Invoke(false);
        }
    }

    private void SendLoginMessage()
    {
        var loginMsg = new Gomoku.GomokuMessage
        {
            Login = new Gomoku.LoginMessage
            {
                PlayerId = playerId
            }
        };
        Debug.Log(playerId);
        SendMessage(loginMsg);
    }

    public void RequestGame()
    {
        if (!isConnected) return;
        
        var requestMsg = new Gomoku.GomokuMessage
        {
            RequestGame = new Gomoku.RequestGame
            {
                PlayerId = playerId
            }
        };
        SendMessage(requestMsg);
    }



    private void SendMessage(Gomoku.GomokuMessage message)
    {
        try
        {
            byte[] messageBytes = message.ToByteArray();
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            
            // Little Endian으로 길이 전송
            if (BitConverter.IsLittleEndian)
            {
                stream.Write(lengthBytes, 0, 4);
            }
            else
            {
                Array.Reverse(lengthBytes);
                stream.Write(lengthBytes, 0, 4);
            }
            
            stream.Write(messageBytes, 0, messageBytes.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send message: {e.Message}");
            HandleDisconnect();
        }
    }

    private void ReceiveLoop()
    {
        byte[] lengthBuffer = new byte[4];
        
        while (isConnected)
        {
            try
            {
                // 메시지 길이 읽기
                if (!ReadExactly(stream, lengthBuffer, 4))
                {
                    HandleDisconnect();
                    break;
                }
                
                int messageLength = BitConverter.IsLittleEndian ?
                    BitConverter.ToInt32(lengthBuffer, 0) :
                    BitConverter.ToInt32(new[] { lengthBuffer[3], lengthBuffer[2], lengthBuffer[1], lengthBuffer[0] }, 0);
                
                // 메시지 본문 읽기
                byte[] messageBuffer = new byte[messageLength];
                if (!ReadExactly(stream, messageBuffer, messageLength))
                {
                    HandleDisconnect();
                    break;
                }
                
                var message = Gomoku.GomokuMessage.Parser.ParseFrom(messageBuffer);
                ProcessMessage(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in receive loop: {e.Message}");
                HandleDisconnect();
                break;
            }
        }
    }

    private bool ReadExactly(NetworkStream stream, byte[] buffer, int count)
    {
        int totalBytesRead = 0;
        
        while (totalBytesRead < count)
        {
            int bytesRead = stream.Read(buffer, totalBytesRead, count - totalBytesRead);
            if (bytesRead == 0)
            {
                return false;
            }
            totalBytesRead += bytesRead;
        }
        
        return true;
    }

    private void ProcessMessage(Gomoku.GomokuMessage message)
    {
        mainThreadActions.Enqueue(() =>
        {
            switch (message.MessageCase)
            {
                case Gomoku.GomokuMessage.MessageOneofCase.GameState:
                    var gameState = message.GameState;
                    board.UpdateFromState(gameState.Board);
                    
                    // 현재 턴 업데이트
                    canPlay = (currentColor == "black" && gameState.CurrentTurn == 1) ||
                              (currentColor == "white" && gameState.CurrentTurn == 2);
                    
                    // 게임 종료 확인
                    if (gameState.IsGameOver)
                    {
                        bool isWinner = (currentColor == "black" && gameState.Winner == 1) ||
                                        (currentColor == "white" && gameState.Winner == 2);
                        OnGameOver?.Invoke(isWinner, gameState.Winner);
                        StartCoroutine(HandleGameOver());
                    }
                    break;
                
                case Gomoku.GomokuMessage.MessageOneofCase.GameStart:
                    var gameStart = message.GameStart;
                    currentColor = gameStart.BlackPlayer == playerId ? "black" : "white";
                    canPlay = currentColor == "black"; // 흑이 먼저 시작
                    OnGameStarted?.Invoke(gameStart.BlackPlayer, gameStart.WhitePlayer);
                    break;
                
                default:
                    Debug.LogWarning($"Unhandled message type: {message.MessageCase}");
                    break;
            }
        });
    }

    private IEnumerator HandleDisconnect()
    {
        yield return null;
    }
    private IEnumerator HandleGameOver()
    {
        canPlay = false;
        yield return new WaitForSeconds(2.0f);
        board.Clear();
        currentColor = null;
    }
    
    private void Disconnect()
    {
        if (!isConnected) return;
        
        try
        {
            // 로그아웃 메시지 전송
            var logoutMsg = new Gomoku.GomokuMessage
            {
                Logout = new Gomoku.LogoutMessage
                {
                    PlayerId = playerId
                }
            };
            SendMessage(logoutMsg);
        }
        finally
        {
            HandleDisconnect();
        }
    }
}