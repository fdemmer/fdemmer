/*

PasteToFile

Version     0.2
Constact    <florian@demmer.org>
WWW         http://florian.demmer.org
License     GPL 2.0

requires .net Framework 2.0

*/


using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PasteToFile
{

    class RegistryHandler
    {
        private RegistryKey hkcu;       // HKEY_CURRENT_USER
        private RegistryKey hkcr;       // HKEY_CLASSES_ROOT

        private String PathText;        // path of registry key for contextmenu text
        private String PathCommand;     // path of registry key for contextmenu command 
        private String PathSoftware;    // path of registry key for configuration

        internal RegistryKey Config;    // registry key for configuration key/values
        private RegistryKey RegMenu;    // registry key for contextmenu text
        private RegistryKey RegCommand; // registry key for contextmenu command

        public RegistryHandler()
        {
            hkcu = Registry.CurrentUser;
            hkcr = Registry.ClassesRoot;

            PathText = "Folder\\shell\\" + Ressource.Title;
            PathCommand = "Folder\\shell\\" + Ressource.Title + "\\command";
            PathSoftware = "Software\\";

            // open existing settings, writeable
            //if ((Config = open(Ressource.RegistryPath + Ressource.Title, true)) == null)
                // key was not found, create a new one, writeable
            Config = createCurrentUserKey(PathSoftware + Ressource.Title);
        }

        private Microsoft.Win32.RegistryKey open(String name, bool writeable)
        {
            try
            {
                // open subkey
                Config = hkcu.OpenSubKey(PathSoftware + name, writeable);
            }
            catch (System.ObjectDisposedException)
            {
                // registry key is closed and connot be opened
            }
            catch (System.Security.SecurityException)
            {
                // insufficient rights to access key
            }
            return Config;
        }

        // create a new registry key
        private RegistryKey createKey(String name, RegistryKey hierachy)
        {
            RegistryKey key = null;

            try
            {
                // open/create *key* with read/write in *hierachy*
                key = hierachy.CreateSubKey(name);
            }
            catch (System.ObjectDisposedException)
            {
                // registry key is closed and connot be opened
            }
            catch (System.Security.SecurityException)
            {
                // insufficient rights to access key
            }
            catch (System.IO.IOException)
            {
                // systemerror, boxing > 510, wrong tree (local machine)
            }
            catch (System.UnauthorizedAccessException)
            {
                // insufficient rights to access key
            }

            return key;
        }

        // create a new registry key in the CURRENT_USER hierachy
        internal RegistryKey createCurrentUserKey(String name)
        {
            return createKey(name, hkcu);
        }

        // create a new registry key in the CLASSES_ROOT hierachy
        internal RegistryKey createClassesRootKey(String name)
        {
            return createKey(name, hkcr);
        }

        // remove a registry key from the CURRENT_USER hierachy
        private void removeCurrentUserKey(String name)
        {
            hkcu.DeleteSubKeyTree(name);
        }

        // remove a registry key from the CLASSES_ROOT hierachy
        private void removeClassesRootKey(String name)
        {
            hkcr.DeleteSubKeyTree(name);
        }

        internal void registerContextItem(String text, String command)
        {
            try
            {
                RegMenu = Registry.ClassesRoot.CreateSubKey(PathText);
                if (RegMenu != null)
                    RegMenu.SetValue("", text);
                RegCommand = Registry.ClassesRoot.CreateSubKey(PathCommand);
                if (RegCommand != null)
                    RegCommand.SetValue("", command);
            }
            catch (Exception ex)
            {
                //TODO make doballoon global
                //prog.doBalloon(ex.ToString(), Ressource.Title, ToolTipIcon.Error);
            }
            finally
            {
                if (RegMenu != null)
                    RegMenu.Close();
                if (RegCommand != null)
                    RegCommand.Close();
            }
        }

        internal void deleteContextItem()
        {
            RegistryKey RegKey = null;
            try
            {
                RegKey = hkcr.OpenSubKey(PathCommand);
                if (RegKey != null)
                {
                    RegKey.Close();
                    hkcr.DeleteSubKey(PathCommand);
                }
                RegKey = Registry.ClassesRoot.OpenSubKey(PathText);
                if (RegKey != null)
                {
                    RegKey.Close();
                    hkcr.DeleteSubKey(PathText);
                }
            }
            catch (Exception ex)
            {
                //TODO make doballoon global
                //prog.doBalloon(ex.ToString(), Ressource.Title, ToolTipIcon.Error);
            }
        }

        internal bool existingContextItem()
        {
            bool retval = false;

            try
            {
                RegCommand = hkcr.OpenSubKey(PathCommand);
                RegMenu = hkcr.OpenSubKey(PathText);
                if (RegCommand != null && RegMenu != null)
                    retval = true;
            }
            catch (Exception ex)
            {
                //TODO make doballoon global
                //prog.doBalloon(ex.ToString(), Ressource.Title, ToolTipIcon.Error);
            }

            return retval;
        }

    } // end of class

    class Ressource
    {
        internal static String Title        = "PasteToFile";
        internal static String Version      = "0.2";
        internal static String Author       = "Florian Demmer";
        internal static String Website      = "http://florian.demmer.org";
        internal static String Email        = "florian@demmer.org";
        internal static String License      = "GPL 2.0";
    
        internal static String RegKey_Version       = "Version";
        internal static String RegKey_Mask_File     = "FileMask";
        internal static String RegKey_Mask_Date     = "DateMask";
        internal static String RegKey_Mask_Time     = "TimeMask";
        internal static String RegKey_OutputPath    = "OutputPath";
        internal static String RegKey_ImageFormat   = "ImageFormat";
        internal static String RegKey_UpperCaseExt  = "UppercaseExtension";
        internal static String RegKey_BalloonTimeout = "BalloonTimeout";
        internal static String Mask_Date            = "<date>";
        internal static String Mask_Time            = "<time>";
        internal static String Mask_Extension       = "<ext>";
        internal static String Default_Mask_File    = "sshot-<date>-<time>.<ext>";
        internal static String Default_Mask_Date    = "yyyyMMdd";
        internal static String Default_Mask_Time    = "HHmmss";
        internal static String Default_OutputPath   = "";
        internal static bool   Default_UpperCaseExt = false;
        internal static bool   Default_ContextItem  = true;
        internal static int    Default_ImageFormat  = 0;
        internal static int    Default_BalloonTimeout = 3000;

    } // end of class

    class Program
    {
        internal RegistryHandler Reg;
        internal NotifyIcon TrayIcon;

        public Program()
        {
            // access registry
            Reg = new RegistryHandler();

            // prepare notification area
            TrayIcon = new System.Windows.Forms.NotifyIcon();
            TrayIcon.Icon = Resources.IconApp;
        }

        internal void showAbout()
        {
            Application.Run(new About());
        }

        internal void showOptions()
        {
            Application.Run(new Options(Reg));
        }
        
        private String getFilename(String extension)
        {
            // retrieve filename mask setting
            String filename = (String)Reg.Config.GetValue(Ressource.RegKey_Mask_File, Ressource.Default_Mask_File);

            // generate timestamps
            DateTime d = DateTime.Now;
            String date = d.ToString((String)Reg.Config.GetValue(Ressource.RegKey_Mask_Date, Ressource.Default_Mask_Date));
            String time = d.ToString((String)Reg.Config.GetValue(Ressource.RegKey_Mask_Time, Ressource.Default_Mask_Time));

            // replace tags in filemask
            if (filename.Contains(Ressource.Mask_Date))
                filename = filename.Replace(Ressource.Mask_Date, date);
            if (filename.Contains(Ressource.Mask_Time))
                filename = filename.Replace(Ressource.Mask_Time, time);
            if (filename.Contains(Ressource.Mask_Extension))
                filename = filename.Replace(Ressource.Mask_Extension, extension);

            String path = (string)Reg.Config.GetValue(Ressource.RegKey_OutputPath, Ressource.Default_OutputPath);

            if (path == "")
                path = Application.StartupPath + "\\" + filename;
            else if (System.IO.Directory.Exists(path))
                path = path + "\\" + filename;
            else
            {
                doBalloon("Directory \"" + path + "\" does not exist!\n Writing file to execution directory.", Ressource.Title, ToolTipIcon.Error);
                path = Application.StartupPath + "\\" + filename;
            }

            /*
             *    .    does not work, use just ""
             *    ""   empty... pastes to execution dir
             *    /    root of the directory p2f runs on
             *    ../  one directoy up of place where p2f is run
             *    c:\  well... c:! :)
             * 
             */

            return path;
        }

        internal void doBalloon(String Message, String Title, ToolTipIcon Icon)
        {
            int timeout = (int)Reg.Config.GetValue(Ressource.RegKey_BalloonTimeout, Ressource.Default_BalloonTimeout);
            TrayIcon.Visible = true;
            TrayIcon.ShowBalloonTip(0, Title, Message, Icon);
            System.Threading.Thread.Sleep(timeout); //TODO: that's ugly, but until we have a persistent tray icon...
            TrayIcon.Visible = false;
        }

        internal void doPaste(IDataObject obj) //, bool currentDirectory
        {
            // get the image data from the clipboard
            Image image = (Image)obj.GetData(DataFormats.Bitmap);

            // set output format and file extension
            ImageFormat format;
            String extension;
            switch ((int)Reg.Config.GetValue(Ressource.RegKey_ImageFormat, Ressource.Default_ImageFormat))
            {
                case 0:
                    format = ImageFormat.Png; extension = "png"; break;
                case 1:
                    format = ImageFormat.Jpeg; extension = "jpg"; break;
                case 2:
                    format = ImageFormat.Gif; extension = "gif"; break;
                case 3:
                    format = ImageFormat.Bmp; extension = "bmp"; break;
                case 4:
                    format = ImageFormat.Tiff; extension = "tif"; break;
                default:
                    format = ImageFormat.Png; extension = "png"; break;
            }

            // make extension uppercase
            if(bool.Parse((String)Reg.Config.GetValue(Ressource.RegKey_UpperCaseExt, Ressource.Default_UpperCaseExt)))
                extension.ToUpper();

            // create a filename
            String filename = getFilename(extension);
            //TODO seperate filename and directory retrival to make paste here and paste to default dir possible

            try
            {
                // write output file
                image.Save(filename, format);
                // notify user
                doBalloon("\"" + filename + "\" successfully written...", Ressource.Title, ToolTipIcon.Info);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                doBalloon("System.Runtime.InteropServices.ExternalException", Ressource.Title, ToolTipIcon.Warning);
            }
            catch (System.Security.SecurityException)
            {
                doBalloon("Sorry, this cannot be run on a network drive!", Ressource.Title, ToolTipIcon.Warning);
            }
            catch (System.ArgumentException)
            {
                doBalloon("Check your filename for invalid characters! (" + filename + ")", Ressource.Title, ToolTipIcon.Warning);
            }
            catch (System.NotSupportedException)
            {
                doBalloon("Check your filename for invalid characters! (" + filename + ")", Ressource.Title, ToolTipIcon.Warning);
            }

        }


    }

    class PasteToFile
    {
        [STAThread]
        static void Main(string[] args)
        {
            // parse commandline arguments
            CommandLine.Utility.Arguments Switches = new CommandLine.Utility.Arguments(args);
            Program prog = new Program();

            // decide next steps...
            if (Switches["about"] != null)
                prog.showAbout();
            else if (Switches["help"] != null)
                prog.showAbout();
            else if (Switches["options"] != null)
                prog.showOptions();
            else if (Switches["setup"] != null)
                prog.showOptions();
            else
            {
                // do not continue if there is nothing in the clipboard
                if (Clipboard.GetDataObject() == null)
                {
                    prog.doBalloon("Sorry, the clipboard seems to be empty!", Ressource.Title, ToolTipIcon.Warning);
                }
                else
                {
                   // also do not continue if there is not an image in the clipboard
                    IDataObject obj = Clipboard.GetDataObject();
                    if (!obj.GetDataPresent(DataFormats.Bitmap))
                    {
                        prog.doBalloon("Sorry, there is no useable image data in the clipboard!", Ressource.Title, ToolTipIcon.Warning);
                    }
                    else
                    {
                        // ONLY if there really is something useful available continue...
                        //if (Switches["here"] != null)
                        //    prog.doPaste(obj,bool);
                        //else
                            prog.doPaste(obj);
                    }            
                }            

            } // end of else

        } // end of Main

    } // end of class


} // end of namespace
