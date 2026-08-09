using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class LocalSaveRepository
{
    private const string SAVE_DIRECTORY_NAME = "Save";
    private const string SAVE_FILE_PREFIX = "save_";
    private const string SETTING_DIRECTORY_NAME = "Settings";
    private const string SETTING_FILE_NAME = "user_settings.json";

    private string _saveDirectoryPath;
    private string _settingFilePath;

    public void Initialize()
    {
        _saveDirectoryPath = Path.Combine(Application.persistentDataPath, SAVE_DIRECTORY_NAME);
        Directory.CreateDirectory(_saveDirectoryPath);

        string settingDirectoryPath = Path.Combine(Application.persistentDataPath, SETTING_DIRECTORY_NAME);
        Directory.CreateDirectory(settingDirectoryPath);
        _settingFilePath = Path.Combine(settingDirectoryPath, SETTING_FILE_NAME);
    }

    public SaveData Load(SaveContext context)
    {
        ValidateContext(context);

        try
        {
            string filePath = GetSaveFilePath(context.UserId);

            if(!File.Exists(filePath))
            {
                Debug.Log("[LocalSaveRepository] Save file does not exist.");
                return null;
            };

            string json = File.ReadAllText(filePath);
            SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json);
            saveData?.ApplyMigrations();

            return saveData;
        }
        catch(Exception e)
        {
            Debug.LogError( $"[LocalSaveRepository] Load failed: {e.Message}");
            throw;
        }
    }

    public void Save(SaveData saveData, SaveContext context)
    {
        Validate(saveData, context);

        try
        {
            string filePath = GetSaveFilePath(context.UserId);
            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);

            File.WriteAllText(filePath, json);
            Debug.Log($"[LocalSaveRepository] Save success: {filePath}");
        }
        catch(Exception e)
        {
            Debug.LogError($"[LocalSaveRepository] Save failed: {e.Message}");
            throw;
        }
    }

    public void Delete(SaveContext context)
    {
        ValidateContext(context);

        try
        {
            string filePath = GetSaveFilePath(context.UserId);
            if (File.Exists(filePath))
                File.Delete(filePath);

            Debug.Log($"[LocalSaveRepository] Save deleted: {filePath}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[LocalSaveRepository] Delete failed: {exception.Message}");
            throw;
        }
    }

    public UserSettingData LoadUserSetting()
    {
        if (!File.Exists(_settingFilePath))
            return null;

        try
        {
            string json = File.ReadAllText(_settingFilePath);
            return JsonConvert.DeserializeObject<UserSettingData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[LocalSaveRepository] User setting load failed. Default settings will be used: {exception.Message}");
            return null;
        }
    }

    public bool SaveUserSetting(UserSettingData settingData)
    {
        if (settingData == null)
            return false;

        try
        {
            string json = JsonConvert.SerializeObject(settingData, Formatting.Indented);
            File.WriteAllText(_settingFilePath, json);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[LocalSaveRepository] User setting save failed: {exception.Message}");
            return false;
        }
    }

    private string GetSaveFilePath(string userId)
    {
        return Path.Combine(_saveDirectoryPath, $"{SAVE_FILE_PREFIX}{userId}.json");
    }

    //인스턴스의 접근을 막기위해 static으로 사용
    // ex. _saveDirectoryPath 접근 막힘
    private static void Validate(SaveData saveData, SaveContext context)
    {
        if(saveData == null)
            throw new ArgumentNullException(nameof(saveData));
        
        ValidateContext(context);
    }

    private static void ValidateContext(SaveContext context)
    {
        if(context == null || !context.HasUserId)
        {
            throw new InvalidOperationException("Local save requires a signed-in user.");
        }
    }
}
