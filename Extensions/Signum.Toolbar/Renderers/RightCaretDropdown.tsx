import React from "react";
import Dropdown from "react-bootstrap/Dropdown";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { JSX } from "react/jsx-runtime";
import "./RightCaretDropdown.css";
export interface RightCaretDropdownOption<T> {
  value: T;
  label: string;
  icon?: React.ReactNode;
}

interface RightCaretDropdownProps<T> {
  options: RightCaretDropdownOption<T>[];
  value: T | null;
  onChange: (value: T) => void;
  disabled?: boolean;
  placeholder?: string;
  renderOption?: (option: RightCaretDropdownOption<T>) => React.ReactNode;
}

export function RightCaretDropdown<T>({
  options,
  value,
  onChange,
  disabled,
  placeholder,
  renderOption,
}: RightCaretDropdownProps<T>): JSX.Element {
  const selected = options.find((o) => o.value === value);

  return (
    <Dropdown style={{ width: '100%' }} drop="end">
      <Dropdown.Toggle
        variant="tertiary"
        disabled={disabled}
        className="d-flex align-items-center"
        style={{ width: '100%' }}
      >
        {selected?.icon}
        <span className="flex-grow-1 mx-2">
          {selected ? selected.label : placeholder || "Select..."}
        </span>
        {/*<FontAwesomeIcon icon="grip-vertical" />*/}
      </Dropdown.Toggle>
      <Dropdown.Menu
       className="menu-right-of-caret"
        align="end"
      >
        {options.map((opt, idx) => (
          <Dropdown.Item
            key={idx}
            active={opt.value === value}
            onClick={() => onChange(opt.value)}
          >
            {renderOption ? renderOption(opt) : (<>{opt.icon} {opt.label}</>)}
          </Dropdown.Item>
        ))}
      </Dropdown.Menu>
    </Dropdown>
  );
}
