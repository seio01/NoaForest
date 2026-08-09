public interface ICollectionItem
{
    string Id { get; }
    string DisplayName { get; }
    Define.CollectionType CollectionType { get; }
}
