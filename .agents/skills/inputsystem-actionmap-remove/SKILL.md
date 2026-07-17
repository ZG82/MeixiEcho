---
name: inputsystem-actionmap-remove
description: Remove an ActionMap (and all its Actions and Bindings) from a `.inputactions` asset and save it.
---

# InputSystem / Remove ActionMap

Remove an `InputActionMap` from an InputActionAsset. This deletes the map along with every Action and Binding it contains.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` — required. Name of the ActionMap to remove.

## Behavior

Loads the asset, removes the named ActionMap (failing if it does not exist), saves and re-imports the asset. Destructive. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-actionmap-remove --input '{
  "assetPath": "string_value",
  "mapName": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-actionmap-remove --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-actionmap-remove --input-file - <<'EOF'
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
| `mapName` | `string` | Yes | Name of the ActionMap to remove. |

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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ActionMapRemoveResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ActionMapRemoveResponse": {
      "type": "object",
      "properties": {
        "assetPath": {
          "type": "string",
          "description": "Path of the modified asset."
        },
        "mapName": {
          "type": "string",
          "description": "Name of the removed ActionMap."
        },
        "actionMapCount": {
          "type": "integer",
          "description": "Total number of ActionMaps remaining in the asset."
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

