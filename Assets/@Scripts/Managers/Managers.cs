using System;
using UnityEngine;

public class Managers : MonoBehaviour
{
    #region core
    private readonly CoroutineManager _coroutine = new CoroutineManager();
    private readonly UIManager _ui = new UIManager();
    private readonly ResourceManager _resource = new ResourceManager();
    private readonly SoundManager _sound = new SoundManager();
    private readonly FirebaseManager _firebase = new FirebaseManager();
    private readonly AuthManager _auth = new AuthManager();
    private readonly DataManager _data = new DataManager();
    private readonly ProfileManager _profile = new ProfileManager();
    private readonly SceneManager _scene = new SceneManager();
    private readonly ContentIconManager _contentIcon = new ContentIconManager();
    private readonly GameDataManager _gameData = new GameDataManager();
    private readonly CurrencyManager _currency = new CurrencyManager();
    private readonly CollectionManager _collection = new CollectionManager();
    private readonly PurifyManager _purify = new PurifyManager();
    private readonly RewardManager _reward = new RewardManager();
    private readonly UserSettingManager _userSetting = new UserSettingManager();

    public static CoroutineManager Coroutine => Instance._coroutine;
    public static UIManager UI => Instance._ui;
    public static ResourceManager Resource => Instance._resource;
    public static SoundManager Sound => Instance._sound;
    public static FirebaseManager Firebase => Instance._firebase;
    public static AuthManager Auth => Instance._auth;
    public static DataManager Data => Instance._data;
    public static ProfileManager Profile => Instance._profile;
    public static SceneManager Scene => Instance._scene;
    public static ContentIconManager ContentIcon => Instance._contentIcon;
    public static GameDataManager GameData => Instance._gameData;
    public static CurrencyManager Currency => Instance._currency;
    public static CollectionManager Collection => Instance._collection;
    public static PurifyManager Purify => Instance._purify;
    public static RewardManager Reward => Instance._reward;
    public static UserSettingManager UserSetting => Instance._userSetting;
    
    #endregion

    #region content

    

    #endregion
    
    private static Managers _instance;
    private static bool _isInitialized;

    public static Managers Instance
    {
        get
        {
            if(!_isInitialized)
                Init();

            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _isInitialized = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        if(_isInitialized) return;

        if (!Application.isPlaying)
        {
            return;
        }

        GameObject go = GameObject.Find("@Managers");
        if (go == null)
        {
            go = new GameObject { name = "@Managers" };
        }
        _instance = go.GetComponent<Managers>();
        if(_instance == null)
        {
            _instance = go.AddComponent<Managers>();
        }

        _isInitialized = true;
        DontDestroyOnLoad(go);

        try
        {
            InitForce();
        }
        catch
        {
            _instance = null;
            _isInitialized = false;
            throw;
        }
    }

    private static void InitForce()
    {
        //Managers Init
        _instance._userSetting.Initialize();
        _instance._sound.Init();
        UserSettingData settingData = _instance._userSetting.CurrentSetting;
        _instance._sound.ApplySettings(settingData.IsBgmEnabled, settingData.IsSfxEnabled);
        _instance._ui.Init();
    }

    private DateTime _lastPauseTime = DateTime.MinValue;
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("앱이 일시 중지됨 (백그라운드)");
            _lastPauseTime = DateTime.Now;
        }
        else
        {
            Debug.Log("앱이 재개됨 (포그라운드)");
            if (_lastPauseTime != DateTime.MinValue)
            {
                TimeSpan elapsedTime = DateTime.Now - _lastPauseTime;
                Debug.Log($"앱이 일시 중지된 시간: {elapsedTime.TotalSeconds}초");
            }
        }
    }

    private void OnDestroy()
    {
        _currency.Dispose();
        _contentIcon.Dispose();
        _auth.Dispose();
        _sound.Destroy();
    }
        

}
