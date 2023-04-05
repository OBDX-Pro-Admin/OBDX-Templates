using OBDXWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static OBDXWindows.Scantool;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OBDXWindowsExample1
{
    public partial class Form1 : Form
    {
        //This is our scantool instance from OBDXWindows library
        Scantool MyOBDXScantool = new Scantool();
        List<OBDXDevice> FoundOBDXDevicesList = new List<OBDXDevice>();

        public Form1()
        {
            InitializeComponent(); //Sets up form UI and controls
        }

        /*
        * This code runs as soon as the Form Loads
        * We use this point to set the communication type we want to use
        */
        private void Form1_Load(object sender, EventArgs e)
        {
            MyOBDXScantool.SelectCommunicationType(ConnectionTypeEnum.USB);//select communication type as USB
            // TODO: Add support for classic BT and USB.
            AppendToLog("Form Loaded");
        }


        /*
         * combo box will display all currently available OBDX tools which are not in use
         */
        private async void comboBoxTools_DropDown(object sender, EventArgs e)
        {
            comboBoxTools.Items.Clear(); //clear old found tools from combo box
            FoundOBDXDevicesList.Clear(); //clear old found tool from list
            Tuple<Errors, List<OBDXDevice>> FoundDevices = await MyOBDXScantool.SearchForDevices(); //search for available devices
            if (FoundDevices.Item1 != Errors.Success)
            {
                //Error occured while searching for device, error can be processed here
                return;
            }

            AppendToLog("OBDX Scantools Found: " + FoundDevices.Item2.Count);
            foreach (OBDXDevice tempdevice in FoundDevices.Item2)
            {
                //Add each found device to the combo box and list.
                FoundOBDXDevicesList.Add(tempdevice);
                comboBoxTools.Items.Add(tempdevice.UniqueIDString);
            }
        }


        /*
         * This function will do the following actions in order:
         * 1) Connect to selected scantool from the combo box
         * 2) Read scantool information and display to richtextbox
         * 3) Set OBD protocol to HS CAN
         * 4) Set a filter to 7E8 (GM ECU ID)
         * 5) Enable OBD communication
         * 6) Send an OBD request (Mode 1, 00)
         * 7) Search for a response and display to screen
         * 
         */
        private async void buttonConnect_Click(object sender, EventArgs e)
        {
            if (comboBoxTools.SelectedIndex == -1)
            {
                //No tool selected
                MessageBox.Show("Please select a tool from the drop down box before attempting to connect.", "Hold Up!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Attempts to connect to scantool selected from combobox.
            AppendToLog("Connecting to Scantool...");
           Errors RsltError1 = await MyOBDXScantool.Connect(FoundOBDXDevicesList[comboBoxTools.SelectedIndex]);
            if (RsltError1 != Errors.Success)
            {
                AppendToLog("Failed to Connect");
                await MyOBDXScantool.Disconnect();
                //Process error here if not successful.
                //Errors possible in Connect are: 
                MessageBox.Show("An error has occured while trying to connect to the selected OBDX device." + Environment.NewLine + Environment.NewLine +
                    "Error: " + RsltError1.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            //Get scantool name
            // MyOBDXScantool.Details.Name
            AppendToLog("OBDX Scantool Connected" + Environment.NewLine +
                "Name: " + MyOBDXScantool.Details.Name + Environment.NewLine +
                "Hardware Version: " + MyOBDXScantool.Details.Hardware + Environment.NewLine +
                "Firmware Version: " + MyOBDXScantool.Details.Firmware + Environment.NewLine +
                "Unique ID: " + MyOBDXScantool.Details.UniqueSerial + Environment.NewLine +
                "Supported OBD Protocols: " + MyOBDXScantool.Details.SupportedProtocols + Environment.NewLine +
                "Supported Communication: " + MyOBDXScantool.Details.SupportedPCComms);



            //Read Battery Voltage from pin 16 of diagnostic connector (DLC).
            Tuple<Errors, double> RsltError0 = await MyOBDXScantool.ReadBatteryVoltage();
            if (RsltError0.Item1 != Errors.Success)
            {
                //Errors possible in set OBD Protocol are: 
                await MyOBDXScantool.Disconnect();
                MessageBox.Show("An error has occured while trying to read voltage." + Environment.NewLine + Environment.NewLine +
                    "Error: " + RsltError0.Item1.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            AppendToLog("Battery Voltage is: " + RsltError0.Item2.ToString("N2"));


            //Set OBD Protocol to CANBus - returns error code plus protocol byte set.
            AppendToLog("Setting OBD Protocol to HS CAN");
           Tuple<Errors, byte> RsltError2 = await MyOBDXScantool.SetOBDProtocol(Protocols.HSCAN);
            if (RsltError2.Item1 != Errors.Success)
            {
                //Errors possible in set OBD Protocol are: 
                await MyOBDXScantool.Disconnect();
                MessageBox.Show("An error has occured while trying to set OBD Protocol." + Environment.NewLine + Environment.NewLine +
                    "Error: " + RsltError2.Item1.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

          

            /*Set filter for the following:
             * ID = 0x7E8,  Response ID from GM ECUs
             * Mask = 0x7FF, all bits for 11bit filter must match exactly
             * Flow = 0x7E0, this is the header ID the tool will use when a flow control message is required on larger messages
             * IsExtended = 0, 11bit filter (0 = 11bit, 1=29bit)
             * Type = 1, Set filter to use Flow (0=Pass, 1=Flow, 2=Block)
             * Enabled = 1, enable this filter (0=disable, 1=enable)
             */
            AppendToLog("Setting Filter");
            Tuple<Errors, byte[]> RsltError3 = await MyOBDXScantool.CANCommands.SetRxFilterEntire(0, new CAN_Class.Filter(0x7E8, 0x7FF, 0x7E0, 0, 1, 1));
            if (RsltError3.Item1 != Errors.Success)
            {
                //Errors possible in set OBD Protocol are: 
                await MyOBDXScantool.Disconnect();
                MessageBox.Show("An error has occured while trying to set OBD Filter." + Environment.NewLine + Environment.NewLine +
                    "Error: " + RsltError3.Item1.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            //enable the protocol for reading and writing
            AppendToLog("Enabling OBD protocol communication");
            RsltError2 = await MyOBDXScantool.SetOBDEnabledStatus(1);
            if (RsltError2.Item1 != Errors.Success)
            {
                //Errors possible in set OBD status are: 
                await MyOBDXScantool.Disconnect();
                MessageBox.Show("An error has occured while trying to enable OBD status." + Environment.NewLine + Environment.NewLine +
                    "Error: " + RsltError2.Item1.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }




            //Send OBD message to GM ECU of mode 1, table 0.
            AppendToLog("Sending OBD Network Message to ECU");
            RsltError3 = await MyOBDXScantool.WriteNetworkFrame(0x7E0, new byte[] { 0x01,0x00 });
            if (RsltError3.Item1 != Errors.Success)
            {
                //Errors possible in set OBD Protocol are: 
                await MyOBDXScantool.Disconnect();
                MessageBox.Show("An error has occured while trying write OBD message." + Environment.NewLine + Environment.NewLine +
                    "Error: " + RsltError3.Item1.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            //search for response
            AppendToLog("Searching for response from ECU");
            Tuple<Errors, NetworkMessage> RsltError4 = await MyOBDXScantool.ReadNetworkFrame(2000, 2); //Retry twice with a max of 2000ms timeout on each attempt.
            if (RsltError4.Item1 != Errors.Success)
            {
                //Errors possible in set OBD Protocol are: 
                await MyOBDXScantool.Disconnect();
                MessageBox.Show("An error has occured while trying read OBD message." + Environment.NewLine + Environment.NewLine +
                    "Error: " + RsltError4.Item1.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            AppendToLog("Message Found: " + RsltError4.Item2.ToString());


            AppendToLog("Disconnecting from Scantool");
            await MyOBDXScantool.Disconnect();

        }


        //Appends a message to the debug log along with a timetamp.
        private void AppendToLog(string message)
        {
            richTextBoxLog.AppendText("[" + DateTime.Now.ToString() + "] " + message + Environment.NewLine);
            richTextBoxLog.HideSelection = false; //allows automatic scrolling
        }


    }

}
