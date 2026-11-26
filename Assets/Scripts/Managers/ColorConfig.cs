using UnityEngine;
using System.Collections.Generic;

namespace RescuePrincess.Managers
{
    [System.Serializable]
    public class ColorData
    {
        [Header("Color Information")]
        [Tooltip("Unique identifier for this color")]
        public string ID;

        [Tooltip("Type of color (can be used for game logic)")]
        public ColorType colorType;

        [Header("Visual Properties")]
        [Tooltip("Material to apply for this color")]
        public Material material;

        [Tooltip("Color tint to apply")]
        public Color colorTint = Color.white;

        [Header("Dragon Body Sprites")]
        [Tooltip("Sprite for dragon body segment with this color")]
        public Sprite dragonBodySprite;

        [Tooltip("Alternative sprite variations for this color (optional)")]
        public Sprite[] dragonBodySpriteVariations;

        [Header("Game Properties")]
        [Tooltip("Score value for this color")]
        public int scoreValue = 10;

        [Tooltip("Is this color special/rare?")]
        public bool isSpecial = false;

        [Header("Arrow Sprite")]
        [Tooltip("Arrow sprite for this color")]
        public Sprite arrowSprite;

        [Header("Explosion Effect")]
        [Tooltip("Prefab hiệu ứng nổ cho dragon part với màu này")]
        public GameObject DragonPartExplosion;
    }

    public enum ColorType
    {
        Red,
        Brown,
        Green,
        Blue,
        Yellow,
        Purple,
        Pink,
        Cyan
    }

    [CreateAssetMenu(fileName = "ColorConfig", menuName = "RescuePrincess/Color Config")]
    public class ColorConfig : ScriptableObject
    {
        [Header("Color Configuration")]
        [Tooltip("List of all available colors in the game")]
        public List<ColorData> colorList = new List<ColorData>();

        [Header("Default Settings")]
        [Tooltip("Default color to use when no specific color is assigned")]
        public ColorData defaultColor;

        /// <summary>
        /// Get color data by ID
        /// </summary>
        public ColorData GetColorById(string id)
        {
            foreach (var colorData in colorList)
            {
                if (colorData.ID == id)
                    return colorData;
            }

            Debug.LogWarning($"Color with ID '{id}' not found. Returning default color.");
            return defaultColor;
        }

        /// <summary>
        /// Get color data by ColorType
        /// </summary>
        public ColorData GetColorByType(ColorType colorType)
        {
            foreach (var colorData in colorList)
            {
                if (colorData.colorType == colorType)
                    return colorData;
            }

            Debug.LogWarning($"Color with type '{colorType}' not found. Returning default color.");
            return defaultColor;
        }

        /// <summary>
        /// Get random color from the list
        /// </summary>
        public ColorData GetRandomColor()
        {
            if (colorList.Count == 0)
            {
                Debug.LogWarning("No colors available in ColorConfig. Returning default color.");
                return defaultColor;
            }

            int randomIndex = Random.Range(0, colorList.Count);
            return colorList[randomIndex];
        }

        /// <summary>
        /// Get random special color from the list
        /// </summary>
        public ColorData GetRandomSpecialColor()
        {
            List<ColorData> specialColors = new List<ColorData>();

            foreach (var colorData in colorList)
            {
                if (colorData.isSpecial)
                    specialColors.Add(colorData);
            }

            if (specialColors.Count == 0)
            {
                Debug.LogWarning("No special colors available. Returning random color.");
                return GetRandomColor();
            }

            int randomIndex = Random.Range(0, specialColors.Count);
            return specialColors[randomIndex];
        }

        /// <summary>
        /// Apply color data to a SpriteRenderer
        /// </summary>
        public void ApplyColorToRenderer(SpriteRenderer renderer, ColorData colorData)
        {
            if (renderer == null || colorData == null)
                return;

            if (colorData.material != null)
            {
                renderer.material = colorData.material;
            }

            renderer.color = colorData.colorTint;
        }

        /// <summary>
        /// Apply color by ID to a SpriteRenderer
        /// </summary>
        public void ApplyColorById(SpriteRenderer renderer, string colorId)
        {
            ColorData colorData = GetColorById(colorId);
            ApplyColorToRenderer(renderer, colorData);
        }

        /// <summary>
        /// Apply color by type to a SpriteRenderer
        /// </summary>
        public void ApplyColorByType(SpriteRenderer renderer, ColorType colorType)
        {
            ColorData colorData = GetColorByType(colorType);
            ApplyColorToRenderer(renderer, colorData);
        }

