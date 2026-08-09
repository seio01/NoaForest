/// <summary>
/// 데이터 저장의 사용자 정보
/// </summary>
public class SaveContext
{
    public string UserId {get;}
    public bool HasUserId => !string.IsNullOrEmpty(UserId);

    public SaveContext(string userId)
    {
        UserId = userId;
    }
}
