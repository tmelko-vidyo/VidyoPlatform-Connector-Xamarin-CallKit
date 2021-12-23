namespace VidyoConnector.Call
{
    public interface ICallCenter
    {
        void ReportIncomingCall(ActiveCall call);
        void EndCall();
    }
}
