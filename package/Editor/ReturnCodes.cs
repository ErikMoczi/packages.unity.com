namespace UnityEditor.Build
{
    public enum ReturnCodes
    {
        // Success Codes are Positive!
        Success = 0,
        SuccessCached = 1,
        // Error Codes are Negative!
        Error = -1,
        Exception = -2,
        Canceled = -3,
        UnsavedChanges = -4,
        MissingRequiredObjects = -5
    }
}