        /// <summary>
        /// Get color data by color index (1-10)
        /// </summary>
        public ColorData GetColorByIndex(int colorIndex)
        {
            // Map color index to ColorType
            ColorType colorType = GetColorTypeFromIndex(colorIndex);
            return GetColorByType(colorType);
        }

        /// <summary>
        /// Get dragon body sprite by color index
        /// </summary>
        public Sprite GetDragonBodySprite(int colorIndex)
        {
            ColorData colorData = GetColorByIndex(colorIndex);
            return colorData?.dragonBodySprite;
        }

        /// <summary>
        /// Get random dragon body sprite variation by color index
        /// </summary>
        public Sprite GetRandomDragonBodySpriteVariation(int colorIndex)
        {
            ColorData colorData = GetColorByIndex(colorIndex);

            if (colorData?.dragonBodySpriteVariations != null && colorData.dragonBodySpriteVariations.Length > 0)
            {
                int randomIndex = Random.Range(0, colorData.dragonBodySpriteVariations.Length);
                return colorData.dragonBodySpriteVariations[randomIndex];
            }

            return colorData?.dragonBodySprite;
        }

        /// <summary>
        /// Apply dragon body sprite and color to a SpriteRenderer
        /// </summary>
        public void ApplyDragonBodyToRenderer(SpriteRenderer renderer, int colorIndex)
        {
            //Debug.Log("Apply dragon body sprite to renderer: " + renderer.name);
            if (renderer == null)
            {
                Debug.Log("SpriteRenderer is null. Cannot apply dragon body sprite.");
                return;
            }


            ColorData colorData = GetColorByIndex(colorIndex);
            if (colorData == null)
            {
                Debug.Log("ColorData is null. Cannot apply dragon body sprite.");
                return;
            }
                
            //Debug.Log("Apply dragon body sprite to renderer: " + colorData.dragonBodySprite.name);
            // Apply sprite
            if (colorData.dragonBodySprite != null)
            {
                renderer.sprite = colorData.dragonBodySprite;
            }

            // Apply material and color
            //ApplyColorToRenderer(renderer, colorData);
        }

        /// <summary>
        /// Apply random dragon body sprite variation and color to a SpriteRenderer
        /// </summary>
        public void ApplyRandomDragonBodyToRenderer(SpriteRenderer renderer, int colorIndex)
        {
            if (renderer == null)
                return;

            ColorData colorData = GetColorByIndex(colorIndex);
            if (colorData == null)
                return;

            // Apply random sprite variation
            Sprite randomSprite = GetRandomDragonBodySpriteVariation(colorIndex);
            if (randomSprite != null)
            {
                renderer.sprite = randomSprite;
            }

            // Apply material and color
            ApplyColorToRenderer(renderer, colorData);
        }

        /// <summary>
        /// Map color index (1-10) to ColorType
        /// </summary>
        private ColorType GetColorTypeFromIndex(int colorIndex)
        {
            switch (colorIndex)
            {
                case 1: return ColorType.Red;
                case 2: return ColorType.Brown;
                case 3: return ColorType.Green;
                case 4: return ColorType.Blue;
                case 5: return ColorType.Yellow;
                case 6: return ColorType.Purple;
                case 7: return ColorType.Pink;
                case 8: return ColorType.Cyan;
                default: return ColorType.Red;
            }
        }

        /// <summary>
        /// Get all available dragon body sprites for a color index
        /// </summary>
        public Sprite[] GetAllDragonBodySprites(int colorIndex)
        {
            ColorData colorData = GetColorByIndex(colorIndex);
            if (colorData == null)
                return new Sprite[0];

            List<Sprite> allSprites = new List<Sprite>();

            if (colorData.dragonBodySprite != null)
            {
                allSprites.Add(colorData.dragonBodySprite);
            }

            if (colorData.dragonBodySpriteVariations != null)
            {
                allSprites.AddRange(colorData.dragonBodySpriteVariations);
            }

            return allSprites.ToArray();
        }

        /// <summary>
        /// Check if a color index has dragon body sprites
        /// </summary>
        public bool HasDragonBodySprites(int colorIndex)
        {
            ColorData colorData = GetColorByIndex(colorIndex);
            return colorData?.dragonBodySprite != null ||
                   (colorData?.dragonBodySpriteVariations != null && colorData.dragonBodySpriteVariations.Length > 0);
        }
    }
}