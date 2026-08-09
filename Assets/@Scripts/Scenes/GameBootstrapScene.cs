using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GameBootstrapScene : MonoBehaviour
{
    private class InitializationStep
    {
        public string Name { get; }
        public Func<Task> ExecuteAsync { get; }

        public InitializationStep(string name, Func<Task> executeAsync)
        {
            Name = name;
            ExecuteAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        }
    }
    
    [SerializeField] private Slider sliderLoading;

    private readonly List<InitializationStep> _initSteps = new();
    private bool _isDestroyed;

    private async void Start()
    {
        Application.targetFrameRate = 60;
        
        RegisterInitializationSteps();
        SetProgress(0f);

        try
        {
            await ExecuteInitializationStepsAsync();

            if (_isDestroyed)
            {
                return;
            }

            Debug.Log("[GameBootstrap] Ready to load the Home scene.");

            Managers.Scene.LoadScene(Define.Scene.Home);
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("[GameBootstrap] Initialization was canceled.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[GameBootstrap] Initialization failed: {exception}");
        }
    }

    private void RegisterInitializationSteps()
    {
        _initSteps.Clear();

        _initSteps.Add(new InitializationStep("Firebase", InitializeFirebaseAsync));
        _initSteps.Add(new InitializationStep("Authentication", InitializeAuthAsync));
        _initSteps.Add(new InitializationStep("Guest Sign-In", SignInAsync));
        _initSteps.Add(new InitializationStep("Data", InitializeDataAsync));
        _initSteps.Add(new InitializationStep("Save Data", LoadSaveDataAsync));
        _initSteps.Add(new InitializationStep("BGM", PreloadBgmAsync));
        _initSteps.Add(new InitializationStep("SFX", PreloadSfxAsync));
        _initSteps.Add(new InitializationStep("Game SO Data", InitializeGameDataAsync));
        _initSteps.Add(new InitializationStep("Profile", InitializeProfileAsync));
        _initSteps.Add(new InitializationStep("Currency", InitializeCurrencyAsync));
        _initSteps.Add(new InitializationStep("Content Icon Atlases", PreloadContentIconAtlasesAsync));
    }

    private async Task ExecuteInitializationStepsAsync()
    {
        int totalStepCount = _initSteps.Count;
        if (totalStepCount == 0)
        {
            SetProgress(1f);
            return;
        }

        for (int index = 0; index < totalStepCount; index++)
        {
            if (_isDestroyed)
            {
                return;
            }

            InitializationStep step = _initSteps[index];
            Debug.Log($"[GameBootstrap] Initialization started: {step.Name}");

            await step.ExecuteAsync.Invoke();

            if (_isDestroyed)
            {
                return;
            }

            SetProgress((index + 1f) / totalStepCount);
            Debug.Log($"[GameBootstrap] Initialization completed: {step.Name}");
        }

        Debug.Log(
            $"[GameBootstrap] Initialization complete. " +
            $"UserId: {Managers.Auth.UserId}, " +
            $"IsGuest: {Managers.Auth.IsGuest}, " +
            $"IsGoogleLinked: {Managers.Auth.IsGoogleLinked}");
    }

    private async Task InitializeFirebaseAsync()
    {
        await Managers.Firebase.InitializeAsync();
        if (!Managers.Firebase.IsInitialized)
        {
            throw new InvalidOperationException("Firebase initialization failed.");
        }
    }

    private async Task InitializeAuthAsync()
    {
        await Managers.Auth.InitializeAsync();
        if (!Managers.Auth.IsInitialized)
        {
            throw new InvalidOperationException("Authentication initialization failed.");
        }
    }

    private async Task SignInAsync()
    {
        await Managers.Auth.SignInAsGuestAsync();
        if (!Managers.Auth.IsLoggedIn)
        {
            throw new InvalidOperationException("Guest sign-in failed.");
        }
    }

    private Task InitializeDataAsync()
    {
        Managers.Data.Initialize();
        return Task.CompletedTask;
    }

    private async Task LoadSaveDataAsync()
    {
        await Managers.Data.LoadAsync();
    }

    private Task PreloadBgmAsync()
    {
        return Managers.Sound.PreloadBgmAsync();
    }

    private Task PreloadSfxAsync()
    {
        return Managers.Sound.PreloadSfxAsync();
    }

    private Task InitializeProfileAsync()
    {
        return Managers.Profile.InitializeAsync();
    }

    private Task InitializeGameDataAsync()
    {
        return Managers.GameData.InitializeAsync();
    }

    private Task InitializeCurrencyAsync()
    {
        return Managers.Currency.InitializeAsync();
    }

    private Task PreloadContentIconAtlasesAsync()
    {
        return Task.WhenAll(
            Managers.ContentIcon.PreloadAsync(Define.ContentIconType.Noa),
            Managers.ContentIcon.PreloadAsync(Define.ContentIconType.Blessing),
            Managers.ContentIcon.PreloadAsync(Define.ContentIconType.CommonUI));
    }

    private void OnDestroy()
    {
        _isDestroyed = true;
    }

    private void SetProgress(float progress)
    {
        if (_isDestroyed || sliderLoading == null)
        {
            return;
        }
        
        sliderLoading.SetValueWithoutNotify(Mathf.Clamp01(progress));
    }
}
