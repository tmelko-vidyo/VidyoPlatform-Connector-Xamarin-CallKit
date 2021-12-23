using System;
using System.ComponentModel;
using VidyoClient;
using Xamarin.Forms;
using VidyoConnector.Controls;
using static VidyoConnector.IVidyoController;

#if __ANDROID__
using Android.App;
#endif

namespace VidyoConnector
{
    public class VidyoController : IVidyoController, INotifyPropertyChanged, Connector.IConnect,
        Connector.IRegisterLogEventListener,

        /* Device manager listeners */
        Connector.IRegisterLocalCameraEventListener,
        Connector.IRegisterLocalMicrophoneEventListener,
        Connector.IRegisterLocalSpeakerEventListener
    {

        private static readonly uint MAX_PARTICIPANTS = 8;

        /* Shared instance */
        private static readonly VidyoController _instance = new VidyoController();
        public static IVidyoController GetInstance() { return _instance; }

        private VidyoController() { }

        private Connector mConnector;

        /* Init Vidyo Client only once per app lifecycle */
        private bool mIsVidyoClientInitialized;

        private bool mIsDebugEnabled;
        private string mExperimentalOptions;
        private bool mCameraPrivacyState;

        private Logger mLogger = Logger.GetInstance();
        private String mLogLevel = "debug@VidyoClient debug@VidyoConnector warning";

        private VidyoConnectorState mState;

        private NativeView mVideoViewHolder;

        public event PropertyChangedEventHandler PropertyChanged;

        private Action mWrapCallAction;

        public VidyoConnectorState ConnectorState
        {
            get { return mState; }
            set
            {
                mState = value;
                // Raise PropertyChanged event
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ConnectorState"));
            }
        }

        /* Initialize Vidyo Client. Called only once */
        private bool Initialize()
        {
            if (mIsVidyoClientInitialized)
            {
                return true;
            }

            // Initialize VidyoClient library.
            // This should be called only once throughout the lifetime of the app.
            mIsVidyoClientInitialized = ConnectorPKG.Initialize();


            if (!mIsVidyoClientInitialized)
            {
                mLogger.Log("VidyoConnector Failed to initialize!");
                return false;
            } else
            {
                mLogger.Log("VidyoConnector Init successfully!");
            }


#if __ANDROID__
            ConnectorPKG.SetApplicationUIContext(Forms.Context);
#endif
            return mIsVidyoClientInitialized;
        }

        public String Construct(NativeView videoView, Action wrapCallAction)
        {
            bool result = Initialize();
            if (!result)
            {
                throw new Exception("Client initialization error.");
            }

            this.mWrapCallAction = wrapCallAction;

            // Remember the reference to video view
            this.mVideoViewHolder = videoView;

            mConnector = new Connector(this.mVideoViewHolder.Handle,
                                               Connector.ConnectorViewStyle.ConnectorviewstyleDefault,
                                               MAX_PARTICIPANTS,
                                               mLogLevel,
                                               "",
                                               0);
            // Get the version of VidyoClient
            string clientVersion = mConnector.GetVersion();

            // If enableDebug is configured then enable debugging
            if (mIsDebugEnabled)
            {
                mConnector.EnableDebug(7776, "debug@VidyoClient debug@VidyoConnector warning");
            }

            // Set experimental options if any exist
            if (mExperimentalOptions != null)
            {
                ConnectorPKG.SetExperimentalOptions(mExperimentalOptions);
            }

            if (!mConnector.RegisterLocalCameraEventListener(this))
            {
                mLogger.Log("RegisterLocalCameraEventListener failed!");
            }

            // Register for log callbacks
            if (!mConnector.RegisterLogEventListener(this, mLogLevel))
            {
                mLogger.Log("VidyoConnector RegisterLogEventListener failed");
            }

            if (!mConnector.RegisterLocalSpeakerEventListener(this))
            {
                mLogger.Log("VidyoConnector RegisterLocalSpeakerEventListener failed");
            }

            if (!mConnector.RegisterLocalMicrophoneEventListener(this))
            {
                mLogger.Log("VidyoConnector RegisterLocalSpeakerEventListener failed");
            }

            mLogger.Log("Connector instance has been created.");
            return clientVersion;
        }

        /* App state changed to background mode */
        public void OnAppSleep()
        {
            if (mConnector != null)
            {
                mConnector.SetCameraPrivacy(true);
                mConnector.SetMode(Connector.ConnectorMode.ConnectormodeBackground);
            }
        }

        /* App state changed to foreground mode */
        public void OnAppResume()
        {
            if (mConnector != null)
            {
                mConnector.SetMode(Connector.ConnectorMode.ConnectormodeForeground);
                mConnector.SetCameraPrivacy(this.mCameraPrivacyState);
            }
        }

        public void CleanUp()
        {
            mConnector.UnregisterLocalCameraEventListener();
            mConnector.UnregisterLocalSpeakerEventListener();
            mConnector.UnregisterLocalMicrophoneEventListener();

            mConnector.UnregisterLogEventListener();

            mConnector.SelectLocalCamera(null);
            mConnector.SelectLocalMicrophone(null);
            mConnector.SelectLocalSpeaker(null);

            mConnector.Disable();
            mConnector = null;

            mLogger.Log("Connector instance has been released.");
        }

        public bool Connect(string portal, string roomKey, string displayName, string pin)
        {
            return mConnector.ConnectToRoomAsGuest(portal, displayName, roomKey, pin, this);
        }

        public void Disconnect()
        {
            mConnector.Disconnect();
        }

        public void WrapCall()
        {
            Connector.ConnectorState connectorState = mConnector.GetState();
            if (connectorState == Connector.ConnectorState.ConnectorstateConnected)
            {
                mLogger.Log("Connector is connected. Disconnect first...");
                Disconnect();
            }
            else if (connectorState == Connector.ConnectorState.ConnectorstateIdle || connectorState == Connector.ConnectorState.ConnectorstateReady)
            {
                if (mWrapCallAction != null) mWrapCallAction.Invoke();
            }
        }

        // Set the microphone privacy
        public void SetMicrophonePrivacy(bool privacy)
        {
            mConnector.SetMicrophonePrivacy(privacy);
        }

        // Set the camera privacy
        public void SetCameraPrivacy(bool privacy)
        {
            this.mCameraPrivacyState = privacy;
            mConnector.SetCameraPrivacy(privacy);
        }

        // Cycle the camera
        public void CycleCamera()
        {
            mConnector.CycleCamera();
        }

        /*
         * Private Utility Functions
         */

        /* Refresh renderer */
        public void RefreshUI()
        {
            // Refresh the rendering of the video
            if (mConnector != null)
            {
                uint width = mVideoViewHolder.NativeWidth;
                uint height = mVideoViewHolder.NativeHeight;

                mConnector.ShowViewAt(mVideoViewHolder.Handle, 0, 0, width, height);
                mLogger.Log("VidyoConnectorShowViewAt: x = 0, y = 0, w = " + width + ", h = " + height);
            }
        }

        /* Connection callbacks */

        public void OnSuccess()
        {
            mLogger.Log("OnSuccess");
            ConnectorState = VidyoConnectorState.VidyoConnectorStateConnected;
        }

        public void OnFailure(Connector.ConnectorFailReason reason)
        {
            mLogger.Log("OnFailure. Reason: " + reason);
            ConnectorState = VidyoConnectorState.VidyoConnectorStateConnectionFailure;

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                if (mWrapCallAction != null)
                {
                    mWrapCallAction.Invoke();
                }
            });
        }

        public void OnDisconnected(Connector.ConnectorDisconnectReason reason)
        {
            mLogger.Log("OnDisconnected. Reason: " + reason);

            switch (reason)
            {
                case Connector.ConnectorDisconnectReason.ConnectordisconnectreasonDisconnected:
                    ConnectorState = VidyoConnectorState.VidyoConnectorStateDisconnected;
                    break;
                default:
                    ConnectorState = VidyoConnectorState.VidyoConnectorStateDisconnectedUnexpected;
                    break;
            }

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
            {
                if (mWrapCallAction != null)
                {
                    mWrapCallAction.Invoke();
                }
            });
        }

        /* Debug option */

        public void EnableDebugging()
        {
            mConnector.EnableDebug(7776, mLogLevel);
        }

        public void DisableDebugging()
        {
            mConnector.DisableDebug();
        }

        /* Log event listener */

        public void OnLog(LogRecord logRecord)
        {
            mLogger.LogClientLib(logRecord.message);
        }

        /* Local camera */

        public void OnLocalCameraAdded(LocalCamera localCamera)
        {
            mLogger.Log("OnLocalCameraAdded");
        }

        public void OnLocalCameraRemoved(LocalCamera localCamera)
        {
            mLogger.Log("OnLocalCameraRemoved");
        }

        public void OnLocalCameraSelected(LocalCamera localCamera)
        {
            mLogger.Log("OnLocalCameraSelected");
        }

        public void OnLocalCameraStateUpdated(LocalCamera localCamera, VidyoClient.Device.DeviceState state)
        {
            mLogger.Log("OnLocalCameraStateUpdated");
        }

        /* Local speaker */

        public void OnLocalSpeakerAdded(LocalSpeaker localSpeaker)
        {
            mLogger.Log("OnLocalSpeakerAdded");
        }

        public void OnLocalSpeakerRemoved(LocalSpeaker localSpeaker)
        {
            mLogger.Log("OnLocalSpeakerRemoved");
        }

        public void OnLocalSpeakerSelected(LocalSpeaker localSpeaker)
        {
            mLogger.Log("OnLocalSpeakerSelected");
        }

        public void OnLocalSpeakerStateUpdated(LocalSpeaker localSpeaker, VidyoClient.Device.DeviceState state)
        {
            mLogger.Log("OnLocalSpeakerStateUpdated");
        }

        /* Local microphone */

        public void OnLocalMicrophoneAdded(LocalMicrophone localMicrophone)
        {
            mLogger.Log("OnLocalMicrophoneAdded");
        }

        public void OnLocalMicrophoneRemoved(LocalMicrophone localMicrophone)
        {
            mLogger.Log("OnLocalMicrophoneRemoved");
        }

        public void OnLocalMicrophoneSelected(LocalMicrophone localMicrophone)
        {
            mLogger.Log("OnLocalMicrophoneSelected");
        }

        public void OnLocalMicrophoneStateUpdated(LocalMicrophone localMicrophone, VidyoClient.Device.DeviceState state)
        {
            mLogger.Log("OnLocalMicrophoneStateUpdated");
        }
    }
}