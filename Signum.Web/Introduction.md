## Introduction

Signum.Web is the assembly that helps you be more productive building ASP.Net MVC client applications. 

It tries to replicate the experience of `Signum.Windows` for web application, finding consistency between the two assemblies whenever it makes sense, but at the same time provides a close-to-the-metal native web solution that does not get in your way building nice HTML5/CSS3 applications. 

> Note: This documentation has certain level of duplication from `Signum.Windows` dues to the similarities. If you already know `Signum.Windows` some parts will look familiar.

There are three main design principles that we follow in order to make `Signum.Web`:

* **Take profit of Signum Entities:**  Building a UI with Signum Web is faster than writing it with ASP.Net MVC alone, not because Microsoft guys have been too lazy to do it right, but because we have a more solid foundation, `Signum.Entities`. We enforce [every entity](../Signum.Entities/BaseEntities.md) to have `Id` and `ToStr`, and how the relationships, and the [collections](../Signum.Entities/MList.md) have to be implemented. We also have a consistent model for [validation](../Signum.Entities/Validation.md) and [change tracking](../Signum.Entities/ChangeTracking.md), and we know how to deal with [collections](../Signum.Entities/Lite.md) objects and enums. All these requirements make it easier for us to build something like Signum.Web.

* **Being a good ASP.Net citizen:** ASP.Net MVC is a nice framework. Having Views and Controllers separated gives you more power to create complex navigation workflows and AJAX-enabled websites. We didn't want to reinvent the wheel, moving you to some proprietary DSL, control or environment that makes you more productive at the cost of loosing control. Instead we used Razor and `HtmlHelpers` to create clean an concise code for the common business applications requirements, without loosing any expressiveness that empowers your creativity when needed. 

* **Use common web libraries if necessary:** Signum.Framework tries to be conservative adding dependencies   to third-party libraries, building on top of .Net Framework alone. However, client-side web development is way too austere if done using just the DOM and no other libraries. We took `jQuery` and `Bootstrap` as base dependencies of `Signum.Web`, and we also promote `TypeScript` as the language for writing client-side interaction.  

## Prerequisites

In order to fully understand Signum.Web is necessary to have some knowledge of modern web development in .Net:

* **ASP.Net MVC:** Controllers, Actions, Views, Routes, Filters...
* **HTML5, CSS3 and JavaScript:** CSS selectors and jQuery
* **Typescript:** Classes and Interfaces, Promises, External modules...
* **Bootstrap:** Grid System and Forms.    

## Overview

The two main pages in a `Signum.Web` application are the [SearchPage](../Signum/Views/SearchPage.m), that is mostly auto-generated from a [registered query](../Signum.Engine/DynamicQuery/DynamicQueries.md"), and the [NormalPage](../Signum/Views/NormalPage.md) that provides a common frame for the detail view of any entity and contains a **custom control** for each type of entity.  

The main task building a User Interface using Signum Web is then creating the custom control for each entity type that defines how an entity (or graph of entities) is going to be displayed to the user. 

This custom controls are just ASP.Net MVC Content Page written in Razor (a mixture of HTML and C#), but are typically composed of a set of `HtmlHelpers` [EntityControls](EntityControl.md) (like `EntityLine`, `ValueLine`, `EntityCombo`, and so on...) that play well with [TypeContext](TypeContext/TypeContext.md) class, but you are free to use any other controls defined in Signum Web, or any HTML5 or ASP.Net MVC control to compose it.

This custom controls have to be registered in [Navigator](Facades/Navigator.md) class for the framework to find them, and `Navigator` is also responsible of returning and configuring `NormalPage` or `NormalControl` as `ActionResult`.

[Finder](Facadades/Finder.md), on the other side, is responsible of returning `SearchPage`.

[Constructor](Constructor.md) is the class responsible of registering any custom client-side constructor for our entities.

[Mappings](Mappings.md) are responsible of applying the changes of a `Form` coming from a `HTTP POST` request to modify or create a new entity, returning possible validation errors at the same time. 

Finally, the **TypeScript API** will let you develop client-side scripts in this fantastic strongly-typed language, using client-side counterparts for `EntityControls`, `Navigator`, `Finder`, etc... And `JsFunction` is the bridge to cross **from C# API to TypeScript API**. 



## Design decisions (bla, bla, bla...)

When designing Signum.Web we were between two main UI strategies, with their pros and cons. 

### Avoid Code Generation
One solution is to generate the user interface code from your data model (tables or entities). Code generations save you from writing the code, not from maintaining it. 

In fact, code needs to be automatically generated when it takes too much code for something to work, and this happens if the framework is not expressive enough.

There is a situations where generating code is a good idea: If only the source will be modified, not the product. This is not usually the case for User Interfaces, where the entity (source) will change, and also the user interface (product) has to be customized to make it more user-friendly. 

> Ruby on Rails follows this approach to generate the ActiveRecords and also the html of your website. 

### Avoid Dynamic Generation
The second main strategy is to automatically generate the user interface at run-time, using some kind of type introspection (reflection). This completely saves us from writing and maintaining user interface code, but you have to agree with the results because changing them is hard. 

Any change you want to make have to be defined as an exception of the dynamic generator default behavior, so you are limited to the expressiveness of the available exceptions provided by this run-time. Also, because this information can't be specified in the source itself, these exceptions can get quite messy.

> Django Admin interface or ASP.Net Dynamic Data uses this approach. 

### Avoid custom Domain Specific Languages
Dynamic generation is fast, but not flexible enough for a general purpose UI framework. Generating code, on the other hand, makes writing code faster, but creates a nightmare if you have to modify it. 

The only way to solve this, in our opinion, is to work at the right level of abstraction: Being able to specify the common things you find in business user interfaces more easily, like collections, value fields, etc...

The obvious way of doing it is to build your own User Interfaces Domain Specific Language. The problem with this approach is that you end up being as expressive as the DSL allows you to be. 

> In our previous framework, we had our own custom Xml format for defining user interfaces for WebForms.

### Our Solution: Smarter helpers that understand entities
What we do instead is writing a set of convenient `HtmlHelpers` that make your HTML/Razor look like a DSL for business application. The good news is that you are still writing code inside a familiar environment so you are still able to integrate Signum.Web code with any other HTML5/MVC code seamlessly. 

We archive productiveness by having convenient controls and a solid basement that allows us to work at the right level of abstraction. Actually, there's not a lot of code in `Signum.Web` compared to its' benefits. The important thing about Signum.Web is not that it's already programmed and ready for you to use, no way! 

The important thing is that, once you have an standardized way of dealing with your entities, it's possible (and easy) to build a visualization framework that increases your productivity without having to invent anything extremely clever. In fact, we can take advantage of the solid base provided by `Signum.Entities` in any other environment, like `Signum.Windows`.


