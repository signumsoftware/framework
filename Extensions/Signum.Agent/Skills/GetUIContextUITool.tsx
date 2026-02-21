import { ToolCallEmbedded } from "../Signum.Agent";
import { UITool } from "../ChatbotClient";
import { GlobalModalManager } from "@framework/Modals";

export class GetUIContextUITool extends UITool {
  uiToolName = "GetUIContext";

  override async handleDirectly(call: ToolCallEmbedded, sendToolResponse: (call: ToolCallEmbedded, response: string) => void ): Promise<void> {
    sendToolResponse(call, JSON.stringify({
      url: window.location.href,
      language: navigator.language,
      screenWidth: window.screen.width,
      screenHeight: window.screen.height,
      pageUIState: GlobalModalManager.getPageUIState(),
    }));
  }
}
