using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Google;
using UnityEngine;

public enum GoogleAccountLinkResult
{
    Failed,
    Linked,
    ExistingAccount
}

public class AuthManager
{
    private const string FUNCTION_WITHDRAW_ACCOUNT = "withdrawAccount";

    private readonly FirebaseFunctionClient _functionClient = new();
    private FirebaseAuth _auth;
    private FirebaseUser _currentUser;
    private Credential _pendingGoogleCredential;
    private static bool _isGoogleSignInConfigured;
    private bool _isInitialized;
    private Task _initializeTask;

    public bool IsInitialized => _isInitialized;
    public bool IsLoggedIn => _currentUser != null;
    public bool IsGuest => _currentUser != null && _currentUser.IsAnonymous;
    public bool IsGoogleLinked => HasProvider(GoogleAuthProvider.ProviderId);
    public string UserId => _currentUser?.UserId;

    //초기화
    public Task InitializeAsync()
    {
        if(_isInitialized) return Task.CompletedTask;

        _initializeTask ??= InitializeInternalAsync();
        return _initializeTask;
    }

    private async Task InitializeInternalAsync()
    {
        await Managers.Firebase.InitializeAsync();

        if(!Managers.Firebase.IsInitialized)
        {
            Debug.LogError("[AuthManager] Firebase is not initialized.");
            return;
        }

        _auth = FirebaseAuth.DefaultInstance;
        _auth.StateChanged += OnAuthStateChanged;

        UpdateCurrentUser();
        _isInitialized = true;

        Debug.Log("[AuthManager] Auth initialized.");
    }

    

    //게스트 로그인
    public async Task SignInAsGuestAsync()
    {
        await InitializeAsync();

        if (!_isInitialized)
        {
            Debug.LogError("[AuthManager] Auth is not initialized.");
            return;
        }

        if (IsLoggedIn)
        {
            Debug.Log($"[AuthManager] Already signed in. UserId: {UserId}");
            return;
        }

        try
        {
            AuthResult authResult = await _auth.SignInAnonymouslyAsync();
            _currentUser = authResult.User;

            Debug.Log($"[AuthManager] Guest sign-in success. UserId: {UserId}");
        }
        catch (Firebase.FirebaseException exception)
        {
            Debug.LogError($"[AuthManager] Guest sign-in failed. ErrorCode: {exception.ErrorCode}, Message: {exception.Message}");
        }
    }

    //구글 로그인
    public async Task<GoogleAccountLinkResult> LinkGuestWithGoogleAsync()
    {
#if UNITY_EDITOR
        if (Application.isEditor)
        {
            Debug.LogWarning("[AuthManager] Google account linking is not supported in the Unity Editor. Test it on an Android device.");
            return GoogleAccountLinkResult.Failed;
        }
#endif

        CancelPendingGoogleAccount();
        await InitializeAsync();

        if (!_isInitialized)
        {
            Debug.LogError("[AuthManager] Auth is not initialized.");
            return GoogleAccountLinkResult.Failed;
        }

        if (!IsLoggedIn || !IsGuest)
        {
            Debug.LogWarning("[AuthManager] Current user is not guest.");
            return GoogleAccountLinkResult.Failed;
        }

        Credential credential = null;
        try
        {
            credential = await GetGoogleCredentialAsync();

            if (credential == null)
            {
                return GoogleAccountLinkResult.Failed;
            }

            AuthResult authResult = await _currentUser.LinkWithCredentialAsync(credential);
            _currentUser = authResult.User;

            Debug.Log($"[AuthManager] Guest linked with Google. UserId: {UserId}");
            return GoogleAccountLinkResult.Linked;
        }
        catch (GoogleSignIn.SignInException exception)
        {
            Debug.LogWarning($"[AuthManager] Google link failed. Status: {exception.Status}, Message: {exception.Message}");
            return GoogleAccountLinkResult.Failed;
        }
        catch (FirebaseAccountLinkException exception)
        {
            if (credential != null && IsExistingAccountError(exception.ErrorCode))
            {
                _pendingGoogleCredential = credential;
                Debug.Log("[AuthManager] The selected Google account is already linked to an existing user.");
                return GoogleAccountLinkResult.ExistingAccount;
            }

            Debug.LogError($"[AuthManager] Firebase account link failed. ErrorCode: {exception.ErrorCode}, Message: {exception.Message}");
            return GoogleAccountLinkResult.Failed;
        }
        catch (Firebase.FirebaseException exception)
        {
            if (credential != null && IsExistingAccountError(exception.ErrorCode))
            {
                _pendingGoogleCredential = credential;
                Debug.Log("[AuthManager] The selected Google account is already linked to an existing user.");
                return GoogleAccountLinkResult.ExistingAccount;
            }

            Debug.LogError($"[AuthManager] Firebase Google link failed. ErrorCode: {exception.ErrorCode}, Message: {exception.Message}");
            return GoogleAccountLinkResult.Failed;
        }
    }

