using UnityEngine;

public enum StoneColor
{
    Black,
    White
}

public class Stone : MonoBehaviour
{
    public Material blackMaterial;
    public Material whiteMaterial;
    
    private StoneColor color;
    public StoneColor Color => color;

    public void Initialize(StoneColor newColor)
    {
        color = newColor;
        GetComponent<MeshRenderer>().material = 
            color == StoneColor.Black ? blackMaterial : whiteMaterial;
    }
    
    public void SetPreviewMode(bool isPreview)
    {
        if (isPreview)
        {
            // 프리뷰 모드일 때는 반투명하게
            Color c = GetComponent<MeshRenderer>().material.color;
            c.a = 0.5f;
            GetComponent<MeshRenderer>().material.color = c;
        }
        else
        {
            // 실제 돌을 놓을 때는 불투명하게
            Color c = GetComponent<MeshRenderer>().material.color;
            c.a = 1f;
            GetComponent<MeshRenderer>().material.color = c;
        }
    }
}