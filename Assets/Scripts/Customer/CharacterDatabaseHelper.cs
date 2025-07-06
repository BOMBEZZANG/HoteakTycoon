// Assets/Scripts/Customer/CharacterDatabaseHelper.cs
// Helper script to create and manage character databases

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public static class CharacterDatabaseHelper
{
    [MenuItem("Tools/Customer System/Create Character Database")]
    public static void CreateCharacterDatabase()
    {
        CharacterDatabase database = ScriptableObject.CreateInstance<CharacterDatabase>();
        
        // Initialize with default structure
        database.characters = new CharacterData[3];
        
        for (int i = 0; i < database.characters.Length; i++)
        {
            database.characters[i] = new CharacterData
            {
                characterName = $"Customer Type {i + 1}",
                characterDescription = $"Character variation {i + 1}",
                sprites = new Sprite[6],
                scale = 0.3f,
                tintColor = Color.white,
                patienceMultiplier = 1.0f,
                orderComplexity = 1.0f
            };
        }
        
        string path = "Assets/Data/CharacterDatabase.asset";
        
        // Create directory if it doesn't exist
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        AssetDatabase.CreateAsset(database, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select the created asset
        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
        
        Debug.Log($"Character Database created at: {path}");
    }
    
    [MenuItem("Tools/Customer System/Migrate Legacy Characters")]
    public static void MigrateLegacyCharacters()
    {
        // Find all Customer prefabs/objects in the scene
        Customer[] customers = Object.FindObjectsOfType<Customer>();
        
        if (customers.Length == 0)
        {
            Debug.LogWarning("No Customer objects found in the scene to migrate.");
            return;
        }
        
        Debug.Log($"Found {customers.Length} Customer objects. Checking for legacy character data...");
        
        foreach (Customer customer in customers)
        {
            // Check if customer has legacy sprite arrays (this would need to be done manually
            // since we've already removed the fields, but we can check for missing database)
            if (customer.characterDatabase == null)
            {
                Debug.LogWarning($"Customer '{customer.name}' has no CharacterDatabase assigned. Please assign one manually.");
            }
        }
        
        // Find CustomerSpawner and check for character database
        CustomerSpawner spawner = Object.FindObjectOfType<CustomerSpawner>();
        if (spawner != null)
        {
            if (spawner.characterDatabase == null)
            {
                Debug.LogWarning("CustomerSpawner has no CharacterDatabase assigned. Please assign one manually.");
            }
            else
            {
                Debug.Log($"CustomerSpawner is using CharacterDatabase: {spawner.characterDatabase.name}");
            }
        }
    }
    
    [MenuItem("Tools/Customer System/Validate Character System")]
    public static void ValidateCharacterSystem()
    {
        Debug.Log("=== Character System Validation ===");
        
        // Find all CharacterDatabase assets
        string[] guids = AssetDatabase.FindAssets("t:CharacterDatabase");
        
        if (guids.Length == 0)
        {
            Debug.LogWarning("No CharacterDatabase assets found. Create one using 'Tools/Customer System/Create Character Database'");
            return;
        }
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterDatabase database = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(path);
            
            if (database != null)
            {
                Debug.Log($"Validating database: {database.name}");
                database.ValidateDatabase();
            }
        }
        
        // Check CustomerSpawner setup
        CustomerSpawner spawner = Object.FindObjectOfType<CustomerSpawner>();
        if (spawner != null)
        {
            if (spawner.characterDatabase == null)
            {
                Debug.LogError("CustomerSpawner.characterDatabase is not assigned!");
            }
            else
            {
                Debug.Log($"CustomerSpawner is properly configured with database: {spawner.characterDatabase.name}");
            }
        }
        else
        {
            Debug.LogWarning("No CustomerSpawner found in the scene.");
        }
        
        Debug.Log("=== Validation Complete ===");
    }
}
#endif

[System.Serializable]
public class CharacterMigrationData
{
    public string characterName;
    public Sprite[] legacySprites;
    public float scale;
    
    public CharacterData ToCharacterData()
    {
        return new CharacterData
        {
            characterName = characterName,
            sprites = legacySprites ?? new Sprite[6],
            scale = scale,
            tintColor = Color.white,
            patienceMultiplier = 1.0f,
            orderComplexity = 1.0f
        };
    }
}