---
name: inputsystem-binding-add
description: Add a simple (non-composite) Binding to an Action in a `.inputactions` asset, with an optional control path, groups, interactions and processors, then save it.
---

# InputSystem / Add Binding

Add an `InputBinding` to an existing `InputAction`. A binding maps a physical control path (e.g. `<Keyboard>/space`) to the Action. For composite bindings (2DVector / 1DAxis) use 'inputsystem-binding-composite-add' instead.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` — required. ActionMap containing the Action.
- `actionName` — required. Action to add the binding to.
- `path` — control path (e.g. `<Keyboard>/space`, `<Gamepad>/leftStick`).
- `groups` — optional control-scheme group(s) (semicolon-separated, e.g. `Keyboard&Mouse`).
- `interactions` — optional interactions (e.g. `hold`, `press,tap`).
- `processors` — optional processors (e.g. `invert`, `scale(factor=2)`).

## Behavior

Loads the asset, resolves the Action, appends a binding with the supplied metadata, saves and re-imports the asset, and returns the new binding's index. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-binding-add --input '{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value",
  "path": "string_value",
  "groups": "string_value",
  "interactions": "string_value",
  "processors": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-add --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-add --input-file - <<'EOF'
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
| `actionName` | `string` | Yes | Name of the Action to add the binding to. |
| `path` | `string` | Yes | Control path for the binding (e.g. '<Keyboard>/space'). |
| `groups` | `string` | No | Optional control-scheme group(s), semicolon-separated. |
| `interactions` | `string` | No | Optional interactions (e.g. 'hold', 'press,tap'). |
| `processors` | `string` | No | Optional processors (e.g. 'invert'). |

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
    "path"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-BindingAddResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-BindingAddResponse": {
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
        "path": {
          "type": "string",
          "description": "The control path that was bound."
        },
        "bindingIndex": {
          "type": "integer",
          "description": "Index of the new binding within the Action's binding list."
        },
        "bindingCount": {
          "type": "integer",
          "description": "Number of bindings on the Action after the addition."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
        "bindingIndex",
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

