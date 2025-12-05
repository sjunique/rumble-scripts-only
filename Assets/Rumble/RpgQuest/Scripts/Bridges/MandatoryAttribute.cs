using System;

namespace RpgQuest.Utilities
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class MandatoryAttribute : Attribute
    {
        public string CustomMessage { get; set; }
        
        public MandatoryAttribute() { }
        
        public MandatoryAttribute(string customMessage)
        {
            CustomMessage = customMessage;
        }
    }
}


