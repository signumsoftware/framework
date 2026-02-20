### Browser UI Context

The tool `getUIContext` retrieves live context from the user's browser session:

- **url**: the current page URL the user is looking at
- **language**: the browser's preferred language (e.g. `en-US`)
- **screenWidth** / **screenHeight**: the user's screen dimensions

This tool suspends the agent loop and asks the client to reply — the result arrives in the next request. Use it when you need to know where the user is in the application or what their locale is.
