using System.Collections.ObjectModel;
using System.Diagnostics;
using static MAUIExample1.SharedElements;
using MAUIExample1.Templates;
using System.Runtime.InteropServices;
using OBDXMAUI;
using static OBDXMAUI.Scantool;

namespace MAUIExample1;

public partial class MainPage : ContentPage
{
    public Command LoadItemsCommand { get; }
    public ObservableCollection<NewsItem> ListNewsItems { get; }

    private Thread BGThread = new Thread(() => { });

    public MainPage()
    {
        ListNewsItems = new ObservableCollection<NewsItem>();
        // LoadItemsCommand = new Command(async () => await ExecuteLoadNewsItemsCommand());

        InitializeComponent();

        Task.Run(async () => { await LoadBackgroundItems(); }); //runs on background thread                    

        DeviceDisplay.Current.KeepScreenOn = true;

    }

    protected override async void OnAppearing()// works on App() constructor , App OnStart(), MainPage() constructor
    {
        base.OnAppearing();
    }


    private async Task LoadBackgroundItems()
    {
        ExecuteLoadNewsItemsCommand();
        ItemsListViewNews.ItemsSource = ListNewsItems;
    }

    async void ExecuteLoadNewsItemsCommand()
    {
        try
        {
            ListNewsItems.Clear();

            for (int i = 0; i < 30; i++)
            {
                ListNewsItems.Add(new NewsItem(DateTime.Now.AddDays(-i), "Hello This is a News Feed Header!", "And this is the body which will continue across the screen and automatically truncate when offscreen"));
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
        }

    }


    private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {

    }


    private async void UpdateStatus(string text, bool status)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            LayoutUpdateLabel.IsVisible = status;
            LabelUpdateInfo.Text = text;
        });

    }


    private async void ConnectVehicleButton_Clicked(object sender, EventArgs e)
    {

        if (BGThread.IsAlive) { return; }
 

        BGThread = new Thread(() => ConnectVehicle_UsingMyScantoolImplmentation());
        BGThread.IsBackground = true;
        BGThread.Start();

    }

 
    private async void ConnectVehicle_UsingMyScantoolImplmentation()
    {
        //Select connection type - This will initiate the IOBDX type as the Bluetooth partial class.
        MyScantool.SelectCommunicationType(IOBDXBase.ConnectionTypeEnum.BLE);

        //Check Permissions
        if (await MyScantool.CheckIfPermissionsAreAllowed() == false)
        {
            UpdateStatus("Requesting Permissions", false);
            if (await MyScantool.RequestForPermissions() == false)
            {
                if (DeviceInfo.Platform == DevicePlatform.iOS) //iOS will only ask ONCE, after this, it will no longer display the permissions request, user MUST open settings
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await App.Current.MainPage.DisplayAlert("Permissions", "Permissions were not granted, please open settings and allow all requested permission to proceed."
                        + Environment.NewLine + "Application will now close.", "OK");
                    });
                    Application.Current.Quit(); //Close application because cannot do anything!
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Permissions", "Permissions were not granted, unable to proceed with scantool connection", "OK"); });
                }
                UpdateStatus("", false);
                return;
            }
        }


        //Fast connect can be used for getting PAIRED devices and trying them first
        //or manually entering development tools to avoid the 10-15second scan period.
        bool fastconnect = false;
        OBDXDevice TempDeviceToConnect = new OBDXDevice("OBDX Pro GT", "94E686ACD44E");
        if (fastconnect == false)
        {

            //Search for new devices
            //get paired devices first, match against scanned devices
            UpdateStatus("Searching for Devices", true);
            var FoundTools = await MyScantool.SearchForDevices(false);
            if (FoundTools.Item1 != Scantool.Errors.Success)
            {
                //Process error
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Connection Failed", "An error has occured while scanning for devices.", "OK"); });
                UpdateStatus("", false);
                return;
            }

            if (FoundTools.Item2.Count == 0)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Connection Failed", "There are no scantools available, please make sure it is connected to the vehicle before retrying again.", "OK"); });
                UpdateStatus("", false);
                return;
            }

            TempDeviceToConnect = FoundTools.Item2[0];
            if (FoundTools.Item2.Count > 1)
            {
                string[] tempstrings = new string[FoundTools.Item2.Count];
                for (int i = 0; i < FoundTools.Item2.Count; i++)
                {
                    tempstrings[i] = FoundTools.Item2[i].Name + "   (" + FoundTools.Item2[i].UniqueIDString + ")";
                }

                //run on main ui.
                string result = "";
                await MainThread.InvokeOnMainThreadAsync(async () => { result = await App.Current.MainPage.DisplayActionSheet("Please Select Scantool", "", "", tempstrings); });

                //Return failed if user selected nothing.
                if (result == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Connection Failed", "A scantool must be selected to be able to proceed, connection has been cancelled.", "OK"); });
                    UpdateStatus("", false);
                    return;
                }

                //Update with selected tool
                TempDeviceToConnect = FoundTools.Item2[tempstrings.ToList().IndexOf(result)];
            }
        }

        UpdateStatus("Found: " + TempDeviceToConnect.Name + Environment.NewLine +
            "Connecting to Device", true);

        if (await MyScantool.Connect(TempDeviceToConnect) != Errors.Success)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Connection Failed", "Failed to connect to selected scantool, please ensure the tool is connected to the vehicle and you are close to the tool.", "OK"); });
            UpdateStatus("", false);
            return;
        }


        UpdateStatus("Preparing Scantool..", true);

        var myrslt = await MyScantool.SetOBDProtocol(Protocols.HSCAN);
        if (myrslt.Item1 != Errors.Success)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to configure scantool OBD protocol.", "OK"); });
            UpdateStatus("", false);
            return;
        }

        CAN_Class.Filter_Struct myfilter = new CAN_Class.Filter_Struct();
        myfilter.Enabled = 1;
        myfilter.Type = 1;
        myfilter.RTR = 0;
        myfilter.Flow = 0x7E0;
        myfilter.Mask = 0x7FF;
        myfilter.ID = 0x7E8;
        myfilter.IsExtended = 0;
        var myrslt2 = await MyScantool.CANCommands.SetRxFilterEntire(0, myfilter);
        if (myrslt2.Item1 != Errors.Success)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to configure scantool filter.", "OK"); });
            UpdateStatus("", false);
            return;
        }

        myrslt = await MyScantool.SetOBDEnabledStatus(1);
        if (myrslt.Item1 != Errors.Success)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to enable OBD communciation.", "OK"); });
            UpdateStatus("", false);
            return;
        }


        //Detect ECU present - 1000ms timeout, Only try once.
        var myreslt3 = await MyScantool.WriteThenReadNetworkFrame(0x7E0, new byte[] { 0x1A, 0x90 }, 0x7E8, 0, 1000, 1);
        if (myreslt3.Item1 != Errors.Success)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to detect communciation from Vehicle.", "OK"); });
            UpdateStatus("", false);
            return;
        }


        var myvolts = await MyScantool.ReadBatteryVoltage();
        if (myvolts.Item1 != Errors.Success)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to read battery voltage.", "OK"); });
            UpdateStatus("", false);
            return;
        }


        UpdateStatus("Connection Successful" + Environment.NewLine +
            "ECU was Detected" + Environment.NewLine +
            "Voltage is: " + myvolts.Item2.ToString("0.00") + "V", true);

        await Task.Delay(2000);


        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            ConnectVehicleButton.Text = "Connected!";
            ConnectVehicleButton.IsEnabled = false;
            ConnectVehicleButton.BackgroundColor = Colors.Green;
            UpdateStatus("", false);
        });

    }





}


