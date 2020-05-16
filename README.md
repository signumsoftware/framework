[Signum Framework](http://www.signumframework.com/)
===================================================

[![Join the chat at https://gitter.im/signumsoftware/framework](https://badges.gitter.im/signumsoftware/framework.svg)](https://gitter.im/signumsoftware/framework?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Signum Framework is an Open Source framework from Signum Software for writing data-centric applications over the latest technologies from Microsoft (.Net Core 3.1, C# 8.0, ASP.Net Web.API and Typescript 3.7) and web standard libraries (React, Bootstrap and D3). It uses either  Microsoft SQL Server or PostgreSQL to store data.   

The main focus of the framework is being able to write vertical modules (database tables, entities, logic and React UI components) that can be shared between projects.

It provides a consistent model for N-layer architectures by moving the entities to the very center of your solution using Signum.Entities.

Our cutting-edge ORM, Signum.Engine, has a full LINQ Providers that avoids N + 1 problem and lets you UPDATE / DELETE / INSERT entities without having to retrieve them first.

Signum.React let you write a Single Page Application using React, Bootstrap and TypeScript.
 

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
