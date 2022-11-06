﻿namespace Pancake
{
    public readonly struct ValidationResult
    {
        public static ValidationResult Valid => new ValidationResult(true, null, EMessageType.None);

        public ValidationResult(bool valid, string message, EMessageType messageType)
        {
            IsValid = valid;
            Message = message;
            MessageType = messageType;
        }

        public bool IsValid { get; }
        public string Message { get; }
        public EMessageType MessageType { get; }

        public static ValidationResult Error(string error) { return new ValidationResult(false, error, EMessageType.Error); }

        public static ValidationResult Warning(string error) { return new ValidationResult(false, error, EMessageType.Warning); }
    }

    public enum EMessageType
    {
        None,
        Info,
        Warning,
        Error,
    }
}