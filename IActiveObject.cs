using Application.Core.Patterns;

namespace Application.Core.Patterns.ActiveObject
{
    /// <summary>
    /// Active Object (runnable) interface
    /// </summary>
    public interface IActiveObject
    {
        /// <summary>
        /// Initialize an active object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        void Initialize(string name, 
                        Action action);

        /// <summary>
        /// Initialize an active object with a timeout elapse event trigger
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <param name="timeout"></param>
        void Initialize(string name, Action action, int timeout);

        /// <summary>
        /// Initialize an active object with a externally supplied cancellation token
        /// </summary>
        /// <param name="name">Name of the active object</param>
        /// <param name="action">Action delegate to perform</param>
        /// <param name="cancellationToken">A cancellation token which is used to send a single TERM signal
        /// to a group of active objects</param>
        void Initialize(string name, 
                        Action action, 
                        CancellationToken cancellationToken);

        /// <summary>
        /// Signal the active object to perform its loop action.
        /// </summary>
        /// <remarks>
        /// Application may call this after some simple or complex condition evaluation
        /// </remarks>
        void Signal();

        /// <summary>
        /// Signals to shotdown this active object
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Used to enqueue pending actions
        /// </summary>
        /// <param name="action"></param>
        void Enqueue(Action action);

        /// <summary>
        /// Returns true when the active object is busy
        /// </summary>
        bool Busy
        {
            get;
        }

        /// <summary>
        /// Opaque state injected into this active object
        /// </summary>
        object State
        {
            get;
            set;
        }

        /// <summary>
        /// External sync root for consumers
        /// </summary>
        object SyncRoot
        {
            get;
        }
    }
}