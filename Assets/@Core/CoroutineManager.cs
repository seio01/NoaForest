using System.Collections;
using UnityEngine;

public class CoroutineManager
{
    private MonoBehaviour _mono;

    private void Init()
    {
        _mono = Managers.Instance;
    }

    public Coroutine StartCoroutine(IEnumerator coroutine)
    {
        if (_mono == null)
        {
            Init();
        }
        return _mono.StartCoroutine(coroutine);
    }

    public Coroutine StartDelayed(float seconds, IEnumerator routine)
    {
        return StartCoroutine(DelayThen(seconds, routine));
    }

    private IEnumerator DelayThen(float seconds, IEnumerator routine)
    {
        if (seconds > 0f)
        {
            yield return new WaitForSeconds(seconds);
        }

        yield return routine;
    }

    public void StopCoroutine(Coroutine coroutine)
    {
        _mono?.StopCoroutine(coroutine);
    }

    public void StopAll()
    {
        _mono?.StopAllCoroutines();
    }
}

