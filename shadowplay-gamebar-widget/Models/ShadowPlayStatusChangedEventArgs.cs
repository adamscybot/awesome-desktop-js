using System;

namespace ShadowPlayReminderWidget.Models
{
    public sealed class ShadowPlayStatusChangedEventArgs : EventArgs
    {
        public ShadowPlayStatusChangedEventArgs(
            ShadowPlayState state,
            string message,
            ShadowPlayTrigger trigger,
            DateTimeOffset checkedAt,
            Exception error,
            bool stateChanged)
        {
            State = state;
            Message = message;
            Trigger = trigger;
            CheckedAt = checkedAt;
            Error = error;
            StateChanged = stateChanged;
        }

        public ShadowPlayState State { get; }

        public string Message { get; }

        public ShadowPlayTrigger Trigger { get; }

        public DateTimeOffset CheckedAt { get; }

        public Exception Error { get; }

        public bool StateChanged { get; }
    }
}
