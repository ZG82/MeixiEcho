---
name: inputsystem-action-add
description: Add an Action (with type and optional expectedControlType) to an ActionMap in a `.inputactions` asset, optionally with an initial binding path, and save it.
---

# InputSystem / Add Action

Add an `InputAction` to an existing `InputActionMap`. An Action represents a single input intent (e.g. 'Jump', 'Move', 'Fire').

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` — required. Name of the ActionMap to add the Action to.
- `actionName` — required. Name of the new Action (unique within the map).
- `actionType` — input behavior type: `Button` (default), `Value`, or `PassThrough`.
- `expectedControlType` — optional control layout the action expects (e.g. `Button`, `Vector2`, `Axis`).
- `binding` — optional initial binding control path (e.g. `<Gamepad>/buttonSouth`).
- `groups` / `interactions` / `processors` — optional binding metadata applied to the initial binding.

## Behavior

Loads the asset, adds the Action to the named map (failing if the action name already exists), sets its `expectedControlType` when provided, saves and re-imports the asset. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-action-add --input '{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value",
  "actionType": "string_value",
  "expectedControlType": "string_value",
  "binding": "string_value",
  "groups": "string_value",
  "interactions": "string_value",
  "processors": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-action-add --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-action-add --input-file - <<'EOF'
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
| `mapName` | `string` | Yes | Name of the ActionMap to add the Action to. |
| `actionName` | `string` | Yes | Name of the new Action (unique within the map). |
| `actionType` | `string` | No | Input behavior type: Button (default), Value, or PassThrough. |
| `expectedControlType` | `string` | No | Optional expected control type / layout (e.g. 'Button', 'Vector2', 'Axis'). |
| `binding` | `string` | No | Optional initial binding control path (e.g. '<Gamepad>/buttonSouth'). |
| `groups` | `string` | No | Optional binding group(s) for the initial binding (semicolon-separated). |
| `interactions` | `string` | No | Optional interactions for the initial binding (e.g. 'press,hold'). |
| `processors` | `string` | No | Optional processors for the initial binding (e.g. 'invert'). |

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
    "actionType": {
      "type": "string",
      "enum": [
        "Value",
        "Button",
        "PassThrough"
      ]
    },
    "expectedControlType": {
      "type": "string"
    },
    "binding": {
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ActionAddResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ActionAddResponse": {
      "type": "object",
      "properties": {
        "assetPath": {
          "type": "string",
          "description": "Path of the modified asset."
        },
        "mapName": {
          "type": "string",
          "description": "Name of the ActionMap the Action was added to."
        },
        "actionName": {
          "type": "string",
          "description": "Name of the added Action."
        },
        "actionType": {
          "type": "string",
          "description": "Resolved input behavior type."
        },
        "expectedControlType": {
          "type": "string",
          "description": "Resolved expected control type, or null."
        },
        "bindingCount": {
          "type": "integer",
          "description": "Number of bindings on the new Action."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
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

