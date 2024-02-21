using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Kesa.Japanese.Common;

public class KeyedDebounceAction
{
    private readonly object _versionLock;
    private int _version;

    public int Version => _version;

    public Timer Timer { get; set; }

    public Action<KeyedDebounceActionContext> Action { get; set; }

    public KeyedDebounceAction()
    {
        _versionLock = new object();
    }

    public void Execute(object _)
    {
        var version = -1;

        lock (_versionLock)
        {
            version = ++_version;
        }

        Action(new(this, version));
    }

    public void IncrementVersion() => ++_version;
}

public record struct KeyedDebounceActionContext(KeyedDebounceAction Action, int InitialVersion)
{
    public readonly bool IsActive => Action.Version == InitialVersion;
}

public class KeyedDebounce
{
    private readonly Dictionary<string, KeyedDebounceAction> _actions = [];

    public void Execute(string key, int delay, Action<KeyedDebounceActionContext> action)
    {
        var item = GetOrCreateAction(key);
        item.Action = action;
        item.Timer.Change(delay, -1);
    }

    public void Cancel(string key)
    {
        var action = GetOrCreateAction(key);
        action.Timer.Change(-1, -1);
        action.IncrementVersion();
    }

    private KeyedDebounceAction GetOrCreateAction(string key)
    {
        KeyedDebounceAction item;

        lock (_actions)
        {
            ref var result = ref CollectionsMarshal.GetValueRefOrAddDefault(_actions, key, out var found);

            if (found)
            {
                item = result;
            }
            else
            {
                item = new KeyedDebounceAction();
                result = item;
                result.Timer = new Timer(item.Execute, null, -1, -1);
            }
        }

        return item;
    }
}