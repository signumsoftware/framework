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

Configure the MCP client to auto-connect to the running Chrome instance (no manual port needed):

```json
"mcpServers": {
  "chrome-devtools": {
    "command": "npx",
    "args": ["chrome-devtools-mcp@latest", "--autoConnect"]
  }
}
```

Once connected, use the available tools to inspect the stuck page:

- `list_pages` — find the test tab
- `select_page` — switch to it
- `take_screenshot` — see what the UI looks like
- `take_snapshot` — get the full a11y tree
- `evaluate_script` — run JavaScript (e.g. read `data-refresh-count`, intercept `fetch`)
- `list_network_requests` / `list_console_messages` — check for errors
