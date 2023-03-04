# Signum.Engine

Signum Engine is the assembly that contains the classes to access the database in terms of your entities, for this reason, this assembly only needs to be known by the server. 

The main classes in Signum.Engine are: 

* [`Database`](Database.md): static class used for normal manipulation of the database. It's the gate to the ORM (`Save` method) and the LINQ provider (`Query` method). 
* [`Administrator`](Administrator.md): static class used for administrative tasks like generating and synchronizing the schema. 
* [`Executor`](Executor.md): low-level static class to send SQL commands to the current connection.
* [`Schema`](Schema.md): Data structure that represents the mapping between the entities and the database.
* **Scopes:** Pattern that allows ambient implicit variables to be set
 * [`Connector`](Connector.md): Encapsulates the SqlConnection factory as well as the reference to the Schema, and could be overridden in the future to support other database providers.     
 * [`Transaction`](Transaction.md): Similar to `TranslactionScope` but nestable by default and allows to control the isolation level and create named transactions.
 * [`EntityCache`](EntityCache.md): Activates a cache of entities to avoid clones in a region of code. This feature is opt-in instead of default. 
