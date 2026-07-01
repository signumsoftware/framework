# React UI Testing — Environment & Debugging

NOTE: Replace "Southwind" with your project name if different.

## Starting the tests

Common issues when running React UI tests with Playwright.

### 1. Playwright is not installed

If you see `Executable doesn't exist at ...ms-playwright\chromium-...\chrome.exe`, install the bundled Chromium, as the user to install it with the full path, like

```powershell
C:\myCode\Southwind\Southwind.Test.React\bin\Debug\net10.0\playwright.ps1 install chromium
```

### 2. The Web App Is Not Running

Open the server URL in a browser:

```
http://localhost/Southwind.Server
```

If you get **503 Service Unavailable**, the IIS application pool is stopped.

> **Note:** `%windir%` is CMD syntax and does not work in PowerShell. Use `$env:windir` instead.

Start the app pool from an **elevated PowerShell**:

```powershell
& "$env:windir\system32\inetsrv\appcmd.exe" start apppool /apppool.name:"Southwind.Server AppPool"
```

To list all app pools and find the exact name:

```powershell
& "$env:windir\system32\inetsrv\appcmd.exe" list apppool
```

### 3. The vite server is not running 

If `http://localhost/Southwind.Server` returns 200 but the content is something like: 

```
URIErrorThe script http://localhost:3118/main.tsx didn't load correctly
```

Is because the vite server is not running (in this case in port 3118). You can start it from the command line:

```powershell
cd Southwind.Server
yarn run dev
```

### 4. Environment is not set up

You need to execute `GenerateEnvironment` test. It will put the database in a base state and create an snapshot (in SQL Server)
or a template (in Postgres) that will be restored before running any UI Test.

## Debugging the test

### PLAYWRIGHT_MODE — controlling how Chrome is launched

The test reads `PLAYWRIGHT_MODE` from the environment variable or from `Southwind.Test.React/PLAYWRIGHT_MODE.txt` (first line wins):

| Value | Behaviour |
|---|---|
| *(absent / anything else)* | Playwright launches a visible Chromium window (default) |
| `headless` | Playwright launches Chromium in headless mode (no window) |
| `debug` | Playwright launches a real Chrome via `--remote-debugging-port=9222` and connects over CDP, keeping the window open after a failure so you can inspect it |

To enable debug mode, edit the file:

```
Southwind.Test.React/PLAYWRIGHT_MODE.txt
```

and set its content to:

```
debug
```

To disable it, comment it out:

```
//debug
```

Or set the environment variable before running:

```powershell
$env:PLAYWRIGHT_MODE = "debug"
dotnet test ...
```

### Connecting Chrome DevTools MCP after a failure

In `debug` mode the Chrome window stays open after a test failure. You can inspect the page directly using the **chrome-devtools-mcp** MCP server.

Since the test launches Chrome with `--remote-debugging-port=9222`, configure the MCP server to connect directly to that port:

```json
"mcpServers": {
  "chrome-devtools": {
    "type": "stdio",
    "command": "npx",
    "args": [
      "-y",
      "chrome-devtools-mcp@latest",
      "--browserUrl=http://127.0.0.1:9222"
    ]
  }
}
```

This ensures the MCP server connects to the same Chrome instance that the test is using. Use the available MCP tools to inspect the page state, take screenshots/snapshots, check console messages, and review network requests.

### Fixing a failing test

When a test fails and you're investigating via MCP:

1. **Check the failing state via MCP** - Use Chrome MCP tools to understand the actual page state when the test failed

2. **Check the implementation (tsx)** - Review the React component to verify the test matches the actual UI behavior

3. **DO NOT remove or comment out failing test code** - If the test needs to be modified or removed, get explicit user confirmation first. Never silently delete assertions or test functionality without understanding and discussing why they fail
