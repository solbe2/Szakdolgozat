/////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// This project demonstrates how to write a simple vJoy feeder in C#
//
// You can compile it with either #define ROBUST OR #define EFFICIENT
// The fuctionality is similar - 
// The ROBUST section demonstrate the usage of functions that are easy and safe to use but are less efficient
// The EFFICIENT ection demonstrate the usage of functions that are more efficient
//
// Functionality:
//	The program starts with creating one joystick object. 
//	Then it petches the device id from the command-line and makes sure that it is within range
//	After testing that the driver is enabled it gets information about the driver
//	Gets information about the specified virtual device
//	This feeder uses only a few axes. It checks their existence and 
//	checks the number of buttons and POV Hat switches.
//	Then the feeder acquires the virtual device
//	Here starts and endless loop that feedes data into the virtual device
//
/////////////////////////////////////////////////////////////////////////////////////////////////////////


/*
A programot a vJoy fejlesztői csapata készítette, egy vJoy feeder alkalmazás mintakódjaként.
Én csak néhány változást eszközöltem rajta, hogy tudjon kapcsolatot kiépíteni a mobiltelefonnal, illetve, hogy feldolgozza a telefon által küldött adatokat.
*/



using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;

// Don't forget to add this
using vJoyInterfaceWrap;

namespace FeederDemoCS
{
    class Program
    {
        // Declaring one joystick (Device id 1) and a position structure. 
        static public vJoy joystick;
        static public vJoy.JoystickState iReport;
        static public uint id = 1;


