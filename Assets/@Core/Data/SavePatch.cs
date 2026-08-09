using System.Collections.Generic;
using System.Text;

public enum SaveField
{
    None = 0,
    SelectedStageId = 2,
    ClearedStageIds = 3,
    NoaLevels = 4,
    BlessingLevels = 5,
    UnlockedNoaIds = 6,
    UserData = 7,
    BgmEnabled = 8,
    SfxEnabled = 9,
    VibrationEnabled = 10,
    BlessingPieceCounts = 11,
    BestFlows = 12
}

public enum SaveValueOperation
{
    Set,
    Increment,
    AddToSet
}

public class SaveChange
{
    public SaveField Field { get; }
    public SaveValueOperation Operation { get; }
    public object Value { get; }

    public SaveChange(SaveField field, SaveValueOperation operation, object value)
    {
        Field = field;
        Operation = operation;
        Value = value;
    }
}

public class SavePatch
{
    private readonly Dictionary<SaveField, SaveChange> _changeByField = new();

    public IEnumerable<SaveChange> Changes => _changeByField.Values;
    public bool IsEmpty => _changeByField.Count == 0;

    public SavePatch Set<T>(SaveField field, T value)
    {
        SetChange(field, SaveValueOperation.Set, value);
        return this;
    }

    public SavePatch Increment<T>(SaveField field, T value)
    {
        SetChange(field, SaveValueOperation.Increment, value);
        return this;
    }

    public SavePatch AddToSet<T>(SaveField field, T value)
    {
        SetChange(field, SaveValueOperation.AddToSet, value);
        return this;
    }

    public SavePatch CreateSnapshot()
    {
        SavePatch snapshot = new();

        foreach (SaveChange change in _changeByField.Values)
        {
            snapshot._changeByField.Add(change.Field, new SaveChange(change.Field, change.Operation, change.Value));
        }

        return snapshot;
    }

    public string GetDebugSummary()
    {
        if (IsEmpty) return "<empty>";

        StringBuilder summary = new();

        foreach (SaveChange change in _changeByField.Values)
        {
            if (summary.Length > 0)
            {
                summary.Append(", ");
            }

            summary.Append(change.Field)
                .Append(':')
                .Append(change.Operation)
                .Append('(')
                .Append(SaveFieldSchema.GetSafeLogValue(change))
                .Append(')');
        }

        return summary.ToString();
    }

    private void SetChange<T>(SaveField field, SaveValueOperation operation, T value)
    {
        _changeByField[field] = new SaveChange(field, operation, value);
    }
}

public static class SaveFieldSchema
{
    public static string GetValidationError(SavePatch patch)
    {
        foreach (SaveChange change in patch.Changes)
        {
            string validationError = GetValidationError(change);
            if (validationError != null) return validationError;
        }

        return null;
    }

    public static void Apply(SaveData saveData, SavePatch patch)
    {
        foreach (SaveChange change in patch.Changes)
        {
            switch (change.Field)
            {
                case SaveField.SelectedStageId:
                    saveData.SelectedStageId = (int)change.Value;
                    break;
                case SaveField.ClearedStageIds:
                    ApplyClearedStageId(saveData, change);
                    break;
                case SaveField.BestFlows:
                    saveData.BestFlows = new Dictionary<string, int>((Dictionary<string, int>)change.Value);
                    break;
                case SaveField.NoaLevels:
                    saveData.NoaLevels = new Dictionary<string, int>((Dictionary<string, int>)change.Value);
                    break;
                case SaveField.BlessingLevels:
                    saveData.BlessingLevels = new Dictionary<string, int>((Dictionary<string, int>)change.Value);
                    break;
                case SaveField.BlessingPieceCounts:
                    saveData.BlessingPieceCounts = new Dictionary<string, int>((Dictionary<string, int>)change.Value);
                    break;
                case SaveField.UnlockedNoaIds:
                    saveData.UnlockedNoaIds = new List<string>((List<string>)change.Value);
                    break;
                case SaveField.UserData:
                    saveData.User = ((UserData)change.Value).CreateSnapshot();
                    break;
                case SaveField.BgmEnabled:
                    saveData.IsBgmEnabled = (bool)change.Value;
                    break;
                case SaveField.SfxEnabled:
                    saveData.IsSfxEnabled = (bool)change.Value;
                    break;
                case SaveField.VibrationEnabled:
                    saveData.IsVibrationEnabled = (bool)change.Value;
                    break;
            }
        }
    }

