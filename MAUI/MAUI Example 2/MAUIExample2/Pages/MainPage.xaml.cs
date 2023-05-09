using System.Collections.ObjectModel;
using System.Diagnostics;
using static MAUIExample.SharedElements;
using MAUIExample.Templates;
using System.Runtime.InteropServices;
using OBDXMAUI;
using static OBDXMAUI.Scantool;

namespace MAUIExample;

public partial class MainPage : ContentPage
{
    public Command LoadItemsCommand { get; }
    private Thread BGThread = new Thread(() => { });
    private Protocols ProtocolSelected = Protocols.NotConnected; /// FF = not connected

    public MainPage()
    {    
        InitializeComponent();          
        DeviceDisplay.Current.KeepScreenOn = true;
    }

    protected override async void OnAppearing()// works on App() constructor , App OnStart(), MainPage() constructor
    {
        base.OnAppearing();
    }


    private async void UpdateStatus(string text, bool status)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            LayoutUpdateLabel.IsVisible = status;
            LabelUpdateInfo.Text = text;
        });

    }

    private async void AppendLog(string text)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            RichTextBoxLog.Text += text + Environment.NewLine;
        });

    }


    private async void DropDownBoxProtocol_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (DropDownBoxProtocol.SelectedIndex == -1) { ProtocolSelected = Protocols.NotConnected; }
        else if (DropDownBoxProtocol.SelectedIndex == 0) { ProtocolSelected = Protocols.HSCAN; }
        else if (DropDownBoxProtocol.SelectedIndex == 1) { ProtocolSelected = Protocols.MSCAN; }
        else if (DropDownBoxProtocol.SelectedIndex == 2) { ProtocolSelected = Protocols.GMLAN; }
        else if (DropDownBoxProtocol.SelectedIndex == 3) { ProtocolSelected = Protocols.VPW; }
        else if (DropDownBoxProtocol.SelectedIndex == 4) { ProtocolSelected = Protocols.ALDL; }

    }

    private async void ConnectVehicleButton_Clicked(object sender, EventArgs e)
    {


        if (BGThread.IsAlive) { return; }

        if (ProtocolSelected == Protocols.NotConnected) { return; }

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

        AppendLog("Name: " + MyScantool.ToolDetails.Name + Environment.NewLine +
            "Serial: " + MyScantool.ToolDetails.UniqueSerial + Environment.NewLine +
            "Firmware Version: " + MyScantool.ToolDetails.Firmware + Environment.NewLine +
            "Hardware Version: " + MyScantool.ToolDetails.Hardware + Environment.NewLine +
            "Supported Comms: " + MyScantool.ToolDetails.SupportedPCComms + Environment.NewLine +
            "Supported OBD: " + MyScantool.ToolDetails.SupportedProtocols);
        

        UpdateStatus("Preparing Scantool..", true);

        var myrslt = await MyScantool.SetOBDProtocol(ProtocolSelected);
        if (myrslt.Item1 != Errors.Success)
        {
            await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to configure scantool OBD protocol.", "OK"); });
            UpdateStatus("", false);
            return;
        }

        if (ProtocolSelected == Protocols.HSCAN)
        {
             

            //This is setting a filter for a typical GM ECU on id 0x7E0
            CAN_Class.Filter_Struct myfilter = new CAN_Class.Filter_Struct();
            myfilter.Enabled = 1;
            myfilter.Type = 1; //00=Pass,1=flow,2=block
            myfilter.Flow = 0x7E0;
            myfilter.Mask = 0x7FF;
            myfilter.ID = 0x7E8;
            myfilter.IsExtended = 0;
            //Set the filter ID,Mask,Flow, and type.
            var myrslt2 = await MyScantool.CANCommands.SetRxFilterEntire(0, myfilter);
            if (myrslt2.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to configure scantool filter.", "OK"); });
                UpdateStatus("", false);
                return;
            }

            //Enable the protocol to begin reading and writing
            myrslt = await MyScantool.SetOBDEnabledStatus(1);
            if (myrslt.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to enable OBD communciation.", "OK"); });
                UpdateStatus("", false);
                return;
            }



            //Detect ecu present - 1000ms timeout, Only try once.
            var myreslt3 = await MyScantool.WriteThenReadNetworkFrame(0x7E0, new byte[] { 0x1A, 0x90 }, 0x7E8, 0, 1000, 1);
            if (myreslt3.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to detect communciation from Vehicle.", "OK"); });
                UpdateStatus("", false);
                return;
            }
        }
        else if (ProtocolSelected == Protocols.GMLAN)
        {
            //This is setting a filter for a typical GM Radio on id 0x244.
            CAN_Class.Filter_Struct myfilter = new CAN_Class.Filter_Struct();
            myfilter.Enabled = 1;
            myfilter.Type = 1; //00=Pass,1=flow,2=block
            myfilter.Flow = 0x244; 
            myfilter.Mask = 0x7FF;
            myfilter.ID = 0x644;
            myfilter.IsExtended = 0;
            //Set the filter ID,Mask,Flow, and type.
            var myrslt2 = await MyScantool.CANCommands.SetRxFilterEntire(0, myfilter);
            if (myrslt2.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to configure scantool filter.", "OK"); });
                UpdateStatus("", false);
                return;
            }

            //Enable the protocol to begin reading and writing
            myrslt = await MyScantool.SetOBDEnabledStatus(1);
            if (myrslt.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to enable OBD communciation.", "OK"); });
                UpdateStatus("", false);
                return;
            }


            //Detect radio present - 1000ms timeout, Only try once.
            var myreslt3 = await MyScantool.WriteThenReadNetworkFrame(0x244, new byte[] { 0x20 }, 0x644, 0, 1000, 1); //
            if (myreslt3.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to detect communciation from Vehicle.", "OK"); });
                UpdateStatus("", false);
                return;
            }
        }
        else if (ProtocolSelected == Protocols.MSCAN)
        {
            //This is setting a filter for a typical Ford instrument cluster on id 0x720
            CAN_Class.Filter_Struct myfilter = new CAN_Class.Filter_Struct();
            myfilter.Enabled = 1;
            myfilter.Type = 1; //00=Pass,1=flow,2=block
            myfilter.Flow = 0x720;
            myfilter.Mask = 0x7FF;
            myfilter.ID = 0x728;
            myfilter.IsExtended = 0;
            //Set the filter ID,Mask,Flow, and type.
            var myrslt2 = await MyScantool.CANCommands.SetRxFilterEntire(0, myfilter);
            if (myrslt2.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to configure scantool filter.", "OK"); });
                UpdateStatus("", false);
                return;
            }

            //Enable the protocol to begin reading and writing
            myrslt = await MyScantool.SetOBDEnabledStatus(1);
            if (myrslt.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to enable OBD communciation.", "OK"); });
                UpdateStatus("", false);
                return;
            }


            //Detect cluster present - 1000ms timeout, Only try once.
            var myreslt3 = await MyScantool.WriteThenReadNetworkFrame(0x720, new byte[] { 0x01, 0x00 }, 0x728, 0, 1000, 1);
            if (myreslt3.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to detect communciation from Vehicle.", "OK"); });
                UpdateStatus("", false);
                return;
            }
        }
        else if (ProtocolSelected == Protocols.VPW)
        {
            VPW_Class.FilterEntire_Struct myfilter = new VPW_Class.FilterEntire_Struct();
            myfilter.ID = 0x486B10;
            myfilter.Mask = 0x00FFFF;
            myfilter.Type = 0;
            myfilter.Enabled = 1;
            var myrslt2 =  await MyScantool.VPWCommands.SetEntireFilter(0, myfilter);
            if (myrslt2.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to configure scantool filter.", "OK"); });
                UpdateStatus("", false);
                return;
            }

            //Enable the protocol to begin reading and writing
            myrslt = await MyScantool.SetOBDEnabledStatus(1);
            if (myrslt.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to enable OBD communciation.", "OK"); });
                UpdateStatus("", false);
                return;
            }

            //Detect LS1 0411 ECU
            var myreslt3 = await MyScantool.WriteThenReadNetworkFrame(0x686AF1, new byte[] { 0x01, 0x00 }, 0x486B10, 0, 1000, 1);
            if (myreslt3.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to detect communciation from Vehicle.", "OK"); });
                UpdateStatus("", false);
                return;
            }

        }
        else if (ProtocolSelected == Protocols.ALDL)
        {
            var myrslt2 = await MyScantool.ALDLCommands.SetFilterEntire(0,0xF1,0xFF,0,1);
            if (myrslt2.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to configure scantool filter.", "OK"); });
                UpdateStatus("", false);
                return;
            }

            //Enable the protocol to begin reading and writing
            myrslt = await MyScantool.SetOBDEnabledStatus(1);
            if (myrslt.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to enable OBD communciation.", "OK"); });
                UpdateStatus("", false);
                return;
            }

            //Set heartbeat sync message (This is used to search for this meassage BEFORE sending a message
            //this ensure a message is sent with the largest available time). This frame is specific to VT/VX/VY/VZ Holden commodores.
            //This is not required to be done but it highly recommended unless manually processing frames to search for this before sending.
            myrslt2 = await MyScantool.ALDLCommands.SetHeartBeatSyncFrame(new byte[] { 0x8, 0x55, 0xA3 });
            if (myrslt2.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to set ALDL heartbeat frame", "OK"); });
                UpdateStatus("", false);
                return;
            }

            //enables this feature.
            myrslt2 = await MyScantool.ALDLCommands.SetHeartBeatSyncStatus(1);
            if (myrslt2.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to enable hearbeat frame.", "OK"); });
                UpdateStatus("", false);
                return;
            }



            //Detect Holden VT/VX/VY/VZ BCM present - 1000ms timeout, Only try once.
            var myreslt3 = await MyScantool.WriteThenReadNetworkFrame(0xF1, new byte[] { 0x01, 0x00 }, 0xF1, 0, 1000, 1);
            if (myreslt3.Item1 != Errors.Success)
            {
                await MainThread.InvokeOnMainThreadAsync(async () => { await App.Current.MainPage.DisplayAlert("Setup Failed", "Unable to detect communciation from Vehicle.", "OK"); });
                UpdateStatus("", false);
                return;
            }
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
            DropDownBoxProtocol.IsEnabled = false;
            UpdateStatus("", false);
        });

    }

   
}


