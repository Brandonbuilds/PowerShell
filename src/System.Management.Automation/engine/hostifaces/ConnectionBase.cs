/********************************************************************++
 * Copyright (c) Microsoft Corporation.  All rights reserved.
 * --********************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using Dbg = System.Management.Automation.Diagnostics;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Runspaces is base class for different kind of Runspaces.
    /// </summary>
    /// <remarks>There should be a class derived from it for each type of
    /// Runspace. Types of Runspace which we support are Local, X-AppDomain,
    /// X-Process and X-Machine.</remarks>
    internal abstract class RunspaceBase : Runspace
    {
        #region constructors
        
        /// <summary>
        /// Construct an instance of an Runspace using a custom 
        /// implementation of PSHost.
        /// </summary>
        /// <param name="host">The explicit PSHost implementation</param>
        /// <exception cref="System.ArgumentNullException">
        /// Host is null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// host is null.
        /// </exception>
        /// <param name="runspaceConfiguration">
        /// configuration information for this minshell.
        /// </param>
        protected RunspaceBase(PSHost host, RunspaceConfiguration runspaceConfiguration)
        {
            if (host == null)
            {
                throw PSTraceSource.NewArgumentNullException ("host");
            }
            if (runspaceConfiguration == null)
            {
                throw PSTraceSource.NewArgumentNullException("runspaceConfiguration");
            }

            _host = host; 
            _runspaceConfiguration = runspaceConfiguration;
        }

        /// <summary>
        /// Construct an instance of an Runspace using a custom 
        /// implementation of PSHost.
        /// </summary>
        /// <param name="host">The explicit PSHost implementation</param>
        /// <exception cref="System.ArgumentNullException">
        /// Host is null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// host is null.
        /// </exception>
        /// <param name="initialSessionState">
        /// configuration information for this runspace instance.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification="OK to call ThreadOptions")]
        protected RunspaceBase(PSHost host, InitialSessionState initialSessionState)
        {
            if (host == null)
            {
                throw PSTraceSource.NewArgumentNullException("host");
            }
            if (initialSessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException("initialSessionState");
            }

            _host = host;
            _initialSessionState = initialSessionState.Clone();
            this.ThreadOptions = initialSessionState.ThreadOptions;

#if !CORECLR // No ApartmentState In CoreCLR
            this.ApartmentState = initialSessionState.ApartmentState;
#endif
        }

        /// <summary>
        /// Construct an instance of an Runspace using a custom 
        /// implementation of PSHost.
        /// </summary>
        /// <param name="host">
        /// The explicit PSHost implementation
        /// </param>
        /// <param name="initialSessionState">
        /// configuration information for this runspace instance.
        /// </param>
        /// <param name="suppressClone">
        /// If true, don't make a copy of the initial session state object.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Host is null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// host is null.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "OK to call ThreadOptions")]
        protected RunspaceBase(PSHost host, InitialSessionState initialSessionState, bool suppressClone)
        {
            if (host == null)
            {
                throw PSTraceSource.NewArgumentNullException("host");
            }
            if (initialSessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException("initialSessionState");
            }

            _host = host;
            if (suppressClone)
            {
                _initialSessionState = initialSessionState;
            }
            else
            {
                _initialSessionState = initialSessionState.Clone();
            }
            this.ThreadOptions = initialSessionState.ThreadOptions;

#if !CORECLR // No ApartmentState In CoreCLR
            this.ApartmentState = initialSessionState.ApartmentState;
#endif
        }

        private PSHost _host;
        /// <summary>
        /// The host implemented PSHost interface
        /// </summary>
        protected PSHost Host
        {
            get
            {
                return _host;
            }
        }

        RunspaceConfiguration _runspaceConfiguration;
        /// <summary>
        /// runspaceConfiguration information for this runspace
        /// </summary>
        public override RunspaceConfiguration RunspaceConfiguration
        {
            get
            {
                return _runspaceConfiguration;
            }
        }

        InitialSessionState _initialSessionState;
        /// <summary>
        /// runspaceConfiguration information for this runspace
        /// </summary>
        public override InitialSessionState InitialSessionState
        {
            get
            {
                return _initialSessionState;
            }
        }
     
        #endregion constructors

        #region properties

        private Version _version = PSVersionInfo.PSVersion;

        /// <summary>
        /// Return version of this runspace
        /// </summary>
        public override Version Version
        {
            get
            {
                return _version;
            }
        }

        private RunspaceStateInfo _runspaceStateInfo = new RunspaceStateInfo (RunspaceState.BeforeOpen);

        /// <summary>
        /// Retrieve information about current state of the runspace
        /// </summary>
        public override RunspaceStateInfo RunspaceStateInfo
        {
            get
            {
                lock (_syncRoot)
                {
                    //Do not return internal state.
                    return _runspaceStateInfo.Clone();
                }
            }
        }

        /// <summary>
        /// Gets the current availability of the Runspace
        /// </summary>
        public override RunspaceAvailability RunspaceAvailability
        {
            get { return _runspaceAvailability; }
            protected set { _runspaceAvailability = value; }
        }
        private RunspaceAvailability _runspaceAvailability = RunspaceAvailability.None;

        private object _syncRoot = new object();
        /// <summary>
        /// Object used for synchronization
        /// </summary>
        protected internal object SyncRoot
        {
            get
            {
                return _syncRoot;
            }
        }

        /// <summary>
        /// Information about the computer where this runspace is created
        /// </summary>
        public override RunspaceConnectionInfo ConnectionInfo
        {
            get 
            { 
                // null refers to local case for path
                return null; 
            }
        }

        /// <summary>
        /// Original Connection Info that the user passed
        /// </summary>
        public override RunspaceConnectionInfo OriginalConnectionInfo
        {
            get { return null; }
        }

        #endregion properties
       
        #region Open
        
        /// <summary>
        /// Open the runspace synchronously.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not BeforeOpen
        /// </exception>
        override public void Open()
        {
            CoreOpen (true);
        }

        /// <summary>
        /// Open the runspace Asynchronously.
        /// </summary>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not BeforeOpen
        /// </exception>
        override public void OpenAsync ()
        {
            CoreOpen (false);
        }

        /// <summary>
        /// Opens the runspace.
        /// </summary>
        /// <param name="syncCall">If true runspace is opened synchronously
        /// else runspaces is opened asynchronously
        /// </param>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is not BeforeOpen
        /// </exception>
        private void CoreOpen (bool syncCall)
        {
            bool etwEnabled = RunspaceEventSource.Log.IsEnabled();
            if (etwEnabled) RunspaceEventSource.Log.OpenRunspaceStart();
            lock (SyncRoot)
            {
                //Call fails if RunspaceState is not BeforeOpen. 
                if (RunspaceState != RunspaceState.BeforeOpen)
                {
                    InvalidRunspaceStateException e =
                        new InvalidRunspaceStateException
                        (
                            StringUtil.Format(RunspaceStrings.CannotOpenAgain,new object[] { RunspaceState.ToString() }),
                            RunspaceState,
                            RunspaceState.BeforeOpen
                        );
                    throw e;
                }

                SetRunspaceState(RunspaceState.Opening);
            }

            //Raise event outside the lock	
            RaiseRunspaceStateEvents();

            OpenHelper(syncCall);
            if (etwEnabled) RunspaceEventSource.Log.OpenRunspaceStop();
        }


        /// <summary>
        /// Derived class's open implementation
        /// </summary>
        protected abstract void OpenHelper(bool syncCall);

        
        #endregion open
        
        #region close
        /// <summary>
        /// Close the runspace synchronously.
        /// </summary>
        /// <remarks>
        /// Attempts to execute pipelines after a call to close will fail.
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is BeforeOpen or Opening
        /// </exception>
        override public void Close ()
        {
            CoreClose (true);
        }

        /// <summary>
        /// Close the runspace Asynchronously.
        /// </summary>
        /// <remarks>
        /// Attempts to execute pipelines after a call to 
        /// close will fail.
        /// </remarks>
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is BeforeOpen or Opening
        /// </exception>
        override public void CloseAsync()
        {
            CoreClose (false);
        }

        /// <summary>
        /// Close the runspace 
        /// </summary>
        /// <param name="syncCall">If true runspace is closed synchronously
        /// else runspaces is closed asynchronously
        /// </param>
        /// 
        /// <exception cref="InvalidRunspaceStateException">
        /// RunspaceState is BeforeOpen or Opening
        /// </exception>
        /// 
        /// <exception cref="InvalidOperationException">
        /// If SessionStateProxy has some method call in progress
        /// </exception>
        private void CoreClose (bool syncCall)
        {
            bool alreadyClosing = false;

            lock (SyncRoot)
            {
                if (RunspaceState == RunspaceState.Closed ||
                    RunspaceState == RunspaceState.Broken)
                {
                    return;
                }
                else if (RunspaceState == RunspaceState.BeforeOpen)
                {
                    SetRunspaceState(RunspaceState.Closing, null);
                    SetRunspaceState(RunspaceState.Closed, null);

                    RaiseRunspaceStateEvents();

                    return;
                }
                else if (RunspaceState == RunspaceState.Opening)
                {
                    // Wait till the runspace is opened - This is set in DoOpenHelper()
                    // Release the lock before we wait
                    Monitor.Exit(SyncRoot);
                    try
                    {
                        RunspaceOpening.Wait();
                    }
                    finally
                    {
                        // Acquire the lock before we carry on with the rest operations 
                        Monitor.Enter(SyncRoot);
                    }
                }

                if (_bSessionStateProxyCallInProgress)
                {
                    throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.RunspaceCloseInvalidWhileSessionStateProxy);
                }

                if (RunspaceState == RunspaceState.Closing)
                {
                    alreadyClosing = true;
                }
                else
                {
                    if (RunspaceState != RunspaceState.Opened)
                    {
                        InvalidRunspaceStateException e =
                            new InvalidRunspaceStateException
                            (
                                StringUtil.Format(RunspaceStrings.RunspaceNotInOpenedState, RunspaceState.ToString()),
                                RunspaceState,
                                RunspaceState.Opened
                            );
                        throw e;
                    }

                    SetRunspaceState(RunspaceState.Closing);
                }
            }

            if (alreadyClosing)
            {
                //Already closing is set to true if Runspace is already
                //in closing. In this case wait for runspace to close.
                //This can happen in two scenarios:
                //1) User calls Runspace.Close from two threads. 
                //2) In remoting, some error from data structure handler layer can start
                //runspace closure. At the same time, user can call 
                //remove runspace. 
                //

                if (syncCall)
                {
                    WaitForFinishofPipelines();
                }
                return;
            }

            //Raise Event outside the lock
            RaiseRunspaceStateEvents();

            //Call the derived class implementation to do the actual work
            CloseHelper(syncCall);
        }


        /// <summary>
        /// Derived class's close implementation
        /// </summary>
        /// <param name="syncCall">If true runspace is closed synchronously
        /// else runspaces is closed asynchronously
        /// </param>
        protected abstract void CloseHelper(bool syncCall);

        #endregion close

        #region Disconnect-Connect

        /// <summary>
        /// Disconnects the runspace synchronously.
        /// </summary>
        public override void Disconnect()
        {
            //
            // Disconnect operation is not supported on local runspaces.
            //
            throw new InvalidRunspaceStateException(
                            RunspaceStrings.DisconnectNotSupported);
        }

        /// <summary>
        /// Disconnects the runspace asynchronously.
        /// </summary>
        public override void DisconnectAsync()
        {
            //
            // Disconnect operation is not supported on local runspaces.
            //
            throw new InvalidRunspaceStateException(
                            RunspaceStrings.DisconnectNotSupported);
        }

        /// <summary>
        /// Connects a runspace to its remote counterpart synchronously.
        /// </summary>
        public override void Connect()
        {
            //
            // Connect operation is not supported on local runspaces.
            //
            throw new InvalidRunspaceStateException(
                            RunspaceStrings.ConnectNotSupported);
        }

        /// <summary>
        /// Connects a runspace to its remote counterpart asynchronously.
        /// </summary>
        public override void ConnectAsync()
        {
            //
            // Connect operation is not supported on local runspaces.
            //
            throw new InvalidRunspaceStateException(
                            RunspaceStrings.ConnectNotSupported);
        }

        /// <summary>
        /// Creates a pipeline object in the Disconnected state.
        /// </summary>
        /// <returns>Pipeline</returns>
        public override Pipeline CreateDisconnectedPipeline()
        {
            //
            // Disconnect-Connect is not supported on local runspaces.
            //
            throw new InvalidRunspaceStateException(
                            RunspaceStrings.DisconnectConnectNotSupported);
        }

        /// <summary>
        /// Creates a powershell object in the Disconnected state.
        /// </summary>
        /// <returns>PowerShell</returns>
        public override PowerShell CreateDisconnectedPowerShell()
        {
            //
            // Disconnect-Connect is not supported on local runspaces.
            //
            throw new InvalidRunspaceStateException(
                            RunspaceStrings.DisconnectConnectNotSupported);
        }

        /// <summary>
        /// Returns Runspace capabilities.
        /// </summary>
        /// <returns>RunspaceCapability</returns>
        public override RunspaceCapability GetCapabilities()
        {
            return RunspaceCapability.Default;
        }

        #endregion

        #region CreatePipeline

        /// <summary>
        /// Create an empty pipeline
        /// </summary>
        /// <returns>An empty pipeline</returns>
        public override Pipeline CreatePipeline ()
        {
            return CoreCreatePipeline (null, false, false);
        }

        /// <summary>
        /// Createa a pipeline froma command string
        /// </summary>
        /// <param name="command">A valid command string</param>
        /// <returns>
        /// A pipline pre-filled with Commands specified in commandString.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        public override Pipeline CreatePipeline(string command)
        {
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException ("command");
            }

            return CoreCreatePipeline (command, false, false);
        }
        
        /// <summary>
        /// Create a pipeline from a command string.
        /// </summary>
        /// <param name="command">A valid command string</param>
        /// <param name="addToHistory">if true command is added to history</param>
        /// <returns>
        /// A pipline pre-filled with Commands specified in commandString.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        public override Pipeline CreatePipeline (string command, bool addToHistory)
        {
            if (command == null)
            {
                throw PSTraceSource.NewArgumentNullException ("command");
            }

            return CoreCreatePipeline (command, addToHistory, false);
        }

        /// <summary>
        /// Creates a nested pipeline. 
        /// </summary>
        /// <remarks>
        /// Nested pipelines are needed for nested prompt scenario. Nested
        /// prompt requires that we execute new pipelines( child pipelines)
        /// while current pipeline (lets call it parent pipeline) is blocked.
        /// </remarks>
        public override Pipeline CreateNestedPipeline ()
        {
            return CoreCreatePipeline (null, false, true);
        }

        /// <summary>
        /// Creates a nested pipeline. 
        /// </summary>
        /// <param name="command">A valid command string</param>
        /// <param name="addToHistory">if true command is added to history</param>
        /// <returns>
        /// A pipline pre-filled with Commands specified in commandString.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// command is null
        /// </exception>
        public override Pipeline CreateNestedPipeline(string command, bool addToHistory)
        {
            if (command == null)
            {

                throw PSTraceSource.NewArgumentNullException ("command");
            }

            return CoreCreatePipeline (command, addToHistory, true);
        }

        /// <summary>
        /// Create a pipeline from a command string 
        /// </summary>
        /// 
        /// <param name="command">A valid command string or String.Empty.</param>
        /// <param name="addToHistory">if true command is added to history</param>
        /// <param name="isNested">True for nested pipeline</param>
        /// <returns>
        /// A pipline pre-filled with Commands specified in commandString.
        /// </returns>
        protected abstract Pipeline CoreCreatePipeline (string command, bool addToHistory, bool isNested);

        #endregion CreatePipeline

        #region state change event

        /// <summary>
        /// Event raised when RunspaceState changes.
        /// </summary>
        public override event EventHandler<RunspaceStateEventArgs> StateChanged;

        /// <summary>
        /// Event raised when the availability of the Runspace changes.
        /// </summary>
        public override event EventHandler<RunspaceAvailabilityEventArgs> AvailabilityChanged;

        /// <summary>
        /// Returns true if there are any subscribers to the AvailabilityChanged event
        /// </summary>
        internal override bool HasAvailabilityChangedSubscribers
        {
            get { return this.AvailabilityChanged != null; }
        }

        /// <summary>
        /// Raises the AvailabilityChanged event
        /// </summary>
        protected override void OnAvailabilityChanged(RunspaceAvailabilityEventArgs e)
        {
            EventHandler<RunspaceAvailabilityEventArgs> eh = this.AvailabilityChanged;

            if (eh != null)
            {
                try
                {
                    eh(this, e);
                }
                catch (Exception exception) // ignore non-severe exceptions
                {
                    CommandProcessorBase.CheckForSevereException(exception);
                }
            }
        }

        /// <summary>
        /// Retrieve the current state of the runspace. 
        /// <see cref="RunspaceState"/>
        /// </summary>
        protected RunspaceState RunspaceState
        {
            get
            {
                return _runspaceStateInfo.State;
            }
        }

        /// <summary>
        /// This is queue of all the state change event which have occured for
        /// this runspace. RaiseRunspaceStateEvents raises event for each 
        /// item in this queue. We don't raise events from with SetRunspaceState
        /// because SetRunspaceState is often called from with in the a lock.
        /// Raising event with in a lock introduces chances of deadlock in GUI 
        /// applications.
        /// </summary>
        Queue<RunspaceEventQueueItem> _runspaceEventQueue = new Queue<RunspaceEventQueueItem>();

        private class RunspaceEventQueueItem
        {
            public RunspaceEventQueueItem(RunspaceStateInfo runspaceStateInfo, RunspaceAvailability currentAvailability, RunspaceAvailability newAvailability)
            {
                this.RunspaceStateInfo = runspaceStateInfo;
                this.CurrentRunspaceAvailability = currentAvailability;
                this.NewRunspaceAvailability = newAvailability;
            }

            public RunspaceStateInfo    RunspaceStateInfo;
            public RunspaceAvailability CurrentRunspaceAvailability;
            public RunspaceAvailability NewRunspaceAvailability;
        }

        // This is to notify once runspace has been opened (RunspaceState.Opened)
        internal ManualResetEventSlim RunspaceOpening = new ManualResetEventSlim(false);

        /// <summary>
        /// Set the new runspace state.
        /// </summary>
        /// <param name="state">the new state</param>
        /// <param name="reason">An exception indicating the state change is the 
        /// result of an error, otherwise; null.
        /// </param>
        /// <remarks>
        /// Sets the internal runspace state information member variable. It also 
        /// adds RunspaceStateInfo to a queue. 
        /// RaiseRunspaceStateEvents raises event for each item in this queue.
        /// </remarks>        
        protected void SetRunspaceState (RunspaceState state, Exception reason)
        {
            lock (SyncRoot)
            {
                if (state != RunspaceState)
                {
                    _runspaceStateInfo = new RunspaceStateInfo (state, reason);

                    //Add _runspaceStateInfo to _runspaceEventQueue.
                    //RaiseRunspaceStateEvents will raise event for each item
                    //in this queue.
                    //Note:We are doing clone here instead of passing the member 
                    //_runspaceStateInfo because we donot want outside
                    //to change our runspace state.
                    RunspaceAvailability previousAvailability = _runspaceAvailability;

                    this.UpdateRunspaceAvailability(_runspaceStateInfo.State, false);

                    _runspaceEventQueue.Enqueue(
                        new RunspaceEventQueueItem(
                            _runspaceStateInfo.Clone(), 
                            previousAvailability, 
                            _runspaceAvailability));
                }
            }
        }

        /// <summary>
        /// Set the current runspace state - no error
        /// </summary>
        /// <param name="state">the new state</param>
        protected void SetRunspaceState (RunspaceState state)
        {
            this.SetRunspaceState (state, null);
        }


        /// <summary>
        /// Raises events for changes in runspace state.
        /// </summary>
        protected void RaiseRunspaceStateEvents ()
        {
            Queue<RunspaceEventQueueItem> tempEventQueue = null;
            EventHandler<RunspaceStateEventArgs> stateChanged = null;
            bool hasAvailabilityChangedSubscribers = false;

            lock (SyncRoot)
            {
                stateChanged = this.StateChanged;
                hasAvailabilityChangedSubscribers = this.HasAvailabilityChangedSubscribers;

                if (stateChanged != null || hasAvailabilityChangedSubscribers)
                {
                    tempEventQueue = _runspaceEventQueue;
                    _runspaceEventQueue = new Queue<RunspaceEventQueueItem>();
                }
                else
                {
                    //Clear the events if there are no EventHandlers. This 
                    //ensures that events do not get called for state 
                    //changes prior to their registration.
                    _runspaceEventQueue.Clear ();
                }
            }

            if (tempEventQueue != null)
            {
                while (tempEventQueue.Count > 0)
                {
                    RunspaceEventQueueItem queueItem = tempEventQueue.Dequeue();

                    if (hasAvailabilityChangedSubscribers && queueItem.NewRunspaceAvailability != queueItem.CurrentRunspaceAvailability)
                    {
                        this.OnAvailabilityChanged(new RunspaceAvailabilityEventArgs(queueItem.NewRunspaceAvailability));
                    }

#pragma warning disable 56500
                    //Exception raised by events are not error condition for runspace
                    //object.
                    if (stateChanged != null)
                    {
                        try
                        {
                            stateChanged(this, new RunspaceStateEventArgs(queueItem.RunspaceStateInfo));
                        }
                        catch (Exception exception) // ignore non-severe exceptions
                        {
                            CommandProcessorBase.CheckForSevereException(exception);
                        }
                    }
#pragma warning restore 56500
                }
            }
        }

        #endregion state change event

        #region running pipeline management

        private bool _bypassRunspaceStateCheck;
        /// <summary>
        /// In RemoteRunspace, it is required to invoke pipeline 
        /// as part of open call (i.e. while state is Opening).
        /// If this property is true, runspace state check is 
        /// not performed in AddToRunningPipelineList call.
        /// </summary>
        protected bool ByPassRunspaceStateCheck
        {
            get
            {
                return _bypassRunspaceStateCheck;
            }
            set
            {
                _bypassRunspaceStateCheck = value;
            }
        }

        private readonly List<Pipeline> _runningPipelines = new List<Pipeline>();
        private readonly object _pipelineListLock = new object();

        /// <summary>
        /// List of pipeline which are currently executing in this runspace.
        /// </summary>
        protected List<Pipeline> RunningPipelines
        {
            get
            {
                return _runningPipelines;
            }
        }

        /// <summary>
        /// Add the pipeline to list of pipelines in execution.
        /// </summary>
        /// <param name="pipeline">Pipeline to add to the 
        /// list of pipelines in execution</param>      
        /// 
        /// <exception cref="InvalidRunspaceStateException">
        /// Thrown if the runspace  is not in the Opened state.
        /// <see cref="RunspaceState"/>.
        /// </exception>
        /// 
        /// <exception cref="ArgumentNullException">Thrown if 
        /// <paramref name="pipeline"/> is null.
        /// </exception>
        internal void AddToRunningPipelineList (PipelineBase pipeline)
        {
            Dbg.Assert(pipeline != null, "caller should validate the parameter");

            lock (_pipelineListLock)
            {
                if (_bypassRunspaceStateCheck == false && RunspaceState != RunspaceState.Opened)
                {
                    InvalidRunspaceStateException e =
                        new InvalidRunspaceStateException 
                        (
                            StringUtil.Format(RunspaceStrings.RunspaceNotOpenForPipeline,RunspaceState.ToString()),
                            RunspaceState, 
                            RunspaceState.Opened
                        );
                    throw e;
                }

                //Add the pipeline to list of Excuting pipeline.
                //Note:_runningPipelines is always accessed with the lock so
                //there is no need to create a synchronized version of list
                _runningPipelines.Add (pipeline);
                currentlyRunningPipeline = pipeline;
            }
        }

        /// <summary>
        /// Remove the pipeline from list of pipelines in execution.
        /// </summary>
        /// <param name="pipeline">Pipeline to remove from the 
        /// list of pipelines in execution</param>      
        /// 
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="pipeline"/> is null.
        /// </exception>        
        internal void RemoveFromRunningPipelineList (PipelineBase pipeline)
        {
            Dbg.Assert(pipeline != null, "caller should validate the parameter");

            lock (_pipelineListLock)
            {
                Dbg.Assert(RunspaceState != RunspaceState.BeforeOpen,
                             "Runspace should not be before open when pipeline is running");

                //Remove the pipeline to list of Excuting pipeline.
                //Note:_runningPipelines is always accessed with the lock so
                //there is no need to create a synchronized version of list
                _runningPipelines.Remove (pipeline);

                // Update the running pipeline
                if(_runningPipelines.Count == 0)
                {
                    currentlyRunningPipeline = null;
                }
                else
                {
                    currentlyRunningPipeline = _runningPipelines[_runningPipelines.Count - 1];
                }

                pipeline.PipelineFinishedEvent.Set ();
            }
        }

        /// <summary>
        /// Waits till all the pipelines running in the runspace have 
        /// finished execution.
        /// </summary>
        internal bool WaitForFinishofPipelines ()
        {
            //Take a snapshot of list of active pipelines.
            //Note:Before we enter to this CloseHelper routine
            //CoreClose has already set the state of Runspace
            //to closing. So no new pipelines can be executed on this
            //runspace and so no new pipelines will be added to
            //_runningPipelines. However we still need to lock because
            //running pipelines can be removed from this.
            PipelineBase[] runningPipelines;

            lock (_pipelineListLock)
            {
                runningPipelines = RunningPipelines.Cast<PipelineBase>().ToArray();
            }

            if (runningPipelines.Length > 0)
            {
                WaitHandle[] waitHandles = new WaitHandle[runningPipelines.Length];

                for (int i = 0; i < runningPipelines.Length; i++)
                {
                    waitHandles[i] = runningPipelines[i].PipelineFinishedEvent;
                }

#if !CORECLR    // No ApartmentState.STA In CoreCLR
                // WaitAll for multiple handles on a STA (single-thread apartment) thread is not supported as WaitAll will prevent the message pump to run
                if (runningPipelines.Length > 1 && Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    // We use a worker thread to wait for all handles, and the STA thread can just wait on the worker thread -- the worker
                    // threads from the ThreadPool are MTA.
                    using(ManualResetEvent waitAllIsDone = new ManualResetEvent(false))
                    {
                        Tuple<WaitHandle[], ManualResetEvent> stateInfo = new Tuple<WaitHandle[], ManualResetEvent>(waitHandles, waitAllIsDone);

                        ThreadPool.QueueUserWorkItem(new WaitCallback(
                                                         delegate(object state)
                                                         {
                                                             var tuple = (Tuple<WaitHandle[], ManualResetEvent>)state;
                                                             WaitHandle.WaitAll(tuple.Item1);
                                                             tuple.Item2.Set();
                                                         }), stateInfo);
                        return waitAllIsDone.WaitOne();
                    }
                }
#endif
                return WaitHandle.WaitAll(waitHandles);
            }
            else
            {
                return true;
            }
        }


        /// <summary>
        /// Stops all the running pipelines 
        /// </summary>
        protected void StopPipelines ()
        {
            PipelineBase[] runningPipelines;

            lock (_pipelineListLock)
            {
                runningPipelines = RunningPipelines.Cast<PipelineBase>().ToArray();
            }

            if (runningPipelines.Length > 0)
            {
                //Start from the most recent pipeline.
                for (int i = runningPipelines.Length - 1; i >= 0; i--)
                {
                    runningPipelines[i].Stop ();
                }
            }
        }

        internal bool RunActionIfNoRunningPipelinesWithThreadCheck(Action action)
        {
            bool ranit = false;
            bool shouldRunAction = false;
            lock (_pipelineListLock)
            {
                // If we have no running pipeline, or if the currently running pipeline is
                // the same as the current thread, then execute the action.

                var pipelineRunning = currentlyRunningPipeline as PipelineBase;
                if (pipelineRunning == null || 
                    Thread.CurrentThread.Equals(pipelineRunning.NestedPipelineExecutionThread))
                {
                    shouldRunAction = true;
                }
            }
            if (shouldRunAction)
            {
                action();
                ranit = true;
            }
            return ranit;
        }

        /// <summary>
        /// Gets the currently executing pipeline. 
        /// </summary>
        /// <remarks>Internal because it is needed by invoke-history</remarks>
        internal override Pipeline GetCurrentlyRunningPipeline()
        {
            return currentlyRunningPipeline;
        }
        Pipeline currentlyRunningPipeline = null;

        /// <summary>
        /// This method stops all the pipelines which are nested
        /// under specified pipeline
        /// </summary>
        /// <param name="pipeline"></param>
        /// <returns></returns>
        internal void StopNestedPipelines (Pipeline pipeline)
        {
            List<Pipeline> nestedPipelines = null;

            lock (_pipelineListLock)
            {
                //first check if this pipeline is in the list of running 
                //pipelines. It is possible that pipeline has already 
                //completed.
                if (_runningPipelines.Contains(pipeline) == false)
                {
                    return;
                }

                //If this pipeline is currently running pipeline,
                //then it does not have nested pipelines
                if (GetCurrentlyRunningPipeline () == pipeline)
                {
                    return;
                }

                //Build list of nested pipelines
                nestedPipelines = new List<Pipeline>();
                for (int i = _runningPipelines.Count - 1; i >= 0; i--)
                {
                    if (_runningPipelines[i] == pipeline)
                        break;
                    nestedPipelines.Add(_runningPipelines[i]);
                }
            }

            foreach (Pipeline np in nestedPipelines)
            {
                try
                {
                    np.Stop();
                }
                catch (InvalidPipelineStateException)
                {
                }
            }
        }

        internal
        void
        DoConcurrentCheckAndAddToRunningPipelines(PipelineBase pipeline, bool syncCall)
        {
            //Concurrency check should be done under runspace lock 
            lock (_syncRoot)
            {
                if (_bSessionStateProxyCallInProgress == true)
                {
                    throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.NoPipelineWhenSessionStateProxyInProgress);
                }

                //Delegate to pipeline to do check if it is fine to invoke if another
                //pipeline is running.
                pipeline.DoConcurrentCheck(syncCall, _syncRoot, true);
                //Finally add to the list of running pipelines.
                AddToRunningPipelineList(pipeline);
            }
        }

        // PowerShell support for async notifications happen through the 
        // CheckForInterrupts() method on ParseTreeNode. These are only called when
        // the engine is active (and processing,) so the Pulse() method
        // executes the equivalent of a NOP so that async events
        // can be processed when the engine is idle.
        internal void Pulse()
        {
            // If we don't already have a pipeline running, pulse the engine.
            bool pipelineCreated = false;
            if (GetCurrentlyRunningPipeline() == null)
            {
                lock (SyncRoot)
                {
                    if (GetCurrentlyRunningPipeline() == null)
                    {
                        // This is a pipeline that does the least amount possible.
                        // It evaluates a constant, and results in the execution of only two parse tree nodes.
                        // We don't need to void it, as we aren't using the results. In addition, voiding
                        // (as opposed to ignoring) is 1.6x slower.
                        try
                        {
                            pulsePipeline = (PipelineBase)CreatePipeline("0");
                            pulsePipeline.IsPulsePipeline = true;
                            pipelineCreated = true;
                        }
                        catch (ObjectDisposedException)
                        {
                            // Ignore. The runspace is closing. The event was not processed,
                            // but this should not crash PowerShell.
                        }
                    }
                }
            }

            // Invoke pipeline outside the runspace lock.
            // A concurrency check will be made on the runspace before this
            // pipeline is invoked.
            if (pipelineCreated)
            {
                try
                {
                    pulsePipeline.Invoke();
                }
                catch (PSInvalidOperationException)
                {
                    // Ignore. A pipeline was created between the time
                    // we checked for it, and when we invoked the pipeline.
                    // This is unlikely, but taking a lock on the runspace
                    // means that OUR invoke will not be able to run.
                }
                catch (InvalidRunspaceStateException)
                {
                    // Ignore. The runspace is closing. The event was not processed,
                    // but this should not crash PowerShell.
                }
                catch (ObjectDisposedException)
                {
                    // Ignore. The runspace is closing. The event was not processed,
                    // but this should not crash PowerShell.
                }
            }
        }

        internal PipelineBase PulsePipeline
        {
            get { return pulsePipeline; }
        }
        private PipelineBase pulsePipeline;

        #endregion running pipeline management

        #region session state proxy
        
        //Note: When SessionStateProxy calls are in progress,
        //pipeline cannot be invoked. Also when pipeline is in
        //progress, SessionStateProxy calls cannot be made.
        bool _bSessionStateProxyCallInProgress;

        /// <summary>
        /// This method ensures that SessionStateProxy call is allowed and if
        /// allowed it sets a variable to disallow further SessionStateProxy or
        /// pipeline calls.
        /// </summary>
        void DoConcurrentCheckAndMarkSessionStateProxyCallInProgress()
        {
            lock (_syncRoot)
            {
                if (RunspaceState != RunspaceState.Opened)
                {
                    InvalidRunspaceStateException e =
                        new InvalidRunspaceStateException
                        (
                            StringUtil.Format(RunspaceStrings.RunspaceNotInOpenedState, RunspaceState.ToString()),
                            RunspaceState,
                            RunspaceState.Opened
                        );
                    throw e;
                }

                if (_bSessionStateProxyCallInProgress == true)
                {
                    throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.AnotherSessionStateProxyInProgress);
                }

                Pipeline runningPipeline = GetCurrentlyRunningPipeline();
                if (runningPipeline != null)
                {
                    // Detect if we're running an engine pulse, or we're running a nested pipeline
                    // from an engine pulse
                    if (runningPipeline == pulsePipeline ||
                        (runningPipeline.IsNested && pulsePipeline != null))
                    {
                        // If so, wait and try again
                        // Release the lock before we wait for the pulse pipelines
                        Monitor.Exit(_syncRoot);

                        try
                        {
                            WaitForFinishofPipelines();
                        }
                        finally
                        {
                            // Acquire the lock before we carry on with the rest operations
                            Monitor.Enter(_syncRoot);
                        }

                        DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                        return;
                    }
                    else
                    {
                        throw PSTraceSource.NewInvalidOperationException(RunspaceStrings.NoSessionStateProxyWhenPipelineInProgress);
                    }
                }

                //Now we can invoke session state proxy
                _bSessionStateProxyCallInProgress = true;
            }
        }

        /// <summary>
        /// SetVariable implementation. This class does the necessary checks to ensure
        /// that no pipeline or other SessionStateProxy calls are in progress.
        /// It delegates to derived class worker method for actual operation.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        internal void SetVariable(string name, object value)
        {
            DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
            try
            {
                DoSetVariable(name, value);
            }
            finally
            {
                lock (SyncRoot)
                {
                    _bSessionStateProxyCallInProgress = false;
                }
            }
        }

        /// <summary>
        /// GetVariable implementation. This class does the necessary checks to ensure
        /// that no pipeline or other SessionStateProxy calls are in progress.
        /// It delegates to derived class worker method for actual operation.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal object GetVariable(string name)
        {
            DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
            try
            {
                return DoGetVariable(name);
            }
            finally
            {
                lock (_syncRoot)
                {
                    _bSessionStateProxyCallInProgress = false;
                }
            }
        }

        /// <summary>
        /// Applications implementation. This class does the necessary checks to ensure
        /// that no pipeline or other SessionStateProxy calls are in progress.
        /// It delegates to derived class worker method for actual operation.
        /// </summary>
        /// <returns></returns>
        internal List<string> Applications
        {
            get
            {
                DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                try
                {
                    return DoApplications;
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _bSessionStateProxyCallInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Scripts implementation. This class does the necessary checks to ensure
        /// that no pipeline or other SessionStateProxy calls are in progress.
        /// It delegates to derived class worker method for actual operation.
        /// </summary>
        /// <returns></returns>
        internal List<string> Scripts
        {
            get
            {
                DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                try
                {
                    return DoScripts;
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _bSessionStateProxyCallInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        internal DriveManagementIntrinsics Drive
        {
            get
            {
                DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                try
                {
                    return DoDrive;
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _bSessionStateProxyCallInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        public PSLanguageMode LanguageMode
        {
            get
            {
                if (RunspaceState != RunspaceState.Opened)
                {
                    InvalidRunspaceStateException e =
                        new InvalidRunspaceStateException
                        (
                            StringUtil.Format(RunspaceStrings.RunspaceNotInOpenedState, RunspaceState.ToString()),
                            RunspaceState,
                            RunspaceState.Opened
                        );
                    throw e;
                }
                return DoLanguageMode;
            }
            set
            {
                if (RunspaceState != RunspaceState.Opened)
                {
                    InvalidRunspaceStateException e =
                        new InvalidRunspaceStateException
                        (
                            StringUtil.Format(RunspaceStrings.RunspaceNotInOpenedState, RunspaceState.ToString()),
                            RunspaceState,
                            RunspaceState.Opened
                        );
                    throw e;
                }
                DoLanguageMode = value;
            }
        }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        internal PSModuleInfo Module
        {
            get
            {
                DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                try
                {
                    return DoModule;
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _bSessionStateProxyCallInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        internal PathIntrinsics PathIntrinsics
        {
            get
            {
                DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                try
                {
                    return DoPath;
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _bSessionStateProxyCallInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        internal CmdletProviderManagementIntrinsics Provider
        {
            get
            {
                DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                try
                {
                    return DoProvider;
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _bSessionStateProxyCallInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        internal PSVariableIntrinsics PSVariable
        {
            get
            {
                DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                try
                {
                    return DoPSVariable;
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _bSessionStateProxyCallInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        internal CommandInvocationIntrinsics InvokeCommand
        {
            get
            {
                DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                try
                {
                    return DoInvokeCommand;
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _bSessionStateProxyCallInProgress = false;
                    }
                }
            }
        }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        internal ProviderIntrinsics InvokeProvider
        {
            get
            {
                DoConcurrentCheckAndMarkSessionStateProxyCallInProgress();
                try
                {
                    return DoInvokeProvider;
                }
                finally
                {
                    lock (_syncRoot)
                    {
                        _bSessionStateProxyCallInProgress = false;
                    }
                }
            }
        }


        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the acutal work of setting variable.
        /// </summary>
        /// <param name="name">Name of the variable to set</param>
        /// <param name="value">The value to set it to</param>
        protected abstract void DoSetVariable(string name, object value);

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting variable.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected abstract object DoGetVariable(string name);

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting applications.
        /// </summary>
        protected abstract List<string> DoApplications { get; }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        protected abstract List<string> DoScripts { get; }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        protected abstract DriveManagementIntrinsics DoDrive { get; }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        protected abstract PSLanguageMode DoLanguageMode { get; set; }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        protected abstract PSModuleInfo DoModule { get; }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        protected abstract PathIntrinsics DoPath { get; }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        protected abstract CmdletProviderManagementIntrinsics DoProvider { get; }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        protected abstract PSVariableIntrinsics DoPSVariable { get; }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        protected abstract CommandInvocationIntrinsics DoInvokeCommand { get; }

        /// <summary>
        /// Protected methods to be implemented by derived class.
        /// This does the actual work of getting scripts.
        /// </summary>
        protected abstract ProviderIntrinsics DoInvokeProvider { get; }

        SessionStateProxy _sessionStateProxy;
        /// <summary>
        /// Returns SessionState proxy object.
        /// </summary>
        /// <returns></returns>
        internal override SessionStateProxy GetSessionStateProxy()
        {
            if (_sessionStateProxy == null)
            {
                _sessionStateProxy = new SessionStateProxy(this);
            }
            return _sessionStateProxy;
        }

        #endregion session state proxy
    }
}