using System;
using Foundation;
using CallKit;
using VidyoConnector.Call;
using AVFoundation;

namespace VidyoConnector.iOS.Call
{
    public class ProviderDelegate : CXProviderDelegate
    {
        #region Computed Properties
        public CallManager CallManager { get; set; }
        public CXProvider Provider { get; set; }
        #endregion

        #region Constructors
        public ProviderDelegate(CallManager callManager)
        {
            // Save connection to call manager
            CallManager = callManager;

            // Define handle types
            var handleTypes = new[] { (NSNumber)(int)CXHandleType.PhoneNumber };

            CXProviderConfiguration Configuration = new CXProviderConfiguration("Vidyo Call")
            {
                MaximumCallsPerCallGroup = 1,
                SupportsVideo = true,
                SupportedHandleTypes = new NSSet<NSNumber>(handleTypes),
            };

            // Create a new provider
            Provider = new CXProvider(Configuration);

            // Attach this delegate
            Provider.SetDelegate(this, null);

        }
        #endregion

        #region Override Methods
        public override void DidReset(CXProvider provider)
        {
            // Remove call
            CallManager.PlaceCall(null);
        }

        public override void PerformStartCallAction(CXProvider provider, CXStartCallAction action)
        {
            ConfigureAudioSession();

            // Create new call record
            var activeCall = new ActiveCall(action.CallUuid.AsString(), action.CallHandle.Value, true);

            // Monitor state changes
            activeCall.StartingConnectionChanged += (call) =>
            {
                if (call.IsConnecting)
                {
                    // Inform system that the call is starting
                    Provider.ReportConnectingOutgoingCall(new NSUuid(call.UUID), new NSDate(call.StartedConnectingOn));
                }
            };

            activeCall.ConnectedChanged += (ActiveCall call) =>
            {
                if (call.IsConnected)
                {
                    // Inform system that the call has connected
                    provider.ReportConnectedOutgoingCall(new NSUuid(call.UUID), new NSDate(call.ConnectedOn));
                }
            };


            // Yes, inform the system
            action.Fulfill();

            // Add call to manager
            CallManager.PlaceCall(activeCall);
        }

        public override void PerformAnswerCallAction(CXProvider provider, CXAnswerCallAction action)
        {
            ConfigureAudioSession();

            var call = CallManager.GetActiveCall();
            if (call == null)
            {
                action.Fail();
                return;
            }

            action.Fulfill();
        }

        public override void PerformEndCallAction(CXProvider provider, CXEndCallAction action)
        {
            var call = CallManager.GetActiveCall();
            if (call == null)
            {
                action.Fail();
                return;
            }

            call.EndCall();
            CallManager.PlaceCall(null);

            action.Fulfill();
        }

        public override void PerformSetHeldCallAction(CXProvider provider, CXSetHeldCallAction action)
        {
            var call = CallManager.GetActiveCall();
            if (call == null)
            {
                action.Fail();
                return;
            }

            call.IsOnHold = action.OnHold;
            action.Fulfill();
        }

        public override void TimedOutPerformingAction(CXProvider provider, CXAction action)
        {
            // Inform user that the action has timed out
        }

        public override void DidActivateAudioSession(CXProvider provider, AVAudioSession audioSession)
        {
            // Start the calls audio session here
            Console.WriteLine("Activate AVAudioSession");

            var call = CallManager.GetActiveCall();
            if (call != null)
            {
                call.StartCall();
            }
        }

        public override void DidDeactivateAudioSession(CXProvider provider, AVAudioSession audioSession)
        {
            // End the calls audio session and restart any non-call
            // related audio

            Console.WriteLine("Did deactivate AVAudioSession");
            CallManager.PlaceCall(null);
        }
        #endregion

        #region Public Methods
        public void ReportIncomingCall(ActiveCall call)
        {
            Console.WriteLine("Report incoming call. UUID:" + call.UUID + ", Handle: " + call.Handle);

            // Create update to describe the incoming call and caller
            var update = new CXCallUpdate();
            update.RemoteHandle = new CXHandle(CXHandleType.Generic, call.Handle);

            // Report incoming call to system
            Provider.ReportNewIncomingCall(new NSUuid(call.UUID), update, (error) =>
            {
                // Was the call accepted
                if (error == null)
                {
                    // Yes, report to call manager
                    CallManager.PlaceCall(call);
                }
                else
                {
                    // Report error to user here
                    Console.WriteLine("Error: {0}", error);
                }
            });
        }
        #endregion

        private void ConfigureAudioSession()
        {
            AVAudioSession audioSession = AVAudioSession.SharedInstance();
            audioSession.SetCategory(AVAudioSession.CategoryPlayAndRecord);
            audioSession.SetActive(true);
        }
    }
}
