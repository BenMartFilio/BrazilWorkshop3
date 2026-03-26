using UnityEngine;

public class ChangeSkin : MonoBehaviour
{
    private SpriteRenderer spriteToChange;
    [SerializeField] private Sprite[] sprites;
    private int randomNumber = 1;

    private int myNumber;
    private void OnEnable()
    {
        ChangerSkin();
    }
    public void ChangerSkin()
    {
        SpriteRenderer spriteToChange = GetComponent<SpriteRenderer>();
        randomNumber = Random.Range(0, sprites.Length);
        spriteToChange.sprite = sprites[randomNumber];
        myNumber = randomNumber;
    }

    public int OnAspiration()
    {
        return myNumber;
    }

}