    public static string GetSafeLogValue(SaveChange change)
    {
        switch (change.Field)
        {
            case SaveField.SelectedStageId:
            case SaveField.ClearedStageIds:
            case SaveField.BestFlows:
            case SaveField.NoaLevels:
            case SaveField.BlessingLevels:
            case SaveField.BlessingPieceCounts:
            case SaveField.UnlockedNoaIds:
                return change.Value?.ToString() ?? "null";
            default:
                return "<hidden>";
        }
    }

    private static string GetValidationError(SaveChange change)
    {
        switch (change.Field)
        {
            case SaveField.SelectedStageId:
                if (change.Value is not int)
                {
                    return $"[SavePatch] {SaveField.SelectedStageId} requires an Int32 value.";
                }

                return change.Operation == SaveValueOperation.Set
                    ? null
                    : $"[SavePatch] {change.Operation} is not supported for {SaveField.SelectedStageId}.";
            case SaveField.ClearedStageIds:
                if (change.Value is not int)
                {
                    return $"[SavePatch] {SaveField.ClearedStageIds} requires an Int32 value.";
                }

                return change.Operation == SaveValueOperation.AddToSet
                    ? null
                    : $"[SavePatch] {change.Operation} is not supported for {SaveField.ClearedStageIds}.";
            case SaveField.BestFlows:
                if (change.Value is not Dictionary<string, int>)
                    return $"[SavePatch] {SaveField.BestFlows} requires a Dictionary<string, int> value.";

                return change.Operation == SaveValueOperation.Set
                    ? null
                    : $"[SavePatch] {change.Operation} is not supported for {SaveField.BestFlows}.";
            case SaveField.NoaLevels:
                if (change.Value is not Dictionary<string, int>)
                    return $"[SavePatch] {SaveField.NoaLevels} requires a Dictionary<string, int> value.";

                return change.Operation == SaveValueOperation.Set
                    ? null
                    : $"[SavePatch] {change.Operation} is not supported for {SaveField.NoaLevels}.";
            case SaveField.BlessingLevels:
                if (change.Value is not Dictionary<string, int>)
                    return $"[SavePatch] {SaveField.BlessingLevels} requires a Dictionary<string, int> value.";

                return change.Operation == SaveValueOperation.Set
                    ? null
                    : $"[SavePatch] {change.Operation} is not supported for {SaveField.BlessingLevels}.";
            case SaveField.BlessingPieceCounts:
                if (change.Value is not Dictionary<string, int>)
                    return $"[SavePatch] {SaveField.BlessingPieceCounts} requires a Dictionary<string, int> value.";

                return change.Operation == SaveValueOperation.Set
                    ? null
                    : $"[SavePatch] {change.Operation} is not supported for {SaveField.BlessingPieceCounts}.";
            case SaveField.UnlockedNoaIds:
                if (change.Value is not List<string>)
                    return $"[SavePatch] {SaveField.UnlockedNoaIds} requires a List<string> value.";

                return change.Operation == SaveValueOperation.Set
                    ? null
                    : $"[SavePatch] {change.Operation} is not supported for {SaveField.UnlockedNoaIds}.";
            case SaveField.UserData:
                if (change.Value is not UserData)
                    return $"[SavePatch] {SaveField.UserData} requires a UserData value.";

                return change.Operation == SaveValueOperation.Set
                    ? null
                    : $"[SavePatch] {change.Operation} is not supported for {SaveField.UserData}.";
            case SaveField.BgmEnabled:
            case SaveField.SfxEnabled:
            case SaveField.VibrationEnabled:
                if (change.Value is not bool)
                    return $"[SavePatch] {change.Field} requires a Boolean value.";

                return change.Operation == SaveValueOperation.Set
                    ? null
                    : $"[SavePatch] {change.Operation} is not supported for {change.Field}.";
            default:
                return $"[SavePatch] Unsupported save field: {change.Field}.";
        }
    }

    private static void ApplyClearedStageId(SaveData saveData, SaveChange change)
    {
        int stageId = (int)change.Value;
        if (!saveData.ClearedStageIds.Contains(stageId))
        {
            saveData.ClearedStageIds.Add(stageId);
        }
    }

}
