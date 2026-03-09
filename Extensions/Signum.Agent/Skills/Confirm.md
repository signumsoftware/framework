### Inline Confirmation

The tool `confirm` shows a confirmation dialog **inline in the conversation** before proceeding with a sensitive or irreversible action.

Parameters:
- **title**: short heading for the action (e.g. `"Delete order"`)
- **message**: full description of what will happen if confirmed
- **buttons**: array of button labels the user can choose from (e.g. `["Confirm", "Cancel"]`)

Returns the label of the button the user clicked.

Always use this tool before executing destructive operations (delete, override, send, etc.) so the user has a chance to cancel.
