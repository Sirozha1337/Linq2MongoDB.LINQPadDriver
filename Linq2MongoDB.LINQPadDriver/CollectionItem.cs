namespace Linq2MongoDB.LINQPadDriver
{
    public class CollectionItem
    {
        public CollectionItem(string mongoCollectionName, string collectionPropertyName)
        {
            MongoCollectionName = mongoCollectionName;
            CollectionPropertyName = collectionPropertyName;
        }

        public string MongoCollectionName { get; }
        public string CollectionPropertyName { get; }

        public string DisplayName()
        {
            if (CollectionPropertyName == MongoCollectionName)
            {
                return CollectionPropertyName;
            }

            return $"{CollectionPropertyName} ({MongoCollectionName})";
        }
    }
}
