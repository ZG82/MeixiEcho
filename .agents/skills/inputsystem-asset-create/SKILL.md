---
name: inputsystem-asset-create
description: Create a new `.inputactions` InputActionAsset at an 'Assets/'-rooted path, optionally seeding an initial ActionMap. Returns the created asset path.
---

# InputSystem / Create InputActionAsset

Create a new Unity Input System `InputActionAsset` (a `.inputactions` file). This is the container for ActionMaps, Actions, Bindings and Control Schemes.

## Inputs

- `assetPath` — required. An 'Assets/'-rooted path ending in `.inputactions` (e.g. `Assets/Input/Player.inputactions`). Intermediate folders must already exist (use 'assets-create-folder').
- `initialActionMap` — optional. When provided, an empty ActionMap with this name is added to the new asset.

## Behavior

Creates an empty `InputActionAsset`, optionally adds the initial ActionMap, serializes it to JSON, writes the file, and imports it into the AssetDatabase. Fails if an asset already exists at the path. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-asset-create --input '{
  "assetPath": "string_value",
  "initialActionMap": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-asset-create --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-asset-create --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path ending in '.inputactions' for the new asset. |
| `initialActionMap` | `string` | No | Optional name for an initial ActionMap to add to the new asset. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "assetPath": {
      "type": "string"
    },
    "initialActionMap": {
      "type": "string"
    }
  },
  "required": [
    "assetPath"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-AssetCreateResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-AssetCreateResponse": {
      "type": "object",
      "properties": {
        "assetPath": {
          "type": "string",
          "description": "Path of the created InputActionAsset."
        },
        "initialActionMap": {
          "type": "string",
          "description": "Name of the initial ActionMap that was added, or null."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the asset was created successfully."
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

