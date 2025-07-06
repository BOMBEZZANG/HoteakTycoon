// Assets/Scripts/Customer/CharacterDatabase.cs
// Data-driven customer character management system

using UnityEngine;

[System.Serializable]
public class CharacterData
{
    [Header("Character Info")]
    public string characterName = "Customer";
    public string characterDescription = "";
    
    [Header("Visual Settings")]
    public Sprite[] sprites = new Sprite[6];  // 6 variations per character
    public float scale = 0.3f;                // Character scale
    public Color tintColor = Color.white;     // Color tint
    
    [Header("Behavior Settings")]
    public float patienceMultiplier = 1.0f;   // Patience modifier
    public float orderComplexity = 1.0f;      // Order complexity modifier
    
    [Header("Audio")]
    public AudioClip[] voiceSounds;           // Character-specific sounds
    
    public bool IsValid()
    {
        return sprites != null && sprites.Length > 0 && sprites[0] != null;
    }
    
    public Sprite GetRandomSprite()
    {
        if (!IsValid()) return null;
        
        // Filter out null sprites
        var validSprites = System.Array.FindAll(sprites, s => s != null);
        if (validSprites.Length == 0) return null;
        
        return validSprites[Random.Range(0, validSprites.Length)];
    }
    
    public AudioClip GetRandomVoiceSound()
    {
        if (voiceSounds == null || voiceSounds.Length == 0) return null;
        
        var validSounds = System.Array.FindAll(voiceSounds, s => s != null);
        if (validSounds.Length == 0) return null;
        
        return validSounds[Random.Range(0, validSounds.Length)];
    }
}

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Customer/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [Header("Character Collection")]
    public CharacterData[] characters = new CharacterData[3];  // Default 3 character types
    
    [Header("Settings")]
    public bool enableDebugLogs = true;
    
    public CharacterData GetRandomCharacter()
    {
        if (characters == null || characters.Length == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("CharacterDatabase: No characters available!");
            return null;
        }
        
        // Filter valid characters
        var validCharacters = System.Array.FindAll(characters, c => c != null && c.IsValid());
        if (validCharacters.Length == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("CharacterDatabase: No valid characters found!");
            return null;
        }
        
        CharacterData selectedCharacter = validCharacters[Random.Range(0, validCharacters.Length)];
        
        if (enableDebugLogs)
        {
            Debug.Log($"CharacterDatabase: Selected character '{selectedCharacter.characterName}'");
        }
        
        return selectedCharacter;
    }
    
    public CharacterData GetCharacterByIndex(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length)
            return null;
            
        return characters[index];
    }
    
    public CharacterData GetCharacterByName(string name)
    {
        if (characters == null || string.IsNullOrEmpty(name))
            return null;
            
        return System.Array.Find(characters, c => c != null && c.characterName == name);
    }
    
    public int GetValidCharacterCount()
    {
        if (characters == null) return 0;
        
        int count = 0;
        foreach (var character in characters)
        {
            if (character != null && character.IsValid())
                count++;
        }
        return count;
    }
    
    public string[] GetCharacterNames()
    {
        if (characters == null) return new string[0];
        
        var validCharacters = System.Array.FindAll(characters, c => c != null && c.IsValid());
        string[] names = new string[validCharacters.Length];
        
        for (int i = 0; i < validCharacters.Length; i++)
        {
            names[i] = validCharacters[i].characterName;
        }
        
        return names;
    }
    
    [ContextMenu("Validate Database")]
    public void ValidateDatabase()
    {
        Debug.Log("=== Character Database Validation ===");
        Debug.Log($"Total characters: {(characters?.Length ?? 0)}");
        Debug.Log($"Valid characters: {GetValidCharacterCount()}");
        
        if (characters != null)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] == null)
                {
                    Debug.LogWarning($"Character {i}: NULL");
                }
                else if (!characters[i].IsValid())
                {
                    Debug.LogWarning($"Character {i} ({characters[i].characterName}): Invalid - no sprites");
                }
                else
                {
                    var validSprites = System.Array.FindAll(characters[i].sprites, s => s != null);
                    Debug.Log($"Character {i} ({characters[i].characterName}): Valid - {validSprites.Length} sprites");
                }
            }
        }
    }
}