using System;

[Serializable]
public class UserData
{
    public string Name;
    public string Id;
    public string Uuid;
    public int Level = 1;

    public bool HasIdentity => !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Uuid);

    public UserData CreateSnapshot()
    {
        return new UserData
        {
            Name = Name,
            Id = Id,
            Uuid = Uuid,
            Level = Level
        };
    }
}
