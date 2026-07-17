---
name: cinemachine-set-body
description: Add or replace the position-control (Body) component of a `CinemachineCamera`, chosen by `BodyType` (Follow, OrbitalFollow, ThirdPersonFollow, PositionComposer, HardLockToTarget, or None). Applies common params (followOffset, damping, cameraDistance) where applicable.
---

# Cinemachine / Set Body

Add or replace the Body (position-control) component of a `CinemachineCamera`. Any existing Body component is removed first, then the chosen one is added — so this is destructive to the previous Body component.

## Inputs

- `gameObjectRef` — the GameObject hosting the `CinemachineCamera` (required).
- `bodyType` — `BodyType` enum: `Follow`, `OrbitalFollow`, `ThirdPersonFollow`, `PositionComposer`, `HardLockToTarget`, or `None` (removes the Body component).
- `followOffset` — optional `Vector3` offset from the Follow target (applies to `Follow`).
- `damping` — optional uniform damping value (applied where the component supports damping).
- `cameraDistance` — optional distance from the target (applies to `ThirdPersonFollow` / `PositionComposer` / `OrbitalFollow` radius).

## Behavior

Removes every existing `CinemachineFollow`, `CinemachineOrbitalFollow`, `CinemachineThirdPersonFollow`, `CinemachinePositionComposer`, and `CinemachineHardLockToTarget` on the GameObject, then adds the requested one and applies the optional params. Marks the scene dirty and repaints. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-set-body --input '{
  "gameObjectRef": "string_value",
  "bodyType": "string_value",
  "followOffset": "string_value",
  "damping": "string_value",
  "cameraDistance": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-set-body --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-set-body --input-file - <<'EOF'
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
| `bodyType` | `string` | Yes | Which position-control (Body) component to use. |
| `followOffset` | `any` | No | Offset from the Follow target (applies to Follow). |
| `damping` | `any` | No | Uniform damping value applied where the component supports damping. |
| `cameraDistance` | `any` | No | Distance from the target (ThirdPersonFollow/PositionComposer/OrbitalFollow radius). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
    },
    "bodyType": {
      "type": "string",
      "enum": [
        "None",
        "Follow",
        "OrbitalFollow",
        "ThirdPersonFollow",
        "PositionComposer",
        "HardLockToTarget"
      ]
    },
    "followOffset": {
      "$ref": "#/$defs/UnityEngine.Vector3"
    },
    "damping": {
      "$ref": "#/$defs/System.Single"
    },
    "cameraDistance": {
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
    "UnityEngine.Vector3": {
      "type": "object",
      "properties": {
        "x": {
          "type": "number"
        },
        "y": {
          "type": "number"
        },
        "z": {
          "type": "number"
        }
      },
      "required": [
        "x",
        "y",
        "z"
      ],
      "additionalProperties": false
    },
    "System.Single": {
      "type": "number"
    }
  },
  "required": [
    "gameObjectRef",
    "bodyType"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-SetBodyResponse"
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
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-SetBodyResponse": {
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
        "bodyType": {
          "type": "string",
          "description": "The BodyType that was applied."
        },
        "componentName": {
          "type": "string",
          "description": "The Cinemachine component type name that was added (or 'None')."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
        "success"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

