---
name: cinemachine-set-noise
description: Add or configure a `CinemachineBasicMultiChannelPerlin` (procedural camera shake / handheld noise) on a `CinemachineCamera`. Sets `AmplitudeGain` and `FrequencyGain`. The noise profile asset is optional and left untouched when not supplied.
---

# Cinemachine / Set Noise

Add or configure a `CinemachineBasicMultiChannelPerlin` noise component on a `CinemachineCamera`. This drives procedural Perlin-noise shake (handheld camera feel). The actual noise shape comes from a `NoiseSettings` profile asset; without one, the amplitude/frequency gains have no visible effect, so this tool is null-safe and does not require a profile.

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `amplitudeGain` — optional gain applied to the profile amplitudes (1 = normal).
- `frequencyGain` — optional gain applied to the profile frequencies (1 = normal).

## Behavior

Reuses an existing `CinemachineBasicMultiChannelPerlin` on the GameObject or adds one, then applies the provided gains. The noise profile asset is never modified here (leave it null-safe). Marks the scene dirty and repaints. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-set-noise --input '{
  "gameObjectRef": "string_value",
  "amplitudeGain": "string_value",
  "frequencyGain": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-set-noise --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-set-noise --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Reference to the GameObject containing the CinemachineCamera component. |
| `amplitudeGain` | `any` | No | Gain applied to the noise profile amplitudes (1 = normal). |
| `frequencyGain` | `any` | No | Gain applied to the noise profile frequencies (1 = normal). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
    },
    "amplitudeGain": {
      "$ref": "#/$defs/System.Single"
    },
    "frequencyGain": {
      "$ref": "#/$defs/System.Single"
    }
  },
  "$defs": {
    "UnityEngine.EntityId": {
      "type": "string",
      "pattern": "^[0-9]+$"
    },
    "System.Type": {
      "type": "string"
    },
    "AIGD.GameObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "$ref": "#/$defs/UnityEngine.EntityId",
          "description": "instanceID of the UnityEngine.Object. If it is '0' and 'path', 'name', 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'. Priority: 1 (Recommended)"
        },
        "path": {
          "type": "string",
          "description": "Path of a GameObject in the hierarchy Sample 'character/hand/finger/particle'. Priority: 2."
        },
        "name": {
          "type": "string",
          "description": "Name of a GameObject in hierarchy. Priority: 3."
        },
        "assetType": {
          "$ref": "#/$defs/System.Type",
          "description": "Type of the asset."
        },
        "assetPath": {
          "type": "string",
          "description": "Path to the asset within the project. Starts with 'Assets/'"
        },
        "assetGuid": {
          "type": "string",
          "description": "Unique identifier for the asset."
        }
      },
      "required": [
        "instanceID"
      ],
      "description": "Find GameObject in opened Prefab or in the active Scene."
    },
    "System.Single": {
      "type": "number"
    }
  },
  "required": [
    "gameObjectRef"
  ]
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-SetNoiseResponse"
    }
  },
  "$defs": {
    "AIGD.GameObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "$ref": "#/$defs/UnityEngine.EntityId",
          "description": "instanceID of the UnityEngine.Object. If it is '0' and 'path', 'name', 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'. Priority: 1 (Recommended)"
        },
        "path": {
          "type": "string",
          "description": "Path of a GameObject in the hierarchy Sample 'character/hand/finger/particle'. Priority: 2."
        },
        "name": {
          "type": "string",
          "description": "Name of a GameObject in hierarchy. Priority: 3."
        },
        "assetType": {
          "$ref": "#/$defs/System.Type",
          "description": "Type of the asset."
        },
        "assetPath": {
          "type": "string",
          "description": "Path to the asset within the project. Starts with 'Assets/'"
        },
        "assetGuid": {
          "type": "string",
          "description": "Unique identifier for the asset."
        }
      },
      "required": [
        "instanceID"
      ],
      "description": "Find GameObject in opened Prefab or in the active Scene."
    },
    "UnityEngine.EntityId": {
      "type": "string",
      "pattern": "^[0-9]+$"
    },
    "System.Type": {
      "type": "string"
    },
    "AIGD.ComponentRef": {
      "type": "object",
      "properties": {
        "index": {
          "type": "integer",
          "description": "Component 'index' attached to a gameObject. The first index is '0' and that is usually Transform or RectTransform. Priority: 2. Default value is -1."
        },
        "typeName": {
          "type": "string",
          "description": "Component type full name. Sample 'UnityEngine.Transform'. If the gameObject has two components of the same type, the output component is unpredictable. Priority: 3. Default value is null."
        },
        "instanceID": {
          "$ref": "#/$defs/UnityEngine.EntityId",
          "description": "instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'."
        }
      },
      "required": [
        "index",
        "instanceID"
      ],
      "description": "Component reference. Used to find a Component at GameObject."
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-SetNoiseResponse": {
      "type": "object",
      "properties": {
        "gameObjectRef": {
          "$ref": "#/$defs/AIGD.GameObjectRef",
          "description": "Reference to the CinemachineCamera GameObject."
        },
        "cameraRef": {
          "$ref": "#/$defs/AIGD.ComponentRef",
          "description": "Reference to the CinemachineCamera component."
        },
        "amplitudeGain": {
          "type": "number",
          "description": "Resulting amplitude gain."
        },
        "frequencyGain": {
          "type": "number",
          "description": "Resulting frequency gain."
        },
        "hasNoiseProfile": {
          "type": "boolean",
          "description": "Whether a NoiseSettings profile asset is assigned."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
        "amplitudeGain",
        "frequencyGain",
        "hasNoiseProfile",
        "success"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

