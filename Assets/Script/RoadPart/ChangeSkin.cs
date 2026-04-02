using System;
using Barrage.Formulaires;
using UnityEngine;

public class ChangeSkin : MonoBehaviour
{
    [Serializable]
    public struct SkinMapping
    {
        public Sprite sprite;
        public FormulaireType formulaireType;
    }

    [Tooltip("Each entry maps a sprite to the FormulaireType it represents.")]
    [SerializeField] private SkinMapping[] mappings;

    /// <summary>
    /// When >= 0, overrides the random skin selection with a fixed index into mappings.
    /// Set to -1 to restore random behavior.
    /// </summary>
    [HideInInspector] public int forcedSkinIndex = -1;

    private int _currentIndex;
    private SpriteRenderer _spriteRenderer;

    private void OnEnable()
    {
        ChangerSkin();
    }

    /// <summary>Picks a skin (random or forced) and applies it to the SpriteRenderer.</summary>
    public void ChangerSkin()
    {
        if (mappings == null || mappings.Length == 0) return;

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (forcedSkinIndex >= 0 && forcedSkinIndex < mappings.Length)
            _currentIndex = forcedSkinIndex;
        else
            _currentIndex = UnityEngine.Random.Range(0, mappings.Length);

        if (_spriteRenderer != null)
            _spriteRenderer.sprite = mappings[_currentIndex].sprite;
    }

    /// <summary>Forces this car to display the skin at the given index.
    /// Call with -1 to restore random selection.</summary>
    public void ForceSkin(int index)
    {
        forcedSkinIndex = index;
        ChangerSkin();
    }

    /// <summary>Returns the index of the currently displayed skin mapping.</summary>
    public int OnAspiration()
    {
        return _currentIndex;
    }

    /// <summary>Returns the FormulaireType of the currently displayed skin.</summary>
    public FormulaireType? ObtenirType()
    {
        if (mappings == null || mappings.Length == 0) return null;
        return mappings[_currentIndex].formulaireType;
    }
}
