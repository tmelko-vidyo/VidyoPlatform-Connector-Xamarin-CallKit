using VidyoConnector.Call;

namespace VidyoConnector.iOS.Call
{
    public class CallCenter : ICallCenter
    {

        private CallManager CallManager;
        private ProviderDelegate ProviderDelegate;

        public CallCenter(ProviderDelegate providerDelegate, CallManager callManager)
        {
            this.CallManager = callManager;
            this.ProviderDelegate = providerDelegate;
        }

        public void ReportIncomingCall(ActiveCall call)
        {
            this.ProviderDelegate.ReportIncomingCall(call);
        }

        public void EndCall()
        {
            this.CallManager.EndCall();
            this.CallManager.PlaceCall(null);
        }
    }
}
