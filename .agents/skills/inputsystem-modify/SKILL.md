---
name: inputsystem-modify
description: "Generic write escape-hatch: apply a `SerializedMember` diff to the `InputActionAsset` object itself via ReflectorNet `TryModify`, for fields not covered by the dedicated tools. Use 'inputsystem-get' first to inspect the structure."
---

# InputSystem / Modify Asset (generic)

Modify an `InputActionAsset` by applying a `SerializedMember` diff via ReflectorNet. This is the generic escape hatch for asset-level fields/properties that the dedicated tools do not expose.

## Inputs

- `assetPath` — required. Path to the existing `.inputactions` asset.
- `data` — the `SerializedMember` diff to apply. Route **fields** through the `fields` channel and **properties** through the `props` channel — ReflectorNet resolves them separately with no cross-fallback.

## Behavior

Loads the asset, applies the diff via `Reflector.TryModify`, and on success re-serializes and saves the asset back to disk. The applied logs are returned. Runs on the Unity main thread.

## How to Call

```bash
unity-mcp-cli run-tool inputsystem-modify --input '{
  "assetPath": "string_value",
  "data": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool inputsystem-modify --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool inputsystem-modify --input-file - <<'EOF'
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
| `data` | `any` | Yes | The SerializedMember diff to apply. Fields go through the 'fields' channel; properties through 'props'. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "assetPath": {
      "type": "string"
    },
    "data": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
    }
  },
  "$defs": {
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMember": {
      "type": "object",
      "properties": {
        "typeName": {
          "type": "string",
          "description": "Full type name. Eg: 'System.String', 'System.Int32', 'UnityEngine.Vector3', etc."
        },
        "name": {
          "type": "string",
          "description": "Object name."
        },
        "value": {
          "description": "Value of the object, serialized as a non stringified JSON element. Can be null if the value is not set. Can be default value if the value is an empty object or array json."
        },
        "fields": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested field value."
          },
          "description": "Fields of the object, serialized as a list of 'SerializedMember'."
        },
        "props": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested property value."
          },
          "description": "Properties of the object, serialized as a list of 'SerializedMember'."
        }
      },
      "required": [
        "typeName"
      ],
      "additionalProperties": false
    }
  },
  "required": [
    "assetPath",
    "data"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ModifyResponse"
    }
  },
  "$defs": {
    "System.String-1": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_InputSystem-ModifyResponse": {
      "type": "object",
      "properties": {
        "success": {
          "type": "boolean",
          "description": "Whether the modification was successful."
        },
        "assetPath": {
          "type": "string",
          "description": "Path of the modified asset."
        },
        "assetType": {
          "type": "string",
          "description": "Full type name of the modified asset."
        },
        "logs": {
          "$ref": "#/$defs/System.String-1",
          "description": "Log of modifications and any warnings/errors."
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

