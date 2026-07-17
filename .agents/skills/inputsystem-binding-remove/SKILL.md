---
name: inputsystem-binding-remove
description: Remove a Binding by its index from an Action in a `.inputactions` asset, then save it.
---

# InputSystem / Remove Binding

Remove an `InputBinding` from an `InputAction` by its index within that Action's binding list. Use 'inputsystem-get' to discover binding indices first.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` / `actionName` — required. The Action that owns the binding.
- `bindingIndex` — required. Zero-based index of the binding within the Action.

## Behavior

Loads the asset, resolves the Action, erases the binding at the given index (failing if out of range), saves and re-imports the asset. Note: erasing a composite root also affects its parts. Destructive. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-binding-remove --input '{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value",
  "bindingIndex": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-remove --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-remove --input-file - <<'EOF'
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
| `actionName` | `string` | Yes | Name of the Action that owns the binding. |
| `bindingIndex` | `integer` | Yes | Zero-based index of the binding within the Action's binding list. |

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
    },
    "bindingIndex": {
      "type": "integer"
    }
  },
  "required": [
    "assetPath",
    "mapName",
    "actionName",
    "bindingIndex"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-BindingRemoveResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-BindingRemoveResponse": {
      "type": "object",
      "properties": {
        "assetPath": {
          "type": "string",
          "description": "Path of the modified asset."
        },
        "mapName": {
          "type": "string",
          "description": "Name of the ActionMap."
        },
        "actionName": {
          "type": "string",
          "description": "Name of the Action."
        },
        "removedIndex": {
          "type": "integer",
          "description": "Index of the binding that was removed."
        },
        "bindingCount": {
          "type": "integer",
          "description": "Number of bindings remaining on the Action."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
        "removedIndex",
        "bindingCount",
        "success"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

