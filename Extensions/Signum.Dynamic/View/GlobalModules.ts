import * as React from 'react'
import * as luxon from 'luxon'
import * as Entities from '@framework/Signum.Entities'
import { Operations } from '@framework/Operations'
import { Constructor } from '@framework/Constructor'
import * as Globals from '@framework/Globals'
import { Finder } from '@framework/Finder'
import * as Reflection from '@framework/Reflection'
import * as Navigator from '@framework/Navigator'
import * as Components from '@framework/Components'
import { AuthClient } from '../../Signum.Authorization/AuthClient'
import * as Services from '@framework/Services'
import { TreeClient } from '../../Signum.Tree/TreeClient'
import * as AutoCompleteConfig from '@framework/Lines/AutoCompleteConfig'
import * as Hooks from '@framework/Hooks'
import * as SelectorModal from '@framework/SelectorModal'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

export const globalModules: any = {
  luxon,
  React,
  Components,
  Globals,
  Navigator,
  Finder,
  Reflection,
  Entities,
  AuthClient,
  Operations,
  Constructor,
  Services,
  TreeClient,
  AutoCompleteConfig,
  Hooks,
  SelectorModal,
  FontAwesomeIcon,
};
