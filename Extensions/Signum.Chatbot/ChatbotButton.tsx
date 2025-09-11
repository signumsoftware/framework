import React from "react";
import "./ChatButton.css";

// Font Awesome imports
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faComments } from "@fortawesome/free-solid-svg-icons";
import { ErrorBoundary } from "@framework/Components";
const ChatbotModal = React.lazy(() => import("./ChatbotModal"));

export default function ChatbotButton(): React.ReactElement {
  const [showModal, setShowModal] = React.useState(false);

  return (
    <>
      {!showModal && <button
        className="btn btn-primary chat-button shadow-lg rounded-circle"
        onClick={() => setShowModal(true)}
        aria-label="Chat"      >
        <FontAwesomeIcon icon={faComments} size="lg" />
      </button >}
      {showModal && (
          <React.Suspense fallback={null}>
            <ChatbotModal onClose={() => setShowModal(false)} />
          </React.Suspense>
      )}
    </>
  );
}
