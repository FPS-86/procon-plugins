/* Anu5Admin by Smoke_The_Dank for Anu5 BF3 servers!
   You are free to use this as you wish, including modifying as long as you give credits to me.
  
   Credits to: Authors of NotifyMe[MorpheusX(AUT)](Notably the mailing system), InsaneLimits[Miguel Mendoza - miguel@micovery.com, PapaCharlie9] and CKillStreaks[[LAB]HeliMagnet] for some code
   snippets and examples I used to understand the workings of ProCon.
  
   Plugin can be seen in use on our BF3 Servers: Bf3Anu5.com
*/


using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Odbc;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Timers;
using System.Text;
using System.Security.Cryptography;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;


namespace PRoConEvents
{
    public class Anu5Admin : PRoConPluginAPI, IPRoConPluginInterface
    {

        #region Plugin Variables
        // GENERAL______________________________________
        private bool blPluginEnabled;
        private CServerInfo csiServer;
        private List<string> adminList;
        public Dictionary<string, int> onlineAdmins = new Dictionary<string, int>();
        public Dictionary<string, string> gamelist = new Dictionary<string, string>();
        private string chatTag;
        private string banAppend;

        private enumBoolYesNo blNotifyEmail;
        private enumBoolYesNo blNotifyTaskbar;
        private List<CPlayerInfo> lstPlayers;
        private List<MaplistEntry> mapList = new List<MaplistEntry>();
        private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();   //Players      
        private List<KeyValuePair<string, string>> adminlog = new List<KeyValuePair<string, string>>();
        private string logString;
        private Dictionary<string, string> commandsList = new Dictionary<string, string>();
        //--------------------------------------------

        // MAILING SYSTEM_____________________________
        private string strHostName;
        private string strPort;
        private enumBoolYesNo sendmail;
        private enumBoolYesNo blUseSSL;
        private string strSMTPServer;
        private int iSMTPPort;
        private string strSenderMail;
        private List<string> lstReceiverMail;
        private string strSMTPUser;
        private string strSMTPPassword;
        //--------------------------------------------

        // CHAT COMMANDS______________________________
        private enumBoolYesNo logCmds;
        private string logPath;
        private string adminlistCMD, calladminCMD;
        //admin commands    
        private string commandsCMD;
        private string spankCMD, pardonCMD, killCMD, kickCMD, banCMD, tbanCMD, moveCMD, swapCMD;
        private string resetCMD, endroundCMD, nextmapCMD, maplistCMD, addmapCMD, removemapCMD, switchmapCMD, finishCMD;
        private string maillogCMD, clearlogCMD, adminsayCMD;
        private string spankString, pardonString, killString, kickString, banString, tbanString, moveString, swapString;
        private string resetString, endroundString, nextmapString, maplistString, addmapString, removemapString, switchmapString, finishString;
        private string maillogString, clearlogString, adminsayString;
        private int spankLVL, pardonLVL, killLVL, kickLVL, banLVL, tbanLVL, moveLVL, swapLVL;
        private int resetLVL, endroundLVL, nextmapLVL, maplistLVL, addmapLVL, removemapLVL, switchmapLVL, finishLVL;
        private int maillogLVL, clearlogLVL, adminsayLVL;
        //--------------------------------------------

        // NANNYBOT___________________________________
        private Dictionary<string, int> d_SpankKills = new Dictionary<string, int>();
        private List<string> nannylist;
        private int SpankKillsMax;
        //--------------------------------------------

        // WARNING SYSTEM_____________________________
        private int kdrlim;
        private int minkillwarn;
        private int minhswarn;
        private enumBoolYesNo chatnotify;
        private enumBoolYesNo taskbarnotify;
        private enumBoolYesNo monitorplayers;
        private enumBoolYesNo emailwarning;
        private List<string> lstmonitorlist;
        private List<string> lstwhitelist;
        private Dictionary<string, int> d_Headshots = new Dictionary<string, int>();
        private List<string> d_Warned;
        private Dictionary<string, string> d_Weapons = new Dictionary<string, string>();
        //--------------------------------------------

        public Anu5Admin()
        {
            // GENERAL__________________________________
            this.blPluginEnabled = false;
            this.adminList = new List<string>();
            this.logString = String.Empty;
            this.blNotifyEmail = enumBoolYesNo.No;
            this.blNotifyTaskbar = enumBoolYesNo.No;
            this.chatTag = "[Anu5Admin]";
            this.banAppend = "[Appeal at www.your_site.com]";
            //----------------------------------------

            // MAILING SYSTEM_________________________
            this.sendmail = enumBoolYesNo.No;
            this.blUseSSL = enumBoolYesNo.No;
            this.strSMTPServer = String.Empty;
            this.iSMTPPort = 25;
            this.strSenderMail = String.Empty;
            this.lstReceiverMail = new List<string>();
            this.strSMTPUser = String.Empty;
            this.strSMTPPassword = String.Empty;
            //-----------------------------------------

            // CHAT COMMANDS___________________________  
            this.logCmds = enumBoolYesNo.Yes;
            this.logPath = "adminlog.txt";

            this.adminlistCMD = "admins";
            this.calladminCMD = "calladmin";
            this.commandsCMD = "commands";
            this.spankCMD = "spank"; this.pardonCMD = "pardon"; this.killCMD = "kill"; this.kickCMD = "kick"; this.banCMD = "ban"; this.tbanCMD = "tban"; this.moveCMD = "move"; this.swapCMD = "swap";
            this.resetCMD = "reset"; this.endroundCMD = "endround"; this.nextmapCMD = "nextmap"; this.maplistCMD = "listmaps"; this.addmapCMD = "addmap"; this.removemapCMD = "removemap"; this.switchmapCMD = "switchmap"; this.finishCMD = "finish";
            this.maillogCMD = "maillog"; this.clearlogCMD = "clearlog"; this.adminsayCMD = "say";

            this.spankString = "spank 1";
            this.pardonString = "pardon 1";
            this.killString = "kill 1";
            this.kickString = "kick 2";
            this.banString = "ban 2";
            this.tbanString = "tban 1";
            this.moveString = "move 1";
            this.swapString = "swap 1";
            this.resetString = "reset 5";
            this.endroundString = "endround 5";
            this.finishString = "finish 5";
            this.nextmapString = "nextmap 5";
            this.addmapString = "addmap 5";
            this.removemapString = "removemap 5";
            this.switchmapString = "switchmap 5";
            this.maplistString = "listmaps 5";
            this.maillogString = "maillog 5";
            this.clearlogString = "clearlog 5";
            this.adminsayString = "say 1";

            this.spankLVL = 1; this.pardonLVL = 1; this.killLVL = 1; this.kickLVL = 2; this.banLVL = 2; this.tbanLVL = 1; this.moveLVL = 1; this.swapLVL = 1;
            this.resetLVL = 5; this.endroundLVL = 5; this.nextmapLVL = 5; this.maplistLVL = 5; this.addmapLVL = 5; this.removemapLVL = 5; this.switchmapLVL = 5; this.finishLVL = 5;
            this.maillogLVL = 5; this.clearlogLVL = 5; this.adminsayLVL = 1;
            //-----------------------------------------

            // NANNYBOT________________________________
            this.nannylist = new List<string>();
            this.SpankKillsMax = 3;
            //-----------------------------------------

            // WARNING SYSTEM__________________________
            this.kdrlim = 5;
            this.minhswarn = 50;
            this.minkillwarn = 40;
            this.chatnotify = enumBoolYesNo.No;
            this.taskbarnotify = enumBoolYesNo.No;
            this.monitorplayers = enumBoolYesNo.No;
            this.lstmonitorlist = new List<string>();
            this.lstwhitelist = new List<string>();
            this.d_Warned = new List<string>();
            this.emailwarning = enumBoolYesNo.No;
            //-----------------------------------------
        }
        #endregion

        #region Details
        // sets the name displayed in Procon's plugin-tab
        public string GetPluginName() { return "Anu5Admin"; }

        // plugin-version shown in the plugin-tab
        public string GetPluginVersion() { return "0.3"; }

        // Author
        public string GetPluginAuthor() { return "Smoke_The_RAGE@www.battlelog.battlefield.com / Dank@www.bf3anu5.com / Dank@www.phogue.net"; }

        // HomePage URL
        public string GetPluginWebsite() { return "www.bf3anu5.com"; }

