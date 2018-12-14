namespace UnityEngine.Experimental.Localization.Samples
{
    /// <summary>
    /// This example shows how to create a LocaleIdentifier using the value 9, a recognized Microsoft LCID value for English.
    /// </summary>
    public class CreatingLocaleIdentifierExample : MonoBehaviour
    {
        void Start()
        {
            // Microsoft LCID code 9 is English.
            var lcid = new LocaleIdentifier(9);
            var code = new LocaleIdentifier("en");
        
            Debug.Log("LCID 9 and code 'en' are the same: " + (lcid == code));
        }
    }
}