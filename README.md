# This is a LINQPad driver for MongoDB

This driver allows you to query MongoDB using [LINQPad](https://www.linqpad.net/).

This driver is based on the mkjeff's [MongoDB driver for .NET](https://github.com/mkjeff/Mongodb.LINQPadDriver).

Requirement
-------------
* LINQPad 7.x, 
* .net 6.x

Installation
-------------
1. Click `Add connection`
2. Click `View more drivers...`
3. On LINQPad Nuget Manager window, switch the radio button to `Search all drivers`
4. Select package `Linq2MongoDB.LINQPadDriver` and click `install`
 
Setup connection
-------------
1. Add connection, choose `Build data context automatically` and select MongoDB Driver click `Next`.
2. Configure Connection String, Database Name and optionally Display name (otherwise Host + DB names are used)
3. Make queries to your MongoDB database

**Note**
* Collections will be exposed as capitalized names with all non-alphanumeric characters removed, e.g.: 'users-data' will be exposed as 'Usersdata'
* Collections are exposed as IQueryable<BsonDocument>, to access the underlying collection use the `Collection` property, e.g.: `Usersdata.Collection`
* To access the underlying MongoDB instance, use `Database`
* See [examples](Linq2MongoDB.LINQPadDriver/linqpad-samples/CRUD.linq) for more information
