<Query Kind="Statements">
</Query>

// Create Collection
Database.CreateCollection("MyCollection");

// Insert
Console.WriteLine($"Collection has {MyCollection.Count()} documents");
MyCollection.InsertOne(new { test = "test" } );
Console.WriteLine($"Collection now has {MyCollection.Count()} documents");
Console.WriteLine(MyCollection.First());

// Update
var myObject = MyCollection.First();
Console.WriteLine("My object before update:");
Console.WriteLine(myObject);
myObject["test"] = "updated";
MyCollection.ReplaceOne(myObject);
Console.WriteLine("My object after update:");
Console.WriteLine(MyCollection.First());

// Delete
Console.WriteLine($"Collection has {MyCollection.Count()} documents");
var deleteResult = MyCollection.DeleteOne(o => o["test"] == "updated");
Console.WriteLine("Delete result:");
Console.WriteLine(deleteResult);
Console.WriteLine($"Collection now has {MyCollection.Count()} documents");

// Insert Many
Console.WriteLine($"Collection has {MyCollection.Count()} documents");
MyCollection.InsertMany(new [] {
 new { name = "test1", update_me = true, some_val = "initial" },
 new { name = "test2", update_me = false, some_val = "initial" },
 new { name = "test3", update_me = true, some_val = "initial" },
});
Console.WriteLine($"Collection now has {MyCollection.Count()} documents");
Console.WriteLine("New documents:");
Console.WriteLine(MyCollection.Where(o => o["some_val"] == "initial").ToArray());

// Update Many
Console.WriteLine($"Collection has {MyCollection.Count(u => u["update_me"] == true)} documents to update");
var updateManyResult = MyCollection.UpdateMany(u => u["update_me"] == true, u => { u["update_me"] = false; u["some_val"] = "new"; return u; });
Console.WriteLine("Update Result:");
Console.WriteLine(updateManyResult);
Console.WriteLine("New documents:");
Console.WriteLine(MyCollection.Where(o => o["update_me"] == false).ToArray());

// Delete Many
Console.WriteLine($"Collection has {MyCollection.Count(u => u["update_me"] == false)} documents to delete");
var deleteManyResult = MyCollection.DeleteMany(o => o["update_me"] == false);
Console.WriteLine("Delete Result:");
Console.WriteLine(deleteManyResult);
Console.WriteLine($"Collection now has {MyCollection.Count()} documents");

// Drop Collection
Database.DropCollection("MyCollection");