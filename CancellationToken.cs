using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Application.Core.Patterns.ActiveObject
{
    /// <summary>
    /// A cancellation token for <seealso cref="ActiveObject"/>
    /// </summary>
    public class CancellationToken
    {
        private ManualResetEvent      m_CancelSignal;
        private object                m_SyncRoot;
        private List<IActiveObject>   m_ActiveObjects;
        
        public CancellationToken()
        {
            m_CancelSignal = new ManualResetEvent(false);
            m_ActiveObjects = new List<IActiveObject>();
            m_SyncRoot = new object();
        }
        
        /// <summary>
        /// Register an active object with token
        /// </summary>
        /// <param name="activeObject"></param>
        public void Register(IActiveObject activeObject)
        {
            lock(m_SyncRoot)
            {
                m_ActiveObjects.Add(activeObject);
            }
        }

        /// <summary>
        /// Send a TERM (Calcel/Terminate) signal
        /// </summary>
        /// <param name="wait">If true, this API blocks and returns when all active 
        /// objects, the token is passed to, returns.</param>
        public void Cancel(bool wait)
        {
            m_CancelSignal.Set();
            if (wait)
            {
                lock(m_SyncRoot)
                {
                    foreach(IActiveObject ao in m_ActiveObjects)
                    {
                        ao.Shutdown();
                    }
                }
            }
        }

        /// <summary>
        /// Cancel signal
        /// </summary>
        public ManualResetEvent CancelSignal
        {
            get
            {
                return m_CancelSignal;
            }
        }
    }
}
