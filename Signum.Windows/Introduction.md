## Introduction

`Signum.Windows` is the assembly that helps you be more productive building WPF client applications. It tries to provide a first-class experience building WPF user interfaces for your entities. 

> Note: This documentation has certain level of duplication from `Signum.Web` dues to the similarities. If you already know `Signum.Web` some parts will look familiar.

There are three main design principles that we follow in order to make `Signum.Windows`:

* **Take profit of Signum Entities:** Building a UI with Signum Windows is faster than writing it with WPF alone, not because Microsoft guys have been too lazy to do it right, but because we have a more solid foundation, `Signum.Entities`. We enforce [every entity](../Signum.Entities/BaseEntities.md) to have `Id` and `ToStr`, and how the relationships, and the [collections](../Signum.Entities/MList.md) have to be implemented. We also have a consistent model for [validation](../Signum.Entities/Validation.md) and [change tracking](../Signum.Entities/ChangeTracking.md), and we know how to deal with [collections](../Signum.Entities/Lite.md) objects and enums. All these requirements make it easier for us to build something like Signum Windows.

* **Being a good WPF citizen:** WPF is a nice product, it has very deep concepts that have been missing in any previous frameworks (like declarative [XAML synthax](http://msdn.microsoft.com/en-us/library/ms752059.aspx), [Attached Properties](http://msdn.microsoft.com/en-us/library/ms749011.aspx), [Items Control](http://www.wpf-tutorial.com/list-controls/itemscontrol/), and a very powerful [Databinding](http://msdn.microsoft.com/en-us/library/ms750612.aspx) technology). We didn't want to reinvent the wheel, moving you to some proprietary DSL, control or environment that makes you more productive at the cost of loosing control. Instead we used XAML power to create clean an concise syntax for the common business applications requirements, without loosing any expressiveness that empowers your creativity when needed. 

* **Fill WPF Gaps:** WPF has archived excellent results in defining the foundations for windows presentation development, but it has been poor in the final polishing for building real life applications. There is no `DateTimePicker` control, no `ColorPiker`, neither easy auto-completion in `TextBox`. We tried to fill these gaps to make it usable until Microsoft comes up with an official alternative. 

## Overview

The two main windows in a `Signum.Windows` application are the [SearchWindows](DynamicQuery/SearchControl.m), that is mostly auto-generated from a [registered query](../Signum.Engine/DynamicQuery/DynamicQueries.md"), and the [NormalWindow](NormalWindow.md) that provides a common frame for the detail view of any entity and contains a **custom control** for each type of entity.  

The main task building a User Interface using Signum Windows is then creating the custom control for each entity type that defines how an entity (or graph of entities) is going to be displayed to the user. 

This custom controls are just `UserControl` written in XAML, but are typically composed of a set of helper [EntityControls](EntityControl.md) (like `EntityLine`, `ValueLine`, `EntityCombo`, and so on...) that play well with the attached properties defined in [Common](Common.md) class, but you are free to use other controls defined in Signum Windows, or any standard WPF control to compose it.

This custom controls have to be registered in [Navigator](Facades/Navigator.md) class for the framework to find them, and `Navigator` is also responsible of opening `NormalWindows` as part of our client processes.

[Finder](Facadades/Finder.md), on the other side, is responsible of opening `SearchWindows`.

[Constructor](Constructor.md) is the class responsible of registering any custom client-side constructor for our entities.

And finally [Server](Server.md) class is used as the main gate to reach the server through the WCF Service, think of it as the equivalent of [Database](../Signum.Entities/Database.md) from the client side.  


## Design decisions (bla, bla, bla...)

When designing Signum.Windows we were between three main UI strategies, with their pros and cons. 

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

> In our previous framework, we had our own custom Xml format for defining user interfaces for WinForms.

### Our Solution: Smarter controls that understand entities
Now, with Xaml available, there's no point in writing our own Xml format any more. Just writing a set of convenient controls and attached properties that make your Xaml look like a DSL for business application is enough. The good news is that you are still writing code inside of a very powerful UI environment (WPF) so you are still able to integrate Signum.Windows code with any other WPF code seamlessly. 

We archive productiveness by having convenient controls and a solid basement that allows us to work at the right level of abstraction. Actually, there's not a lot of code in Signum.Windows compared to its' benefits. The important thing about Signum.Windows is not that it's already programmed and ready for you to use, no way! 

The important thing is that, once you have an standardized way of dealing with your entities, it's possible (and easy) to build a visualization framework that increases your productivity without having to invent anything extremely clever. In fact, we can take advantage of the solid base provided by Signum.Entities in any other environment, like Web.
