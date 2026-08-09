using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ProfileManager
{
    private const string FUNCTION_ENSURE_USER_PROFILE = "ensureUserProfile";
    private const string FUNCTION_UPDATE_USER_NAME = "updateUserName";

    private readonly BadWordFilter _badWordFilter = new();
    private Task _initializeTask;

    public bool IsInitialized { get; private set; }
    public UserData CurrentUserData => Managers.Data.CurrentSaveData.User;

    public Task InitializeAsync()
    {
        _initializeTask ??= InitializeInternalAsync();
        return _initializeTask;
    }

    public void ResetForAccountChange()
    {
        _initializeTask = null;
        IsInitialized = false;
    }

    public bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        string normalizedName = NormalizeName(name);
        if (!HasOnlySupportedCharacters(normalizedName))
            return false;
        if (!_badWordFilter.IsInitialized)
            return false;

        return !_badWordFilter.ContainsBadWord(normalizedName);
    }

    public async Task<bool> UpdateNameAsync(string name)
    {
        await InitializeAsync();

        string normalizedName = NormalizeName(name);
        if (!IsValidName(normalizedName))
            return false;

        Dictionary<string, object> request = new()
        {
            { "name", normalizedName }
        };
        UserProfileResponse response = await Managers.Data.SaveAsync<UserProfileResponse>(
            FUNCTION_UPDATE_USER_NAME,
            request,
            serverResponse => new SavePatch().Set(SaveField.UserData, serverResponse.ToUserData()));
        return response != null;
    }

    private async Task InitializeInternalAsync()
    {
        await _badWordFilter.InitializeAsync();

        UserProfileResponse response = await Managers.Data.SaveAsync<UserProfileResponse>(
            FUNCTION_ENSURE_USER_PROFILE,
            new Dictionary<string, object>(),
            serverResponse => new SavePatch().Set(SaveField.UserData, serverResponse.ToUserData()));
        if (response == null)
        {
            if (CurrentUserData?.HasIdentity == true)
            {
                IsInitialized = true;
                Debug.LogWarning("[ProfileManager] Server profile is unavailable. Using the local profile cache.");
                return;
            }

            throw new InvalidOperationException("User profile initialization failed.");
        }

        IsInitialized = true;
        Debug.Log("[ProfileManager] Initialized.");
    }

    private static string NormalizeName(string name)
    {
        return name?.Normalize(NormalizationForm.FormKC).Trim() ?? string.Empty;
    }

    private static bool HasOnlySupportedCharacters(string value)
    {
        foreach (char character in value)
        {
            bool isEnglish = character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
            bool isNumber = character is >= '0' and <= '9';
            bool isSeparator = character is ' ' or '_';
            if (!isEnglish && !isNumber && !isSeparator && !IsHangulCharacter(character))
                return false;
        }

        return true;
    }

    private static bool IsHangulCharacter(char character)
    {
        return character is >= '\u1100' and <= '\u11FF'
            or >= '\u3130' and <= '\u318F'
            or >= '\uA960' and <= '\uA97F'
            or >= '\uAC00' and <= '\uD7A3'
            or >= '\uD7B0' and <= '\uD7FF';
    }
}
