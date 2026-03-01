import * as React from "react";
import { ChatMessageEntity, ToolCallEmbedded } from "../Signum.Agent";
import { ChatbotClient,UITool } from "../ChatbotClient";
import ChatMarkdown from "../Templates/ChatMarkdown";

interface ConfirmPayload {
  title: string;
  message: string;
  buttons: string[];
}

function ConfirmWidget(p: {
  payload: ConfirmPayload;
  onConfirm: (label: string) => void;
  response?: ChatMessageEntity;  // undefined = live (waiting), defined = replayed (frozen)
}): React.ReactElement {

  // If already answered (replayed history), parse the persisted result from the Tool message content
  const answeredLabel: string | null = p.response?.content ? JSON.parse(p.response.content) : null;

  function handleClick(label: string) {
    debugger;
    p.onConfirm(label);
  }

  return (
    <div className="chat-ui-confirm mb-2">
      <div className="chat-bubble bot">
        <strong>{p.payload.title}</strong>
        <p className="mb-2"><ChatMarkdown content={p.payload.message}/></p>
        <div className="d-flex gap-2 flex-wrap">
          {p.payload.buttons.map(label => (
            <button
              key={label}
              className={`btn btn-sm ${answeredLabel === label ? "btn-primary" : "btn-outline-primary"}`}
              onClick={() => handleClick(label)}
              disabled={answeredLabel !== null}
            >
              {label}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

export class ConfirmUITool extends UITool {
  uiToolName = "Confirm";

  override renderWidget(call: ToolCallEmbedded, sendToolResponse: (call: ToolCallEmbedded, response: unknown) => void): React.ReactElement {
    const payload: ConfirmPayload = JSON.parse(call.arguments);
    return (
      <ConfirmWidget
        payload={payload}
        onConfirm={label => sendToolResponse(call, label)}
        response={call._response}
      />
    );
  }
}
