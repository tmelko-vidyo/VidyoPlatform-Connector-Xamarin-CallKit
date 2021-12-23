using System;
using System.Diagnostics;

namespace VidyoConnector.Call
{
    public class ActiveCall
    {
        #region Private Variables
        private bool isConnecting;
        private bool isConnected;
        private bool isOnhold;
        #endregion

        #region Computed Properties
        public string UUID { get; set; }
        public bool isOutgoing { get; set; }
        public string Handle { get; set; }
        public long StartedConnectingOn { get; set; }
        public long ConnectedOn { get; set; }
        public long EndedOn { get; set; }

        public bool IsConnecting
        {
            get { return isConnecting; }
            set
            {
                isConnecting = value;
                if (isConnecting) StartedConnectingOn = DateTime.Now.Millisecond;
                RaiseStartingConnectionChanged();
            }
        }

        public bool IsConnected
        {
            get { return isConnected; }
            set
            {
                isConnected = value;
                if (isConnected)
                {
                    ConnectedOn = DateTime.Now.Millisecond;
                }
                else
                {
                    EndedOn = DateTime.Now.Millisecond;
                }
                RaiseConnectedChanged();
            }
        }

        public bool IsOnHold
        {
            get { return isOnhold; }
            set
            {
                isOnhold = value;
            }
        }
        #endregion

        #region Constructors
        public ActiveCall()
        {
        }

        public ActiveCall(string uuid, string handle, bool outgoing)
        {
            // Initialize
            this.UUID = uuid;
            this.Handle = handle;
            this.isOutgoing = outgoing;
        }
        #endregion

        #region Public Methods
        public void StartCall()
        {
            IsConnected = true;
        }

        public void AnswerCall()
        {
            IsConnected = true;
            Debug.WriteLine("Call has been answered.");
        }

        public void EndCall()
        {
            IsConnected = false;
            Debug.WriteLine("Call has been ended.");
        }
        #endregion

        #region Events
        public delegate void ActiveCallbackDelegate(bool successful);
        public delegate void ActiveCallStateChangedDelegate(ActiveCall call);

        public event ActiveCallStateChangedDelegate StartingConnectionChanged;
        internal void RaiseStartingConnectionChanged()
        {
            if (this.StartingConnectionChanged != null) this.StartingConnectionChanged(this);
        }

        public event ActiveCallStateChangedDelegate ConnectedChanged;
        internal void RaiseConnectedChanged()
        {
            if (this.ConnectedChanged != null) this.ConnectedChanged(this);
        }
        #endregion
    }
}
