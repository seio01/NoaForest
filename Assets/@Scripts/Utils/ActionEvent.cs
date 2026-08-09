using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionEvent
{
    private event Action _handlers;

    public void AddListener(Action action) => _handlers += action;
    public void RemoveListener(Action action) => _handlers -= action;
    public void RemoveAllListeners() => _handlers = null;
    public void Invoke() => _handlers?.Invoke();
}

