using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Board Visuals")]
    public float stoneSize = 0.4f; // 돌의 크기
    
    public static readonly int BOARD_SIZE = 15; // 15x15 바둑판
    private Stone[,] stones;  // 돌 배열
    public GameObject stonePrefab; // 돌 프리팩
    
    private int currentTurn = 1; // 1: Black, 2: White
    
    private void Start()
    {
        InitializeBoard();
        
        if (!GetComponent<BoardVisualizer>())
        {
            gameObject.AddComponent<BoardVisualizer>();
        }
    }

    private void InitializeBoard()
    {
        stones = new Stone[BOARD_SIZE, BOARD_SIZE];
        currentTurn = 1; // 게임 시작시 항상 흑돌(1) 차례
    }

    public bool PlaceStone(int x, int y, StoneColor color)
    {
        if (!IsValidPosition(x, y) || stones[x, y] != null)
            return false;

        GameObject stoneObj = Instantiate(stonePrefab, GetWorldPosition(x, y), Quaternion.identity);
        stoneObj.transform.localScale = Vector3.one * stoneSize; // 돌 크기 조정
        
        Stone stone = stoneObj.GetComponent<Stone>();
        stone.Initialize(color);
        stones[x, y] = stone;

        // 돌을 놓은 후 턴 변경
        currentTurn = currentTurn == 1 ? 2 : 1;

        return true;
    }
    
    // 현재 턴을 반환하는 메서드 추가
    public int GetCurrentTurn()
    {
        return currentTurn;
    }
    
    // 보드를 초기화하는 메서드 추가
    public void Clear()
    {
        // 모든 돌 제거
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                if (stones[x, y] != null)
                {
                    Destroy(stones[x, y].gameObject);
                    stones[x, y] = null;
                }
            }
        }
        
        // 턴 초기화
        currentTurn = 1;
    }

    public bool CheckWin(int x, int y)
    {
        StoneColor color = stones[x, y].Color;
        
        // 8방향 승리 체크
        int[][] directions = new int[][]
        {
            new int[] {1, 0},   // 가로
            new int[] {0, 1},   // 세로
            new int[] {1, 1},   // 대각선 ↘
            new int[] {1, -1}   // 대각선 ↗
        };

        foreach (int[] dir in directions)
        {
            int count = 1;
            count += CountDirection(x, y, dir[0], dir[1], color);
            count += CountDirection(x, y, -dir[0], -dir[1], color);
            
            if (count >= 5) return true;
        }
        
        return false;
    }

    private int CountDirection(int x, int y, int dx, int dy, StoneColor color)
    {
        int count = 0;
        x += dx;
        y += dy;
        
        while (IsValidPosition(x, y) && stones[x,y] != null && stones[x,y].Color == color)
        {
            count++;
            x += dx;
            y += dy;
        }
        
        return count;
    }
    
    public bool HasStone(int x, int y)
    {
        if (!IsValidPosition(x, y)) return true;
        return stones[x, y] != null;
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        Vector3 position = new Vector3(x - BOARD_SIZE/2, 0.1f, y - BOARD_SIZE/2);
        return position;
    }
    
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE;
    }
    
    public void UpdateFromState(Google.Protobuf.Collections.RepeatedField<int> boardState)
    {
        // 기존 돌들 제거
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                if (stones[x, y] != null)
                {
                    Destroy(stones[x, y].gameObject);
                    stones[x, y] = null;
                }
            }
        }

        // 새로운 상태로 업데이트
        int blackCount = 0;
        int whiteCount = 0;
        
        // 새로운 상태로 업데이트
        for (int i = 0; i < boardState.Count; i++)
        {
            int x = i % BOARD_SIZE;
            int y = i / BOARD_SIZE;
            
            if (boardState[i] != 0) // 0은 빈 칸
            {
                StoneColor color = boardState[i] == 1 ? StoneColor.Black : StoneColor.White;
                PlaceStone(x, y, color);
                
                // 각 색상의 돌 개수 세기
                if (boardState[i] == 1) blackCount++;
                else whiteCount++;
            }
        }
        
        // 현재 턴 결정 (흑돌이 더 적으면 흑의 차례)
        currentTurn = blackCount <= whiteCount ? 1 : 2;
    }
}