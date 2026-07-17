---
name: cinemachine-set-default-blend
description: Set the `DefaultBlend` of a `CinemachineBrain` — the blend style (Cut, EaseInOut, Linear, etc.) and duration used when transitioning between CinemachineCameras. Targets the referenced Camera's brain, or `Camera.main`'s brain.
---

# Cinemachine / Set Default Blend

Set the default blend on a `CinemachineBrain`. The default blend is used whenever the brain transitions from one live CinemachineCamera to another and no custom blend overrides it.

## Inputs

- `cameraRef` — optional GameObject hosting the Camera + `CinemachineBrain`. When omitted, `Camera.main` is used.
- `style` — `CinemachineBlendDefinition.Styles` enum (`Cut`, `EaseInOut`, `EaseIn`, `EaseOut`, `HardIn`, `HardOut`, `Linear`, `Custom`).
- `time` — blend duration in seconds.

## Behavior

Resolves the brain (throws if the Camera has none), assigns a new `CinemachineBlendDefinition(style, time)` to `DefaultBlend`, marks the scene dirty, and repaints. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-set-default-blend --input '{
  "style": "string_value",
  "time": 0,
  "cameraRef": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-set-default-blend --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-set-default-blend --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `style` | `string` | Yes | Blend style used when transitioning between CinemachineCameras. |
| `time` | `number` | No | Blend duration in seconds. |
| `cameraRef` | `any` | No | Optional reference to the GameObject hosting the Camera + CinemachineBrain. If omitted, Camera.main is used. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "style": {
      "type": "string",
      "enum": [
        "Cut",
        "EaseInOut",
        "EaseIn",
        "EaseOut",
        "HardIn",
        "HardOut",
        "Linear",
        "Custom"
      ]
    },
    "time": {
      "type": "number"
    },
    "cameraRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
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
    }
  },
  "required": [
    "style"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-SetDefaultBlendResponse"
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
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-SetDefaultBlendResponse": {
      "type": "object",
      "properties": {
        "cameraRef": {
          "$ref": "#/$defs/AIGD.GameObjectRef",
          "description": "Reference to the Camera GameObject hosting the CinemachineBrain."
        },
        "brainRef": {
          "$ref": "#/$defs/AIGD.ComponentRef",
          "description": "Reference to the CinemachineBrain component."
        },
        "style": {
          "type": "string",
          "description": "Resulting blend style."
        },
        "time": {
          "type": "number",
          "description": "Resulting blend time (seconds)."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
        "time",
        "success"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

