using System;
using Foundation;
using CallKit;
using VidyoConnector.Call;

namespace VidyoConnector.iOS.Call
{
    public class CallManager
    {

        #region Private Variables
        private CXCallController CallController = new CXCallController();
        #endregion

        #region Computed Properties
        private ActiveCall ActiveCall { get; set; }
        #endregion

        #region Private Methods
        private void SendTransactionRequest(CXTransaction transaction)
        {
            // Send request to call controller
            CallController.RequestTransaction(transaction, (error) => {
                // Was there an error?
                if (error == null)
                {
                    // No, report success
                    Console.WriteLine("Transaction request sent successfully.");
                }
                else
                {
                    // Yes, report error
                    Console.WriteLine("Error requesting transaction: {0}", error);
                }
            });
        }
        #endregion

        #region Public Methods
        public ActiveCall GetActiveCall()
        {
            return ActiveCall;
        }

        public void StartCall(string contact)
        {
            // Build call action
            var handle = new CXHandle(CXHandleType.Generic, contact);
            var startCallAction = new CXStartCallAction(new NSUuid(), handle);

            // Create transaction
            var transaction = new CXTransaction(startCallAction);

            // Inform system of call request
            SendTransactionRequest(transaction);
        }

        public void PlaceCall(ActiveCall call)
        {
            this.ActiveCall = call;
        }

        public void EndCall()
        {
            if (ActiveCall == null) throw new Exception("No active calls");

            // Build action
            var endCallAction = new CXEndCallAction(new NSUuid(ActiveCall.UUID));

            // Create transaction
            var transaction = new CXTransaction(endCallAction);

            // Inform system of call request
            SendTransactionRequest(transaction);
        }

        public void PlaceCallOnHold()
        {
            if (ActiveCall == null) throw new Exception("No active calls");

            // Build action
            var holdCallAction = new CXSetHeldCallAction(new NSUuid(ActiveCall.UUID), true);

            // Create transaction
            var transaction = new CXTransaction(holdCallAction);

            // Inform system of call request
            SendTransactionRequest(transaction);
        }

        public void RemoveCallFromOnHold()
        {
            if (ActiveCall == null) throw new Exception("No active calls");

            // Build action
            var holdCallAction = new CXSetHeldCallAction(new NSUuid(ActiveCall.UUID), false);

            // Create transaction
            var transaction = new CXTransaction(holdCallAction);

            // Inform system of call request
            SendTransactionRequest(transaction);
        }

        #endregion
    }
}