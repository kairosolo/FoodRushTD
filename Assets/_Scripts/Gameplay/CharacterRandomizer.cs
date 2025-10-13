using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CustomCosmetic
{
    public string cosmeticName;
    public SpriteRenderer renderer;
    public List<Sprite> options;
}

[System.Serializable]
public class SkinVariant
{
    public string skinName;
    public Sprite sprite;
}

[System.Serializable]
public class BodyPart
{
    public string partName;
    public SpriteRenderer renderer;
    public List<SkinVariant> skinVariants;
}

public class CharacterRandomizer : MonoBehaviour
{
    [SerializeField] private bool disableRandomization;
    [SerializeField] private List<CustomCosmetic> cosmeticParts;
    [SerializeField] private List<BodyPart> bodyParts;

    public void RandomizeAll()
    {
        if (!disableRandomization)
        {
            if (cosmeticParts.Count == 0) return;

            foreach (var part in cosmeticParts)
            {
                if (part == null || part.renderer == null || part.options == null || part.options.Count == 0) continue;

                int randomIndex = Random.Range(0, part.options.Count);
                part.renderer.sprite = part.options[randomIndex];
            }



            if (bodyParts.Count == 0) return;

            int skinIndex = Random.Range(0, bodyParts[0].skinVariants.Count);

            foreach (var part in bodyParts)
            {
                if (skinIndex < part.skinVariants.Count)
                    part.renderer.sprite = part.skinVariants[skinIndex].sprite;
            }
        }
    }
}