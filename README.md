[Signum Framework](http://www.signumframework.com/)
===================================================

[![Join the chat at https://gitter.im/signumsoftware/framework](https://badges.gitter.im/signumsoftware/framework.svg)](https://gitter.im/signumsoftware/framework?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Signum Framework is an Open Source framework from Signum Software for writing data-centric applications over the latest technologies from Microsoft (.Net Core, C#, ASP.Net Web.API and Typescript in their latest versions) and web standard libraries (React, Bootstrap and D3). It uses either Microsoft SQL Server or PostgreSQL to store data.   

The main focus of the framework is being able to write vertical modules (database tables, entities, logic and React UI components) that can be shared between projects.

It provides a consistent model for N-layer architectures by moving the entities to the very center of your solution using Signum.Entities.

Our cutting-edge ORM, Signum.Engine, has a full LINQ Providers that avoids N + 1 problem and lets you UPDATE / DELETE / INSERT entities without having to retrieve them first.

Signum.React let you write a Single Page Application using React, Bootstrap and TypeScript.
 
## [Demo Application](https://github.com/signumsoftware/southwind#online-version-in-azure)
Check some of the features of Signum Framework in Southwind, a Northwind port running in Azure. 
 
## Signum Extensions
Set of modules that complements [Signum Framework](https://www.signumsoftware.com/es/Framework) like Authorization, Charting, Dashboards, Mailing, Processes, Scheduled Tasks, Disconnected, User Queries...

Check the different modules in https://www.signumsoftware.com/es/Extensions


### Main Features ###
* Designed for vertical modules (aka: Bounded Context)
* Entities-first approach
* ORM with a full LINQ Provider
* Unified validation
* Schema generation and synchronization
* WPF smart-client interface
* React/Typescript SPA


### Principles ###
* Promote simple and clean code, avoiding astronautical architectures.
* Favor compile-time checked code over dynamic code.
* Create a solid foundation for the integration of application modules (on schema, BL and UI code).
* Encourage a more functional way of programming.
* Avoid code duplication.
* Be a good citizen wherever we are (.Net, LINQ, React) following common practices and conventions.

## Getting Started

* **[Signum Framework](http://www.signumsoftware.com/en/Framework)**: Know what makes Signum Framework suited for building complex business applications. 
* **[Signum Extensions](http://www.signumsoftware.com/en/Framework)**: A set of ready-to-use modules that work with any Signum Framework application. 
* **[Documentation](http://www.signumsoftware.com/Documentation)**:  Documentation (from Markdown files in GitHub)
* **[Tutorials](https://github.com/signumsoftware/docs)**: Tutorials (in docx and pptx) 
* **[Create Application](http://www.signumsoftware.com/en/DuplicateApplication)**: The simplest way to get started. Create a new project by renaming and customizing Southwind example application.  

## ChangeLog

Signum Framework doesn't use any numeric versioning, since is distributed as source code we just use Git commit hashes.

Whenever there are big changes worth to mention, we typicaly write it in the related commit. Here is the list of the relevant changes: 

* [2025.12.15 Run TSGenerator on demand](https://github.com/signumsoftware/framework/commit/3cba964eef6dccedebd4517e027dcb5853f5914f#commitcomment-172898719)
* [2025.12.14 TypeScript Native (tsgo) Previews 🚀](https://github.com/signumsoftware/framework/commit/0d154f2feff976298b34900beec6b50741a3e7a9#commitcomment-172744788)
* [2025.12.06 Split ActiveDirectory into AzureAD and WindowsAD 🔑](https://github.com/signumsoftware/framework/commit/de37f4fef3ad3dd1ce85a3112feb2f2848b5bebc#commitcomment-172594546)
* [2025.11.28 Import/Export Help 📚](https://github.com/signumsoftware/framework/commit/da567c70a0a3af3fc70aec0dc52e1df2f0945c5f#comments)
* [2025.11.23 .Net 10](https://github.com/signumsoftware/framework/commit/fcb69ddcec43ea5c3fe5d096dc5368df293f7fd6#commitcomment-171171659)
* [2025.10.01 TypeScript Compilation Performance and Signum.TSCBuild](https://github.com/signumsoftware/framework/commit/5d740e37effc19469b097f9ce763f5560e4a1d7a#commitcomment-167686513)
* [2025.09.22 Toolbar Improvements 🛠️](https://github.com/signumsoftware/framework/commit/d9a168aae9d9a59f6416652c270a444b9d160c1e#commitcomment-167596454)
* [2025.09.01 Dark Mode, ThemeSelector and shadow panels 🌙☀️](https://github.com/signumsoftware/framework/commit/91eda3e81d898d7c8412b4d54022128cbba7efab#commitcomment-167592981)
* [2025.08.22 Webpack -> Vite, React 19, React Router 7.7 and react-widgets-up](https://github.com/signumsoftware/framework/commit/f9aacfc47447c435e3708e86b92538238ca5c9b0#commitcomment-164464873)
* 2025.07.21 Improvements in charting (translations, scales) ChartColumnType
* 2025.07.16 Nicer Dashboards
* 2025.06.23 AutoExpand in QueryTokens
* 2025.05.25 Adopt LF (\n) as new-line separator for all file types (.ts, .tsx, .cs, etc...) using `.gitattributes`
* 2025.04.01 Switch `Signum.HtmlEditor` from draft.js (deprecated) to lexical (great work @fwo-bechtle!)
* 2025.03.03 MList tables have _ separating main table name from MList by default (dbo.Order_Details) 
* 2024.11.16 .Net 9
* [2024.10.28 CSV Performance Improvements?](https://github.com/signumsoftware/framework/commit/e17aeb8df452dfd8289586ccb0135019ad92de38#commitcomment-150176560)
* [2024.08.19 Time Machine in Charting! ⏲📈](https://github.com/signumsoftware/framework/commit/4a7cc1097f8e862b47d2dfd61216082b1c171be0)
* [2024.07.02 Isolated Declarations](https://github.com/signumsoftware/framework/commit/bf08a77c430dd133b8eca50d502a14b91224267a)
* [2024.03.27 Navigator, Finder, Operations, and *Client modules get a `namespace`](https://github.com/signumsoftware/framework/commit/2cf26f9cfe05f8707930bccf3c7427cc1522b99b#commitcomment-140281789)
* [2024.02.17 `EntityLine<T>` with TS 5.4 beta](https://github.com/signumsoftware/framework/commit/4fcdba33a4d53c32477a8b44fd3088722dc49664#commitcomment-138753911)
* [2023.10.31 From ValueLine to AutoLine](https://github.com/signumsoftware/framework/commit/44ca21e578187949932b6861f2a3be66b78ff290#commitcomment-131412697)
* [2023.09.02 Presenting QueryAuditors (aka TypeConditionLogic.RegisterWhenAlreadyFilteringBy)](https://github.com/signumsoftware/framework/commit/b93dea738b259640790a470b25357eedad022dd4#comments)
* [2023.08.30 QuickLinks in SearchControl Columns](https://github.com/signumsoftware/framework/commit/25f239479afa9027d24b7cc12f75722550411f06#comments)
* [2023.05.09 Modular Revolution 🔥🔥🔥](https://github.com/signumsoftware/framework/commit/25f239479afa9027d24b7cc12f75722550411f06#comments)
* [2023.04.18 Full-Text-Search 🔎](https://github.com/signumsoftware/framework/commit/fbba1e4e124a610bdf7b90afd81f681cb00566a0#commitcomment-111853358)
* [2023.04.14 Simple Pinned Filters, Split Values and EntityStrip in SearchControl](https://github.com/signumsoftware/framework/commit/e9705497df53fbfd6965bd7e0ba448c2726a2e96#commitcomment-111858344)
* [2023.03.24 Accessibility 🧑‍🦯](https://github.com/signumsoftware/framework/commit/7fc7c5efa2c39cdd881d6d6dd0bc57caa7da9f08#commitcomment-111843293)
* [2023.02.18 Time Machine 2.0 🕰️](https://github.com/signumsoftware/framework/commit/68914f6239a9a849a2a07d7e647aeda2d8c9dbf1#commitcomment-101162063)
* [2023.02.07 Presenting Custom Drilldowns](https://github.com/signumsoftware/framework/commit/75c713fbd8023824344f8a270afc1c24dc5496b7#comments)
* [2023.01.31 Upgrade to react-router 6.7.0](https://github.com/signumsoftware/framework/commit/76755e743fd32bd787cef5e20a76ccb2155107b4#comments)
* [2022.12.16 Responsive SearchControl](https://github.com/signumsoftware/framework/commit/41fc3c0c4732e2ce5750648d65b7030bce08c5e2#comments)
* [2022.12.14 Client-side Diff](https://github.com/signumsoftware/framework/commit/880c1a7860573310e2cd300c45fe7bb92f1954de#comments)
* [2022.12.06 ChatGPT 🤖 can program with Signum Framework!](https://github.com/signumsoftware/framework/commit/901e069a5b0ef0fd8b80d0e1632fd66671cb8b8f#commitcomment-92227611)
* [2022.12.01 Color Palette 🌈](https://github.com/signumsoftware/framework/commit/838eea190ea1bec22c83f845d7aab76e7d989a1d#comments)
* [2022.11.08 Upgrade to .Net 7](https://github.com/signumsoftware/framework/commit/8377d1b78b12c2572e881f06da6680d27d21fa1b#comments)
* [2022.10.25 Cleanup in EmailSenderConfigurationEntity](https://github.com/signumsoftware/framework/commit/d9d219a5f68f3b035a8477254c3adc396de98d2c#comments)
* [2022.08.30 Presenting EntityAccordion](https://github.com/signumsoftware/framework/commit/3ba48fae1456207992abadf74075bb05502507f5#comments)
* [2022.08.18 Presenting EntityTypeToken](https://github.com/signumsoftware/framework/commit/2ec0dd7c53067c34e01e4570968ae9e14263a65e#comments) 
* [2022.08.11 Logging Client-Side Errors](https://github.com/signumsoftware/framework/commit/0b422b488458677614383875b4fbc0e499093bb7#comments)
* [2022.08.09 Presenting SeparatedByNewLine / SeparatedByComma](https://github.com/signumsoftware/framework/commit/94401cd2b1a785b4e69735c80e54d67ca7a9e7d8#comments)
* [2022.08.05 Remove Promise.done()](https://github.com/signumsoftware/framework/commit/390b34f5206f4467dc517747c351e206ffb89d63#comments)
* [2022.07.30 CellOperationButton and commonOnClick](https://github.com/signumsoftware/framework/commit/65054d7a700dd6a5d14581e0427544f05a6be2d8#commitcomment-79818203)
* [2022.07.27 RoleEntity IsTrivialMerge](https://github.com/signumsoftware/framework/commit/c468115b100f1d1df032afcbf7a8561d01dc6025#comments)
* [2022.07.22 TypeCondition intersection](https://github.com/signumsoftware/framework/commit/15024fa5f81b7e627b564ee229edb19990411e1a#commitcomment-79437098)
* [2022.06.26 Presenting Lite Model](https://github.com/signumsoftware/framework/commit/160ddc3b3261c544b15c144c4d2e47f04b305feb#commitcomment-77033687)
* [2022.06.13 Initial Migration](https://github.com/signumsoftware/framework/commit/c57b3d6563ec5859bf131cfd7800d32d1a07336f#commitcomment-75943113)
* [2022.04.05 ImplementedByAll with multiple ID columns](https://github.com/signumsoftware/framework/commit/51321d1a236925fbf874c39ea0c9445f96a46089#commitcomment-70655266)
* [2022.03.29 ValueSearchControl(Line) renamed to SearchValue(Line)](https://github.com/signumsoftware/framework/commit/8057968f8f7b5aed429487364cb83c1f5be21937#comments)
* [2022.03.18 Improvements in Charting](https://github.com/signumsoftware/framework/commit/740b67b0d509410e2a87b202954a9ee58bf84d6e#comments)
* [2022.03.16 Faster queries using @foreach on WordTemplate / EmailTemplate](https://github.com/signumsoftware/framework/commit/b4922d08f89e731c5d3b5201e8ef14008c638928#comments)
* [2022.03.16 Cleaner translation from dynamic queries to LINQ](https://github.com/signumsoftware/framework/commit/c11a80be0a2cb52823718c746a59af3501bef133#comments)
* [2022.03.15 Presenting ConcurrentUser](https://github.com/signumsoftware/framework/commit/4031bfb6d1e57c9ab2ad1f682ddf1c6f11c25059#comments)
* [2022.02.07 New Responsive Sidebar](https://github.com/signumsoftware/framework/commit/2a613da38e306011b5e9efcbf0c90fbf948da39a#commitcomment-66136445)
* [2022.01.08 Dashboard V: Dashboard Pinned Filters](https://github.com/signumsoftware/framework/commit/552079543443bff685e0a5b2fbe48dbefaaf149b#commitcomment-63148535)
* [2022.01.08 Dashboard IV: Dashboard IV: Token Equivalences](https://github.com/signumsoftware/framework/commit/85b2648205a6cd1e3a84ffdac955b74585cddfa2#comments)
* [2021.12.19 Split between Size and Precission and Microsoft.Data.SqlClient 4.0.0](https://github.com/signumsoftware/framework/commit/fe17abd91b3e319d78c7ae2b4dfb0c70c7c4f276)
* [2021.12.17 CacheLogic.ServerBrodcast](https://github.com/signumsoftware/framework/commit/1903c0df005bec80dac4a7fca5b20b257420c591#commitcomment-62129403)
* [2021.12.16 Add SignalR to AlertDropdown](https://github.com/signumsoftware/framework/commit/c3e89ed1dfd53eaaf619c2823de43cb0cc2d3154#commitcomment-62061830)
* [2021.12.13 IsNew and GetType() members supported in ToStringExpression](https://github.com/signumsoftware/framework/commit/644151a2481307fdbb53216d2d71022a71e75d2c#comments)
* [2021.12.03 Dashboard III; InitialSelectionDashboardFilter and DefaultDashboardFilter](https://github.com/signumsoftware/framework/commit/90392c1066b7aed631ec3aaa55258e06f18ad013#comments)
* [2021.12.02 Dashboard II: Presenting CachedQueries](https://github.com/signumsoftware/framework/commit/641293cd886280a857493e6b9b10361220f19702#commitcomment-61271093) 
* [2021.11.30 Target ES2017 (async await welcomed!)](https://github.com/signumsoftware/framework/commit/9caeecfc0a8b489d4460aafe53450ef9ec416194#comments)
* [2021.11.30 Replace GroupsOf -> Chunk, WithMin -> MinBy and WithMax -> MaxBy](https://github.com/signumsoftware/framework/commit/45303a1a81a4335de21191438a574f98019075da#comments)
* [2021.11.14 Operation Executing](https://github.com/signumsoftware/framework/commit/002bcc9665ebf2bb6c0eed5e043cb081aab7e73c#comments)
* [2021.11.13 Bootstrap 5](https://github.com/signumsoftware/framework/commit/7d0804c7c2ab4841d0985e42c3b5fa96b8f01780#comments)
* [2021.11.09 .Net 6 and C# 10](https://github.com/signumsoftware/framework/commit/0669737b11a30dc385eb8fd1bc22ac97fa637cd0#commitcomment-59667330)
* [2021.11.04 WordTemplate with embedded Excel charts from UserCharts / UserQueries](https://github.com/signumsoftware/framework/commit/e8d4ab20e612af0b826beca77b7edf648227e806)
* [2021.11.01 Dashboard I: Dashboard filters and InteractionGroup](https://github.com/signumsoftware/framework/commit/55def717ccf8414e11396faa6b47b12747c56f01)
* [2021.08.28 Update to TypeScript 4.4](https://github.com/signumsoftware/framework/commit/f6412af23da1e225b0b417b329874acd8c820f05#commitcomment-55586120)
* [2021.08.28 Extensions ❤ Framework](https://github.com/signumsoftware/framework/commit/b7848eff42f5d242ed73035a5cc91f35d5ec20c8#commitcomment-55557696)
* [2021.03.31 Update to React Widgets to 5.0.0 final](https://github.com/signumsoftware/framework/commit/f85f1d71be63273d8d55f224274af677e4d586f5#commitcomment-48935445)
* [2021.02.17 Improvements in Instance Translations](https://github.com/signumsoftware/framework/commit/7c3a0da37ad3f8395ae1e65cd10c238034b98f58#commitcomment-47240876)
* [2021.02.16 Improvements in SearchControl and UserQueries](https://github.com/signumsoftware/framework/commit/efbe32018a94f46a41e68199606405924ce66bc4#commitcomment-47203466)
* [2021.01.19 Improvements in Translations (DeepL)](https://github.com/signumsoftware/framework/commit/b33e499e50fc855d2bd044c65361b13bfbbf257e#commitcomment-46112089)
* [2020.12.31 Replacing CNTK with TensorFlow.NET and back to Any CPU](https://github.com/signumsoftware/framework/commit/4f28e79349892c7f5045172c9e2e1d4b374b6dac#commitcomment-45536021)
* [2020.12.20 Upgrade to react 17.0, react-widgets 5.0.0-beta.22, d3 6.0 etc..](https://github.com/signumsoftware/framework/commit/760bdebd1f8457a505a4921ba09c10ac3097f911#commitcomment-45284544)
* [2020.12.08 UI Improvements in FrameModal and FramePage](https://github.com/signumsoftware/framework/commit/b08684db4c8e7630ad47ca957dd47b71d4064d5a#comments)
* [2020.11.24 Update to Typescript 4.1](https://github.com/signumsoftware/framework/commit/9379e400b232dac4e8bf284eee8fbded43f78e2b#comments)
* [2020.11.24 Presenting CombinedUserChartPart](https://github.com/signumsoftware/framework/commit/ef0eee90293604399a3b399d415ab4ccd1c94092#commitcomment-44503677)
* [2020.11.23 IE Support?](https://github.com/signumsoftware/framework/commit/ef0eee90293604399a3b399d415ab4ccd1c94092#comments)
* [2020.11.13 Presenting Signum.Upgrade](https://github.com/signumsoftware/framework/commit/a1a37a4a8bd3291dd244daa0db7e113d5ce4f859#comments)
* [2020.11.13 Switch to .Net 5 and C# 9](https://github.com/signumsoftware/framework/commit/227a8e79aece9d3be5020f2a8dad840c4fba95ad#comments)
* [2020.09.29 Adding support for WebAuthn](https://github.com/signumsoftware/framework/commit/76c66b8a2416b13b74bc4aeba480369651e09645#comments)
* [2020.09.21 Switch to core-js for polyfills](https://github.com/signumsoftware/framework/commit/c7b5c44af40eafd3325f76cb74b39e4a7d712404#comments)
* [2020.09.21 Replace numbro.js by Intl.NumberFormat](https://github.com/signumsoftware/framework/commit/e2de807e055f68e359949d1c6e2c21b5d093ed7f#commitcomment-42575661)
* [2020.09.21 Replace moment.js by luxon](https://github.com/signumsoftware/framework/commit/b2096177de9f84c91d226e1a6080386c55566d2a#commitcomment-42575134)
* [2020.09.16 Introducing MultiPropertySetter](https://github.com/signumsoftware/framework/commit/e11a04d81947b89e1b732f4f88f350cbf690010f)
* [2020.09.10 Adding SignumInitializeFilterAttribute](https://github.com/signumsoftware/framework/commit/8af868d10231629c8f045eb5b86f8431df427811#comments)
* [2020.08.31 Introducing BigStringEmbedded (move logs to FileSystem/Azure Blob Storage)](https://github.com/signumsoftware/framework/commit/726165e34c9323bb17ba2e006d1e7b128fbde8ba#comments)
* [2020.08.30 Update to Typescript 4.0 and clean csproj](https://github.com/signumsoftware/framework/commit/98922089a40eb140a372be7e4d8b28c4327df48d#comments)
* [2020.07.23 improve performance for api/types](https://github.com/signumsoftware/framework/commit/b47a37c386e4085fbc3bf66f68579954f9aac5f6#commitcomment-40851357)
* [2020.06.07 Update NPM packages and remove draft-js-plugins](https://github.com/signumsoftware/framework/commit/39f30297aa7d826082f2c37fe5d09bed429e38a6#comments)
* [2020.05.22 From react-rte to draft-js-plugins](https://github.com/signumsoftware/framework/commit/0f01c7a7d6a24ff8bab1046f136de36de4a93b4a#commitcomment-39362952)
* [2020.05.17 Update to Typescript 3.9.... Is MUCH faster!!!!](https://github.com/signumsoftware/framework/commit/da3afe553537ed18d5e5cb0df32b00f70052223f#comments)
* [2020.05.16 Pivot Table Chart](https://github.com/signumsoftware/framework/commit/91330aad5405df50cd7cc8fc42de36bbfc759b70#comments)
* [2020.05.14 Slimming Signum.React](https://github.com/signumsoftware/framework/commit/31c91dad34f251f0c728cad426d5f5db9e496261#comments)
* [2020.02.27 Updates package.json](https://github.com/signumsoftware/framework/commit/08f78128326fad5eaf80f184d67f76863f5aa8a9#comments)
* [2020.02.25 Typescript 3.8 and other changes](https://github.com/signumsoftware/framework/commit/8cdc9ab0ec5488dcfa65b2539bc7e784f0607f47#comments)
* [2020.02.14 Southwind inside of a Docker Container and running in Azure](https://github.com/signumsoftware/framework/commit/f9cc544a09cf1d80ae52ac659b57af06d8e730c5#comments)
* [2020.02.10 Rename Southwind.Load to Southwind.Terminal](https://github.com/signumsoftware/framework/commit/e5abacb0e234d9ef37158e911eb905a41ddc3a5a#comments)
* [2020.02.09 New Date type in Signum.Utilities](https://github.com/signumsoftware/framework/commit/5602efff5ae1bab352a50d783a1fe886371a9b46#comments)
* [2020.02.04 Improving Security and Performance with optional TypeInfo](https://github.com/signumsoftware/framework/commit/6e8aac61d19d8c15ff225fce6e6e105767643f4a#comments)
* [2020.01.27 New Signum Framework Map in documentation](https://github.com/signumsoftware/framework/commit/f877341021c4bec78d232aa71c0288d838e303d2#comments)
* [2019.01.24 **PostgreSQL support is here!**](https://github.com/signumsoftware/framework/commit/28955281a1e7b36b09f668f33f7b5e433f6b511b#comments)
* [2019.12.04 Update to .Net Core 3.1](https://github.com/signumsoftware/framework/commit/c416518733f67ca6ae73bd2bc0cff83ef18a2c64#comments)
* [2019.12.04 Update to Typescript 3.7](https://github.com/signumsoftware/framework/commit/d9edd5822cc79e8a56f4d534184db4e869956340#comments)
* [2019.12.04 More improvements in compilation speed using TSGenerator caching](https://github.com/signumsoftware/framework/commit/a6d3c795b0c88146c90ec490d3b6400cb6cc4b25#comments)
* [2019.11.25 Typescript MSBuild times 25% to 50% FASTER with Project References](https://github.com/signumsoftware/framework/commit/f4ec62400a5e2382b3c5b9b04d47ba2335ade12c#comments)
* [2019.11.11 Typescript 3.6, React 16.11.0 and back to react-bootstrap](https://github.com/signumsoftware/framework/commit/02e9a95fae7f3fce22792ef151f79c36af59f63b#comments)
* [2019.10.25 Remove ajaxGet / ajaxPost generic parameter](https://github.com/signumsoftware/framework/commit/99ea65e7adc3c581964e22e216469044a90b20f1#comments)
* [2019.09.26 .Net Core 3.0 is here!](https://github.com/signumsoftware/framework/commit/48cdba0030ae9ba649b1f0098ade0e114f4820be#comments)
* [2019.08.11 New [AutoExpressionField] (and [ExpressionField("auto")] removed)](https://github.com/signumsoftware/framework/commit/c99d4da3c8e94c55c868b659211e2868996e8613#comments)
* [2019.08.06 Update to Null Reference Type changes in VS 16.2](https://github.com/signumsoftware/framework/commit/92b213ea2a1ff71501dde1746b1f1376d4893a72#comments)
* [2019.07.16 lite.Retrieve renamed to lite.RetrieveAndRemember](https://github.com/signumsoftware/framework/commit/b3b1189f148477d71ce70bf716b7820693abce4b#comments)
* [2019.07.10 Windows Authentication with Single Sign-On](https://github.com/signumsoftware/framework/commit/1ad8c2405bb5f9bd65301548a232059bb4c1173c#comments)
* [2019.04.08 Fix Signum.Analyzer](https://github.com/signumsoftware/framework/commit/2cbf5c906b2485a252d3db9237e8f131d484f0fc#comments)
* [2019.04.08 Fix TSGenerator](https://github.com/signumsoftware/framework/commit/5c1200c29723352f1c20c58de7607feaf0276164#comments)
* [2019.04.08 Improve TypeScript compilation performance](https://github.com/signumsoftware/framework/commit/84ad9edb7d7368229318ef840e2d51c95f2d330c#comments)
* [2019.04.03 Nullable Reference Types merged! (C# 8, VS2019)](https://github.com/signumsoftware/framework/commit/2033a7d6b0f69801d1f5a7130a8c92e0926b6270#comments)
* [2019.04.03 Update to Typescript 3.4](https://github.com/signumsoftware/framework/commit/8ee55ca68e8c9ad916592e289ddec089442ada59#comments)
* [2019.03.07 React Hooks 2](https://github.com/signumsoftware/framework/commit/ff296a98cdb96cbdb0ce2c6ad26c371a066e7f79#comments)
* [2019.02.20 UseAPI (React Hooks!)](https://github.com/signumsoftware/framework/commit/9ae1966a4c7b835093f69e44c2f17a31e9415a67#comments)
* [2019.02.16 C# 8 Nullable reference types](https://github.com/signumsoftware/framework/commit/28c71a1cb3a02ca2a2ab286e4ddc9f4b7bc36d7c#comments)
* [2018.12.24 Pinned Filters](https://github.com/signumsoftware/framework/commit/8836f7df076e0d31c2ffacaebf4806d706207fd1#comments)
* [2018.12.20 QueryTokenString<T>: Against the last stringly-typed stronghold](https://github.com/signumsoftware/framework/commit/10d24213acc2d76a622014923f3be2741a459709#comments)
 * [2018.12.11 Animated Charts](https://github.com/signumsoftware/framework/commit/8d81a5fc719b0e7f102c541a2fd7e9841244ece8#comments)
 * [2018.12.07 Update to Typescript 3.2](https://github.com/signumsoftware/framework/commit/9552afc293a5c568d3f8283b2dbf6aea6254b8b0#comments)
 * [2018.12.07 Update to .Net Core 2.2](https://github.com/signumsoftware/framework/commit/dc2a3d6f4f6968081ac16fc012ab37cc45de94f9#comments)
 * [2018.12.01 Signum MSBuildTask and TSGenerator as Nuget](https://github.com/signumsoftware/framework/commit/bedc53e6b1bbe38fad8f40faa578d924bbda5797#comments)
 * [2018.11.25 UX Improvements in Charting](https://github.com/signumsoftware/framework/commit/452cba51f62b88e3032502aed49a2ee7e775989f#comments)
