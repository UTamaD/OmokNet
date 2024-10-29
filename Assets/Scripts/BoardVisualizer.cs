using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BoardVisualizer : MonoBehaviour
{
    
    [Header("Coordinate Labels")]
    public float labelOffset = 0.5f; // 라벨과 보드 사이의 간격
    public GameObject labelPrefab; // TextMesh Pro 텍스트 프리팹
    public float labelScale = 0.5f; // 라벨 크기
    
    public float gridSize = 1f; // 격자 한 칸의 크기
    public float lineWidth = 0.02f; // 선 굵기
    public Material lineMaterial; // 선 재질
    public Color lineColor = new Color(0.2f, 0.2f, 0.2f); // 선 색상
    
    private void Start()
    {
        DrawBoard();
        DrawSpecialPoints();
        CreateCoordinateLabels();
    }
    private void CreateCoordinateLabels()
    {
        // 가로축 라벨 (알파벳)
        for (int i = 0; i < Board.BOARD_SIZE; i++)
        {
            CreateLabel(
                ((char)('A' + i)).ToString(),
                new Vector3(i - Board.BOARD_SIZE/2, 0, -Board.BOARD_SIZE/2 - labelOffset),
                Quaternion.Euler(90, 0, 0)
            );
        }

        // 세로축 라벨 (숫자)
        for (int i = 0; i < Board.BOARD_SIZE; i++)
        {
            CreateLabel(
                (i + 1).ToString(),
                new Vector3(-Board.BOARD_SIZE/2 - labelOffset, 0, i - Board.BOARD_SIZE/2),
                Quaternion.Euler(90, 0, 0)
            );
        }
    }
    
    private void CreateLabel(string text, Vector3 position, Quaternion rotation)
    {
        GameObject label = Instantiate(labelPrefab, position, rotation, transform);
        label.transform.localScale = Vector3.one * labelScale;
        TMPro.TextMeshPro tmp = label.GetComponent<TMPro.TextMeshPro>();
        tmp.text = text;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
    }
    
    private void DrawBoard()
    {
        // 가로선과 세로선을 그리기 위해 각각의 LineRenderer를 생성
        for (int i = 0; i < Board.BOARD_SIZE; i++)
        {
            // 가로선
            CreateLine($"HorizontalLine_{i}", new Vector3(-Board.BOARD_SIZE/2, 0, i - Board.BOARD_SIZE/2), 
                new Vector3(Board.BOARD_SIZE/2, 0, i - Board.BOARD_SIZE/2));
            
            // 세로선
            CreateLine($"VerticalLine_{i}", new Vector3(i - Board.BOARD_SIZE/2, 0, -Board.BOARD_SIZE/2),
                new Vector3(i - Board.BOARD_SIZE/2, 0, Board.BOARD_SIZE/2));
        }
    }

    private void DrawSpecialPoints()
    {
        // 화점(정중앙)
        CreateSpecialPoint(0, 0, "CenterPoint");

        // 둘째 화점들 (바둑판의 귀 근처)
        int offset = 3; // 가장자리에서 3칸 떨어진 지점
        int edge = Board.BOARD_SIZE / 2 - offset;

        CreateSpecialPoint(-edge, -edge, "Point1");
        CreateSpecialPoint(-edge, edge, "Point2");
        CreateSpecialPoint(edge, -edge, "Point3");
        CreateSpecialPoint(edge, edge, "Point4");
    }

    private void CreateLine(string name, Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.parent = transform;

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startColor = lineColor;
        line.endColor = lineColor;

        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private void CreateSpecialPoint(float x, float y, string name)
    {
        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        point.name = name;
        point.transform.parent = transform;
        point.transform.localPosition = new Vector3(x, 0, y);
        point.transform.localScale = new Vector3(lineWidth * 3, lineWidth * 3, lineWidth * 3);

        // 머티리얼 설정
        Renderer renderer = point.GetComponent<Renderer>();
        renderer.material = lineMaterial;
        renderer.material.color = lineColor;
    }
}