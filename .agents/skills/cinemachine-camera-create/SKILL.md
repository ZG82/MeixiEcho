---
name: cinemachine-camera-create
description: Create a new GameObject with a `CinemachineCamera` (Cinemachine 3.x virtual camera) in the active scene. Optionally set name, position, rotation, priority, and Follow / LookAt targets. Returns the new GameObject reference and instanceId.
---

# Cinemachine / Create Camera

Create a new GameObject hosting a `CinemachineCamera` component in the active scene. The `CinemachineCamera` is the Cinemachine 3.x virtual camera that a `CinemachineBrain` blends between to drive the real Unity Camera.

## Inputs

- `name` — optional GameObject name (default `CinemachineCamera`).
- `position` — optional world `Vector3` position (default zero).
- `rotation` — optional euler-degrees `Vector3` rotation (default zero).
- `priority` — optional priority value; higher wins when multiple cameras compete (default 0).
- `followRef` — optional GameObject the camera should Follow (position target).
- `lookAtRef` — optional GameObject the camera should LookAt (aim target).

## Behavior

Creates the GameObject, adds a `CinemachineCamera`, sets transform / `Priority.Value` / `Follow` / `LookAt`, marks the scene dirty, repaints the Editor, and returns the new GameObject reference and instanceId. The whole call runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-camera-create --input '{
  "name": "string_value",
  "position": "string_value",
  "rotation": "string_value",
  "priority": 0,
  "followRef": "string_value",
  "lookAtRef": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-camera-create --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-camera-create --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `name` | `string` | No | Name of the new CinemachineCamera GameObject. |
| `position` | `any` | No | World-space position of the camera. |
| `rotation` | `any` | No | World-space rotation of the camera in euler angles (degrees). |
| `priority` | `integer` | No | Priority value. Higher priority wins when several CinemachineCameras are active. |
| `followRef` | `any` | No | Optional GameObject the camera should Follow (drives position). |
| `lookAtRef` | `any` | No | Optional GameObject the camera should LookAt (drives aim). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "name": {
      "type": "string"
    },
    "position": {
      "$ref": "#/$defs/UnityEngine.Vector3"
    },
    "rotation": {
      "$ref": "#/$defs/UnityEngine.Vector3"
    },
    "priority": {
      "type": "integer"
    },
    "followRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
    },
    "lookAtRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
    }
  },
  "$defs": {
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
  }
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-CameraCreateResponse"
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
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-CameraCreateResponse": {
      "type": "object",
      "properties": {
        "gameObjectRef": {
          "$ref": "#/$defs/AIGD.GameObjectRef",
          "description": "Reference to the created CinemachineCamera GameObject."
        },
        "cameraRef": {
          "$ref": "#/$defs/AIGD.ComponentRef",
          "description": "Reference to the created CinemachineCamera component."
        },
        "instanceId": {
          "$ref": "#/$defs/UnityEngine.EntityId",
          "description": "Instance id of the created GameObject."
        },
        "gameObjectName": {
          "type": "string",
          "description": "Name of the created GameObject."
        },
        "priority": {
          "type": "integer",
          "description": "Resolved priority value."
        },
        "follow": {
          "type": "string",
          "description": "Name of the Follow target, or null."
        },
        "lookAt": {
          "type": "string",
          "description": "Name of the LookAt target, or null."
        }
      },
      "required": [
        "instanceId",
        "priority"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

