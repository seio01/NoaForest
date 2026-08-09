using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class SoundManager
{
    private const string SOUND_ROOT = "@Sound_Root";
    private const string BGM_PATH = "Sounds/Bgm";
    private const float BGM_VOLUME = 0.7f;
    private const float SFX_VOLUME = 0.5f;
    private static readonly int _audioSourceTypeCount = System.Enum.GetValues(typeof(Define.AudioSourceType)).Length;

    private AudioSource[] _audioSource = new AudioSource[_audioSourceTypeCount];
    private AudioClip _currentBGMCLip;
    private Coroutine _delayedRoutine;
    private Tween _bgmTween;
    private bool _isBgmEnabled = true;
    private bool _isSfxEnabled;

    public void Init()
    {
        var soundRoot = GameObject.Find(SOUND_ROOT);
        if (soundRoot == null)
        {
            soundRoot = new GameObject(SOUND_ROOT);
            Object.DontDestroyOnLoad(soundRoot);
        }

        string[] sourceTypes = System.Enum.GetNames(typeof(Define.AudioSourceType));
        for (int i = 0; i < sourceTypes.Length; i++)
        {
            Transform sourceTransform = soundRoot.transform.Find(sourceTypes[i]);
            if (!sourceTransform)
            {
                GameObject go = new GameObject(sourceTypes[i]);
                go.transform.SetParent(soundRoot.transform, false);
                sourceTransform = go.transform;
            }

            _audioSource[i] = Utils.GetorAddComponent<AudioSource>(sourceTransform.gameObject);
        }

        ApplySourceVolumes();
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public void ApplySettings(bool isBgmEnabled, bool isSfxEnabled)
    {
        _isBgmEnabled = isBgmEnabled;
        _isSfxEnabled = isSfxEnabled;
        _bgmTween?.Kill();
        ApplySourceVolumes();
    }

    private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
    {
        //씬 전환시 코루틴 정리
        if(_delayedRoutine != null)
        {
            Managers.Coroutine.StopCoroutine(_delayedRoutine);
            _delayedRoutine = null;
        }
    }

    public void Destroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    public void Play(Define.AudioClip sourceName, Define.AudioSourceType sourceType, Define.AudioPath path = Define.AudioPath.None, float volumeScale = 1f)
    {
        PlaySound(sourceName, sourceType, path, volumeScale);
    }

    public Task PreloadBgmAsync()
    {
        return PreloadAudioPathAsync(BGM_PATH);
    }

    public async Task PreloadSfxAsync()
    {
        foreach (Define.AudioPath audioPath in System.Enum.GetValues(typeof(Define.AudioPath)))
        {
            if (audioPath == Define.AudioPath.None) continue;
            await PreloadAudioPathAsync($"Sounds/{Define.AudioSourceType.Sfx}/{audioPath}");
        }
    }

    public void DelayedPlay(Define.AudioClip sourceName, Define.AudioSourceType sourceType, Define.AudioPath path, float delayedTime)
    {
        if(_delayedRoutine != null)
        {
            Managers.Coroutine.StopCoroutine(_delayedRoutine);
        }

        _delayedRoutine = Managers.Coroutine.StartCoroutine(DelayedPlayRoutine(sourceName, sourceType, path, delayedTime));
    }

    private IEnumerator DelayedPlayRoutine(Define.AudioClip sourceName, Define.AudioSourceType sourceType, Define.AudioPath path, float delayedTime)
    {
        yield return new WaitForSeconds(delayedTime);

        PlaySound(sourceName, sourceType, path);
    }

    public void Stop(Define.AudioSourceType sourceType)
    {
        _audioSource[(int)sourceType]?.Stop();
    }

    public void StopAll()
    {
        foreach(var source in _audioSource)
        {
            source?.Stop();
        }
    }

    private void PlaySound(Define.AudioClip sourceName, Define.AudioSourceType sourceType, Define.AudioPath audioPath, float volumeScale = 1f)
    {
        if (sourceType != Define.AudioSourceType.Bgm && !_isSfxEnabled)
            return;

        //TODO 사운드 세팅 반영, 효과음/배경음

        var path = audioPath == Define.AudioPath.None ? $"Sounds/{sourceType}/{sourceName}" : $"Sounds/{sourceType}/{audioPath}/{sourceName}";
        //실제 clip load 부분
        Managers.Resource.LoadAsync<AudioClip>(path, (audioClip) =>
        {
            AudioSource source = _audioSource[(int)sourceType];

            if(audioClip == null || source == null) return;
            switch(sourceType)
            {
                case Define.AudioSourceType.Bgm:
                    if(_currentBGMCLip == audioClip && source.isPlaying) return;
                    _bgmTween?.Kill();
                    TransitionBgm(audioClip, source);
                    break;
                case Define.AudioSourceType.Sfx:
                    SetVolume(_isSfxEnabled ? SFX_VOLUME : 0f, sourceType);
                    source.PlayOneShot(audioClip, Mathf.Max(0f, volumeScale));
                    break;
                case Define.AudioSourceType.LoopSfx:
                    SetVolume(_isSfxEnabled ? SFX_VOLUME : 0f, sourceType);
                    source.clip = audioClip;
                    source.loop = true;
                    source.Play();
                    break;
            }
        });
    }

    private async Task PreloadAudioPathAsync(string path)
    {
        AudioClip[] audioClips = Managers.Resource.LoadAll<AudioClip>(path);
        if (audioClips == null || audioClips.Length == 0)
            throw new System.InvalidOperationException($"[SoundManager] No audio clips found: {path}");

        foreach (AudioClip audioClip in audioClips)
        {
            if (audioClip && audioClip.loadState == AudioDataLoadState.Unloaded && !audioClip.LoadAudioData())
                throw new System.InvalidOperationException($"[SoundManager] Failed to start loading audio data: {path}/{audioClip.name}");
        }

        while (HasLoadingAudioClip(audioClips))
            await Task.Yield();

        foreach (AudioClip audioClip in audioClips)
        {
            if (!audioClip || audioClip.loadState != AudioDataLoadState.Loaded)
                throw new System.InvalidOperationException($"[SoundManager] Failed to preload audio data: {path}/{audioClip?.name}");
        }

        Debug.Log($"[SoundManager] Audio preload completed: {path}, Count: {audioClips.Length}");
    }

    private bool HasLoadingAudioClip(AudioClip[] audioClips)
    {
        foreach (AudioClip audioClip in audioClips)
        {
            if (audioClip && audioClip.loadState == AudioDataLoadState.Loading)
                return true;
        }

        return false;
    }

    private void SetVolume(float volume, Define.AudioSourceType sourceType)
    {
        AudioSource source = _audioSource[(int)sourceType];
        if (source)
            source.volume = volume;
    }

    private void ApplySourceVolumes()
    {
        SetVolume(_isBgmEnabled ? BGM_VOLUME : 0f, Define.AudioSourceType.Bgm);
        SetVolume(_isSfxEnabled ? 1f : 0f, Define.AudioSourceType.Sfx);
        SetVolume(_isSfxEnabled ? 1f : 0f, Define.AudioSourceType.LoopSfx);

        if (!_isSfxEnabled)
        {
            _audioSource[(int)Define.AudioSourceType.Sfx]?.Stop();
            _audioSource[(int)Define.AudioSourceType.LoopSfx]?.Stop();
        }
    }

    private void TransitionBgm(AudioClip clip, AudioSource source, float duration = 0.25f)
    {
        _bgmTween?.Kill();
        
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (source.isPlaying)
        {
            seq.Append(source.DOFade(0f, duration));
            seq.AppendCallback(() => {
                //clip swap
                _currentBGMCLip = clip;
                source.Stop();
                source.clip = clip;
                source.Play();
            });
            seq.Append(source.DOFade(_isBgmEnabled ? BGM_VOLUME : 0f, duration));
        }
        else
        {
            _currentBGMCLip = clip;
            source.clip = clip;
            source.volume = 0f;
            source.loop = true;
            source.Play();
            seq.Append(source.DOFade(_isBgmEnabled ? BGM_VOLUME : 0f, duration));
        }

        _bgmTween = seq;
    }

}
