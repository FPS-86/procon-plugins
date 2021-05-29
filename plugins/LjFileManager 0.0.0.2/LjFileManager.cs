/* 

 * Thanks to PapaCharlie9@gmail.com for the Barebones script to get me started
 * and function to update Procon UI varibles UpdateSettingPage() & SetExternalPluginSetting()
 * 
 * Thanks to BamBam (ProconRulz) as the main script i have been reading to relearn C# and Procon related functions
 * and some code snipits -- procon_admin(string player_name) in particular // and link for InI Handling


*/

using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Plugin;
using System;
using System.IO;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

namespace PRoConEvents
{
    #region Boring stuff


    public class LjFileManager : PRoConPluginAPI, IPRoConPluginInterface
    {
        public LjFileManager()
        {
            fIsEnabled = false;
            fAllowDirect = true; // Change Admin to Diseased-LJ for using commands from Procon Chat
            fPluginName = "LJ File Manager";
            fCurrentPath = "." + Path.DirectorySeparatorChar;
            fFileCopy = String.Empty;
        }
        #endregion
        #region Internal Variables
        private bool fIsEnabled, fAllowDirect;
        private string fPluginName;
        private string fCurrentPath;
        private string fFile, fFileCopy;

        #endregion
        #region Message calls

        public void ConsoleWrite(String msg)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("[^b{0}^n] {1}", fPluginName, msg));
        }

        public void ServerSay(String args) { ServerSay(args, false, true, 0); }
        public void ServerSay(String args, int delay) { ServerSay(args, false, true, delay); }
        public void ServerSay(String args, bool yell) { ServerSay(args, yell, true, 0); }
        public void ServerSay(String args, bool yell, int delay) { ServerSay(args, yell, true, delay); }
        public void ServerSay(String args, bool yell, bool say) { ServerSay(args, yell, say, 0); }
        public void ServerSay(String args, bool yell, bool say, int delay)
        {
            if (say) this.ExecuteCommand("procon.protected.tasks.add", "ServerSay", delay.ToString(), "1", "1", "procon.protected.send", "admin.say", args, "all");
            if (yell) this.ExecuteCommand("procon.protected.tasks.add", "ServerSay", delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", args, "3", "all");
            if (say || yell) this.ExecuteCommand("procon.protected.tasks.add", "ServerSay", delay.ToString(), "1", "1", "procon.protected.chat.write", "^3" + args);
        }

        public void PlayerSay(String player, String args) { PlayerSay(player, args, false, true, 0); }
        public void PlayerSay(String player, String args, int delay) { PlayerSay(player, args, false, true, delay); }
        public void PlayerSay(String player, String args, bool yell) { PlayerSay(player, args, yell, true, 0); }
        public void PlayerSay(String player, String args, bool yell, int delay) { PlayerSay(player, args, yell, true, delay); }
        public void PlayerSay(String player, String args, bool yell, bool say) { PlayerSay(player, args, yell, say, 0); }
        public void PlayerSay(String player, String args, bool yell, bool say, int delay)
        {
            if (say) this.ExecuteCommand("procon.protected.tasks.add", "PlayerSay", delay.ToString(), "1", "1", "procon.protected.send", "admin.say", args, "player", player);
            if (yell) this.ExecuteCommand("procon.protected.tasks.add", "PlayerSay", delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", args, "3", "player", player);
            if (say || yell) this.ExecuteCommand("procon.protected.tasks.add", "ServerSay", delay.ToString(), "1", "1", "procon.protected.chat.write", "^b> ^8" + player + ":^n^6" + args);
        }

        public void ChatSay(String args)
        {
            this.ExecuteCommand("procon.protected.chat.write", "^2" + args);
        }
        #endregion
        #region UI and Variable setup
        public List<CPluginVariable> GetDisplayPluginVariables() { return GetCommonPluginVariables(false); }
        public List<CPluginVariable> GetPluginVariables() { return new List<CPluginVariable>(); } //GetCommonPluginVariables(true); } // we are using our own save file... Hypernia doesnt give access to Config folder for backup.

        public List<CPluginVariable> GetCommonPluginVariables(bool save)
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            if (fFileCopy != String.Empty)
                lstReturn.Add(new CPluginVariable(" View|Copy File", typeof(string), fFileCopy));
            lstReturn.Add(new CPluginVariable(String.Format("{0}|ROOT", fCurrentPath), "enum.FolderAction(Do nothing|Open|Paste)", "Do nothing"));
            lstReturn.Add(new CPluginVariable(String.Format("{0}|..", fCurrentPath), "enum.FolderAction(Do nothing|Open|Paste)", "Do nothing"));
            foreach (string s in Directory.GetDirectories(fCurrentPath))
            {
                lstReturn.Add(new CPluginVariable(String.Format("{0}|{1}", fCurrentPath, s), "enum.FolderAction(Do nothing|Open|Paste)", "Do nothing"));
            }
            foreach (string s in Directory.GetFiles(fCurrentPath))
            {
                lstReturn.Add(new CPluginVariable(String.Format("Files|{0}", s), "enum.FileAction(Do nothing|Copy|Delete)", "Do nothing"));
            }
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            string temp;
            if (strVariable.Contains("|")) temp = strVariable.Remove(0, strVariable.IndexOf('|') + 1);
            else temp = strVariable;
            FileManagerUI(temp, strValue);
        }
        #endregion
        #region UI Functions
        public void FileManagerUI(string strVariable, string strValue)
        {
            string stmp = String.Empty;
            switch (strValue)
            {
                case "Open":
                    stmp = strVariable;
                    if (strVariable == "ROOT") stmp = "." + Path.DirectorySeparatorChar;
                    if (strVariable == "..") stmp = fCurrentPath.Remove(fCurrentPath.LastIndexOf(Path.DirectorySeparatorChar));
                    fCurrentPath = stmp;
                    break;
                case "Paste":
                    if (fFileCopy == String.Empty) return;
                    if (strVariable == "ROOT") stmp = "." + Path.DirectorySeparatorChar + fFileCopy.Substring(fFileCopy.LastIndexOf(Path.DirectorySeparatorChar));
                    else if (strVariable == "..") stmp = fCurrentPath.Remove(fCurrentPath.LastIndexOf(Path.DirectorySeparatorChar)) + fFileCopy.Substring(fFileCopy.LastIndexOf(Path.DirectorySeparatorChar));
                    else stmp = strVariable + fFileCopy.Substring(fFileCopy.LastIndexOf(Path.DirectorySeparatorChar));
                    File.Copy(fFileCopy, stmp, true);
                    break;
                case "Copy":
                    fFileCopy = strVariable;
                    break;
                case "Delete":
                    File.Delete(strVariable);
                    break;
                default:
                    break;
            }
        }
        #endregion
        #region plugin loading / enable and disable
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name,
                                                     "OnPlayerDisconnected",
                                                     "OnServerType");
        }
        public void OnPluginEnable()
        {
            fIsEnabled = true;
            ConsoleWrite("Deleting system32... or im enabled..");
        }
        public void OnPluginDisable()
        {
            fIsEnabled = false;
            ConsoleWrite("ive been Disabled! common gimmie another chance 8(");
        }
        #endregion
        #region Watched items
        public override void OnPlayerDisconnected(string soldierName, string reason)
        {
            ChatSay(String.Format("^8Player {0} Disconnected Reason:", soldierName, reason));
        }
        public override void OnServerType(string value)
        {
            ConsoleWrite(String.Format("Server Type is {0}", value));
        }
        #endregion
        #region Process commands
        public override void OnGlobalChat(string speaker, string message) { DoCommands(speaker, message); }
        public override void OnSquadChat(string speaker, string message, int teamId, int squadId) { DoCommands(speaker, message); }
        public override void OnTeamChat(string speaker, string message, int teamId) { DoCommands(speaker, message); }
        public void DoCommands(string speaker, string message)
        {
            #region DoCommands Boring shit
            string strtemp = String.Empty;
            message.Trim();// removes spaces at the start and end of the string
            if (speaker.Equals("Server"))
            {
                speaker = message.Substring(0, message.IndexOf(':'));
                strtemp = message.Remove(0, message.IndexOf(':') + 2);
                message = strtemp;
                if (fAllowDirect) { if (speaker == "Admin") speaker = "Diseased-LJ"; }
                else { if (speaker == "Admin") return; }
            }
            if (message.StartsWith("!") || message.StartsWith("@") || message.StartsWith("/"))
            {
                string[] split = message.ToLower().Split(' ');
                string command = split[0].Remove(0, 1);
                string targetText = String.Empty;
                try { targetText = message.Remove(0, command.Length + 2); }
                catch { }
                #endregion
                switch (command)
                {
                    default:
                        break;
                }
                if (!procon_admin(speaker)) return; // User commands above .. Admin commands below
                switch (command)
                {
                    case "test":
                        ServerSay("Test 5 seconds", 5);
                        ServerSay("Test 10 seconds", 10);
                        ServerSay("Test 7 seconds", 7);
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion
        #region Command Functions
        public void DisplayHelp(string speaker)
        {
            PlayerSay(speaker, "No help available.");
        }
        #endregion
        #region Utility calls
        private string banTimeToString(int tmp)
        {
            TimeSpan test = new TimeSpan(0, 0, tmp * 60);
            if (tmp <= 0) return "Permenant";
            if (test.Days > 0) return string.Format("{0} Days {1} Hours {2} Mins", test.Days, test.Hours, test.Minutes);
            if (test.Hours > 0) return string.Format("{0} Hours {1} Mins", test.Hours, test.Minutes);
            return string.Format("{0} Mins", test.Minutes);
        }
        public List<string> decode(string strValue)
        {
            string strValueHtmlDecode = strValue.Contains(" ") ? strValue : strValue.Replace("+", " ");
            string strValueUnencoded;
            try
            {
                strValueUnencoded = Uri.UnescapeDataString(strValueHtmlDecode);
            }
            catch
            {
                strValueUnencoded = strValueHtmlDecode;
            }
            return new List<string>(strValueUnencoded.Split(new char[] { '|' }));
        }
        bool procon_admin(string player_name)
        {
            CPrivileges p = this.GetAccountPrivileges(player_name);
            try
            {
                if (p.CanKillPlayers) return true;
            }
            catch { }
            return player_name == "Diseased-LJ"; // Always allowed
        }
        public void ServerCommand(params String[] args)
        {
            List<string> list = new List<string>();
            list.Add("procon.protected.send");
            list.AddRange(args);
            this.ExecuteCommand(list.ToArray());
        }
        public void SetExternalPluginSetting(String pluginName, String settingName, String settingValue)
        {
            if (String.IsNullOrEmpty(pluginName) || String.IsNullOrEmpty(settingName) || settingValue == null)
            {
                ConsoleWrite("Required inputs null or empty in setExternalPluginSetting");
                return;
            }
            ExecuteCommand("procon.protected.plugins.setVariable", pluginName, settingName, settingValue);
        }
        string weapon_desc(string key) // Thanks BamBam
        {
            if (key == null || key == "" || key == "None" || key == "No weapon key") return "No weapon";
            try
            {
                return this.GetLocalized(key, String.Format("global.Weapons.{0}", key.ToLower()));
            }
            catch { }
            return key + "(Weapon has no Procon name)";
        }
        public void UpdateSettingPage()
        {
            SetExternalPluginSetting("LjAdmin", "UpdateSettings", "Update");
        }
        #endregion
        #region Plugin Info
        public string GetPluginName()
        {
            return fPluginName;
        }

        public string GetPluginVersion()
        {
            return "0.0.0.2";
        }

        public string GetPluginAuthor()
        {
            return "Diseased-LJ";
        }

        public string GetPluginWebsite()
        {
            return "There isnt one";
        }

        public string GetPluginDescription()
        {
            return @"
<h1>LjMjollnirs Admin Plugin</h1>
";
        }
        #endregion
    } // Lj Ultimate
} // end namespace PRoConEvents



