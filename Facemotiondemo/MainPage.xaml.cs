using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Facemotiondemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture _mediaCapture;
        public static string facekey = "43dc2c2704e848d2adfc099c36cf32d6";
        IEnumerable<FaceAttributeType> faceAttributes =
        new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Emotion, FaceAttributeType.Glasses, FaceAttributeType.Hair };
        FaceServiceClient fClient = new FaceServiceClient(facekey, "https://southeastasia.api.cognitive.microsoft.com/face/v1.0");
        public static string topic;
        public static string msgbody;
        public static string floor;
        public MainPage()
        {
            this.InitializeComponent();
            Application.Current.Resuming += Application_Resuming;
        }
        private async void Application_Resuming(object sender, object o)
        {
            await InitializeCameraAsync();

        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitializeCameraAsync();

        }
        private async Task InitializeCameraAsync()
        {
            if (_mediaCapture == null)
            {
                // Get the camera devices
                var cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

                // try to get the back facing device for a phone
                var backFacingDevice = cameraDevices
                    .FirstOrDefault(c => c.EnclosureLocation?.Panel == Windows.Devices.Enumeration.Panel.Front);

                // but if that doesn't exist, take the first camera device available
                var preferredDevice = backFacingDevice ?? cameraDevices.FirstOrDefault();

                // Create MediaCapture
                _mediaCapture = new MediaCapture();

                // Initialize MediaCapture and settings
                await _mediaCapture.InitializeAsync(
                    new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = preferredDevice.Id
                    });

                // Set the preview source for the CaptureElement
                PreviewControl.Source = _mediaCapture;

                // Start viewing through the CaptureElement 
                await _mediaCapture.StartPreviewAsync();
            }
        }

        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
        {


            using (var captureStream = new InMemoryRandomAccessStream())
            {
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);
                captureStream.Seek(0);
                try
                {
                    var faces = await fClient.DetectAsync(captureStream.AsStream(), true, false, faceAttributes);
                    //get photo attributes  
                    var id = faces[0].FaceId;
                    var attributes = faces[0].FaceAttributes;
                    var age = attributes.Age;
                    var gender = attributes.Gender;
                    EmotionScores emotionScores = faces[0].FaceAttributes.Emotion;
                    var emotion = emotionScores.ToRankedList().First();
                    var facialHair = attributes.FacialHair;
                    var headPose = attributes.HeadPose;
                    var glasses = attributes.Glasses;
                    txtid.Text = "Id:- " + id.ToString();
                    txtage.Text = "Age:- " + age.ToString() + " Years";
                    txtgender.Text = "Gender:- " + gender.ToUpper();
                    txtemotion.Text = "Emotion:- " + emotion.Key.ToString();
                    txtglass.Text = "Glasses:- " + glasses.ToString();
                    Addtodb(txtid.Text, txtage.Text, txtgender.Text, txtemotion.Text, txtglass.Text);
                }
                catch (FaceAPIException ex)
                {
                    var dialog = new MessageDialog(ex.ErrorMessage);
                    dialog.Title = ex.ErrorCode;
                    await dialog.ShowAsync();
                }
                catch (Exception exp)
                {

                    var dialog = new MessageDialog(exp.Message);
                    dialog.Title = exp.Source;
                    await dialog.ShowAsync();
                }

            }

        }
        private void Addtodb(string id, string age, string gender, string emotion, string glasses)
        {


        }

        private async void btnaddperson_ClickAsync(object sender, RoutedEventArgs e)
        {
            CoreApplicationView newCoreView = CoreApplication.CreateNewView();

            ApplicationView newAppView = null;
            int mainViewId = ApplicationView.GetApplicationViewIdForWindow(
              CoreApplication.MainView.CoreWindow);

            await newCoreView.Dispatcher.RunAsync(
              CoreDispatcherPriority.Normal,
              () =>
              {
                  newAppView = ApplicationView.GetForCurrentView();
                  Window.Current.Content = new addperson();
                  Window.Current.Activate();
              });

            await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
              newAppView.Id,
              ViewSizePreference.UseHalf,
              mainViewId,
              ViewSizePreference.UseHalf);

        }

        private async void Buttonidentify_ClickAsync(object sender, RoutedEventArgs e)
        {
            using (var captureStream = new InMemoryRandomAccessStream())
            {
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);
                captureStream.Seek(0);
                try
                {
                    var faces = await fClient.DetectAsync(captureStream.AsStream());
                    var faceIds = faces.Select(face => face.FaceId).ToArray();
                    string persongrpid = "smartapp";
                    var results = await fClient.IdentifyAsync(persongrpid, faceIds);
                    foreach (var identifyResult in results)
                    {
                        if (identifyResult.Candidates.Length == 0)
                        {
                            txtnamei.Text = "Identified: none";
                        }
                        else
                        {
                            // Get top 1 among all candidates returned
                            var candidateId = identifyResult.Candidates[0].PersonId;
                            var person = await fClient.GetPersonAsync(persongrpid, candidateId);
                            txtnamei.Text = "Identified: " + person.Name;
                            if (person.Name == "naginder")
                            {
                                msgbody = "6";
                            }
                            else
                            {
                                msgbody = floor;
                            }
                            MqttClient client = new MqttClient("10.0.68.69");
                            byte code = client.Connect(Guid.NewGuid().ToString());
                            client.MqttMsgPublished += client_MqttMsgPublished;
                            topic = "SmartBuildingDemo/1/elevator/floor";
                            byte[] msgbyte = Encoding.ASCII.GetBytes(msgbody);
                            ushort msgId = client.Publish(topic, // topic
                                                          msgbyte, // message body
                                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                                          false); // 
                            var dialog = new MessageDialog("Hi " + person.Name + ", Taking you to your residence on level " + msgbody);
                            await dialog.ShowAsync();
                            msgbody = "1";
                            msgbyte = Encoding.ASCII.GetBytes(msgbody);
                            await Task.Delay(9000);
                            msgId = client.Publish(topic, // topic
                                                          msgbyte, // message body
                                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                                          false); // 
                            
                            

                        }
                    }

                }
                catch (FaceAPIException ex)
                {
                    var dialog = new MessageDialog(ex.ErrorMessage);
                    dialog.Title = ex.ErrorCode;
                    await dialog.ShowAsync();
                }
                catch (Exception exp)
                {

                    var dialog = new MessageDialog(exp.Message);
                    dialog.Title = exp.Source;
                    await dialog.ShowAsync();
                }

            }
        }
        void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            //MessageBox.Show("MessageId = " + e.MessageId + " Published = " + e.IsPublished);
        }

    }
}
