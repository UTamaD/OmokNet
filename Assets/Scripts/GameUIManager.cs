using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameUIManager : MonoBehaviour
{
    [Header("References")]
    public OnlineGameManager gameManager;
    
    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public Button playButton;
    public TextMeshProUGUI connectionStatusText;
    
    [Header("Game UI")]
    public GameObject gamePanel;
    public TextMeshProUGUI gameStatusText;
    public TextMeshProUGUI playerInfoText;
    
    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button playAgainButton;
    public Button backToMenuButton;

    private void Start()
    {
        // 초기 UI 설정
        mainMenuPanel.SetActive(true);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        
        // 버튼 이벤트 연결
        playButton.onClick.AddListener(HandlePlayButton);
        playAgainButton.onClick.AddListener(HandlePlayAgain);
        backToMenuButton.onClick.AddListener(ShowMainMenu);
        
        // 게임 매니저 이벤트 연결
        gameManager.OnConnectionStateChanged += HandleConnectionStateChanged;
        gameManager.OnGameStarted += HandleGameStarted;
        gameManager.OnGameOver += HandleGameOver;
    }

    private void HandleConnectionStateChanged(bool isConnected)
    {
        connectionStatusText.text = isConnected ? "Connected" : "Disconnected";
        playButton.interactable = isConnected;
        
        if (!isConnected)
        {
            ShowMainMenu();
        }
    }

    private void HandlePlayButton()
    {
        playButton.interactable = false;
        gameStatusText.text = "Waiting for opponent...";
        gamePanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        gameManager.RequestGame();
    }

    private void HandleGameStarted(string blackPlayer, string whitePlayer)
    {
        playButton.interactable = true;
        gameStatusText.text = "Game in progress";
        
        // 플레이어 정보 표시
        string playerColor = blackPlayer == gameManager.PlayerId ? "Black" : "White";
        playerInfoText.text = $"You are playing as: {playerColor}";
        
        gamePanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    private void HandleGameOver(bool isWinner, int winner)
    {
        gameOverPanel.SetActive(true);
        gamePanel.SetActive(false);
        
        if (winner == 0)
        {
            gameOverText.text = "Draw!";
        }
        else
        {
            string winnerText = winner == 1 ? "Black" : "White";
            gameOverText.text = isWinner ? $"You Win!\nPlaying as {winnerText}" : $"You Lose!\n{winnerText} won the game";
        }
    }

    private void HandlePlayAgain()
    {
        gameOverPanel.SetActive(false);
        HandlePlayButton();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        gamePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        playButton.interactable = true;
    }

    private void OnDestroy()
    {
        // 이벤트 연결 해제
        if (gameManager != null)
        {
            gameManager.OnConnectionStateChanged -= HandleConnectionStateChanged;
            gameManager.OnGameStarted -= HandleGameStarted;
            gameManager.OnGameOver -= HandleGameOver;
        }
    }
}