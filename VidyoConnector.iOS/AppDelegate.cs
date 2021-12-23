using Foundation;
using UIKit;
using VidyoConnector.iOS.Call;

namespace VidyoConnector.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {

        private CallManager CallManager;
        private ProviderDelegate CallProviderDelegate { get; set; }


        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions) {
            global::Xamarin.Forms.Forms.Init();

            CallManager = new CallManager();
            CallProviderDelegate = new ProviderDelegate(CallManager);

            LoadApplication(new App(VidyoController.GetInstance(), new CallCenter(CallProviderDelegate, CallManager)));
            return base.FinishedLaunching(uiApplication, launchOptions);
        }

        public override void WillEnterForeground(UIApplication application)
        {
            base.WillEnterForeground(application);
        }
    }
}