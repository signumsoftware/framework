# Signum Framework

Welcome to Signum Framework Documentation, the main source of information regarding the Framework. 


## Introduction to the documentation

Signum Framework helps you in all the stages of writing a business application: 

 * Modeling entities.
 * Access the database using the ORM and the LINQ provider.
 * Windows and Web user interfaces. 
 * Tests...
  
We try to keep the API clean and consistent, and use technologies that could be familiar to a .Net developer, but still, it can take some time to feel comfortable building applications.

In order to get used to the API, **a good level of C# is necessary**. Also some familiarity with the UI underlying framework is recommended:
*  Signum.Windows: WPF and XAML
*  Signum.Web: ASP.NET MVC, Razor, Javascript, Typescript and Bootstrap. 

### Reading the documentation

This documentation tries to be a good reference that covers most of the framework. 

It is not auto-generated from source code comments, but redacted manually. This means that not every single public method will be explained, on the other side it does a better job introducing each class, giving rationale, comparing with alternatives and giving recommendations.  

While the documentation comes from the Markdown files that are distributed with the source code, it's ordered in a way that is easier to follow. 

In that sense the documentation can be used **as a reference guide**, but can also be read **as a book** to have a general understanding and discover new features. 

## Alternatives 

### Tutorials

If you prefer to learn by example, here are some tutorials in Complex (a little bit out-dated).

* **[Signum Framework Principles](http://www.codeproject.com/Articles/34560/Signum-Framework-Principles)**: Explains the reasons behind Signum Framework, the tecnologies we use, and the projects where Signum Framework fits. Also, we test our LINQ Provider against Frans Bouma's test. 

* **[Signum Framework Tutorials Part 1 – Southwind Entities](http://www.codeproject.com/Articles/224841/Signum-Framework-Tutorials-Part-Southwind-Entiti)**: This is the first tutorial of the Southwind serie. The idea is to create a whole solution using Signum Framework that mimics Microsoft Northwind example database. In this tutorial we explain how to write entities and validators

* **[Signum Framework Tutorials Part 2 – Southwind Logic](http://www.codeproject.com/Articles/230360/Signum-Framework-Tutorials-Part-Southwind-Logic)**: In this part, we will focus on writing business logic, LINQ queries and explain inheritance 

* **[Signum Framework Tutorials Part 3 – Southwind Load](http://www.codeproject.com/Articles/493158/Signum-Framework-Tutorials-Part-3-SouthWind-Load)**: In this tutorial we will focus on moving the data from Northwind Database to the new Southwind 

### Videos

Or if you prefer to watch some YouTube videos to do real work: 

* **[Signum Philosophy and Vision](https://www.youtube.com/watch?v=bJNbv5kHC0s): Cartoon animation explaining the philosophy behind Signum Framework.** The historical context (ORMs and Linq) and what Signum Framework offers to the development of data-centric applications. Finally, a very concrete example shows how Signum Framework simplifies the development and maintenance of an hypothetical application.

* **[Video Tutorial 0 - Basics](https://www.youtube.com/watch?v=6aVDRbVJwX4): Basic tutorial to get comfortable with the ORM.** We use a Console Application to model a simple entity class, create the database and save and retrieve some entity objects.

* **[Video Tutorial 1 -  Writing the Entities](https://www.youtube.com/watch?v=fqaquajRf38):	First tutorial to develop a real application for a Shop.** We explain the typical architecture of a client-server system, and how to model the entities (adding db constraints and validations) the first and more important step when building an application using Signum Framework.

* **[Video Tutorial 2 - Writing the Server Code](https://www.youtube.com/watch?v=Rr21NTMQwTk): Second tutorial to develop a real application for a Shop.** We learn how to create the database, load legacy data, write the business logic, and set-up the web server that will be used by the client applications.

* **[Video Tutorial 3 - Writing the Client Code](http://signumframework.com/VideoTutorialClient.ashx): Third and last tutorial to develop a real application for a Shop.** We learn how to create the UserControls for our entities, customize them, and write LINQ queries on the server for the SearchControl.


### Courses

**[Training courses](http://www.signumsoftware.com/en/Training)**: Designed for teams of developers that need to start fast and want some Skype session, or Face-to-face course during a period of about week. 