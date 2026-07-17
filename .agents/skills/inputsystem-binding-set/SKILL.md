---
name: inputsystem-binding-set
description: Update a Binding's path, groups, interactions and/or processors by index on an Action in a `.inputactions` asset, then save it. Only the supplied fields are changed.
---

# InputSystem / Set Binding

Update fields of an existing `InputBinding` (identified by its index on an `InputAction`). Only the parameters you provide are applied; the rest are left unchanged. Use 'inputsystem-get' to find the binding index first.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` / `actionName` — required. The Action that owns the binding.
- `bindingIndex` — required. Zero-based index of the binding.
- `path` — optional new control path.
- `groups` — optional new control-scheme group(s).
- `interactions` — optional new interactions.
- `processors` — optional new processors.

## Behavior

Loads the asset, resolves the Action and binding index (failing if out of range), applies each supplied field via the `ChangeBinding` syntax (`WithPath` / `WithGroups` / `WithInteractions` / `WithProcessors`), saves and re-imports the asset. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-binding-set --input '{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value",
  "bindingIndex": 0,
  "path": "string_value",
  "groups": "string_value",
  "interactions": "string_value",
  "processors": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-set --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-set --input-file - <<'EOF'
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
| `bindingIndex` | `integer` | Yes | Zero-based index of the binding to update. |
| `path` | `string` | No | Optional new control path. |
| `groups` | `string` | No | Optional new control-scheme group(s). |
| `interactions` | `string` | No | Optional new interactions. |
| `processors` | `string` | No | Optional new processors. |

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
    },
    "path": {
      "type": "string"
    },
    "groups": {
      "type": "string"
    },
    "interactions": {
      "type": "string"
    },
    "processors": {
      "type": "string"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-BindingSetResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-BindingSetResponse": {
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
        "bindingIndex": {
          "type": "integer",
          "description": "Index of the updated binding."
        },
        "path": {
          "type": "string",
          "description": "Resulting control path of the binding."
        },
        "groups": {
          "type": "string",
          "description": "Resulting control-scheme group(s)."
        },
        "interactions": {
          "type": "string",
          "description": "Resulting interactions."
        },
        "processors": {
          "type": "string",
          "description": "Resulting processors."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
        "bindingIndex",
        "success"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

