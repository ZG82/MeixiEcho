---
name: inputsystem-binding-composite-add
description: Add a composite Binding (e.g. `2DVector` / WASD, `1DAxis`) with its named parts to an Action in a `.inputactions` asset, then save it.
---

# InputSystem / Add Composite Binding

Add a composite `InputBinding` to an `InputAction`. A composite synthesizes a value (e.g. a `Vector2`) from several part bindings. The classic example is a `2DVector` (WASD) composite with `up`/`down`/`left`/`right` parts; a `1DAxis` composite has `negative`/`positive` parts.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `mapName` / `actionName` — required. The Action that receives the composite.
- `composite` — composite type, e.g. `2DVector`, `1DAxis`, `Axis`, `Dpad` (default `2DVector`).
- `parts` — required. One or more `{ name, path }` entries (e.g. `up` → `<Keyboard>/w`).
- `interactions` / `processors` — optional, applied to the composite root.

## Behavior

Loads the asset, resolves the Action, adds the composite via `AddCompositeBinding` and chains each part via `.With(name, path)`, saves and re-imports the asset. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-binding-composite-add --input '{
  "assetPath": "string_value",
  "mapName": "string_value",
  "actionName": "string_value",
  "parts": "string_value",
  "composite": "string_value",
  "interactions": "string_value",
  "processors": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-composite-add --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-binding-composite-add --input-file - <<'EOF'
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
| `actionName` | `string` | Yes | Name of the Action to add the composite to. |
| `parts` | `any` | Yes | Composite parts: each entry has a 'name' (e.g. 'up') and a control 'path' (e.g. '<Keyboard>/w'). |
| `composite` | `string` | No | Composite type, e.g. '2DVector' (default), '1DAxis', 'Axis', 'Dpad'. |
| `interactions` | `string` | No | Optional interactions applied to the composite root. |
| `processors` | `string` | No | Optional processors applied to the composite root. |

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
    "parts": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-CompositePart-1"
    },
    "composite": {
      "type": "string"
    },
    "interactions": {
      "type": "string"
    },
    "processors": {
      "type": "string"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-CompositePart": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "description": "Part name within the composite (e.g. 'up', 'down', 'left', 'right', 'negative', 'positive')."
        },
        "path": {
          "type": "string",
          "description": "Control path bound to this part (e.g. '<Keyboard>/w')."
        },
        "groups": {
          "type": "string",
          "description": "Optional control-scheme group(s) for this part."
        }
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-CompositePart-1": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-CompositePart"
      }
    }
  },
  "required": [
    "assetPath",
    "mapName",
    "actionName",
    "parts"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-BindingCompositeAddResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-BindingCompositeAddResponse": {
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
        "composite": {
          "type": "string",
          "description": "The composite type that was added."
        },
        "partCount": {
          "type": "integer",
          "description": "Number of composite parts added."
        },
        "bindingCount": {
          "type": "integer",
          "description": "Number of bindings on the Action after the addition (composite root + parts)."
        },
        "success": {
          "type": "boolean",
          "description": "Whether the operation succeeded."
        }
      },
      "required": [
        "partCount",
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

