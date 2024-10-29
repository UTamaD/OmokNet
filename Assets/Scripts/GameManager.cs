using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Board board;
    public OnlineGameManager onlineManager;
    
    [Header("Preview")]
    public GameObject stonePrefab;
    private GameObject previewStone;
    private Vector3 lastPreviewPosition = Vector3.zero;

    private void Update()
    {
        if (!onlineManager.CanPlay) 
        {
            DestroyPreviewStone();
            return;
        }

        UpdateStonePreview();

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 hitPoint = hit.point;
            int x = Mathf.RoundToInt(hitPoint.x + Board.BOARD_SIZE/2);
            int y = Mathf.RoundToInt(hitPoint.z + Board.BOARD_SIZE/2);

            if (board.IsValidPosition(x, y) && !board.HasStone(x, y))
            {
                onlineManager.TryPlaceStone(x,y);
                AudioManager.Instance.PlayStoneSound(); // 효과음 재생
            }
        }
    }

    private void UpdateStonePreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 hitPoint = hit.point;
            int x = Mathf.RoundToInt(hitPoint.x + Board.BOARD_SIZE/2);
            int y = Mathf.RoundToInt(hitPoint.z + Board.BOARD_SIZE/2);

            if (board.IsValidPosition(x, y) && !board.HasStone(x, y))
            {
                Vector3 previewPos = board.GetWorldPosition(x, y);
                
                if (previewPos != lastPreviewPosition)
                {
                    lastPreviewPosition = previewPos;
                    UpdatePreviewStone(previewPos);
                }
            }
            else
            {
                DestroyPreviewStone();
            }
        }
        else
        {
            DestroyPreviewStone();
        }
    }

    private void UpdatePreviewStone(Vector3 position)
    {
        if (previewStone == null)
        {
            previewStone = Instantiate(stonePrefab, position, Quaternion.identity);
            Stone stone = previewStone.GetComponent<Stone>();
            stone.Initialize(onlineManager.CurrentPlayerColor);
            stone.SetPreviewMode(true);
        }
        else
        {
            previewStone.transform.position = position;
        }
    }

    private void DestroyPreviewStone()
    {
        if (previewStone != null)
        {
            Destroy(previewStone);
            previewStone = null;
            lastPreviewPosition = Vector3.zero;
        }
    }
}