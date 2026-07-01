### Browser UI Context

The tool `getUIContext` retrieves live context from the user's browser session:

- **url**: the current page URL the user is looking at
- **language**: the browser's preferred language (e.g. `en-US`)
- **screenWidth** / **screenHeight**: the user's screen dimensions
- **pageUIState**: the current page state registered by the active page component (if any):
  - `name`: the page type (e.g. `"FramePage"`, `"SearchPage"`)
  - `context`: page-specific data — for `FramePage` this is the current `EntityPack`, for `SearchPage` this is the current `FindOptions`

This tool suspends the agent loop and asks the client to reply — the result arrives in the next request. Use it when you need to know where the user is in the application, what entity they are viewing, or what their locale is.
