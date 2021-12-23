using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VidyoConnector.Call;
using Xamarin.Forms;

namespace VidyoConnector
{
    public partial class HomePage : ContentPage
    {

        private IVidyoController mVidyoController;
        private ICallCenter CallCenter;

        public HomePage()
        {
            InitializeComponent();
        }

        public HomePage(IVidyoController vidyoController, ICallCenter callCenter)
        {
            InitializeComponent();
            this.mVidyoController = vidyoController;
            this.CallCenter = callCenter;
        }

        void OnStartConference(object sender, EventArgs args)
        {
            ActiveCall activeCall = new ActiveCall(Guid.NewGuid().ToString(), "Vidyo User Incoming", false);
            activeCall.ConnectedChanged += async (ActiveCall call) => {
                if (call.IsConnected)
                {
                    VideoPage compositeLayoutPage = new VideoPage();
                    compositeLayoutPage.Initialize(this.mVidyoController, this.CallCenter);

                    await Navigation.PushModalAsync(compositeLayoutPage);
                }
                else
                {
                    this.mVidyoController.WrapCall();
                }
            };

            this.CallCenter.ReportIncomingCall(activeCall);
        }
    }
}