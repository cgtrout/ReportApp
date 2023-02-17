using CefSharp;
using CefSharp.Wpf;
using ReportApp.Utility;
using System;

namespace ReportApp.ViewModel
{
     public class WebBrowserViewModel : WorkspaceViewModel
     {
          #region Fields

          private static WebBrowserViewModel webBrowserViewModel;
          private BrowserPageMode Mode;

          #endregion Fields

          #region Enums

          public enum BrowserPageMode
          {
               Person,
               Time,
               AccessLog
          };

          #endregion Enums

          #region Properties

          public static ChromiumWebBrowser Browser { get; set; } = new ChromiumWebBrowser();

          public static WebBrowserViewModel Instance
          {
               get
               {
                    if (webBrowserViewModel == null) {
                         webBrowserViewModel = new WebBrowserViewModel();
                    }
                    return webBrowserViewModel;
               }
          }

          public PersonViewModel Person { get; set; } = null;

          //params - currently only used for access list
          public string Param { get; set; }

          public string Param2 { get; set; }

          #endregion Properties

          #region Methods

          //public WebBrowserViewModel(PersonViewModel p)
          //{
          //     Initialize();
          //     Person = p;
          //}
          public void Initialize(BrowserPageMode mode)
          {
               //ensure we start at start page if already loaded
               if (Browser.GetBrowser() != null) {
                    Browser.ExecuteScriptAsync("mainFrame.upperFrame.topMenu.startPage()");
               }

               this.Mode = mode;

               base.DisplayName = "Browser";
               OnPropertyChanged("DisplayName");

               base.OnlyOneCanRun = true;

               if (mode == BrowserPageMode.AccessLog) {
                    LoadAccessLogInvalid();
                    base.DisplayName = $"Access: {Param2}";
                    OnPropertyChanged("DisplayName");
               } else {
                    Browser.Address = "http://192.168.0.200/";
               }
               Browser.FrameLoadEnd += Browser_FrameLoadEnd;
          }

          protected override void OnDispose()
          {
               Browser.FrameLoadEnd -= Browser_FrameLoadEnd;
          }

          //public WebBrowserViewModel()
          //{
          //     Initialize();
          //}
          //TODO ensure navigation to people page
          private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
          {
               var frameNames = e.Browser.GetFrameNames();
               if (frameNames.Contains("upllicense")) {
                    Browser.ExecuteScriptAsync("document.selectForm.elements['username'].value = 'admin'");
                    Browser.ExecuteScriptAsync("document.selectForm.elements['password'].value = 'admin'");

                    Browser.ExecuteScriptAsync("document.selectForm.submit()");
                    Browser.ExecuteScriptAsync("mainFrame.upperFrame.topMenu.startPage()");
               } else if (e.Url == @"http://192.168.0.200/menu/en/people/") {
                    if (Person != null) {
                         Browser.ExecuteScriptAsync($"mainFrame.contentFrame.document.getElementById('searchFrm').idno.value = '{Person.PersonId}'");
                         Browser.ExecuteScriptAsync("mainFrame.contentFrame.document.getElementsByName('btnsubmit')[0].click()");
                         Browser.ExecuteScriptAsync("mainFrame.top.toggleTOC()");
                         DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                              Browser.ZoomLevel = -0.25;
                         }));
                    } else if (Mode == BrowserPageMode.Time) {
                         DispatcherHelper.GetDispatcher().Invoke(() => {
                              Browser.Address = "http://192.168.0.200/cfgntp.asp";
                              base.DisplayName = "Time Sync";
                              OnPropertyChanged("DisplayName");
                         });
                    } else if (Mode == BrowserPageMode.AccessLog) {
                         LoadAccessLogInvalid();
                    }
               } else if (e.Url == @"http://192.168.0.200/cfgntp.asp") {
                    //Run Time change
                    Browser.ExecuteScriptAsync("runNtpSync()");
               }
          }

          private void LoadAccessLogInvalid()
          {
               DispatcherHelper.GetDispatcher().Invoke(() => {
                    Browser.Address = $"http://192.168.0.200/carddecoder.asp?&s2logid={Param}";
               });
          }

          #endregion Methods
     }
}