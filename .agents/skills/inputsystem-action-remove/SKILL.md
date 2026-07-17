---
name: inputsystem-action-remove
description: Remove an Action (and its Bindings) from an ActionMap in a `.inputactions` asset and save it.
---

# InputSystem / Remove Action

Remove an `InputAction` from an `InputActionMap`. This deletes the Action and every Binding associated with it.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` — required. Name of the ActionMap containing the Action.
- `actionName` — required. Name of the Action to remove.

## Behavior

Loads the asset, removes the named Action from the map (failing if it does not exist), saves and re-imports the asset. Destructive. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-action-remove --input '{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-action-remove --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-action-remove --input-file - <<'EOF'
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
| `mapName` | `string` | Yes | Name of the ActionMap containing the Action. |
| `actionName` | `string` | Yes | Name of the Action to remove. |

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
    },
    "actionName": {
      "type": "string"
    }
  },
  "required": [
    "assetPath",
    "mapName",
    "actionName"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ActionRemoveResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ActionRemoveResponse": {
      "type": "object",
      "properties": {
        "assetPath": {
          "type": "string",
          "description": "Path of the modified asset."
        },
        "mapName": {
          "type": "string",
          "description": "Name of the ActionMap the Action was removed from."
        },
        "actionName": {
          "type": "string",
          "description": "Name of the removed Action."
        },
        "actionCount": {
          "type": "integer",
          "description": "Number of Actions remaining in the map."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
        "actionCount",
        "success"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

