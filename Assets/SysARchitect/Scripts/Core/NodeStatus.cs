namespace SysARchitect.Core
{
    /// <summary>
    /// Operational status of an infrastructure node.
    /// </summary>
    public enum NodeStatus
    {
        /// <summary>Node is operating normally within capacity.</summary>
        Healthy,

        /// <summary>Node is approaching capacity limits.</summary>
        Warning,

        /// <summary>Node has exceeded capacity; requests are queuing/failing.</summary>
        Overloaded,

        /// <summary>Node has crashed due to a fatal condition.</summary>
        Crashed,

        /// <summary>Node is offline or not yet started.</summary>
        Offline,

        /// <summary>A restart is in progress; traffic is temporarily unavailable.</summary>
        Recovering
    }
}
