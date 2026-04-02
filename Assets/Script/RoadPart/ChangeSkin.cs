using UnityEngine;

public class ChangeSkin : MonoBehaviour
{
    private SpriteRenderer spriteToChange;
    [SerializeField] private Sprite[] sprites;
    private int randomNumber = 1;

    /// <summary>
    /// When >= 0, overrides the random skin selection with a fixed index.
    /// Set to -1 to restore random behavior.
    /// Sprite indices match the 'sprites' array order in the Inspector:
    ///   0 = ITA, 1 = BSFI, 2 = DCR, 3 = MCS (GoodNormalVoitureObstacle)
    ///   0 = ITA, 1 = BSFI, 2 = DCR, 3 = MCS (GoodInversedVoitureEnnemy)
    /// </summary>
    [HideInInspector] public int forcedSkinIndex = -1;

    private int myNumber;

    private void OnEnable()
    {
        ChangerSkin();
    }

    public void ChangerSkin()
    {
        SpriteRenderer spriteToChange = GetComponent<SpriteRenderer>();

        if (forcedSkinIndex >= 0 && forcedSkinIndex < sprites.Length)
        {
            randomNumber = forcedSkinIndex;
        }
        else
        {
            randomNumber = Random.Range(0, sprites.Length);
        }

        spriteToChange.sprite = sprites[randomNumber];
        myNumber = randomNumber;
    }

    /// <summary>Forces this car to always display the sprite at the given index.
    /// Call with -1 to restore random selection.</summary>
    public void ForceSkin(int index)
    {
        forcedSkinIndex = index;
        ChangerSkin();
    }

    public int OnAspiration()
    {
        return myNumber;
    }
}
