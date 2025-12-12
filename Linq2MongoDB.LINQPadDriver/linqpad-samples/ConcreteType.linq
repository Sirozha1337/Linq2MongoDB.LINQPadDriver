<Query Kind="Program">
  <NuGetReference>MongoDB.Driver</NuGetReference>
  <Namespace>MongoDB.Driver</Namespace>
  <Namespace>MongoDB.Bson</Namespace>
</Query>

// assumes you have a connection with the collection "MyCollection"

void Main()
{
	var query =
	    from x in MyCollection.As<Item>()
            where x.Name.StartsWith("A")
            select new { 
                id = x.Id, 
                x.Name
            };
        
	query.Dump();
}

public class Item
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
}