    public async Task<bool> SignInWithPendingGoogleAccountAsync()
    {
        Credential credential = _pendingGoogleCredential;
        _pendingGoogleCredential = null;
        if (credential == null)
        {
            Debug.LogWarning("[AuthManager] Pending Google credential does not exist.");
            return false;
        }

        try
        {
            FirebaseUser user = await _auth.SignInWithCredentialAsync(credential);
            _currentUser = user;
            Debug.Log($"[AuthManager] Existing Google account sign-in success. UserId: {UserId}");
            return true;
        }
        catch (Firebase.FirebaseException exception)
        {
            Debug.LogError($"[AuthManager] Existing Google account sign-in failed. ErrorCode: {exception.ErrorCode}, Message: {exception.Message}");
            return false;
        }
    }

    public void CancelPendingGoogleAccount()
    {
        _pendingGoogleCredential = null;
        if (_isGoogleSignInConfigured) GoogleSignIn.DefaultInstance.SignOut();
    }

    public async Task<bool> WithdrawAsync()
    {
        await InitializeAsync();

        if (!_isInitialized || !IsLoggedIn)
        {
            Debug.LogWarning("[AuthManager] Account withdrawal requires a signed-in user.");
            return false;
        }

        AccountWithdrawalResponse response = await _functionClient.CallAsync<AccountWithdrawalResponse>(
            FUNCTION_WITHDRAW_ACCOUNT,
            new Dictionary<string, object>());
        if (response?.Success != true)
        {
            Debug.LogError("[AuthManager] Account withdrawal failed.");
            return false;
        }

        SignOut();
        Debug.Log("[AuthManager] Account withdrawal completed.");
        return true;
    }

    private async Task<Credential> GetGoogleCredentialAsync()
    {
        ConfigureGoogleSignIn();

        GoogleSignInUser googleUser = await GoogleSignIn.DefaultInstance.SignIn();

        if (googleUser == null)
        {
            Debug.LogError("[AuthManager] Google user is null.");
            return null;
        }

        if (string.IsNullOrEmpty(googleUser.IdToken))
        {
            Debug.LogError("[AuthManager] Google IdToken is empty.");
            return null;
        }

        return GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
    }

    private void ConfigureGoogleSignIn()
    {
        if (_isGoogleSignInConfigured) return;

        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId = Constants.GOOGLE_WEB_CLIENT_ID,
            RequestIdToken = true,
            RequestEmail = true,
            UseGameSignIn = false
        };

        _isGoogleSignInConfigured = true;
    }


    //공통
    public void SignOut()
    {
        if(_auth == null) return;

        _pendingGoogleCredential = null;
        _auth.SignOut();
        if (_isGoogleSignInConfigured) GoogleSignIn.DefaultInstance.SignOut();
        UpdateCurrentUser();

        Debug.Log("[AuthManager] Signed Out");
    }

    public void Dispose()
    {
        if (_auth != null)
        {
            _auth.StateChanged -= OnAuthStateChanged;
        }
    }

    private void UpdateCurrentUser()
    {
        _currentUser = _auth?.CurrentUser;
    }

    private void OnAuthStateChanged(object sender, System.EventArgs args)
    {
        UpdateCurrentUser();
    }

    private bool HasProvider(string providerId)
    {
        if (_currentUser == null || string.IsNullOrEmpty(providerId))
        {
            return false;
        }

        foreach (IUserInfo userInfo in _currentUser.ProviderData)
        {
            if (userInfo.ProviderId == providerId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExistingAccountError(int errorCode)
    {
        AuthError authError = (AuthError)errorCode;
        return authError == AuthError.CredentialAlreadyInUse || authError == AuthError.AccountExistsWithDifferentCredentials;
    }
}
