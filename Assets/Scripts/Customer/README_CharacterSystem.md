# Customer Character System Setup Guide

## Overview
The refactored customer character system uses a data-driven approach with ScriptableObjects for easy character management.

## Migration from Legacy System

### What Changed
- **Before**: Manual sprite arrays (`characterSpot1Sprites`, `characterSpot2Sprites`, etc.) in Customer.cs
- **After**: Centralized `CharacterDatabase` ScriptableObject with `CharacterData` entries

### Benefits
- ✅ Easier to add new characters without code changes
- ✅ Designer-friendly workflow
- ✅ Better memory efficiency
- ✅ Type-safe character management
- ✅ Extensible character properties

## Setup Instructions

### 1. Create Character Database
1. In Unity Editor: `Tools > Customer System > Create Character Database`
2. This creates `Assets/Data/CharacterDatabase.asset`
3. Select the asset in Project window to configure

### 2. Configure Characters
In the CharacterDatabase Inspector:

**For each Character Data slot:**
- Set `Character Name` (e.g., "Young Customer", "Elder Customer")
- Assign 6 sprite variations to `Sprites` array
- Adjust `Scale` (default: 0.3)
- Set `Tint Color` if needed
- Configure `Patience Multiplier` and `Order Complexity`

### 3. Assign to CustomerSpawner
1. Select CustomerSpawner in scene
2. Drag CharacterDatabase asset to `Character Database` field
3. Configure `Apply Global Scale` and `Global Fixed Scale` if needed

### 4. Assign to Customer Prefab (if using prefabs)
1. Select Customer prefab
2. Drag CharacterDatabase asset to `Character Database` field

## Usage

### Automatic Character Assignment
```csharp
// CustomerSpawner automatically calls this:
customer.SetRandomCharacter();
```

### Manual Character Assignment
```csharp
// Set specific character by name
customer.SetCharacterByName("Young Customer");

// Override scale
customer.SetCharacterScale(0.4f);
```

### Character Information
```csharp
// Get current character info
CharacterData currentChar = customer.GetCurrentCharacter();
string characterName = customer.GetCurrentCharacterName();
```

## Validation

### Check System Health
1. In Unity Editor: `Tools > Customer System > Validate Character System`
2. Check console for validation results

### Migration from Legacy
1. In Unity Editor: `Tools > Customer System > Migrate Legacy Characters`
2. Manually assign CharacterDatabase to any objects missing it

## Character Properties

### CharacterData Fields
- **Character Name**: Display name
- **Character Description**: Optional description
- **Sprites[6]**: Array of sprite variations
- **Scale**: Default character scale
- **Tint Color**: Color overlay
- **Patience Multiplier**: Affects wait time
- **Order Complexity**: Affects order generation
- **Voice Sounds**: Character-specific audio clips

### Runtime Character Selection
Characters are randomly selected from valid entries in the database. A character is considered valid if:
- CharacterData is not null
- Has at least one non-null sprite
- Passes `IsValid()` check

## Troubleshooting

### Common Issues
1. **No characters spawning**: Check CharacterDatabase is assigned to CustomerSpawner
2. **Missing sprites**: Ensure each character has at least one sprite assigned
3. **Wrong scale**: Check character's scale property and global scale settings
4. **Console errors**: Run validation tool to identify issues

### Debug Information
Enable `enableDebugLogs` in CharacterDatabase for detailed logging:
- Character selection process
- Sprite assignment
- Scale application
- Validation results

## API Reference

### CharacterDatabase Methods
- `GetRandomCharacter()`: Returns random valid character
- `GetCharacterByName(string)`: Find character by name
- `GetValidCharacterCount()`: Count of valid characters
- `ValidateDatabase()`: Check database integrity

### Customer Methods
- `SetRandomCharacter()`: Assign random character from database
- `SetCharacterByName(string)`: Assign specific character
- `SetCharacterScale(float)`: Override character scale
- `GetCurrentCharacter()`: Get current CharacterData
- `GetCurrentCharacterName()`: Get current character name