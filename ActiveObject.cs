using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Application.Core.Patterns;

namespace Application.Core.Patterns.ActiveObject
{
    /// <summary>
    /// Implements a simple active object pattern implementation
    /// </summary>
    /// <remarks>
    /// Although there exists a vast number of active objects patterns (in Java they are just "runnable")
    /// scattered, one of the best I found is located at http://blog.gurock.com/wp-content/uploads/2008/01/activeobjects.pdf
    /// </remarks>
    public class ActiveObject : IActiveObject
    {
        /// <summary>
        /// Name of this active object
        /// </summary>
        private string m_Name;
        
        /// <summary>
        /// Underlying active thread
        /// </summary>
        private Thread m_ActiveThreadContext;

        /// <summary>
        /// Abstracted action that the active thread executes
        /// </summary>
        private Action m_ActiveAction;

        /// <summary>
        /// Primary signal object for this active thread.
        /// See the Signal() method for more.
        /// </summary>
        private ManualResetEvent m_SignalObject;

        /// <summary>
        /// Signal object for shutting down this active object
        /// </summary>
        private ManualResetEvent m_ShutdownEvent;

        /// <summary>
        /// A cancellation token
        /// </summary>
        private CancellationToken m_CancellationToken;

        /// <summary>
        /// Interal array of signal objects combining primary signal object and 
        /// shutdown signal object
        /// </summary>
        private WaitHandle[] m_SignalObjects;

        /// <summary>
        /// A queue of pending actions
        /// </summary>
        private Queue<Action> m_ActionQueue;

        /// <summary>
        /// Guard lock for queue
        /// </summary>
        private object m_QueueLock;

        /// <summary>
        /// Timeout elapse interval
        /// </summary>
        private int m_Timeout;
        
        /// <summary>
        /// Injected state for this active object
        /// </summary>
        private object m_State;

        /// <summary>
        /// External syncroot for consumers of this active object
        /// </summary>
        private object m_SyncRoot;


        public object SyncRoot
        {
            get 
            {
                return m_SyncRoot;
            }
        }

        public object State
        {
            get 
            {
                return m_State;
            }
            set 
            {
                m_State = value;
            }
        }
        
        public ActiveObject()
        {
            m_SyncRoot = new object();
        }

        public void Initialize(string name, Action action)
        {
            Initialize(name, action, Timeout.Infinite);
        }
        
        public void Initialize(string name, Action action, int timeout)
        {
            m_CancellationToken = null;
            
            m_Timeout = timeout;
            m_Name = name;
            m_ActiveAction = action;
            m_SignalObject = new ManualResetEvent(false);
            m_ShutdownEvent = new ManualResetEvent(false);
            m_SignalObjects = new WaitHandle[]
                                {
                                    m_ShutdownEvent,
                                    m_SignalObject
                                };
            m_ActionQueue = new Queue<Action>();
            m_QueueLock = new object();

            m_ActiveThreadContext = new Thread(Run);
            m_ActiveThreadContext.Name = string.Concat("ActiveObject.", m_Name);
            m_ActiveThreadContext.Start();
        }

        public void Initialize(string name, Action action, CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
            {
                throw new ArgumentNullException("Cancellation Token can't be NULL");
            }
            
            m_CancellationToken = cancellationToken;
            m_CancellationToken.Register(this);
            
            m_Name = name;
            m_ActiveAction = action;
            m_SignalObject = new ManualResetEvent(false);
            m_SignalObjects = new WaitHandle[]
                                {
                                    m_CancellationToken.CancelSignal,
                                    m_SignalObject
                                };
            m_ActionQueue = new Queue<Action>();
            m_QueueLock = new object();

            m_ActiveThreadContext = new Thread(Run);
            m_ActiveThreadContext.Name = string.Concat("ActiveObject.", m_Name);
            m_ActiveThreadContext.Start();
        }
        
        private bool Guard()
        {
            int index = WaitHandle.WaitAny(m_SignalObjects, m_Timeout, false);
            return (index == 1 || index == WaitHandle.WaitTimeout) ? true : false;
        }
        
        /// <summary>
        /// Signal the active object to perform its loop action.
        /// </summary>
        /// <remarks>
        /// Application may call this after some simple of complex condition evaluation
        /// </remarks>
        public void Signal()
        {
            m_SignalObject.Set();
        }
        
        public void Enqueue(Action action)
        {
            lock(m_QueueLock)
            {
                m_ActionQueue.Enqueue(action);
            }
        }
        
        /// <summary>
        /// Signals to shotdown this active object
        /// </summary>
        public void Shutdown()
        {
            //Following makes sense only when an external cancellation token is not supplied
            if (m_CancellationToken == null)
            {
                m_ShutdownEvent.Set();
            }

            if (m_ActiveThreadContext != null)
            {
                m_ActiveThreadContext.Join();
            }

            m_ActiveThreadContext = null;
        }
        
        public bool Busy
        {
            get
            {
                //Pulse check
                return m_SignalObject.WaitOne(0, false);
            }
        }
        
        /// <summary>
        /// Core run method of this active thread
        /// </summary>
        private void Run()
        {
            try
            {
                while (Guard())
                {
                    try
                    {
                        try
                        {
                            m_ActiveAction();
                        }
                        catch (Exception ex)
                        {
                            /*
                             Logger.Write(new LogData(string.Format("ActiveObject::Run. exec -> m_ActiveAction(): {0}, Error: {1}",
                                                       m_Name,
                                                       ex.Message),
                                     Component.Patterns,
                                     LogLevel.Error));
                            */         
                        }
                        
                        //Before going into wait, check whether we have any pending 
                        int queueCount = 0;
                        Action queuedAction = null;
                        do
                        {
                            lock (m_QueueLock)
                            {
                                queueCount = m_ActionQueue.Count;
                                if (queueCount == 0)
                                {
                                    break;
                                }
                                
                                queuedAction = m_ActionQueue.Dequeue();
                                queueCount = m_ActionQueue.Count;
                            }
                            
                            try
                            {
                                if (queuedAction != null)
                                {
                                    queuedAction();
                                }
                            }
                            catch(Exception ex)
                            {
								/*
                                Logger.Write(new LogData(string.Format("ActiveObject::Run. exec -> queuedAction(): {0}, Error: {1}",
                                                       m_Name,
                                                       ex.Message),
                                     Component.Patterns,
                                     LogLevel.Error));
                                */
                            }
                            
                        } while (queueCount > 0);
                        
                        m_SignalObject.Reset();
                    }
                    catch (Exception ex)
                    {
						/*
                        Logger.Write(new LogData(string.Format("ActiveObject::Run - Name: {0}, Loop Error: {1}",
                                                               m_Name,
                                                               ex.Message),
                                             Component.Patterns,
                                             LogLevel.Error));
                        */
                    }
                }
            }
            catch(Exception ex)
            {
				/*
                Logger.Write(new LogData(string.Format("ActiveObject::Run - Name: {0}, Error: {1}", 
                                                       m_Name,
                                                       ex.Message),
                                     Component.Patterns,
                                     LogLevel.Error));
                 */
            }
            finally
            {
                m_SignalObject.Close();
                
                //In case cacncellation token is passed, we shouldn't dispose something we don't own.
                if (m_CancellationToken == null)
                {
                    m_ShutdownEvent.Close();
                }
                
                m_SignalObject = null;
                m_ShutdownEvent = null;
            }
        }
    }
}
