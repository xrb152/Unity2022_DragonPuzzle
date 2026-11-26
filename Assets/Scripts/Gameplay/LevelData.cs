using UnityEngine;
using RescuePrincess.Managers;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Information")]
    public string levelName;
    public int levelNumber;
    
    [Header("Dragon Body Configuration")]
    [Tooltip("Color indices for each dragon body segment (comma-separated values)")]
    public int[] colorIndices;
    
    [Header("Level Settings")]
    public int targetScore;
    public float timeLimit = 60f;
    
    /// <summary>
    /// Get the color index for a specific body segment
    /// </summary>
    /// <param name="segmentIndex">Index of the body segment</param>
    /// <returns>Color index for the segment, or -1 if invalid index</returns>
    public int GetColorIndex(int segmentIndex)
    {
        if (colorIndices == null || segmentIndex < 0 || segmentIndex >= colorIndices.Length)
            return -1;
            
        return colorIndices[segmentIndex];
    }
    
    /// <summary>
    /// Get the total number of body segments
    /// </summary>
    public int GetSegmentCount()
    {
        return colorIndices != null ? colorIndices.Length : 0;
    }
    
    /// <summary>
    /// Convert color indices array to comma-separated string
    /// </summary>
    public string GetColorIndicesString()
    {
        if (colorIndices == null || colorIndices.Length == 0)
            return string.Empty;
            
        return string.Join(",", colorIndices);
    }
    
    /// <summary>
    /// Set color indices from comma-separated string
    /// </summary>
    public void SetColorIndicesFromString(string colorIndicesString)
    {
        if (string.IsNullOrEmpty(colorIndicesString))
        {
            colorIndices = new int[0];
            return;
        }
        
        string[] parts = colorIndicesString.Split(',');
        colorIndices = new int[parts.Length];
        
        for (int i = 0; i < parts.Length; i++)
        {
            if (int.TryParse(parts[i].Trim(), out int colorIndex))
            {
                colorIndices[i] = colorIndex;
            }
            else
            {
                Debug.LogWarning($"Failed to parse color index at position {i}: {parts[i]}");
                colorIndices[i] = 0; // Default to 0 if parsing fails
            }
        }
    }
} 