        // description displayed in the description-tab of the plugins - use HTML-tags to layout the text
        public string GetPluginDescription()
        {
            return @"
            <p>Brought to you by Dank.<br />
            <b>phogue.net:</b> <a href='http://www.phogue.net/forumvb/member.php?10952-dank' mnethod='post' target='_blank'>Dank</a><br />
            <b>BattleLog:</b> <a href='http://battlelog.battlefield.com/bf3/user/Smoke_The_RAGE/' mnethod='post' target='_blank'>Smoke_The_RAGE</a><br />
            <b>bf3anu5.com:</b> <a href='http://bf3anu5.enjin.com/profile/1157605' mnethod='post' target='_blank'>Dank</a><br />

            <p align='center'>Donate to my awesomeness if you feel so inclined!<br /><br />
            <form action='https://www.paypal.com/cgi-bin/webscr' method='post' target='_blank'>
            <input type='hidden' name='cmd' value='_s-xclick'>
            <input type='hidden' name='hosted_button_id' value='NN8KUFVARXBTJ'>
            <input type='image' src='https://www.paypalobjects.com/en_US/i/btn/btn_donate_SM.gif' border='0' name='submit' alt='PayPal - The safer, easier way to pay online!'>
            <img alt='' border='0' src='https://www.paypalobjects.com/en_US/i/scr/pixel.gif' width='1' height='1'>
            </form>
            <a href='https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=NN8KUFVARXBTJ' method='post' target='_blank'>Donate Here</a>
            </p>

<p align='center'>
   Credits to: Authors of NotifyMe[MorpheusX(AUT)](Notably the mailing system), InsaneLimits[Miguel Mendoza - miguel@micovery.com, PapaCharlie9] and CKillStreaks[[LAB]HeliMagnet] for some code
   snippets and examples I used to understand the workings of ProCon.  
   Plugin can be seen in use on our BF3 Servers: Bf3Anu5.com
</p>
            <h2>Description</h2>
            <p>The Anu5Admin is a collection of tools I put together to help us monitor and take certain actions on our server.<br /></p>             
            <h2>Plugin Settings</h2>
            <h3>General Settings</h3>

            <table border='1' rules='rows'>
            <tr><td><b>Admin List</b>: This is the list of names that will have access to admin commands, this list also contains the 'Permission Level' of said admins. <b>EXAMPLE ADMIN: <i>AdminName 1</i></b>, where AdminName is the name of the admin and '1' is their permission level.</td></tr>
            </td>
            <tr><td><b>Admin Request Email?</b>: A Yes/No value deciding whether to send emails when a player requests an admin with the !calladmin command.</td></tr>
            <tr><td><b>Admin Request Notify SysTray?</b>: A Yes/No value deciding whether to send a SysTray popup when a player requests an admin with the !calladmin command.</td></tr>
            <tr><td><b>Chat Tag.</b>: This is the tag prefixed to lines of chat sent by the plugin. Anywhere in the previous versions where it said '[Anu5]' or '[Anu5Admin]' in chat, will now use this.</td></tr>
            <tr><td><b>Ban Message.</b>: This is a default string appended to ban messages. Great for 'appeal at www.yoursite.com' or whatever you may want to use it for.</td></tr>
            </table>      
            
            <h3>Chat Commands</h3>- Available to any player. 
            <table border='1' rules='rows'>
            <tr><td><b>List Online Admins</b>: Will display a list in chat of all online admins in the custom Admin List, will also display permission level of said admins. DEFAULT='!admins'.</td></tr>
            <tr><td><b>Request Admin</b>: Depending on settings, will notify in systray or will send an email request to all email-addresses in the 'Reciever addresses' list(below). The email will contain DATE/TIME, Player Name who used the command, (optional) reason specified,  and a list of all players with the following info for each:'Playername, Score, Kills, Deaths, HPK%, KDR, GUID'. DEFAULT='!calladmin'. If a player in the 'Admin List' is online, the user of the command will be directed to use the '!admins' command instead and no mail will be sent. Tray notification will still be shown.</td></tr>         
            </table>
        
            <h3>Admin Commands</h3>- Available to players in the 'custom Admin List' only, each command has a permission level, the admin who tries to use it needs to have a permission level at or above that of the command. Admins can use commands on other admins who have a lower permission level. Commands which take a player name as an argument can be matched on any number of letters and on any part of the player name. For example to kick 'smoke_the_dank' I could use '!kick _the_'. Letter Upper/Lower case is ignored. You can use commands silently(won't show in chat) by putting '/' before them. EXAMPLE '/!kick smoke_the_dank'.
            <table border='1' rules='rows'>
            <tr><td><b>Log Admin Commands</b>: A Yes/No value deciding whether or not to log to a text file everytime an admin used a command. Entries in file will be sorted by Admin Name and then by the Date/Time the command was used.
            <br /> SAMPLE ENTRY='Smoke_The_Dank(02/26/2012-12:30:12) - [!move smoke] - [[anu5]metro(2/4)]'. The fields shown are: NAME - DATE/TIME - COMMAND STRING - SERVER NAME(Player Count)</td></tr>
            <tr><td><b>Command Log Path/Filename.</b>: The path to the text file you wish to log admin commands in. YOU MUST CREATE THE FILE YOURSELF. If no path is specified, it will be assumed to be the path of procon.exe. EXAMPLE: 'C:\yourlogname.txt' or 'yourlogname.txt' if in the same directory as procon.exe.</td></tr>
            <tr><td><b>Commands List</b>: DOES NOT USE PERMISSION LEVEL- When an admin uses this command, commands that admin has the permission level to use will be listed in chat. DEFAULT='commands'. EXAMPLE USE IN CHAT='!commands'.

            <tr><td><b>Admin Say</b>: The chat command to chat as admin. DEFAULT='say 1'. CHAT USE='/!say im an admin, woohoo.'.</td></tr>            
            <tr><td><b>Spank Player</b>: The chat command and permission level for 'spanking a player'. DEFAULT='spank 1'. Use of the command expects a player name and can have an optional reason. EXAMPLE USE IN CHAT='!spank smoke_the_dank mav riding'. When a player is 'spanked' they will automatically be admin killed the specified amount of times(spank kills option below). They will be killed immediately and then immediately after respawning until the kill count is reached.</td></tr>
            <tr><td><b>Cancel a Spank</b>: The chat command and permission level for 'canceling a spank'.  When used, player specified will be removed from the 'spank list'. DEFAULT='pardon 1'. EXAMPLE USE IN CHAT='!pardon smoke_the_dank'.</td></tr>
            <tr><td><b>Kill Player</b>: The chat command and permission level to admin kill a player. DEFAULT='kill 1'. EXAMPLE USE IN CHAT='!kill smoke_the_dank mav riding', the reason is optional.</td></tr>
            <tr><td><b>Kick Player</b>: The chat command and permission level to kick a player. DEFAULT='kick 2'. EXAMPLE USE IN CHAT='!kick smoke_the_dank mav riding', the reason is optional.</td></tr>
            <tr><td><b>Ban Player</b>: The chat command and permission level to ban a player. DEFAULT='ban 2'. EXAMPLE USE IN CHAT='!ban smoke_the_dank hacking', the reason is optional. Ban is permanent and issued by EA_GUID.</td></tr>
            <tr><td><b>Temp. Ban Player</b>: The chat command and permission level to temp ban a player. DEFAULT='tban 1'. EXAMPLE USE IN CHAT='!tban smoke_the_dank 5 hacking', the reason is optional. Ban is temporary and issued by Player Name.</td></tr>

            <tr><td><b>Switch Player Team</b>: The chat command and permission level to switch a player to the other team. DEFAULT='move 1'. EXAMPLE USE IN CHAT='!move smoke_the_dank'.</td></tr>
            <tr><td><b>Switch 2 Players Teams</b>: The chat command and permission level to switch the teams of two players at once. DEFAULT='swap 1'. EXAMPLE USE IN CHAT='!swap smoke otherplayername'.</td></tr>
            <tr><td><b>Reset Current Round</b>: The chat command and permission level to reset the current round(scores will be lost). DEFAULT='reset 5'. EXAMPLE USE IN CHAT='!reset'.</td></tr>
            <tr><td><b>End Current Round</b>: The chat command and permission level to end the current round and move on to the next(scores will be carried over into the next round). DEFAULT='endround 5'. EXAMPLE USE IN CHAT='!endround'.</td></tr>
            <tr><td><b>End Round With Winner</b>: The chat command and permission level to end the current round, this will go to the scoresheet and award the team with the highest tickets as winner. DEFAULT='finish 5'. EXAMPLE USE IN CHAT='!finish'.</td></tr>
            <tr><td><b>Set Next Map</b>: The chat command and permission level to set which map will be played next. DEFAULT='nextmap 5'. EXAMPLE USE IN CHAT='!nextmap kharg_tdm'. Map name arguments are short names which you can get by using the list maps command.</td></tr>
            <tr><td><b>Switch Map</b>: The chat command and permission level to switch directly to another map. DEFAULT='switchmap 5'. EXAMPLE USE IN CHAT='!switchmap kharg_tdm'. Map name arguments are short names which you can get by using the list maps command.</td></tr>
            <tr><td><b>List Available Maps</b>: !listmaps will list in chat the available maps to be used with '!nextmap', '!switchmap' and '!removemap'. '!listmaps full' will list in chat all maps/gamemodes to be used with !addmap.</td></tr>
            <tr><td><b>Add map to list</b>: The chat command to add a map to the rotation. Use 'listmaps full' to find the shortname for mapname/gamemode. DEFAULT='addmap 5'. CHAT USE '!addmap caspian_cq64_1'. The last parimeter _1 is the # of rounds.</td></tr>
            <tr><td><b>Remove map from list</b>: The chat command to remove a map from the rotation. Use '!listmaps' to get the index # of the map to remove. DEFAULT='removemap 5'. CHAT USE '!removemap 3'.</td></tr>
                     
            <tr><td><b>Email Command Log</b>: The chat command and permission level to email the 'Admin Command Log' to the specified email address. DEFAULT='maillog 5'. EXAMPLE USE IN CHAT='!maillog youremail@yourdomain.com'.</td></tr>
            <tr><td><b>Clear Command Log</b>: The chat command and permission level to clear the contents of the 'Admin Command Log'. DEFAULT='clearlog 5'. EXAMPLE USE IN CHAT='!clearlog'.</td></tr>
            </table>
            
            <h3>Suspect Player</h3>- When a suspicious player is detected based on the settings below, it will show a notification in the procon console with the following info 'Killer name, weapon last used, headshot per kill %, #kills, #deaths, K/D ratio, GUID, Trigger event(hpk or kdr)'. If 'Suspect Player Email?' is set to Yes, then an email will be dispatched to all addresses listed in 'Reciever Addresses', with the same info and a list of all weapons used for each kill(along with whether or not each kill was a headshot). The email will also contain the Date/Time and Server info.
            <table border='1' rules='rows'>
            <tr><td><b>Suspect Player WhiteList</b>: Names in this list will be ignored by the 'Suspect Player' reports.</td></tr>
            <tr><td><b>Alert VIA Email?</b>: A Yes/No value deciding whether email warnings will be sent to admins when a suspicious player is detected.</td></tr>
            <tr><td><b>Alert In Chat?:</b> A Yes/No value deciding whether to notify in chat when a suspicious player is detected.</td></tr>
            <tr><td><b>Alert In SysTray?:</b> A Yes/No value deciding whether to notify in SysTray when a suspicious player is detected.</td></tr>
            <tr><td><b>Min. Kills Before Warn</b>: The number of kills a player must get before able to trigger a 'suspicious player' report.</td></tr>
            <tr><td><b>KDR Warn Limit</b>: If the 'Min. Kills Before Warn' limit is satisfied and a players Kill/Death ratio becomes higher then this number, the warning will be triggered.</td></tr>    
            <tr><td><b>HS% Warn Limit</b>: If the 'Min. Kills Before Warn' limit is satisfied and a players Headshot ratio in % becomes higher then this number, the warning will be triggered.</td></tr>
            </table>

            <h3>Email Settings</h3>- The required settings to use any of the features which send emails.
            <table border='1' rules='rows'>
            <tr><td><b>Use SSL?</b>: Toggle SSL usage for mail-transmission.</td></tr>
            <tr><td><b>SMTP-Server address</b>: Hostname or IP of the SMTP-server used for sending mails.</td></tr>
            <tr><td><b>SMTP-Server port</b>: SMTP-port used to connect to the server. DEFAULT='25'.</td></tr>
            <tr><td><b>Sender address</b>: Email-address used as a sender for email functions.</td></tr>
            <tr><td><b>Receiver addresses</b>: List of addresses, to which the emails for email functions will be sent.</td></tr>
            <tr><td><b>SMTP-Server username</b>: Username used to authenticate at the SMTP-server.</td></tr>
            <tr><td><b>SMTP-Server password</b>: Password used to authenticate at the SMTP-server.</td></tr>
            </table>

            <h3>Nanny Bot</h3>- Options used for 'Nanny Bot' aka the 'Spank Command'.
            <table border='1' rules='rows'>
            <tr><td><b>Spank Kills</b>: The amount of times a player will be killed when 'spanked'. DEFAULT='3'.</td></tr>
            <tr><td><b>Spank List</b>: The list of players currently being 'spanked' names can be added manually, though I recommend using the chat command instead. Character case is ignored.</td></tr>
            </table>

            <h3>Player Monitoring</h3> When a player is being monitored, everytime they get a kill, reports will be shown in the procon console containing the following information: Killer Name, Weapon Used(headshot or not), Headshots Per Kill %, #Kills, #Deaths, Kill/Death ratio.
            <table border='1' rules='rows'>
            <tr><td><b>Monitor Players?</b>: A Yes/No option determining whether to use this feature or not.</td></tr>
            <tr><td><b>Monitor List</b>: The list of players being monitored. Case is ignored.</td></tr>
            </table>

            <p>The plugin is compatible with Procon's ingame help-system, the custom commands can also be found typing '@help' into ingame chat.</p>
            ";
        }

        // setting up the plugin's variables displayed to the user
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("1. General Settings|Admin List", typeof(string[]), this.adminList.ToArray()));
            lstReturn.Add(new CPluginVariable("1. General Settings|Admin Request Email?", typeof(enumBoolYesNo), this.blNotifyEmail));
            lstReturn.Add(new CPluginVariable("1. General Settings|Admin Request Notify SysTray?", typeof(enumBoolYesNo), this.blNotifyTaskbar));
            lstReturn.Add(new CPluginVariable("1. General Settings|Chat Tag", typeof(string), this.chatTag));
            lstReturn.Add(new CPluginVariable("1. General Settings|Ban Message", typeof(string), this.banAppend));

