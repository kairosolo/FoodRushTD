using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ExportSubSprites : Editor
{
    [MenuItem("Assets/Export Sub-Sprites")]
    private static void ExportSelectedTextures()
    {
        Object[] selectedObjects = Selection.objects;
        int texturesProcessed = 0;

        foreach (Object obj in selectedObjects)
        {
            if (obj is Texture2D)
            {
                ProcessTexture(obj as Texture2D);
                texturesProcessed++;
            }
        }

        if (texturesProcessed > 0)
        {
            Debug.Log($"<b><color=green>Finished exporting sub-sprites from {texturesProcessed} texture(s).</color></b>");
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogWarning("No textures were selected for export.");
        }
    }

    [MenuItem("Assets/Export Sub-Sprites", true)]
    private static bool ExportValidation()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is Texture2D)
            {
                return true;
            }
        }
        return false;
    }

    private static void ProcessTexture(Texture2D texture)
    {
        string texturePath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"Could not get TextureImporter for {texture.name}. Skipping.");
            return;
        }

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        try
        {
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            List<Sprite> subSprites = new List<Sprite>();

            foreach (var asset in allAssets)
            {
                if (asset is Sprite sprite && asset != texture)
                {
                    subSprites.Add(sprite);
                }
            }

            if (subSprites.Count == 0)
            {
                Debug.LogWarning($"No sub-sprites found in texture: {texture.name}. Make sure Sprite Mode is set to 'Multiple'. Skipping...");
                return;
            }

            string exportFolder = Path.Combine(Path.GetDirectoryName(texturePath), Path.GetFileNameWithoutExtension(texturePath) + "_Exported");
            Directory.CreateDirectory(exportFolder);

            int spritesExported = 0;
            foreach (Sprite sprite in subSprites)
            {
                Texture2D newTex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);

                Color[] pixels = sprite.texture.GetPixels((int)sprite.textureRect.x,
                                                           (int)sprite.textureRect.y,
                                                           (int)sprite.textureRect.width,
                                                           (int)sprite.textureRect.height);

                newTex.SetPixels(pixels);
                newTex.Apply();

                byte[] bytes = newTex.EncodeToPNG();
                string fileName = Path.Combine(exportFolder, sprite.name + ".png");
                File.WriteAllBytes(fileName, bytes);
                spritesExported++;
            }

            Debug.Log($"Successfully exported {spritesExported} sub-sprites from '{texture.name}' to folder '{exportFolder}'.");
        }
        finally
        {
            if (!wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }
    }
}