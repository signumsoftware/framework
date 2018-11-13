import * as React from 'react'
import { getQueryNiceName } from '@framework/Reflection'
import * as Finder from '@framework/Finder'
import { QueryEntity } from '@framework/Signum.Entities.Basics'
import { ToolbarConfig, ToolbarResponse } from './ToolbarClient'

export default class QueryToolbarConfig extends ToolbarConfig<QueryEntity> {
  constructor() {
    var type = QueryEntity;
    super(type);
  }

  getLabel(res: ToolbarResponse<QueryEntity>) {
    return res.label || getQueryNiceName(res.content!.toStr!);
  }

  handleNavigateClick(e: React.MouseEvent<any>, res: ToolbarResponse<any>) {
    if (!res.openInPopup)
      super.handleNavigateClick(e, res);
    else {
      Finder.explore({ queryName: res.content!.toStr! }).done()
    }
  }

  navigateTo(res: ToolbarResponse<QueryEntity>): Promise<string> {
    return Promise.resolve(Finder.findOptionsPath({ queryName: res.content!.toStr! }));
  }
}
