import * as React from 'react'
import createToolbarPlugin, { ToolbarProps, ToolbarChildrenProps, Separator } from 'draft-js-static-toolbar-plugin';

import {
  ItalicButton,
  BoldButton,
  UnderlineButton,
  CodeButton,
  HeadlineOneButton,
  HeadlineTwoButton,
  HeadlineThreeButton,
  UnorderedListButton,
  OrderedListButton,
  BlockquoteButton,
  CodeBlockButton,
  DraftJsStyleButtonProps,
} from 'draft-js-buttons';
import HtmlEditor from './HtmlEditor';
//import 'draft-js-static-toolbar-plugin/lib/plugin.css';
import './ToolbarStyles.css';

//configuring like in https://www.draft-js-plugins.com/plugin/static-toolbar

export function setupStaticToolbar() {
  const staticToolbarPlugin = createToolbarPlugin({
    theme: {
      buttonStyles: { active: "draft-active", button: "draft-button", buttonWrapper: "draft-buttonWrapper" },
      toolbarStyles: { toolbar: "draft-toolbar" }
    }
  });
  const Toolbar = staticToolbarPlugin.Toolbar;

  HtmlEditor.defaultPlugins = [staticToolbarPlugin];
  HtmlEditor.defaultBeforeEditor = ref => (
    <Toolbar>
      {(externalProps: ToolbarChildrenProps) => (
        <div>
          <BoldButton {...externalProps as any} />
          <ItalicButton {...externalProps as any} />
          <UnderlineButton {...externalProps as any} />
          <CodeButton {...externalProps as any} />
          <HeadlinesButton {...externalProps as any} />
          <UnorderedListButton {...externalProps as any} />
          <OrderedListButton {...externalProps as any} />
          <BlockquoteButton {...externalProps as any} />
          <CodeBlockButton {...externalProps as any} />
        </div>
      )}
    </Toolbar>
  );
}

class HeadlinesPicker extends React.Component<DraftJsStyleButtonProps> {
  componentDidMount() {
    setTimeout(() => { window.addEventListener('click', this.onWindowClick); });
  }

  componentWillUnmount() {
    window.removeEventListener('click', this.onWindowClick);
  }

  onWindowClick = () =>
    // Call `onOverrideContent` again with `undefined`
    // so the toolbar can show its regular content again.
    (this.props as ToolbarChildrenProps).onOverrideContent(undefined!);

  render() {
    const buttons = [HeadlineOneButton, HeadlineTwoButton, HeadlineThreeButton];
    return (
      <div>
        {buttons.map((Button, i) => // eslint-disable-next-line
          <Button key={i} {...this.props} />
        )}
      </div>
    );
  }
}

class HeadlinesButton extends React.Component<any> {
  onClick = () =>
    // A button can call `onOverrideContent` to replace the content
    // of the toolbar. This can be useful for displaying sub
    // menus or requesting additional information from the user.
    this.props.onOverrideContent(HeadlinesPicker);

  render() {
    return (
      <div className="draft-buttonWrapper">
        <button onClick={this.onClick} className="draft-button">
          H
        </button>
      </div>
    );
  }
}
