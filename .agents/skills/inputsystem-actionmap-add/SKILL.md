---
name: inputsystem-actionmap-add
description: Add a new ActionMap to an existing `.inputactions` InputActionAsset and save it.
---

# InputSystem / Add ActionMap

Add a new `InputActionMap` to an InputActionAsset. An ActionMap groups related Actions (e.g. a 'Player' map and a 'UI' map).

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` — required. Name of the new ActionMap. Must be unique within the asset.

## Behavior

Loads the asset, adds the ActionMap (failing if a map of that name already exists), saves the asset back to disk, and re-imports it. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-actionmap-add --input '{
  "assetPath": "string_value",
  "mapName": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-actionmap-add --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-actionmap-add --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetPath` | `string` | Yes | 'Assets/'-rooted path to the existing '.inputactions' asset. |
| `mapName` | `string` | Yes | Name of the new ActionMap (unique within the asset). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "assetPath": {
      "type": "string"
    },
    "mapName": {
      "type": "string"
    }
  },
  "required": [
    "assetPath",
    "mapName"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ActionMapAddResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ActionMapAddResponse": {
      "type": "object",
      "properties": {
        "assetPath": {
          "type": "string",
          "description": "Path of the modified asset."
        },
        "mapName": {
          "type": "string",
          "description": "Name of the added ActionMap."
        },
        "actionMapCount": {
          "type": "integer",
          "description": "Total number of ActionMaps in the asset after the addition."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
        "actionMapCount",
        "success"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

