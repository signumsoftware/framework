import * as React from 'react'
import { useState } from 'react'
import { Dropdown, DropdownButton } from 'react-bootstrap'

export interface ToolbarSwitcherProps {
    variant?: string;
    onToolbarChange?: (toolbarId: string) => void;
}

export default function ToolbarSwitcher(p: ToolbarSwitcherProps): React.JSX.Element {
    const availableToolbars = [
        { id: "PMFlex", name: "PMFlex" },
        { id: "Agile360", name: "Agile360" },
        { id: "Program", name: "Program" },
        { id: "Projekt", name: "Projekt" },
        { id: "Teams", name: "Teams" },
    ];
    // Default to first toolbar
    const [currentToolbar, setCurrentToolbar] = useState<string>(availableToolbars[0].id);

    // Find the currently selected toolbar object
    const selectedToolbar = availableToolbars.find(tb => tb.id === currentToolbar);

    const handleSelect = (eventKey: string) => {
        if (eventKey) {
            setCurrentToolbar(eventKey)
            if (p.onToolbarChange) {
                p.onToolbarChange(eventKey);
            }
        };
    };

    return (
        <DropdownButton
            id="toolbar-switcher"
            title={selectedToolbar?.name ?? "Toolbar Switcher"}
            variant={p.variant || "outline-secondary"}
            onSelect={(eventKey) => handleSelect(eventKey!)}
        >
            {availableToolbars.map(toolbar => (
                <Dropdown.Item
                    key={toolbar.id}
                    eventKey={toolbar.id}
                    active={toolbar.id === currentToolbar}
                >
                    {toolbar.name}
                </Dropdown.Item>
            ))}
        </DropdownButton>
    );
}