            lstReturn.Add(new CPluginVariable("2 Chat Commands|List Online Admins", typeof(string), this.adminlistCMD));
            lstReturn.Add(new CPluginVariable("2 Chat Commands|Request Admin", typeof(string), this.calladminCMD));

            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Log Admin Commands?", typeof(enumBoolYesNo), this.logCmds));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Command Log Path/Filename.", typeof(string), this.logPath));

            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Command List", typeof(string), this.commandsCMD));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Admin Say", typeof(string), this.adminsayString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Spank Player", typeof(string), this.spankString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Cancel a Spank", typeof(string), this.pardonString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Kill Player", typeof(string), this.killString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Kick Player", typeof(string), this.kickString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Ban Player", typeof(string), this.banString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Temp. Ban Player", typeof(string), this.tbanString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Switch Player Team", typeof(string), this.moveString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Switch 2 Players Team", typeof(string), this.swapString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Reset Current Round", typeof(string), this.resetString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|End Current Round", typeof(string), this.endroundString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|End Round With Winner", typeof(string), this.finishString));

            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Set Next Map", typeof(string), this.nextmapString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|List Available Maps", typeof(string), this.maplistString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Add Map to List", typeof(string), this.addmapString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Remove Map from List", typeof(string), this.removemapString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Switch to Map", typeof(string), this.switchmapString));

            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Email Command Log", typeof(string), this.maillogString));
            lstReturn.Add(new CPluginVariable("2.a Admin Commands|Clear Command Log", typeof(string), this.clearlogString));

            lstReturn.Add(new CPluginVariable("3. Suspect Player|Suspect Player WhiteList", typeof(string[]), this.lstwhitelist.ToArray()));
            lstReturn.Add(new CPluginVariable("3. Suspect Player|Alert VIA Email?", typeof(enumBoolYesNo), this.emailwarning));
            lstReturn.Add(new CPluginVariable("3. Suspect Player|Alert In Chat?", typeof(enumBoolYesNo), this.chatnotify));
            lstReturn.Add(new CPluginVariable("3. Suspect Player|Alert In SysTray?", typeof(enumBoolYesNo), this.taskbarnotify));
            lstReturn.Add(new CPluginVariable("3. Suspect Player|Min. Kills Before Warn", typeof(int), this.minkillwarn));
            lstReturn.Add(new CPluginVariable("3. Suspect Player|KDR Warn Limit", typeof(int), this.kdrlim));
            lstReturn.Add(new CPluginVariable("3. Suspect Player|HS% Warn Limit", typeof(int), this.minhswarn));

            lstReturn.Add(new CPluginVariable("4. Email Settings|Use SSL?", typeof(enumBoolYesNo), this.blUseSSL));
            lstReturn.Add(new CPluginVariable("4. Email Settings|SMTP-Server address", typeof(string), this.strSMTPServer));
            lstReturn.Add(new CPluginVariable("4. Email Settings|SMTP-Server port", typeof(int), this.iSMTPPort));
            lstReturn.Add(new CPluginVariable("4. Email Settings|Sender address", typeof(string), this.strSenderMail));
            lstReturn.Add(new CPluginVariable("4. Email Settings|Receiver addresses", typeof(string[]), this.lstReceiverMail.ToArray()));
            lstReturn.Add(new CPluginVariable("4. Email Settings|SMTP-Server username", typeof(string), this.strSMTPUser));
            lstReturn.Add(new CPluginVariable("4. Email Settings|SMTP-Server password", typeof(string), this.strSMTPPassword));

            lstReturn.Add(new CPluginVariable("5. Nanny Bot|Spank Kills", typeof(int), this.SpankKillsMax));
            lstReturn.Add(new CPluginVariable("5. Nanny Bot|Spank List", typeof(string[]), this.nannylist.ToArray()));

            lstReturn.Add(new CPluginVariable("6. Player Monitoring|Monitor Players?", typeof(enumBoolYesNo), this.monitorplayers));
            if (this.monitorplayers == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("6. Player Monitoring|Monitor List", typeof(string[]), this.lstmonitorlist.ToArray()));
            }

            return lstReturn;
        }

        // setting up the plugin's variables as they are saved
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Admin List", typeof(string[]), this.adminList.ToArray()));
            lstReturn.Add(new CPluginVariable("Suspect Player WhiteList", typeof(string[]), this.lstwhitelist.ToArray()));
            lstReturn.Add(new CPluginVariable("Admin Request Email?", typeof(enumBoolYesNo), this.blNotifyEmail));
            lstReturn.Add(new CPluginVariable("Admin Request Notify SysTray?", typeof(enumBoolYesNo), this.blNotifyTaskbar));
            lstReturn.Add(new CPluginVariable("Chat Tag", typeof(string), this.chatTag));
            lstReturn.Add(new CPluginVariable("Ban Message", typeof(string), this.banAppend));

            lstReturn.Add(new CPluginVariable("List Online Admins", typeof(string), this.adminlistCMD));
            lstReturn.Add(new CPluginVariable("Request Admin", typeof(string), this.calladminCMD));
            lstReturn.Add(new CPluginVariable("Command Log Path/Filename.", typeof(string), this.logPath));
            lstReturn.Add(new CPluginVariable("Log Admin Commands?", typeof(enumBoolYesNo), this.logCmds));
            lstReturn.Add(new CPluginVariable("Command List", typeof(string), this.commandsCMD));
            lstReturn.Add(new CPluginVariable("Admin Say", typeof(string), this.adminsayString));
            lstReturn.Add(new CPluginVariable("Spank Player", typeof(string), this.spankString));
            lstReturn.Add(new CPluginVariable("Cancel a Spank", typeof(string), this.pardonString));
            lstReturn.Add(new CPluginVariable("Kill Player", typeof(string), this.killString));
            lstReturn.Add(new CPluginVariable("Kick Player", typeof(string), this.kickString));
            lstReturn.Add(new CPluginVariable("Ban Player", typeof(string), this.banString));
            lstReturn.Add(new CPluginVariable("Temp. Ban Player", typeof(string), this.tbanString));
            lstReturn.Add(new CPluginVariable("Switch Player Team", typeof(string), this.moveString));
            lstReturn.Add(new CPluginVariable("Switch 2 Players Team", typeof(string), this.swapString));
            lstReturn.Add(new CPluginVariable("Reset Current Round", typeof(string), this.resetString));
            lstReturn.Add(new CPluginVariable("End Current Round", typeof(string), this.endroundString));
            lstReturn.Add(new CPluginVariable("End Round With Winner", typeof(string), this.finishString));
            lstReturn.Add(new CPluginVariable("Set Next Map", typeof(string), this.nextmapString));
            lstReturn.Add(new CPluginVariable("Add Map to List", typeof(string), this.addmapString));
            lstReturn.Add(new CPluginVariable("Remove Map from List", typeof(string), this.removemapString));
            lstReturn.Add(new CPluginVariable("Switch to Map", typeof(string), this.switchmapString));
            lstReturn.Add(new CPluginVariable("List Available Maps", typeof(string), this.maplistString));
            lstReturn.Add(new CPluginVariable("Email Command Log", typeof(string), this.maillogString));
            lstReturn.Add(new CPluginVariable("Clear Command Log", typeof(string), this.clearlogString));

            lstReturn.Add(new CPluginVariable("Alert VIA Email?", typeof(enumBoolYesNo), this.emailwarning));
            lstReturn.Add(new CPluginVariable("Alert In Chat?", typeof(enumBoolYesNo), this.chatnotify));
            lstReturn.Add(new CPluginVariable("Alert In SysTray?", typeof(enumBoolYesNo), this.taskbarnotify));
            lstReturn.Add(new CPluginVariable("Min. Kills Before Warn", typeof(int), this.minkillwarn));
            lstReturn.Add(new CPluginVariable("KDR Warn Limit", typeof(int), this.kdrlim));
            lstReturn.Add(new CPluginVariable("HS% Warn Limit", typeof(int), this.minhswarn));

            lstReturn.Add(new CPluginVariable("Use SSL?", typeof(enumBoolYesNo), this.blUseSSL));
            lstReturn.Add(new CPluginVariable("SMTP-Server address", typeof(string), this.strSMTPServer));
            lstReturn.Add(new CPluginVariable("SMTP-Server port", typeof(int), this.iSMTPPort));
            lstReturn.Add(new CPluginVariable("Sender address", typeof(string), this.strSenderMail));
            lstReturn.Add(new CPluginVariable("Receiver addresses", typeof(string[]), this.lstReceiverMail.ToArray()));
            lstReturn.Add(new CPluginVariable("SMTP-Server username", typeof(string), this.strSMTPUser));
            lstReturn.Add(new CPluginVariable("SMTP-Server password", typeof(string), this.strSMTPPassword));

            lstReturn.Add(new CPluginVariable("Spank Kills", typeof(int), this.SpankKillsMax));
            lstReturn.Add(new CPluginVariable("Spank List", typeof(string[]), this.nannylist.ToArray()));

            lstReturn.Add(new CPluginVariable("Monitor Players?", typeof(enumBoolYesNo), this.monitorplayers));
            lstReturn.Add(new CPluginVariable("Monitor List", typeof(string[]), this.lstmonitorlist.ToArray()));

            return lstReturn;
        }

        // called when a plugin-variable is changed by the admin
        public void SetPluginVariable(string strVariable, string strValue)
        {
            this.UnregisterAllCommands();

            int iPort = 0;
            int iRequests = 0;
            int iKdrlim = 0;
            int iMinkillwarn = 0;
            int iMinhswarn = 0;
            int iMaxSpankKills = 0;

            if (strVariable.CompareTo("Admin List") == 0)
            {
                this.adminList.Clear();
                this.adminList = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            }
            else if (strVariable.CompareTo("Suspect Player WhiteList") == 0)
            {
                this.lstwhitelist = new List<string>(CPluginVariable.DecodeStringArray(strValue.ToLower()));
            }
            else if (strVariable.CompareTo("Admin Request Email?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blNotifyEmail = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Admin Request Notify SysTray?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blNotifyTaskbar = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Chat Tag") == 0)
            {
                this.chatTag = strValue;
            }
            else if (strVariable.CompareTo("Ban Message") == 0)
            {
                this.banAppend = strValue;
            }


            else if (strVariable.CompareTo("List Online Admins") == 0)
            {
                this.adminlistCMD = strValue;
            }
            else if (strVariable.CompareTo("Request Admin") == 0)
            {
                this.calladminCMD = strValue;
            }

            else if (strVariable.CompareTo("Log Admin Commands?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.logCmds = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("Command Log Path/Filename") == 0)
            {
                this.logPath = strValue;
            }

            else if (strVariable.CompareTo("Command List") == 0)
            {
                this.commandsCMD = strValue;
            }
            else if (strVariable.CompareTo("Admin Say") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.adminsayString = strValue; this.adminsayCMD = strParts[0]; this.adminsayLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Spank Player") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.spankString = strValue; this.spankCMD = strParts[0]; this.spankLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Cancel a Spank") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.pardonString = strValue; this.pardonCMD = strParts[0]; this.pardonLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Kill Player") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.killString = strValue; this.killCMD = strParts[0]; this.killLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Kick Player") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.kickString = strValue; this.kickCMD = strParts[0]; this.kickLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Ban Player") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.banString = strValue; this.banCMD = strParts[0]; this.banLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Temp. Ban Player") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.tbanString = strValue; this.tbanCMD = strParts[0]; this.tbanLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Switch Player Team") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.moveString = strValue; this.moveCMD = strParts[0]; this.moveLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Switch 2 Players Team") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.swapString = strValue; this.swapCMD = strParts[0]; this.swapLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Reset Current Round") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.resetString = strValue; this.resetCMD = strParts[0]; this.resetLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("End Current Round") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.endroundString = strValue; this.endroundCMD = strParts[0]; this.endroundLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("End Round With Winner") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.finishString = strValue; this.finishCMD = strParts[0]; this.finishLVL = Convert.ToInt32(strParts[1]);
            }

            else if (strVariable.CompareTo("Set Next Map") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.nextmapString = strValue; this.nextmapCMD = strParts[0]; this.nextmapLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Add Map to List") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.addmapString = strValue; this.addmapCMD = strParts[0]; this.addmapLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Remove Map from List") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.removemapString = strValue; this.removemapCMD = strParts[0]; this.removemapLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Switch to Map") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.switchmapString = strValue; this.switchmapCMD = strParts[0]; this.switchmapLVL = Convert.ToInt32(strParts[1]);
            }

            else if (strVariable.CompareTo("List Available Maps") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.maplistString = strValue; this.maplistCMD = strParts[0]; this.maplistLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Email Command Log") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.maillogString = strValue; this.maillogCMD = strParts[0]; this.maillogLVL = Convert.ToInt32(strParts[1]);
            }
            else if (strVariable.CompareTo("Clear Command Log") == 0)
            {
                char[] delimiters = new char[] { ' ' };
                string[] strParts = strValue.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                this.clearlogString = strValue; this.clearlogCMD = strParts[0]; this.clearlogLVL = Convert.ToInt32(strParts[1]);
            }


            else if (strVariable.CompareTo("Alert VIA Email?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.emailwarning = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Alert In Chat?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.chatnotify = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Alert In SysTray?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.taskbarnotify = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("Min. Kills Before Warn") == 0 && int.TryParse(strValue, out iMinkillwarn) == true)
            { if (iMinkillwarn > 0) { this.minkillwarn = iMinkillwarn; } }
            else if (strVariable.CompareTo("KDR Warn Limit") == 0 && int.TryParse(strValue, out iKdrlim) == true)
            { if (iKdrlim > 0) { this.kdrlim = iKdrlim; } }
            else if (strVariable.CompareTo("HS% Warn Limit") == 0 && int.TryParse(strValue, out iMinhswarn) == true)
            { if (iMinhswarn > 0) { this.minhswarn = iMinhswarn; } }


            else if (strVariable.CompareTo("Use SSL?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blUseSSL = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("SMTP-Server address") == 0)
            {
                this.strSMTPServer = strValue;
            }
            else if (strVariable.CompareTo("SMTP-Server port") == 0 && int.TryParse(strValue, out iPort) == true)
            {
                if (iPort > 0)
                {
                    this.iSMTPPort = iPort;
                }
            }
            else if (strVariable.CompareTo("Sender address") == 0)
            {
                this.strSenderMail = strValue;
            }
            else if (strVariable.CompareTo("Receiver addresses") == 0)
            {
                this.lstReceiverMail = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("SMTP-Server username") == 0)
            {
                this.strSMTPUser = strValue;
            }
            else if (strVariable.CompareTo("SMTP-Server password") == 0)
            {
                this.strSMTPPassword = strValue;
            }

            else if (strVariable.CompareTo("Spank Kills") == 0 && int.TryParse(strValue, out iMaxSpankKills) == true)
            { if (iMaxSpankKills > 0) { this.SpankKillsMax = iMaxSpankKills; } }
            else if (strVariable.CompareTo("Spank List") == 0)
            {
                this.nannylist = new List<string>(CPluginVariable.DecodeStringArray(strValue.ToLower()));
                foreach (string trollName in this.nannylist)
                {
                    if (this.m_dicPlayers.ContainsKey(trollName.ToLower()) == true)
                    {
                        this.ExecuteCommand("procon.protected.tasks.add", "taskSpankNoob", "2", "1", "1", "procon.protected.plugins.call", "Anu5Admin", "SpankNoob", trollName.ToLower());
                    }
                }
            }

            else if (strVariable.CompareTo("Monitor Players?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.monitorplayers = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Monitor List") == 0)
            {
                this.lstmonitorlist = new List<string>(CPluginVariable.DecodeStringArray(strValue.ToLower()));
            }


            this.RegisterAllCommands();
        }



        #endregion

        #region Rcon Events
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) //Called when plugin enabled. 
        {
            this.strHostName = strHostName;
            this.strPort = strPort;
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        public void OnPluginEnable()    //Called when the plugin gets enabled. This also includes loading a plugin, which has been enabled before
        {
            this.blPluginEnabled = true;
            // used events
            this.RegisterEvents(this.GetType().Name, "OnMaplistList", "OnRestartLevel", "OnRunNextLevel", "OnRoundOver", "OnListPlayers", "OnPlayerTeamChange", "OnPlayerJoin",
                "OnPlayerSpawned", "OnPlayerKilled", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnServerInfo", "OnPlayerLeft");
            this.ConsoleWrite("[Anu5Admin]", "Enabled!");
            this.nannylist.Clear(); this.d_SpankKills.Clear();
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            this.ExecuteCommand("procon.protected.send", "mapList.list");
            populateGameList();
            this.RegisterAllCommands();
        }

        public void OnPluginDisable()   //Called when the plugin gets disabled. Doesn't get called when loading a disabled plugin
        {
            this.blPluginEnabled = false;
            this.UnregisterAllCommands();
            this.ConsoleWrite("[Anu5Admin]", "Disabled!");
        }
        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.csiServer = csiServerInfo;
        }
        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                this.lstPlayers = lstPlayers;
            }


            foreach (CPlayerInfo cpiPlayer in lstPlayers)
            {
                if (this.m_dicPlayers.ContainsKey(cpiPlayer.SoldierName.ToLower()) == true)
                { this.m_dicPlayers[cpiPlayer.SoldierName.ToLower()] = cpiPlayer; }
                else
                { this.m_dicPlayers.Add(cpiPlayer.SoldierName.ToLower(), cpiPlayer); }
            }


            this.onlineAdmins.Clear();
            char[] delimiters = new char[] { ' ' };
            foreach (string admins in this.adminList)
            {
                string[] adminParts = admins.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                if (this.m_dicPlayers.ContainsKey(adminParts[0].ToLower()))
                {
                    this.onlineAdmins.Add(adminParts[0].ToLower(), Convert.ToInt32(adminParts[1]));
                }
            }
            this.RegisterAllCommands();
        }

        public override void OnPlayerJoin(string strSoldierName)
        {
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");

            if (this.d_Headshots.ContainsKey(strSoldierName.ToLower()) == false)
            { this.d_Headshots.Add(strSoldierName.ToLower(), 0); }

            if (this.d_Weapons.ContainsKey(strSoldierName.ToLower()) == false)
            { this.d_Weapons.Add(strSoldierName.ToLower(), "Weapons Used -"); }

            if (this.d_SpankKills.ContainsKey(strSoldierName.ToLower()) == false)
            { this.d_SpankKills.Add(strSoldierName.ToLower(), 0); }
            this.RegisterAllCommands();
        }

        public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            foreach (string trollName in this.nannylist)
            {
                if (String.Compare(trollName.ToLower(), soldierName.ToLower()) == 0)
                {
                    this.ExecuteCommand("procon.protected.tasks.add", "taskSpankNoob", "2", "1", "1", "procon.protected.plugins.call", "Anu5Admin", "SpankNoob", trollName.ToLower());
                }
            }
            this.RegisterAllCommands();
        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {
            if (this.m_dicPlayers.ContainsKey(strSoldierName.ToLower()) == true)
            {
                this.m_dicPlayers[strSoldierName].TeamID = iTeamID;
            }
        }

        public override void OnPlayerKilled(Kill kKillerVictimDetails)
        {

            string dVictim = kKillerVictimDetails.Victim.SoldierName.ToLower();
            string dKiller = kKillerVictimDetails.Killer.SoldierName.ToLower();
            int VictimTeam = kKillerVictimDetails.Victim.TeamID;
            int KillerTeam = kKillerVictimDetails.Killer.TeamID;
            if ((String.Compare(dKiller, dVictim) == 0) || (VictimTeam == KillerTeam)) { return; }//suicide or teamkill, we don't need to know which.

            string dWeapon = kKillerVictimDetails.DamageType;
            bool dHeadShot = kKillerVictimDetails.Headshot;
            DateTime killTime = kKillerVictimDetails.TimeOfDeath;
            int dKills = this.m_dicPlayers[dKiller].Kills + 1;
            int dDeaths = this.m_dicPlayers[dKiller].Deaths;
            string dKdr = String.Format("{0:0.##}", this.m_dicPlayers[dKiller].Kdr);
            float d_Kdr = this.m_dicPlayers[dKiller].Kdr;
            string dGUID = this.m_dicPlayers[dKiller].GUID;
            double dHPK = 0;

            if (this.d_Headshots.ContainsKey(dKiller) == false)
            { this.d_Headshots.Add(dKiller, 0); }
            int dHeadshots = d_Headshots[dKiller];
            if (dHeadShot == true) { d_Headshots[dKiller]++; dHeadshots = d_Headshots[dKiller]; }
            dHPK = (double)(dHeadshots * 100) / (dKills);

            if (this.lstwhitelist.Contains(dKiller) == true) { return; }

            if (this.d_Weapons.ContainsKey(dKiller) == false)
            { this.d_Weapons.Add(dKiller, "Weapons Used -"); }

            if (dHeadShot == true) { dWeapon = dWeapon + "[headshot]"; }
            d_Weapons[dKiller] = d_Weapons[dKiller] + "(" + dWeapon + ") | ";

            string Trigger = "None";
            if (dKills >= this.minkillwarn && d_Kdr >= this.kdrlim)
            { Trigger = "KDR"; }
            else if (dKills >= this.minkillwarn && dHPK >= minhswarn)
            { Trigger = "Headshots Per Kill"; }

            if (String.Compare(Trigger, "None") != 0)
            {
                if (this.d_Warned.Contains(dKiller) == false)
                {

                    this.d_Warned.Add(dKiller);
                    ConsoleWrite("[AlertBot]", "^0Name(^8" + dKiller + "^0) - Weapon(^8" + dWeapon + "^0) - HPK(^8" + String.Format("{0:0.##}", dHPK) + "%^0) - Kills(^8" + dKills + "^0) - Deaths(^8" + dDeaths + "^0) - KDR(^8" + dKdr + "^0) - GUID(^8" + dGUID + "^0) - ^bTrigger^n(^8" + Trigger + "^0)");
                    if (this.emailwarning == enumBoolYesNo.Yes)
                    {
                        SuspectMail(dKiller, Trigger, d_Weapons[dKiller]);
                    }
                    if (this.chatnotify == enumBoolYesNo.Yes)
                    {
                        ChatWrite("[AlertBot] - ", "Email Alert Sent to Admins: Suspicious player(" + dKiller + ") Has a been detected!  TRIGGER(" + Trigger + ")");
                    }
                    if (this.taskbarnotify == enumBoolYesNo.Yes)
                    {
                        TaskBar("[AlertBot]", "Suspicious player(" + dKiller + ") Has a been detected!  TRIGGER(" + Trigger + ")");
                    }
                }
            }

            if (this.monitorplayers == enumBoolYesNo.Yes)
            {
                if (this.lstmonitorlist.Contains(dKiller) == true)
                {
                    ConsoleWrite("Player Monitoring", "^0Name(^8" + dKiller + "^0) - Weapon(^8" + dWeapon + "^0) - Headshots(^8" + String.Format("{0:0}", dHPK) + "%^0) - Kills(^8" + dKills + "^0) - Deaths(^8" + dDeaths + "^0) - KDR(^8" + dKdr + "^0)");
                }
            }

        }

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {
            if (this.d_SpankKills.ContainsKey(cpiPlayer.SoldierName.ToLower()))
            {
                this.d_SpankKills.Remove(cpiPlayer.SoldierName.ToLower());
            }
            if (this.nannylist.Contains(cpiPlayer.SoldierName.ToLower()))
            {
                this.nannylist.Remove(cpiPlayer.SoldierName.ToLower());
            }
            if (this.d_Warned.Contains(cpiPlayer.SoldierName.ToLower()))
            {
                this.d_Warned.Remove(cpiPlayer.SoldierName.ToLower());
            }
            if (this.d_Headshots.ContainsKey(cpiPlayer.SoldierName.ToLower()) == true)
            {
                this.d_Headshots.Remove(cpiPlayer.SoldierName.ToLower());
            }
            if (this.d_Weapons.ContainsKey(cpiPlayer.SoldierName.ToLower()) == true)
            {
                this.d_Weapons.Remove(cpiPlayer.SoldierName.ToLower());
            }
            if (this.m_dicPlayers.ContainsKey(cpiPlayer.SoldierName.ToLower()) == true)
            {
                this.m_dicPlayers.Remove(cpiPlayer.SoldierName.ToLower());
            }
            this.RegisterAllCommands();
        }

        public void OnMaplistList(List<MaplistEntry> lstMaplist)
        {
            this.mapList.Clear();
            this.mapList = new List<MaplistEntry>(lstMaplist);
        }
        public void OnRestartLevel()
        {
            ResetLists();
        }
        public void OnRunNextLevel()
        {
            ResetLists();
        }
        public void OnRoundOver(int winningTeamId)
        {
            ResetLists();
        }

        #endregion

        #region Register Commands
        private void RegisterAllCommands()
        {
            if (this.blPluginEnabled)
            {
                List<string> emptyList = new List<string>();

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandAdmins",
                        this.Listify<string>("@", "!", "#"),
                        this.adminlistCMD,
                        this.Listify<MatchArgumentFormat>(),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Lists all admins currently online on this server"
                    )
                );


                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandCommands",
                        this.Listify<string>("@", "!", "#"),
                        this.commandsCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "List Available Commands Per Permission Level"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandSpank",
                        this.Listify<string>("@", "!", "#"),
                        this.spankCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "spank a noob"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandPardon",
                        this.Listify<string>("@", "!", "#"),
                        this.pardonCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "pardon a noob"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandKick",
                        this.Listify<string>("@", "!", "#"),
                        this.kickCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "kick an anu5"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandBan",
                        this.Listify<string>("@", "!", "#"),
                        this.banCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "ban a bitch"
                    )
                );
                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandTempBan",
                        this.Listify<string>("@", "!", "#"),
                        this.tbanCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "temp ban a bitch"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandMove",
                        this.Listify<string>("@", "!", "#"),
                        this.moveCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "move player"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandSwap",
                        this.Listify<string>("@", "!", "#"),
                        this.swapCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "swap two players"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandCallAdmin",
                        this.Listify<string>("@", "!", "#"),
                        this.calladminCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Call for an admin with an optional reason"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandReset",
                        this.Listify<string>("@", "!", "#"),
                        this.resetCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Restart Current Round."
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandNextMap",
                        this.Listify<string>("@", "!", "#"),
                        this.nextmapCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Set the Next Map."
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandAddMap",
                        this.Listify<string>("@", "!", "#"),
                        this.addmapCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Add Map to List."
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandRemoveMap",
                        this.Listify<string>("@", "!", "#"),
                        this.removemapCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Remove Map from List."
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandSwitchMap",
                        this.Listify<string>("@", "!", "#"),
                        this.switchmapCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Switch to Map."
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandListMaps",
                        this.Listify<string>("@", "!", "#"),
                        this.maplistCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "List available maps."
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandEndRound",
                        this.Listify<string>("@", "!", "#"),
                        this.endroundCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "End Current Round"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandFinish",
                        this.Listify<string>("@", "!", "#"),
                        this.finishCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "End Current Round With Winner"
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandKill",
                        this.Listify<string>("@", "!", "#"),
                        this.killCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Kill player with optional reason."
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandMailLog",
                        this.Listify<string>("@", "!", "#"),
                        this.maillogCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Email the Command Log to Specified Address."
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandClearLog",
                        this.Listify<string>("@", "!", "#"),
                        this.clearlogCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Clear Command Log."
                    )
                );

                this.RegisterCommand(
                    new MatchCommand(
                        "Anu5Admin",
                        "OnCommandSay",
                        this.Listify<string>("@", "!", "#"),
                        this.adminsayCMD,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "reason",
                                emptyList
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All),
                        "Admin Chat."
                    )
                );


            }
        }

        private void UnregisterAllCommands()
        {
            List<string> emptyList = new List<string>();

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandAdmins",
                    this.Listify<string>("@", "!", "#"),
                    this.adminlistCMD,
                    this.Listify<MatchArgumentFormat>(),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "Lists all admins currently online on this server"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandCallAdmin",
                    this.Listify<string>("@", "!", "#"),
                    this.calladminCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "Request an admin via email"
                )
            );


            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandCommands",
                    this.Listify<string>("@", "!", "#"),
                    this.commandsCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "List Available Commands Per Permission Level"
                )
            );
            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandAdminSay",
                    this.Listify<string>("@", "!", "#"),
                    this.adminsayCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "Admin Chat."
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandSpank",
                    this.Listify<string>("@", "!", "#"),
                    this.spankCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "spank a noob"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandPardon",
                    this.Listify<string>("@", "!", "#"),
                    this.pardonCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "pardon a noob"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandKick",
                    this.Listify<string>("@", "!", "#"),
                    this.kickCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "kick an anu5"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandBan",
                    this.Listify<string>("@", "!", "#"),
                    this.banCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "ban a bitch"
                )
            );
            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandTempBan",
                    this.Listify<string>("@", "!", "#"),
                    this.tbanCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "temp ban a bitch"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandMove",
                    this.Listify<string>("@", "!", "#"),
                    this.moveCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "move a player"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandSwap",
                    this.Listify<string>("@", "!", "#"),
                    this.swapCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "swap two players"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandReset",
                    this.Listify<string>("@", "!", "#"),
                    this.resetCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "reset current round"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandEndRound",
                    this.Listify<string>("@", "!", "#"),
                    this.endroundCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "End Current Round"
                )
            );
            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandFinish",
                    this.Listify<string>("@", "!", "#"),
                    this.finishCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "End Current Round With Winner"
                )
            );



            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandNextMap",
                    this.Listify<string>("@", "!", "#"),
                    this.nextmapCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "set next map"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandAddMap",
                    this.Listify<string>("@", "!", "#"),
                    this.addmapCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "Add Map to List"
                )
            );
            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandRemoveMap",
                    this.Listify<string>("@", "!", "#"),
                    this.removemapCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "Remove Map from List"
                )
            );
            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandSwitchMap",
                    this.Listify<string>("@", "!", "#"),
                    this.switchmapCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "Switch to Map"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandListMaps",
                    this.Listify<string>("@", "!", "#"),
                    this.maplistCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "list available maps"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandKill",
                    this.Listify<string>("@", "!", "#"),
                    this.killCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "kill player with optional reason"
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandMailLog",
                    this.Listify<string>("@", "!", "#"),
                    this.maillogCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "Email the Command Log to Specified Address."
                )
            );

            this.UnregisterCommand(
                new MatchCommand(
                    "Anu5Admin",
                    "OnCommandClearLog",
                    this.Listify<string>("@", "!", "#"),
                    this.clearlogCMD,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "reason",
                            emptyList
                        )
                    ),
                    new ExecutionRequirements(
                        ExecutionScope.All
                        ),
                    "Clear Command Log."
                )
            );
        }


        #endregion

        #region Registered Command Functions


        public void OnCommandCommands(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can use me!"); return; }
            string adminName = strSpeaker.ToLower();
            this.commandsList.Clear();

            if (this.onlineAdmins[adminName] >= this.spankLVL) { commandsList.Add("Command: spank player", " Syntax- !" + this.spankCMD + "<playername> <reason(optional)>"); }
            if (this.onlineAdmins[adminName] >= this.pardonLVL) { commandsList.Add("Command: pardon player", " Syntax- !" + this.pardonCMD + "<playername>"); }
            if (this.onlineAdmins[adminName] >= this.killLVL) { commandsList.Add("Command: kill player", " Syntax- !" + this.killCMD + "<playername> <reason(optional)>"); }
            if (this.onlineAdmins[adminName] >= this.kickLVL) { commandsList.Add("Command: kick player", " Syntax- !" + this.kickCMD + "<playername> <reason(optional)>"); }
            if (this.onlineAdmins[adminName] >= this.banLVL) { commandsList.Add("Command: ban player", " Syntax- !" + this.banCMD + "<playername> <reason(optional)>"); }
            if (this.onlineAdmins[adminName] >= this.tbanLVL) { commandsList.Add("Command: temp ban player", " Syntax- !" + this.tbanCMD + "<playername> <minutes> <reason(optional)>"); }
            if (this.onlineAdmins[adminName] >= this.moveLVL) { commandsList.Add("Command: switch player team", " Syntax- !" + this.moveCMD + "<playername>"); }
            if (this.onlineAdmins[adminName] >= this.swapLVL) { commandsList.Add("Command: switch two players teams", " Syntax- !" + this.swapCMD + "<playername> <playername>"); }
            if (this.onlineAdmins[adminName] >= this.resetLVL) { commandsList.Add("Command: reset round(scores lost)", " Syntax- !" + this.resetCMD); }
            if (this.onlineAdmins[adminName] >= this.endroundLVL) { commandsList.Add("Command: run next round(scores kept)", " Syntax- !" + this.endroundCMD); }
            if (this.onlineAdmins[adminName] >= this.finishLVL) { commandsList.Add("Command: end round with winner", " Syntax- !" + this.finishCMD + "<(optional)RU or US>"); }
            if (this.onlineAdmins[adminName] >= this.nextmapLVL) { commandsList.Add("Command: set the next map", " Syntax- !" + this.nextmapCMD + "<mapname_gamemode>(shortnames from listmaps"); }
            if (this.onlineAdmins[adminName] >= this.maplistLVL) { commandsList.Add("Command: list map names", " Syntax- !" + this.maplistCMD + " optional argument 'full'."); }
            if (this.onlineAdmins[adminName] >= this.addmapLVL) { commandsList.Add("Command: add new map", " Syntax- !" + this.addmapCMD + " <mapname>_<gamemode>_<rounds>"); }
            if (this.onlineAdmins[adminName] >= this.removemapLVL) { commandsList.Add("Command: remove map", " Syntax- !" + this.removemapCMD + " <index # from listmaps>"); }
            if (this.onlineAdmins[adminName] >= this.switchmapLVL) { commandsList.Add("Command: switch to map", " Syntax- !" + this.switchmapCMD + " <mapname>_<gamemode>"); }
            if (this.onlineAdmins[adminName] >= this.maillogLVL) { commandsList.Add("Command: email the command log", " Syntax- !" + this.maillogCMD + "<email address>"); }
            if (this.onlineAdmins[adminName] >= this.clearlogLVL) { commandsList.Add("Command: clear the command log", " Syntax- !" + this.clearlogCMD); }
            if (this.onlineAdmins[adminName] >= this.adminsayLVL) { commandsList.Add("Command: use admin chat", " Syntax- !" + this.adminsayCMD + " <message>"); }

            ChatWrite("Commands Available at Permission Level[", this.onlineAdmins[adminName] + "]");
            int listDelay = 1;
            foreach (KeyValuePair<string, string> pair in this.commandsList)
            {
                this.ExecuteCommand("procon.protected.tasks.add", "taskmapList", listDelay.ToString(), (this.commandsList.Count * listDelay).ToString(), "1",
                    "procon.protected.send", "admin.say", "[Commands] - " + pair.Key + pair.Value, "all");
                listDelay += 2;
            }

        }

        public void OnCommandSay(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can use me!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.adminsayLVL)
            {
                ChatWrite(this.chatTag,
                    strSpeaker + " you don't have the required permission level to admin speak! AdminSay_Level=" + this.adminsayLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            this.ExecuteCommand("procon.protected.send", "admin.say", capCommand.ExtraArguments, "all");

        }

        public void OnCommandSpank(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can use me!"); return; }

            if (this.onlineAdmins[strSpeaker.ToLower()] < this.spankLVL)
            {
                ChatWrite(this.chatTag,
                strSpeaker + " you don't have the required permission level to spank! Spank_Level=" + this.spankLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            char[] delimiters = new char[] { ' ' };
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            string[] chatParts = chatArg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string plName = chatParts[0];
            string spankReason = chatArg.Substring(plName.Length, chatArg.Length - plName.Length);
            String matchname = checkPlayerName(plName);

            if (String.Compare(matchname, "empty") == 0) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I could not find a match for " + plName); return; }

            if ((this.onlineAdmins.ContainsKey(matchname)) && (this.onlineAdmins[strSpeaker.ToLower()] <= this.onlineAdmins[matchname])) { ChatWrite(this.chatTag, strSpeaker + ", You can only use this command on lower lvl Admins."); return; }

            if (nannylist.Contains(matchname)) { ChatWrite("[NannyBot] - ", matchname + " is already being punished!"); return; }
            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", matchname);
            this.ExecuteCommand("procon.protected.tasks.add", "taskSpankNoob", "2", "1", "1", "procon.protected.plugins.call", "Anu5Admin", "SpankNoob", matchname);
            this.nannylist.Add(matchname);
            ChatWrite("[NannyBot] -", matchname + " is naughty! REASON:" + spankReason);
            ChatWrite("[NannyBot] -", matchname + " will be killed " + this.SpankKillsMax + " times! [Does not affect Stats]");
            ConsoleWrite("[NannyBot] -", "Now spanking (" + matchname + ")");
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
        }

        public void OnCommandPardon(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite("[NannyBot] -", strSpeaker + " you noob, only admins can use me!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.pardonLVL)
            {
                ChatWrite(this.chatTag,
                    strSpeaker + " you don't have the required permission level to pardon! Pardon_Level=" + this.pardonLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            String matchname = checkPlayerName(chatArg);
            if (String.Compare(matchname, "empty") == 0) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I could not find a match for " + chatArg); return; }

            if (this.nannylist.Contains(matchname))
            {
                this.nannylist.Remove(matchname);
                this.d_SpankKills.Remove(matchname);
                ChatWrite("[NannyBot] -", matchname + " has been forgiven for their sins!");
            }
            else { ChatWrite("[NannyBot] -", matchname + " is not currently being punished!"); }
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
        }

        public void OnCommandKill(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can kill players!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.killLVL)
            {
                ChatWrite(this.chatTag,
strSpeaker + " you don't have the required permission level to kill! Kill_Level=" + this.killLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            char[] delimiters = new char[] { ' ' };
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            string[] chatParts = chatArg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string plName = chatParts[0];
            string killReason = chatArg.Substring(plName.Length, chatArg.Length - plName.Length);
            String matchname = checkPlayerName(plName);

            if (String.Compare(matchname, "empty") == 0) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I could not find a match for " + plName); return; }

            if ((this.onlineAdmins.ContainsKey(matchname)) && (this.onlineAdmins[strSpeaker.ToLower()] <= this.onlineAdmins[matchname])) { ChatWrite(this.chatTag, strSpeaker + ", You can only use this command on lower lvl Admins."); return; }

            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", matchname);
            ChatWrite(this.chatTag, matchname + " has been killed. REASON:" + killReason);
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
        }

        public void OnCommandKick(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can kick!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.kickLVL)
            {
                ChatWrite(this.chatTag,
strSpeaker + " you don't have the required permission level to kick! Kick_Level=" + this.kickLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            char[] delimiters = new char[] { ' ' };
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            string[] chatParts = chatArg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string plName = chatParts[0];
            string kickReason = chatArg.Substring(plName.Length, chatArg.Length - plName.Length);
            String matchname = checkPlayerName(plName);

            if (String.Compare(matchname, "empty") == 0) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I could not find a match for " + plName); return; }
            if ((this.onlineAdmins.ContainsKey(matchname)) && (this.onlineAdmins[strSpeaker.ToLower()] <= this.onlineAdmins[matchname])) { ChatWrite(this.chatTag, strSpeaker + ", You can only use this command on lower lvl Admins."); return; }

            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", matchname, kickReason);
            ChatWrite(this.chatTag, matchname + " has been kicked. REASON:" + kickReason);
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
        }


        public void OnCommandBan(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can ban!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.banLVL)
            {
                ChatWrite(this.chatTag,
strSpeaker + " you don't have the required permission level to ban! Ban_Level=" + this.banLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            char[] delimiters = new char[] { ' ' };
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            string[] chatParts = chatArg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string plName = chatParts[0];
            string banReason = chatArg.Substring(plName.Length, chatArg.Length - plName.Length) + " " + this.banAppend;
            String matchname = checkPlayerName(plName);

            if (String.Compare(matchname, "empty") == 0) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I could not find a match for " + plName); return; }
            if ((this.onlineAdmins.ContainsKey(matchname)) && (this.onlineAdmins[strSpeaker.ToLower()] <= this.onlineAdmins[matchname])) { ChatWrite(this.chatTag, strSpeaker + ", You can only use this command on lower lvl Admins."); return; }

            this.ExecuteCommand("procon.protected.send", "banList.add", "guid", m_dicPlayers[matchname].GUID, "perm", banReason);
            this.ExecuteCommand("procon.protected.send", "banList.save");
            ChatWrite(this.chatTag, matchname + " has been banned. REASON:" + banReason);
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
        }

        public void OnCommandTempBan(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can ban!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.banLVL)
            {
                ChatWrite(this.chatTag,
                    strSpeaker + " you don't have the required permission level to ban! Ban_Level=" + this.banLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            char[] delimiters = new char[] { ' ' };
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            string[] chatParts = chatArg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string plName = chatParts[0];
            int banInt = Convert.ToInt32(chatParts[1]) * 60;
            string banTime = banInt.ToString();
            int stringlen = plName.Length + banTime.Length;
            string banReason = chatArg.Substring(stringlen, chatArg.Length - stringlen);
            String matchname = checkPlayerName(plName);

            if (String.Compare(matchname, "empty") == 0) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I could not find a match for " + plName); return; }
            if ((this.onlineAdmins.ContainsKey(matchname)) && (this.onlineAdmins[strSpeaker.ToLower()] <= this.onlineAdmins[matchname])) { ChatWrite(this.chatTag, strSpeaker + ", You can only use this command on lower lvl Admins."); return; }

            this.ExecuteCommand("procon.protected.send", "banList.add", "name", m_dicPlayers[matchname].SoldierName, "seconds", banTime, "BanTime[" + chatParts[1] + "mins], REASON:" + banReason);
            this.ExecuteCommand("procon.protected.send", "banList.save");
            ChatWrite(this.chatTag, matchname + " has been temp banned. BanTime[" + chatParts[1] + "mins]" + ", REASON:" + banReason);
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
        }


        public void OnCommandMove(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            string newTeamID = "0";
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can move players!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.moveLVL)
            {
                ChatWrite(this.chatTag,
strSpeaker + " you don't have the required permission level to move players! Move_Level=" + this.moveLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            String matchname = checkPlayerName(chatArg);
            if (String.Compare(matchname, "empty") == 0) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I could not find a match for " + chatArg); return; }

            if (this.m_dicPlayers[matchname].TeamID == 1) { this.m_dicPlayers[matchname].TeamID = 2; } else { this.m_dicPlayers[matchname].TeamID = 1; }
            this.ExecuteCommand("procon.protected.send", "admin.movePlayer", matchname, this.m_dicPlayers[matchname].TeamID.ToString(), "0", "True");
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
        }

        public void OnCommandSwap(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            string newTeamID = "0";
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            char[] delimiters = new char[] { ' ' };
            string[] chatParts = chatArg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string plOne = chatParts[0];
            string plTwo = chatParts[1];
            String matchname = checkPlayerName(plOne); String matchname2 = checkPlayerName(plTwo);

            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can swap players!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.swapLVL)
            {
                ChatWrite(this.chatTag,
strSpeaker + " you don't have the required permission level to swap players! Swap_Level=" + this.swapLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            if (String.Compare(matchname, "empty") == 0) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I could not find a match for " + plOne); return; }
            if (String.Compare(matchname2, "empty") == 0) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I could not find a match for " + plTwo); return; }


            if (this.m_dicPlayers[matchname].TeamID == 1) { this.m_dicPlayers[matchname].TeamID = 2; } else { this.m_dicPlayers[matchname].TeamID = 1; }
            this.ExecuteCommand("procon.protected.send", "admin.movePlayer", matchname, this.m_dicPlayers[matchname].TeamID.ToString(), "0", "True");

            if (this.m_dicPlayers[matchname2].TeamID == 1) { this.m_dicPlayers[matchname2].TeamID = 2; } else { this.m_dicPlayers[matchname2].TeamID = 1; }
            this.ExecuteCommand("procon.protected.send", "admin.movePlayer", matchname2, this.m_dicPlayers[matchname2].TeamID.ToString(), "0", "True");
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
        }

        public void OnCommandAdmins(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.onlineAdmins.Count > 0)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "[Anu5Admin] - List of admins online:", "player", strSpeaker);
                foreach (KeyValuePair<string, int> str in this.onlineAdmins)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "[Admin List] - " + str.Key + ", Permission Level = " + str.Value, "player", strSpeaker);
                }
                this.ExecuteCommand("procon.protected.send", "admin.say", "[Anu5Admin] - Number of admins online: " + this.onlineAdmins.Count, "player", strSpeaker);
            }
            else { this.ExecuteCommand("procon.protected.send", "admin.say", "[Anu5Admin] - There are currently no admins ingame :(", "player", strSpeaker); }
        }

        public void OnCommandCallAdmin(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.onlineAdmins.Count == 0)
            {
                if ((this.blNotifyEmail == enumBoolYesNo.No) && (this.blNotifyTaskbar == enumBoolYesNo.No)) { ChatWrite(this.chatTag, "Sorry " + strSpeaker + " I have no way of reaching admins atm!"); return; }
                ChatWrite(this.chatTag, "Thanks for your request " + strSpeaker + ", Admins have been notified!");

                if (this.blNotifyEmail == enumBoolYesNo.Yes)
                {

                    this.PrepareEmail(strSpeaker, capCommand.ExtraArguments);
                }
            }
            else { this.ChatWrite(this.chatTag, strSpeaker + " there are already admins online, use the !" + adminlistCMD + " command!"); }
            if (this.blNotifyTaskbar == enumBoolYesNo.Yes)
            {
                TaskBar("Admin Request", strSpeaker + " requested an Admin. " + "Reason:" + capCommand.ExtraArguments);
            }
        }

        public void OnCommandReset(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can reset the round!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.resetLVL)
            {
                ChatWrite(this.chatTag,
strSpeaker + " you don't have the required permission level to reset rounds! reset_Level=" + this.resetLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }
            this.ExecuteCommand("procon.protected.send", "mapList.restartRound");
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        public void OnCommandEndRound(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can end the round!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.endroundLVL)
            {
                ChatWrite(this.chatTag,
strSpeaker + " you don't have the required permission level to end rounds! EndRound_Level=" + this.endroundLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }
            this.ExecuteCommand("procon.protected.send", "mapList.runNextRound");
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }

        public void OnCommandFinish(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.ExecuteCommand("procon.protected.send", "serverInfo");
            this.ExecuteCommand("procon.protected.tasks.add", "taskFinishTask", "2", "1", "1", "procon.protected.plugins.call", "Anu5Admin", "FinishTask", strSpeaker);


        }

        public void FinishTask(string strSpeaker)
        {

            int winnerID = 0;

            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can finish the round!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.finishLVL)
            {
                ChatWrite(this.chatTag,
                    strSpeaker + " you don't have the required permission level to end rounds! FinishRound_Level=" + this.finishLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }
            ConsoleWrite("count", csiServer.TeamScores.Count.ToString());

            for (int i = 0; i < this.csiServer.TeamScores.Count; i++)
            {
                if (csiServer.TeamScores[i].Score > csiServer.TeamScores[winnerID].Score) { winnerID = i; }
                ConsoleWrite("team[" + i.ToString() + "]", csiServer.TeamScores[i].Score.ToString());
                ConsoleWrite("teamid", csiServer.TeamScores[i].TeamID.ToString());
                ConsoleWrite("winner pre-ID", winnerID.ToString());
            }
            winnerID = csiServer.TeamScores[winnerID].TeamID;
            ConsoleWrite("winner", winnerID.ToString());
            //   this.ExecuteCommand("procon.protected.send", "mapList.endRound", winnerID.ToString());
            //   if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }

        }


        public void OnCommandNextMap(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can set the next map!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.nextmapLVL)
            {
                ChatWrite(this.chatTag,
strSpeaker + " you don't have the required permission level to set the next map! NextMap_Level=" + this.nextmapLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            this.ExecuteCommand("procon.protected.send", "mapList.list");
            char[] delimiters = new char[] { '_' };
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            string[] mapParts = chatArg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string nextMap = MapToCode(mapParts[0]);
            string gameMode = gModeToCode(mapParts[1]);
            int index = 0;

            for (int i = 0; i < this.mapList.Count; i++)
            {
                index = i;
                if ((this.mapList[i].MapFileName.CompareTo(nextMap) == 0) && (this.mapList[i].Gamemode.CompareTo(gameMode) == 0))
                {
                    this.ExecuteCommand("procon.protected.send", "mapList.setNextMapIndex", index.ToString());
                    ChatWrite("[MapManager] -", "Next map set to " + chatArg + ".");
                    if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
                    return;
                }
            }
            ChatWrite("[MapManager] -", "Sorry " + strSpeaker + " " + chatArg + " is not on the map list!"); return;
        }

        public void OnCommandSwitchMap(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, only admins can switch maps!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.switchmapLVL)
            {
                ChatWrite(this.chatTag,
                    strSpeaker + " you don't have the required permission level to switch maps! SwitchMap_Level=" + this.switchmapLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            this.ExecuteCommand("procon.protected.send", "mapList.list");
            char[] delimiters = new char[] { '_' };
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            string[] mapParts = chatArg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string nextMap = MapToCode(mapParts[0]);
            string gameMode = gModeToCode(mapParts[1]);
            int index = 0;

            for (int i = 0; i < this.mapList.Count; i++)
            {
                index = i;
                if ((this.mapList[i].MapFileName.CompareTo(nextMap) == 0) && (this.mapList[i].Gamemode.CompareTo(gameMode) == 0))
                {
                    this.ExecuteCommand("procon.protected.send", "mapList.setNextMapIndex", index.ToString());
                    this.ExecuteCommand("procon.protected.send", "mapList.runNextRound");
                    if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
                    return;
                }
            }
            ChatWrite("[MapManager] -", "Sorry " + strSpeaker + " " + chatArg + " is not on the map list!"); return;
        }

        public void OnCommandListMaps(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, this command is for admins!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.maplistLVL)
            {
                ChatWrite(this.chatTag,
strSpeaker + " you don't have the required permission level to use this command! MapList_Level=" + this.maplistLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            if (string.Compare(capCommand.ExtraArguments, "full") == 0)
            {
                string fullList = "karkand, oman, sharqi, wake, bazaar, teheran, caspian, crossing, firestorm, davamand, canals, kharg, metro";
                string GameModes = "cq64(CQ-Large/Assault64), cq0(CQ-Small/Assault), cq1(Assault2), rush, sqr(SquadRush), sqdm(SquadDeathMatch), tdm";
                ChatWrite("[MapList Full] -", fullList);
                ChatWrite("[GameModes] -", GameModes);
                return;
            }

            this.ExecuteCommand("procon.protected.send", "mapList.list");
            int listDelay = 1;
            int mapCount = 0;
            foreach (MaplistEntry map in this.mapList)
            {
                this.ExecuteCommand("procon.protected.tasks.add", "taskmapList", listDelay.ToString(), (this.mapList.Count * listDelay).ToString(), "1",
                    "procon.protected.send", "admin.say", "[MapList] - [" + mapCount + "]" + MapToEnglish(map.MapFileName) + "_" + gModeToEnglish(map.Gamemode), "all");
                listDelay += 2;
                mapCount += 1;
            }
            if (this.logCmds == enumBoolYesNo.Yes) { logAdmin(strSpeaker, strText); }
        }

        public void OnCommandAddMap(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, this command is for admins!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.addmapLVL)
            {
                ChatWrite(this.chatTag,
                    strSpeaker + " you don't have the required permission level to use this command! AddMap_Level=" + this.addmapLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }
            char[] delimiters = new char[] { '_' };
            string chatArg = capCommand.ExtraArguments.ToLower(); if (chatArg == "") { return; }
            string[] mapParts = chatArg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (mapParts.Length < 3)
            {
                ChatWrite(strSpeaker, "You are missing an argument");
                ChatWrite("Syntax: !", addmapCMD + " mapname_gamemode_rounds");
                return;
            }

            string mapName = MapToCode(mapParts[0]);
            string gameMode = gModeToCode(mapParts[1]);
            string rounds = mapParts[2];


            if ((this.gamelist.ContainsKey(mapParts[0]) == false) || (MapToCode(mapParts[0]).CompareTo("empty") == 0)) { ChatWrite("[MapManager] -", "Sorry, " + strSpeaker + " " + mapParts[0] + " is not available, try !" + maplistCMD + " full"); return; }
            if ((this.gamelist[mapParts[0]].Contains(mapParts[1]) == false) || (gModeToCode(mapParts[1]).CompareTo("empty") == 0))
            {
                ChatWrite("[MapManager] -", "Sorry, " + strSpeaker + " " + mapParts[1] + " is not available for " + mapParts[0]);
                ChatWrite("[MapManager] -", "Game modes for " + mapParts[0] + " are " + this.gamelist[mapParts[0]]);
                return;
            }
            ConsoleWrite("[Anu5Admin] -", "Mapname-" + mapParts[0] + ", GameMode-" + mapParts[1] + ", Rounds-" + mapParts[2] + " added to the Map List.");
            ChatWrite("[MapManager] -", "Mapname-" + mapParts[0] + ", GameMode-" + mapParts[1] + ", Rounds-" + mapParts[2] + " added to the Map List.");
            this.ExecuteCommand("procon.protected.send", "mapList.add", mapName, gameMode, rounds);
            this.ExecuteCommand("procon.protected.send", "mapList.save");
            this.ExecuteCommand("procon.protected.send", "mapList.list");
        }
        public void OnCommandRemoveMap(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, this command is for admins!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.removemapLVL)
            {
                ChatWrite(this.chatTag,
                    strSpeaker + " you don't have the required permission level to use this command! RemoveMap_Level=" + this.removemapLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }

            int mapIndex;
            bool parsed = Int32.TryParse(capCommand.ExtraArguments, out mapIndex);
            if (!parsed) { ChatWrite("[MapManager] -", "Sorry, " + strSpeaker + ", " + capCommand.ExtraArguments + " is not a number."); return; }

            if (this.mapList.Count >= mapIndex)
            {
                this.ExecuteCommand("procon.protected.send", "mapList.remove", capCommand.ExtraArguments);
                this.ExecuteCommand("procon.protected.send", "mapList.save");
                this.ExecuteCommand("procon.protected.send", "mapList.list");
                ChatWrite("[MapManager] -", "Map at index:" + mapIndex + ", removed.");
                return;
            }

            ChatWrite("[MapManager] -", "Sorry, " + strSpeaker + " the current maplist does not contain a map with index[" + capCommand.ExtraArguments + "]");
        }

        public void OnCommandMailLog(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, this command is for admins!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.maillogLVL)
            {
                ChatWrite(this.chatTag,
                    strSpeaker + " you don't have the require   d permission level to use this command! MailLog_Level=" + this.maillogLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }
            if (String.Compare(capCommand.ExtraArguments, "") == 0) { ChatWrite(strSpeaker, ", you need to include an email address"); return; }

            string body = String.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("<table border='1' rules='rows'>");
            string[] loglines = System.IO.File.ReadAllLines(this.logPath);
            foreach (string line in loglines)
            {
                sb.Append("<tr><td>" + line + "</td></tr>");
            }
            sb.Append("</table>");
            body = sb.ToString();
            SendLogMail("Command List Log.", body, capCommand.ExtraArguments);
        }

        public void OnCommandClearLog(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.onlineAdmins.ContainsKey(strSpeaker.ToLower())) { ChatWrite(this.chatTag, strSpeaker + " you noob, this command is for admins!"); return; }
            if (this.onlineAdmins[strSpeaker.ToLower()] < this.clearlogLVL)
            {
                ChatWrite(this.chatTag,
                    strSpeaker + " you don't have the required permission level to use this command! ClearLog_Level=" + this.clearlogLVL + ", Your_Level=" + this.onlineAdmins[strSpeaker.ToLower()]); return;
            }
            System.IO.File.WriteAllText(this.logPath, string.Empty);
        }

        #endregion

        #region Other Functions
        private void ConsoleWrite(string subject, string message)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^3" + subject + "^0^n: {0}", message));
        }

        private void ChatWrite(string subject, string message)
        {
            this.ExecuteCommand("procon.protected.send", "admin.say", subject + " " + message, "all");
        }

        private void TaskBar(string title, string message)
        {
            this.ExecuteCommand("procon.protected.notification.write", title, message, "false");
        }

        public void SpankNoob(string player)
        {
            if (this.d_SpankKills.ContainsKey(player.ToLower()) == false) { this.d_SpankKills.Add(player.ToLower(), 0); }

            if (this.d_SpankKills[player.ToLower()] != this.SpankKillsMax)
            {
                this.d_SpankKills[player.ToLower()]++;
                ConsoleWrite("[NannyBot] -", "killed(" + player + ") " + d_SpankKills[player] + " times.");
                this.ExecuteCommand("procon.protected.send", "admin.killPlayer", player);
            }
            else
            {
                this.d_SpankKills.Remove(player);
                this.nannylist.Remove(player);
                ConsoleWrite("[NannyBot] -", "Spanking " + player + " complete!");
                ChatWrite("[NannyBot] -", "Punishing of " + player + " complete! Now play nice children!");
            }
        }

        public String checkPlayerName(String player)
        {
            foreach (CPlayerInfo cpiPlayer in this.lstPlayers)
            {
                if (cpiPlayer.SoldierName.ToLower().Contains(player))
                {
                    return cpiPlayer.SoldierName.ToLower();
                }
            }
            return "empty";
        }

        public void ResetLists()
        {
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            this.d_Warned.Clear();
            this.d_Headshots.Clear();
            this.d_Weapons.Clear();
        }

        public String MapToCode(String mapName)
        {

            if (String.Compare(mapName, "karkand") == 0) { return "XP1_001"; }
            else if (String.Compare(mapName, "oman") == 0) { return "XP1_002"; }
            else if (String.Compare(mapName, "sharqi") == 0) { return "XP1_003"; }
            else if (String.Compare(mapName, "wake") == 0) { return "XP1_004"; }
            else if (String.Compare(mapName, "bazaar") == 0) { return "MP_001"; }
            else if (String.Compare(mapName, "teheran") == 0) { return "MP_003"; }
            else if (String.Compare(mapName, "caspian") == 0) { return "MP_007"; }
            else if (String.Compare(mapName, "crossing") == 0) { return "MP_011"; }
            else if (String.Compare(mapName, "firestorm") == 0) { return "MP_012"; }
            else if (String.Compare(mapName, "davamand") == 0) { return "MP_013"; }
            else if (String.Compare(mapName, "canals") == 0) { return "MP_017"; }
            else if (String.Compare(mapName, "kharg") == 0) { return "MP_018"; }
            else if (String.Compare(mapName, "metro") == 0) { return "MP_Subway"; }
            else { return "empty"; }
        }

        public String MapToEnglish(String mapName)
        {
            if (String.Compare(mapName, "XP1_001") == 0) { return "karkand"; }
            else if (String.Compare(mapName, "XP1_002") == 0) { return "oman"; }
            else if (String.Compare(mapName, "XP1_003") == 0) { return "sharqi"; }
            else if (String.Compare(mapName, "XP1_004") == 0) { return "wake"; }
            else if (String.Compare(mapName, "MP_001") == 0) { return "bazaar"; }
            else if (String.Compare(mapName, "MP_003") == 0) { return "teheran"; }
            else if (String.Compare(mapName, "MP_007") == 0) { return "caspian"; }
            else if (String.Compare(mapName, "MP_011") == 0) { return "crossing"; }
            else if (String.Compare(mapName, "MP_012") == 0) { return "firestorm"; }
            else if (String.Compare(mapName, "MP_013") == 0) { return "davamand"; }
            else if (String.Compare(mapName, "MP_017") == 0) { return "canals"; }
            else if (String.Compare(mapName, "MP_018") == 0) { return "kharg"; }
            else if (String.Compare(mapName, "MP_Subway") == 0) { return "metro"; }
            else { return "empty"; }
        }

        public String gModeToEnglish(String gameName)
        {
            if (String.Compare(gameName, "ConquestLarge0") == 0) { return "cq64"; }
            else if (String.Compare(gameName, "ConquestSmall0") == 0) { return "cq0"; }
            else if (String.Compare(gameName, "ConquestSmall1") == 0) { return "cq1"; }
            else if (String.Compare(gameName, "RushLarge0") == 0) { return "rush"; }
            else if (String.Compare(gameName, "SquadRush0") == 0) { return "sqr"; }
            else if (String.Compare(gameName, "SquadDeathMatch0") == 0) { return "sqdm"; }
            else if (String.Compare(gameName, "TeamDeathMatch0") == 0) { return "tdm"; }
            else { return "empty"; }
        }

        public String gModeToCode(String gameName)
        {
            if (String.Compare(gameName, "cq64") == 0) { return "ConquestLarge0"; }
            else if (String.Compare(gameName, "cq0") == 0) { return "ConquestSmall0"; }
            else if (String.Compare(gameName, "cq1") == 0) { return "ConquestSmall1"; }
            else if (String.Compare(gameName, "rush") == 0) { return "RushLarge0"; }
            else if (String.Compare(gameName, "sqr") == 0) { return "SquadRush0"; }
            else if (String.Compare(gameName, "sqdm") == 0) { return "SquadDeathMatch0"; }
            else if (String.Compare(gameName, "tdm") == 0) { return "TeamDeathMatch0"; }
            else { return "empty"; }
        }
        public void populateGameList()
        {
            gamelist.Clear();
            gamelist.Add("karkand", "cq64, cq0, cq1, tdm, sqdm, sqr, rush");
            gamelist.Add("oman", "cq64, cq0, cq1, tdm, sqdm, sqr, rush");
            gamelist.Add("sharqi", "cq64, cq0, cq1, tdm, sqdm, sqr, rush");
            gamelist.Add("wake", "cq64, cq0, tdm, sqdm, sqr, rush");
            gamelist.Add("bazaar", "cq64, cq0, tdm, sqdm, sqr, rush");
            gamelist.Add("teheran", "cq64, cq0, tdm, sqdm, sqr, rush");
            gamelist.Add("caspian", "cq64, cq0, tdm, sqdm, sqr, rush");
            gamelist.Add("crossing", "cq64, cq0, tdm, sqdm, sqr, rush");
            gamelist.Add("firestorm", "cq64, cq0, tdm, sqdm, sqr, rush");
            gamelist.Add("davamand", "cq64, cq0, tdm, sqdm, sqr, rush");
            gamelist.Add("canals", "cq64, cq0, tdm, sqdm, sqr, rush");
            gamelist.Add("kharg", "cq64, cq0, tdm, sqdm, sqr, rush");
            gamelist.Add("metro", "cq64, cq0, tdm, sqdm, sqr, rush");
        }

        static int Compare1(KeyValuePair<string, string> a, KeyValuePair<string, string> b)
        {
            return a.Key.CompareTo(b.Key);
        }

        public void logAdmin(string adminName, string command)
        {
            DateTime cmdTime = DateTime.Now;
            this.adminlog.Clear();
            command = "- [" + command + "] - [" + this.csiServer.ServerName + "(" + this.csiServer.PlayerCount + "/" + this.csiServer.MaxPlayerCount + ")]";

            this.adminlog.Add(new KeyValuePair<string, string>(adminName + "(" + String.Format("{0:MM/dd/yyyy-HH:mm:ss}", cmdTime) + ")", command));
            this.logString = string.Empty;
            char[] delimiters = new char[] { ' ' };
            string[] loglines = System.IO.File.ReadAllLines(this.logPath);

            foreach (string line in loglines)
            {
                string[] lineParts = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < lineParts.Length; i++)
                {
                    this.logString = this.logString + lineParts[i] + " ";
                }

                this.adminlog.Add(new KeyValuePair<string, string>(lineParts[0], this.logString));
                this.logString = String.Empty;
            }

            this.adminlog.Sort(Compare1);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(this.logPath))
            {
                file.WriteLine(this.adminlog[0].Key + " " + this.adminlog[0].Value);
            }

            if (this.adminlog.Count >= 2)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(this.logPath, true))
                {
                    for (int i = 1; i < this.adminlog.Count; i++)
                    {
                        file.WriteLine(this.adminlog[i].Key + " " + this.adminlog[i].Value);
                    }
                }
            }
        }



        #endregion

        #region Mailing Functions
        private void PrepareEmail(string sender, string message)
        {
            if (this.blNotifyEmail == enumBoolYesNo.Yes)
            {
                string subject = String.Empty;
                string body = String.Empty;

                subject = "[Anu5 Admin Request] - (" + sender + ") requested an admin. Message - " + message;

                StringBuilder sb = new StringBuilder();
                sb.Append("<b>Anu5 Admin Request Notification</b><br /><br />");
                sb.Append("Date/Time of call:<b> " + DateTime.Now.ToString() + "</b><br />");
                sb.Append("Servername:<b> " + this.csiServer.ServerName + "</b><br />");
                sb.Append("Server address:<b> " + this.strHostName + ":" + this.strPort + "</b><br />");
                sb.Append("Playercount:<b> " + this.csiServer.PlayerCount + "/" + this.csiServer.MaxPlayerCount + "</b><br />");
                sb.Append("Map:<b> " + this.csiServer.Map + "</b><br /><br />");
                sb.Append("Request-Sender:<b> " + sender + "</b><br />");
                sb.Append("Message:<b> " + message + "</b><br /><br />");
                sb.Append("<i>Playertable:</i><br />");
                sb.Append("<table border='1' rules='rows'><tr><th>Playername</th><th>Score</th><th>Kills</th><th>Deaths</th><th>HPK%</th><th>KDR</th><th>GUID</th></tr>");
                foreach (CPlayerInfo player in this.lstPlayers)
                {
                    double mHeadshots = 0;
                    if (this.d_Headshots.ContainsKey(player.SoldierName.ToLower()) == true)
                    {
                        if (player.Kills > 0) { mHeadshots = (double)(d_Headshots[player.SoldierName.ToLower()] * 100) / player.Kills; }
                    }
                    sb.Append("<tr align='center'><td>" + player.SoldierName + "</td><td>" + player.Score + "</td><td>" + player.Kills + "</td><td>" + player.Deaths + "</td><td>" + String.Format("{0:0.##}", mHeadshots) + "</td><td>" + String.Format("{0:0.##}", player.Kdr) + "</td><td>" + player.GUID + "</td></tr>");
                }
                sb.Append("</table>");

                body = sb.ToString();

                this.EmailWrite(subject, body);
            }
        }

        private void SuspectMail(string player, string trigger, string weapons)
        {
            string subject = String.Empty;
            string body = String.Empty;
            string adminword = "Please remember the player that triggered this email is just a SUSPECT.<br />Please do not kick or ban just based off the information in this mail.<br />Please make sure you do things like checking their battlelog page,<br />and monitor them in-game so that you can make a fair decision.";
            subject = "[Anu5 Suspicious Player Alert!] - (" + player + ") is a suspected cheater. Trigger(" + trigger + ")";

            StringBuilder sb = new StringBuilder();
            sb.Append("<table border='1' rules='rows'><tr align='left'><td>");
            sb.Append("Suspected Cheater:</td><td><b>" + player + "</b></td></tr><tr align='left'><td>");
            sb.Append("Date/Time of call:</td><td><b>" + DateTime.Now.ToString() + "</b></td></tr><tr align='left'><td>");
            sb.Append("Servername:</td><td><b>" + this.csiServer.ServerName + "</b></td></tr><tr align='left'><td>");
            sb.Append("Server address:</td><td><b>" + this.strHostName + ":" + this.strPort + "</b></td></tr><tr align='left'><td>");
            sb.Append("Playercount:</td><td><b>" + this.csiServer.PlayerCount + "/" + this.csiServer.MaxPlayerCount + "</b></td></tr><tr align='left'><td>");
            sb.Append("Map:</td><td><b>" + this.csiServer.Map + "</b></td></tr><tr align='left'><td>");
            sb.Append("Alert Trigger:</td><td><b>" + trigger + "</b></td></tr><tr align='left'><td>");
            sb.Append("Word to Admins:</td><td><b>" + adminword + "</b>");
            sb.Append("</td></tr></table><br /><br />");

            sb.Append("<table border='1' rules='rows'><tr><th>Playername</th><th>Score</th><th>Kills</th><th>Deaths</th><th>HPK%</th><th>KDR</th><th>GUID</th></tr>");

            double mHeadshots = 0;
            if (this.d_Headshots.ContainsKey(player.ToLower()) == true)
            {
                if (this.m_dicPlayers[player].Kills > 0) { mHeadshots = (double)(this.d_Headshots[player] * 100) / this.m_dicPlayers[player].Kills; }
            }
            sb.Append("<tr align='center'><td>" + player + "</td><td>" + this.m_dicPlayers[player].Score + "</td><td>" + this.m_dicPlayers[player].Kills + "</td><td>" + this.m_dicPlayers[player].Deaths + "</td><td>" + String.Format("{0:0.##}", mHeadshots) + "</td><td>" + String.Format("{0:0.##}", this.m_dicPlayers[player].Kdr) + "</td><td>" + this.m_dicPlayers[player].GUID + "</td></tr>");
            sb.Append("</table><br /><br /><table border='1' rules='rows'><tr align='center'><td>");
            sb.Append("Weapons Used By Suspected Player</td></tr><tr align='left'><td>" + weapons + "</td></tr><tr align='center'><td><b>Keep in mind vehicle kills show as Weapon-(DEATH).</b></td></tr></table>");
            body = sb.ToString();

            this.EmailWrite(subject, body);
        }

        private void EmailWrite(string subject, string body)
        {
            try
            {
                if (this.strSenderMail == null || this.strSenderMail == String.Empty)
                {
                    this.ConsoleWrite("[Mailer]", "No sender-mail is given!");
                    return;
                }

                MailMessage email = new MailMessage();

                email.From = new MailAddress(this.strSenderMail);

                if (this.lstReceiverMail.Count > 0)
                {
                    foreach (string mailto in this.lstReceiverMail)
                    {
                        if (mailto.Contains("@") && mailto.Contains("."))
                        {
                            email.To.Add(new MailAddress(mailto));
                        }
                        else
                        {
                            this.ConsoleWrite("[Mailer]", "Error in receiver-mail: " + mailto);
                        }
                    }
                }
                else
                {
                    this.ConsoleWrite("[Mailer]", "No receiver-mail are given!");
                    return;
                }

                email.Subject = subject;
                email.Body = body;
                email.IsBodyHtml = true;
                email.BodyEncoding = UTF8Encoding.UTF8;

                SmtpClient smtp = new SmtpClient(this.strSMTPServer, this.iSMTPPort);
                if (this.blUseSSL == enumBoolYesNo.Yes)
                {
                    smtp.EnableSsl = true;
                }
                else if (this.blUseSSL == enumBoolYesNo.No)
                {
                    smtp.EnableSsl = false;
                }
                smtp.Timeout = 10000;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(this.strSMTPUser, this.strSMTPPassword);
                smtp.Send(email);

                this.ConsoleWrite("[Mailer]", "A notification email has been sent.");
            }
            catch (Exception e)
            {
                this.ConsoleWrite("[Mailer]", "Error while sending mails: " + e.ToString());
            }
        }


        private void SendLogMail(string subject, string body, string address)
        {
            try
            {
                if (this.strSenderMail == null || this.strSenderMail == String.Empty)
                {
                    this.ConsoleWrite("[Mailer]", "No sender-mail is given!");
                    return;
                }

                MailMessage email = new MailMessage();

                email.From = new MailAddress(this.strSenderMail);

                if (address.Contains("@") && address.Contains("."))
                {
                    email.To.Add(new MailAddress(address));
                }
                else
                {
                    this.ChatWrite("[Mailer]", "Error in receiver-mail: " + address);
                }


                email.Subject = subject;
                email.Body = body;
                email.IsBodyHtml = true;
                email.BodyEncoding = UTF8Encoding.UTF8;

                SmtpClient smtp = new SmtpClient(this.strSMTPServer, this.iSMTPPort);
                if (this.blUseSSL == enumBoolYesNo.Yes)
                {
                    smtp.EnableSsl = true;
                }
                else if (this.blUseSSL == enumBoolYesNo.No)
                {
                    smtp.EnableSsl = false;
                }
                smtp.Timeout = 10000;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(this.strSMTPUser, this.strSMTPPassword);
                smtp.Send(email);

                this.ConsoleWrite("[Mailer]", "Command Log has been sent.");
            }
            catch (Exception e)
            {
                this.ConsoleWrite("[Mailer]", "Error while sending mails: " + e.ToString());
            }
        }
        #endregion

    }
}