        static void Main(string[] args)
        {
            // Create one joystick object and a position structure.
            joystick = new vJoy();
            iReport = new vJoy.JoystickState();

            
            // Device ID can only be in the range 1-16
            if (args.Length>0 && !String.IsNullOrEmpty(args[0]))
                id = Convert.ToUInt32(args[0]);
            if (id <= 0 || id > 16)
            {
                Console.WriteLine("Illegal device ID {0}\nExit!",id); 
                return;
            }

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!joystick.vJoyEnabled())
            {
                Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
                return;
            }
            else
                Console.WriteLine("Vendor: {0}\nProduct :{1}\nVersion Number:{2}\n", joystick.GetvJoyManufacturerString(), joystick.GetvJoyProductString(), joystick.GetvJoySerialNumberString());

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", id);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free\n", id);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id);
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", id);
                    return;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", id);
                    return;
            };

            // Check which axes are supported
            bool AxisX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
            bool AxisY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
            bool AxisZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
            bool AxisRX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
            bool AxisRY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RY);
            bool AxisRZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
            // Get the number of buttons and POV Hat switchessupported by this vJoy device
            int nButtons = joystick.GetVJDButtonNumber(id);
            int ContPovNumber = joystick.GetVJDContPovNumber(id);
            int DiscPovNumber = joystick.GetVJDDiscPovNumber(id);

            // Print results
            Console.WriteLine("\nvJoy Device {0} capabilities:\n", id);
            Console.WriteLine("Numner of buttons\t\t{0}\n", nButtons);
            Console.WriteLine("Numner of Continuous POVs\t{0}\n", ContPovNumber);
            Console.WriteLine("Numner of Descrete POVs\t\t{0}\n", DiscPovNumber);
            Console.WriteLine("Axis X\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Y\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Z\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Rx\t\t{0}\n", AxisRX ? "Yes" : "No");
            Console.WriteLine("Axis Rz\t\t{0}\n", AxisRZ ? "Yes" : "No");

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                Console.WriteLine("Version of Driver Matches DLL Version ({0:X})\n", DllVer);
            else
                Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})\n", DrvVer, DllVer);


            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                Console.WriteLine("Failed to acquire vJoy device number {0}.\n", id);
                return ;
            }
            else
                Console.WriteLine("Acquired: vJoy device number {0}.\n", id);

            Console.WriteLine("\npress enter to stat feeding");
            Console.ReadKey(true);

            int X, Y, Z, ZR, XR, YR;
            uint count = 0;
            long maxval = 0;

            X = 20;
            Y = 30;
            Z = 40;
            XR = 60;
            ZR = 80;
            YR = 50;

            joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref maxval);

	try {

            IPAddress ipAd = IPAddress.Parse("192.168.1.67");

            //Initializes the Listener
            TcpListener myList = new TcpListener(ipAd, 7000);

            //Start Listeneting at the specified port
            myList.Start();

            Console.WriteLine("The server is running at port 7000...");
            Console.WriteLine("The local End point is  :" +
                              myList.LocalEndpoint);
            Console.WriteLine("Waiting for a connection.....");
            Socket s = myList.AcceptSocket();
            Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);
			
            byte[] bytes = new Byte[1024];
            string data = null;
			
			bool res;
			// Reset this device to default values
			joystick.ResetVJD(id);
  
            while (true) {
				
				// Set position of 6 axes
				res = joystick.SetAxis(X, id, HID_USAGES.HID_USAGE_X);
				res = joystick.SetAxis(Y, id, HID_USAGES.HID_USAGE_Y);
				res = joystick.SetAxis(Z, id, HID_USAGES.HID_USAGE_Z);
				res = joystick.SetAxis(XR, id, HID_USAGES.HID_USAGE_RX);
                res = joystick.SetAxis(YR, id, HID_USAGES.HID_USAGE_RY);
				res = joystick.SetAxis(ZR, id, HID_USAGES.HID_USAGE_RZ);
  
                int numByte = s.Receive(bytes);
				
				data = Encoding.ASCII.GetString(bytes, 0, numByte);
				string[] strlist = data.Split('_');

				//Előre definiált értékek, amiket tesztelés útján határoztam meg, illetve pontosítottam
				//Ezekkel az értékekkel a szimulátorban talajszint felett egy picivel, egyensúlyban lebeg a szimulált drón
                X = Convert.ToInt32(maxval * 0.5);
                Y = Convert.ToInt32(maxval * 0.5);
                Z = Convert.ToInt32(maxval * 0.28);
                XR = Convert.ToInt32(maxval * 0.5);
                YR = Convert.ToInt32(maxval * 0.5);
                ZR = Convert.ToInt32(maxval * 0.5);

                if (strlist.Length == 4)
                {
                    if(Convert.ToDouble(strlist[1].Replace('.',',')) > 3.0){
                        Console.WriteLine("Up");
						Z += 750; if (Z > maxval) Z = Convert.ToInt32(maxval);
                    }
                    else if (Convert.ToDouble(strlist[1].Replace('.', ',')) < -3.0)
                    {
                        Console.WriteLine("Down");
                        Z -= 750; if (Z < 0) Z = 0;
                    }
                    else
                    {
                        Z = Convert.ToInt32(maxval * 0.28);
                    }

                    if (Convert.ToDouble(strlist[2].Replace('.', ',')) > 3.0)
                    {
                        Console.WriteLine("Forward");
                        Y += 500; if (Y > maxval) Y = Convert.ToInt32(maxval);
                    }
                    else if (Convert.ToDouble(strlist[2].Replace('.', ',')) < -3.0)
                    {
                        Console.WriteLine("Backward");
						Y -= 500; if (Y < 0) Y = 0;
                    }
                    else
                    {
                        Y = Convert.ToInt32(maxval * 0.5);
                    }

                    if (Convert.ToDouble(strlist[0].Replace('.', ',')) > 3.0)
                    {
                        Console.WriteLine("Left");
                        X -= 750; if (X < 0) X = 0;
                    }
                    else if (Convert.ToDouble(strlist[0].Replace('.', ',')) < -3.0)
                    {
                        Console.WriteLine("Right");
                        X += 750; if (X > maxval) X = Convert.ToInt32(maxval);
                    }
                    else
                    {
                        X = Convert.ToInt32(maxval * 0.5);
                    }
                }

                if (strlist[0] == "exit")
                {
                    X = Convert.ToInt32(maxval * 0.5);
                    Y = Convert.ToInt32(maxval * 0.5);
                    Z = Convert.ToInt32(maxval * 0.28);
                    XR = Convert.ToInt32(maxval * 0.5);
                    YR = Convert.ToInt32(maxval * 0.5);
                    ZR = Convert.ToInt32(maxval * 0.5);
                }

            }
  
            Console.WriteLine("Text received -> {0} ", data);
        }
      
    catch (Exception e) {
        Console.WriteLine(e.ToString());
    }

        } // Main
    } // class Program
} // namespace FeederDemoCS
