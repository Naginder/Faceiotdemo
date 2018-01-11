using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Facemotiondemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class addperson : Page
    {
        public static string facekey1 = MainPage.facekey;
        FaceServiceClient fClient = new FaceServiceClient(facekey1, "https://southeastasia.api.cognitive.microsoft.com/face/v1.0");
        Stream st;
        public addperson()
        {
            this.InitializeComponent();

        }
        private async void btnfile_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".png");

            StorageFile file = await openPicker.PickSingleFileAsync();


            if (file != null)
            {

                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                st = stream.AsStream();
            }
            else
            {
                // 
            }
        }

        private async void btntrain_Click(object sender, RoutedEventArgs e)
        {
            // Create an empty person group
            string persongrpid = "smartapp";
            PersonGroup[] persons = await fClient.ListPersonGroupsAsync();
            bool found = false;
            foreach (PersonGroup person in persons)
            {
                if (person.Name == "smart app")
                    found = true;                                        
            }
            if (!found)
            {
                await fClient.CreatePersonGroupAsync(persongrpid, "smart app");

            }
            // Define myself
            // Id of the person group that the person belonged to
            CreatePersonResult friend1 = await fClient.CreatePersonAsync(persongrpid, txtname.Text);
            await fClient.AddPersonFaceAsync(persongrpid, friend1.PersonId, st);
            await fClient.TrainPersonGroupAsync(persongrpid);
            TrainingStatus trainstat = null;
            while (true)
            {
                trainstat = await fClient.GetPersonGroupTrainingStatusAsync(persongrpid);
                if (trainstat.Status.ToString() != "running")
                {
                    break;
                }
            }
            MainPage.floor = txtfloor.Text;
            var dialog = new MessageDialog("training done");
            await dialog.ShowAsync();
        }

        private async void btngetreg_Click(object sender, RoutedEventArgs e)
        {
            Person[] persons = await fClient.ListPersonsAsync("smartapp");
            foreach (Person person in persons)
            {
                lvpersons.Items.Add(person.Name);
            }

        }

        private async void btndelete_Click(object sender, RoutedEventArgs e)
        {
            Guid personid;
            Person[] persons = await fClient.ListPersonsAsync("smartapp");
            foreach (Person person in persons)
            {
                if (person.Name == lvpersons.SelectedItem.ToString())
                    personid = person.PersonId;
            }
            if (personid !=null)
                await fClient.DeletePersonAsync("smartapp", personid);
            persons = await fClient.ListPersonsAsync("smartapp");
            lvpersons.Items.Clear();
            foreach (Person person in persons)
            {
                lvpersons.Items.Add(person.Name);
            }
        }
    }
}
