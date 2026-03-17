using UnityEngine;

public class SubMenuCategories : MonoBehaviour
{
    private float sizeMultiplier = 1.2f;
    private GameObject actualSelected;
    [SerializeField] private GameObject BaseSelected;

    private void Start()
    {
        ToAim(BaseSelected);
    }

    public void ToAim(GameObject aimed)
    {
        ToSizeUp(aimed);
    }

    public void ToSizeUp(GameObject resized)
    { 
        Vector3 actualSize = resized.transform.position;
        resized.transform.localScale = actualSize*sizeMultiplier;
        if (actualSelected != null)
        {
            ToSizeDown(actualSelected);
        }
        actualSelected = resized;
    }

    public void ToSizeDown(GameObject oldSelected)
    {
        transform.localScale = transform.localScale / sizeMultiplier;
    }
}
