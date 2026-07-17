---
name: cinemachine-brain-ensure
description: Ensure a `CinemachineBrain` exists on a `Camera`. Targets the referenced Camera GameObject, or `Camera.main` when none is given. Returns the brain + camera info. A CinemachineBrain is required for any CinemachineCamera to actually drive the rendering Camera.
---

# Cinemachine / Ensure Brain

Ensure a `CinemachineBrain` component exists on a `Camera`. A CinemachineBrain is the bridge between Cinemachine virtual cameras and a real Unity `Camera`; without it, `CinemachineCamera` components have no effect on the rendered view.

## Inputs

- `cameraRef` — optional GameObject hosting a `Camera`. When omitted, `Camera.main` (a Camera tagged `MainCamera`) is used.

## Behavior

Resolves the Camera, adds a `CinemachineBrain` if one is missing (idempotent — an existing brain is reused), marks the scene dirty, repaints the Editor, and returns the brain instanceId plus the camera name. The whole call runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool cinemachine-brain-ensure --input '{
  "cameraRef": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool cinemachine-brain-ensure --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool cinemachine-brain-ensure --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `cameraRef` | `any` | No | Optional reference to the GameObject hosting the Camera. If omitted, Camera.main is used. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-BrainEnsureResponse"
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
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Cinemachine-BrainEnsureResponse": {
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
        "cameraName": {
          "type": "string",
          "description": "Name of the Camera GameObject."
        },
        "created": {
          "type": "boolean",
          "description": "True if a new CinemachineBrain was created; false if one already existed."
        },
        "defaultBlendStyle": {
          "type": "string",
          "description": "The current default blend style of the brain."
        },
        "defaultBlendTime": {
          "type": "number",
          "description": "The current default blend time (seconds) of the brain."
        }
      },
      "required": [
        "created",
        "defaultBlendTime"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

