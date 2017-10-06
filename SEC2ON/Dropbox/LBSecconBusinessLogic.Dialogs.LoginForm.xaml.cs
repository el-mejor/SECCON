using Dropbox.Api;
using System;
using System.Windows;
using System.Windows.Navigation;

namespace SEC2ON.LBSecconBusinessLogic.Dialogs
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class LoginForm : Window
    {
        private string oauth2State;

        public LoginForm(string appKey)
        {
            InitializeComponent();

            Dispatcher.BeginInvoke(new Action<string>(this.Start), appKey);
        }

        public string AccessToken { get; private set; }

        public string UserId { get; private set; }

        public bool Result { get; private set; }

        private void Start(string appKey)
        {
            this.oauth2State = Guid.NewGuid().ToString("N");
            var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Token, appKey, new Uri(Properties.Settings.Default.dropboxRedirectUrl), state: oauth2State);
            this.Browser.Navigate(authorizeUri);
        }

        private void BrowserNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (!e.Uri.ToString().StartsWith(Properties.Settings.Default.dropboxRedirectUrl, StringComparison.OrdinalIgnoreCase))
            {
                // we need to ignore all navigation that isn't to the redirect uri.
                return;
            }

            try
            {
                OAuth2Response result = DropboxOAuth2Helper.ParseTokenFragment(e.Uri);
                if (result.State != this.oauth2State)
                {
                    return;
                }

                this.AccessToken = result.AccessToken;
                this.Uid = result.Uid;
                this.Result = true;
            }
            catch (ArgumentException)
            {
                // There was an error in the URI passed to ParseTokenFragment
            }
            finally
            {
                e.Cancel = true;
                this.Close();
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
