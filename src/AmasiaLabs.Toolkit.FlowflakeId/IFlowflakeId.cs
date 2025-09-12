namespace AmasiaLabs.Toolkit.FlowflakeId;

/// <summary>
/// Generates globally unique, time-ordered identifiers.
/// </summary>
public interface IFlowflakeId
{
    long Generate();
    long GenerateForDate(DateTime date);
    int GetInstanceId();
    int GetInstanceIdFromGlobalId(long id);
    DateTime GetDateTime(long id);
    string ToBase62(long id);
    long FromBase62(string id);
}

