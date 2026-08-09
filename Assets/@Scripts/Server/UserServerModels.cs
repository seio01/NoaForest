using System;

[Serializable]
public class UserProfileResponse
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Uuid { get; set; }
    public int Level { get; set; }

    public UserData ToUserData()
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

[Serializable]
public class AccountWithdrawalResponse
{
    public bool Success { get; set; }
}
