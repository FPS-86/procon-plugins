using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Plugin;

namespace PRoConEvents
{
    //Aliases

    public class VipHunterAnnoyer : PRoConPluginAPI, IPRoConPluginInterface
    {

        /// <summary>
        /// Random number generated, seeded at initialization.
        /// </summary>
        protected Random Random;

        /// <summary>
        /// A list of EA GUID's for identified dog tag hunters.
        /// </summary>
        protected List<String> HunterGuids;

        /// <summary>
        /// A list of player names of dog tag hunters.
        /// </summary>
        protected List<String> HunterNames;

        /// <summary>
        /// Boolean, false by default, to determine if a reason should be given to the hunter about why the action was taken against them.
        /// </summary>
        public bool OutputPokeMessage;

        /// <summary>
        /// The message to sent to the player when they are poked.
        /// </summary>
        public String PokeMessage;

        /// <summary>
        /// The message that is sent to a Vip when protection is enabled for them.
        /// </summary>
        public String VipProtectionEnabledMessage;

        /// <summary>
        /// A list of vip names. We only want to annoy tag hunters if a vip is in the server and a vip has a positive score.
        /// </summary>
        protected List<String> VipNames;

        /// <summary>
        /// Option to turn on/off the anti-squad-joining functionality.
        /// </summary>
        protected bool PreventHunterSquads;

        /// <summary>
        /// True if a VIP is in the server and has a positive score, false if the annoying stuff is off because no VIP's have a positive score.
        /// </summary>
        protected bool ProtectionEnabled;

        /// <summary>
        /// The chance (0-100) that the player will be killed when they spawn
        /// </summary>
        protected int KillProbability;

        /// <summary>
        /// The chance (0-100) that the player will be kicked when they spawn
        /// </summary>
        protected int KickProbability;

        /// <summary>
        /// The time in which the plugin should be disabled. This allows for the plugin to 'cool down' over map changes so the plugin remains
        /// effective even though VIP's have a score of zero. The default is the current time, so changes can be made immediately.
        /// </summary> 
        protected DateTime ProtectionEnabledCooldown;

        /// <summary>
        /// Dictionary of statistics of actions taken against hunters. The dictionary is indexed by the players name.
        /// </summary>
        public Dictionary<String, HunterActionStatistic> HunterActionStatistics;

        public enum MessageType
        {
            Warning,
            Error,
            Exception,
            Normal
        };

        private int _debugLevel;
        private bool _isEnabled;

        public VipHunterAnnoyer()
        {
            _isEnabled = false;
            _debugLevel = 2;

            this.Random = new Random();

            this.HunterNames = new List<String>();
            this.HunterGuids = new List<String>();

            this.VipNames = new List<String>();

            this.OutputPokeMessage = false;
            this.PokeMessage = "Please go fuck yourself - Love Phogue :)";

            this.VipProtectionEnabledMessage = "VIP protection enabled.";

            this.ProtectionEnabled = false;
            this.ProtectionEnabledCooldown = DateTime.Now;

            this.PreventHunterSquads = true;

            this.KickProbability = 5;
            this.KillProbability = 95;

            this.HunterActionStatistics = new Dictionary<String, HunterActionStatistic>();
        }

        public String GetPluginName()
        {
            return "Phogue's VIP Hunter Annoyer";
        }

        public String GetPluginVersion()
        {
            return "1.0.6.1";
        }

        public String GetPluginAuthor()
        {
            return @"Geoff ""Phogue"" Green";
        }

        public String GetPluginWebsite()
        {
            return "http://myrcon.com";
        }

        public String GetPluginDescription()
        {
            return @"
<h1>Phogue's VIP Hunter Annoyer</h1>

<h2>Thanks</h2>
<ul>
    <li>EBassie for testing and providing ideas.</li>
    <li>PapaCharlie for the neato boilerplate plugin.</li>
    <li>Zaeed, for his terrible suggestions.</li>
</ul>

<h2>Description</h2>

<p>This plugin will read in a list of known DICE/DICE Friends dog tag hunters, killing (80% chance) or kicking (20% chance) them after they spawn.</p>

<p>Provided a VIP is in the server and they have a positive score above 0, then this plugin will be in effect. Otherwise, it will be like the plugin isn't running at all.</p>

<p>They will be killed or kicked, not banned to ensure they waste time waiting in queues and joining the server.</p>

<p>They will be killed or kicked at a random interval after they spawn, hopefully giving a brief illusion that the inevitable will not happen this time around.</p>

<p>The player will be kicked or killed with no messages at all by default. There is an option to enable message output with the default message ""Please go fuck yourself - Love Phogue :)"". You can modify the message in the settings.</p>

<p>The player, by default, will not be able to join any squads. This is optional though as it may conflict with existing plugins.</p>

<p>Every 60 seconds every VIP will be privately sent statistics on who has been killed/kicked because of this plugin. If no actions are taken in the last 60 seconds then the statistics will not be sent. This was done to reduce chat spam, but still let the VIP know action is being taken to protect them.</p>

<h2>The list is moderated entirely by Phogue and it only contains the dog tag hunters that:</h2>
<ul>
    <li>Have friends join your squad or team to communicate where you are</li>
    <li>Have friends join your squad or team to revive you for multiple knife kills.</li>
    <li>Are in a platoon that specifically hunts for these tags</li>
    <li>Ignore objectives completely</li>
    <li>I personally witness the previous actions.</li>
</ul>

<h2>People won't be added to this list if:</h2>
<ul>
    <li>They knife some body with the tags. That's always funny as hell. You should knife MorpheusX(AUT). He loves it :)</li>
    <li>No serious, if you see me in game don't think twice about knifing me. Come and knife me. Just don't join my game with a 400 ping, ignore the objectives and have your friends join my squad to tell you where I am. It's so annoying I wrote this plugin.</li>
</ul>

<h2>You should run this plugin if:</h2>
<ul>
    <li>You have dice developer or friends tags and run a server.</li>
    <li>You have developers or friends frequent your server (they will love you for running this)</li>
    <li>You trust Phogue's judgment not to turn this into a personal hit list.</li>
</ul>

<h2>I'm no longer a tag hunter, can I get taken off this list please?</h2>
<ul>
    <li>Please go fuck yourself - Love Phogue :)</li>
</ul>

<p>The tag-hunter list is updated at <a href=""http://myrcon.com/procon/streams/tag_hunters"" target=""_blank"">myrcon.com</a>. PM Phogue on the forums to add/update people.</p>

<p>The tag-owner list is updated at <a href=""http://myrcon.com/procon/streams/tag_owners"" target=""_blank"">myrcon.com</a>. PM Phogue on the forums to add/update people.</p>

<h2>Commands</h2>
<p>none</p>

<h2>Settings</h2>
<blockquote><h4>Debug Level</h4>2 by default. Set to 0 to turn off debug output.</blockquote>
<blockquote><h4>Prevent Hunter Squads</h4>True by default. When turned on this will prevent tag hunters from squading up.</blockquote>
<blockquote><h4>Output Poke Message</h4>False by default. When turned on this send the ""Poke Message"" to the VIP Hunter whenever an action is taken against them. By default this runs silently so they won't know what's going on, at least until they google it.</blockquote>
<blockquote><h4>Poke Message</h4>The default reason of ""Please go fuck yourself - Love Phogue :)"". I prefix it in the plugin with the plugin name ""[Phogue's VIP Hunter Annoyer]. It's exceptionally rare that I take credit or plaster my name on your servers, but I kinda want this :)</blockquote>
<blockquote><h4>Kick Probability</h4>5 by default. What percentage the tag hunter has of being kicked when they spawn. Value must be between 0-100. Set to 0 to disable kicking.</blockquote>
<blockquote><h4>Kill Probability</h4>5 by default. What percentage the tag hunter has of being killed when they spawn. Value must be between 0-100. Set to 0 to disable killing.</blockquote>
<blockquote><h4>Protection Enabled Message</h4>The default text of ""VIP protection enabled."". This message will be sent to a VIP when protection is enabled or when they join while protection is already enabled.</blockquote>

<h3>Changelog</h3>
<blockquote><h4>1.0.6.1 (13-AUG-2013)</h4>
    <ul>
        <li>Removed the message when hunters join a server. Since this plugin isn't controlled by the people that would receive the chat spam it isn't fair to impose the spam on them. Also, considering the player has a summarized message every 60 seconds they will find out who the tag hunters are anyway when the plugin takes action.</li>
    </ul>
</blockquote>
<blockquote><h4>1.0.6.0 (13-AUG-2013)</h4>
    <ul>
        <li>Added an option to enable/disable a message for the tag hunter about why the were killed/kicked/removed from a squad. This option is set to silent by default (so they don't know what happened).</li>
        <li>Added statistics output for VIP's. This will tell VIP's the stats for the last three victims of the plugin every 60 seconds, provided the stats have changed. Just lets them know stuff is happening. Example output (with example Hunter names):
            <ul>
                <li>PhogueZero [1 kill, 2 kicks], Zaeed_au [3 kills, 1 kick]</li>
            </ul>
        </li>
        <li>Added message to VIP's alerting them to new hunters that join the server.</li>
    </ul>
</blockquote>
<blockquote><h4>1.0.5.0 (12-AUG-2013)</h4>
    <ul>
        <li>Added options to alter the kick/kill probabilty.</li>
        <li>Reduced the kick option default probability. Now 5% down from 20%.</li>
        <li>Increased the update frequency for tag hunters. Now 30 minutes, up from 5 minutes.</li>
        <li>Removed the option for the full list of VIP names. As this was saved in each servers options it would therefore prevent new names from being added in plugin updates.</li>
        <li>Now polls http://myrcon.com/procon/streams/tag_owners for a list of VIP names which can then be modified along with the tag hunter list. The GUID's are not added/required.</li>
    </ul>
</blockquote>
<blockquote><h4>1.0.4.1 (9-AUG-2013)</h4>
    - Uncommented some test code.
</blockquote>
<blockquote><h4>1.0.4.0 (9-AUG-2013)</h4>
    - Added an enabled message for VIP's telling them they are protected by the plugin.<br/>
    - Added a cool down to the protection disabled. Now on round changes VIP's will be protected for three minutes even with a score of zero.<br/>
    - Added an option (on by default) to prevent VIP hunters from joining squads. This was made optional as it may clash with existing plugins.<br/>
    - Now pokes hunters at least every five minutes, regardless of spawning in.<br/>
    - Increased the time before punishment if the player is not in a squad (they have to spawn at a point).<br/>
</blockquote>
<blockquote><h4>1.0.3.0 (8-AUG-2013)</h4>
    - Fine. Reverted to 2.0.<br/>
</blockquote>
<blockquote><h4>1.0.2.0 (7-AUG-2013)</h4>
    - Removed LINQ so it actually compiles in the current release of Procon.<br/>
</blockquote>
<blockquote><h4>1.0.0.1 (7-AUG-2013)</h4>
    - Removed EBassie's stuff.<br/>
</blockquote>
<blockquote><h4>1.0.0.0 (7-AUG-2013)</h4>
    - initial version<br/>
</blockquote>
";
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> variables = new List<CPluginVariable>();

            variables.Add(new CPluginVariable("Settings|Debug level", _debugLevel.GetType(), _debugLevel));
            variables.Add(new CPluginVariable("Settings|Prevent Hunter Squads", this.PreventHunterSquads.GetType(), this.PreventHunterSquads));
            variables.Add(new CPluginVariable("Settings|Output Poke Message", this.OutputPokeMessage.GetType(), this.OutputPokeMessage));

            if (this.OutputPokeMessage == true)
            {
                variables.Add(new CPluginVariable("Settings|Poke Message", this.PokeMessage.GetType(), this.PokeMessage));
            }

            variables.Add(new CPluginVariable("Settings|Kick Probability", this.KickProbability.GetType(), this.KickProbability));
            variables.Add(new CPluginVariable("Settings|Kill Probability", this.KillProbability.GetType(), this.KillProbability));
            variables.Add(new CPluginVariable("VIP|Protection Enabled Message", this.VipProtectionEnabledMessage.GetType(), this.VipProtectionEnabledMessage));

            return variables;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        public void SetPluginVariable(String strVariable, String strValue)
        {
            if (Regex.Match(strVariable, @"Debug level").Success)
            {
                int tmp = 2;
                int.TryParse(strValue, out tmp);
                _debugLevel = tmp;
            }
            else if (Regex.Match(strVariable, @"Prevent Hunter Squads").Success)
            {
                bool tmp = true;
                bool.TryParse(strValue, out tmp);
                this.PreventHunterSquads = tmp;
            }
            else if (Regex.Match(strVariable, @"Output Poke Message").Success)
            {
                bool tmp = true;
                bool.TryParse(strValue, out tmp);
                this.OutputPokeMessage = tmp;
            }
            else if (Regex.Match(strVariable, @"Poke Message").Success)
            {
                this.PokeMessage = strValue;
            }
            else if (Regex.Match(strVariable, @"Kick Probability").Success)
            {
                int tmp = 2;
                int.TryParse(strValue, out tmp);
                this.KickProbability = tmp;

                if (this.KickProbability < 0) this.KickProbability = 0;
                if (this.KickProbability > 100) this.KickProbability = 100;

                this.KillProbability = 100 - this.KickProbability;
            }
            else if (Regex.Match(strVariable, @"Kill Probability").Success)
            {
                int tmp = 2;
                int.TryParse(strValue, out tmp);
                this.KillProbability = tmp;

                if (this.KillProbability < 0) this.KillProbability = 0;
                if (this.KillProbability > 100) this.KillProbability = 100;

                this.KickProbability = 100 - this.KillProbability;
            }
            else if (Regex.Match(strVariable, @"Protection Enabled Message").Success)
            {
                this.VipProtectionEnabledMessage = strValue;
            }
        }

        public void OnPluginLoaded(String strHostName, String strPort, String strPRoConVersion)
        {
            RegisterEvents(GetType().Name, "OnPlayerSpawned", "OnListPlayers", "OnPlayerJoin", "OnPlayerLeft", "OnRoundOver", "OnRestartLevel", "OnRunNextLevel", "OnPlayerSquadChange", "OnPlayerTeamChange");
        }

        public void OnPluginEnable()
        {
            _isEnabled = true;
            ConsoleWrite("Enabled!");

            this.ExecuteCommand("procon.protected.tasks.add", this.GetType().Name + "_OnTaskMyrconProconTagHuntersRequest", "0", "1800", "-1", "procon.protected.plugins.call", this.GetType().Name, "OnTaskMyrconProconTagHuntersRequest");
            this.ExecuteCommand("procon.protected.tasks.add", this.GetType().Name + "_OnTaskMyrconProconTagOwnersRequest", "0", "1800", "-1", "procon.protected.plugins.call", this.GetType().Name, "OnTaskMyrconProconTagOwnersRequest");
            this.ExecuteCommand("procon.protected.tasks.add", this.GetType().Name + "_OnTaskPostStatisticsToVips", "0", "60", "-1", "procon.protected.plugins.call", this.GetType().Name, "OnTaskPostStatisticsToVips");
        }

        public void OnPluginDisable()
        {
            _isEnabled = false;
            ConsoleWrite("Disabled!");

            this.ExecuteCommand("procon.protected.tasks.remove", this.GetType().Name + "_OnTaskMyrconProconTagHuntersRequest");
            this.ExecuteCommand("procon.protected.tasks.remove", this.GetType().Name + "_OnTaskMyrconProconTagOwnersRequest");
            this.ExecuteCommand("procon.protected.tasks.remove", this.GetType().Name + "_OnTaskPostStatisticsToVips");
        }

        public void OnTaskPostStatisticsToVips(params string[] parameters)
        {
            // I don't think tasks are fired when a plugin is disabled, but this is here in case they enable the plugin
            // then disable it and the tasks are still fired.
            if (this._isEnabled == true)
            {
                List<HunterActionStatistic> statistics = new List<HunterActionStatistic>(this.HunterActionStatistics.Values);

                Comparison<HunterActionStatistic> compare = delegate (HunterActionStatistic a, HunterActionStatistic b)
                {
                    // Descending score.
                    return a.LastModified.CompareTo(b.LastModified) * -1;
                };

                statistics.Sort(compare);

                // If we have more than one statistic and the most recent statistic was modified within 60 seconds.
                if (statistics.Count > 0 && statistics[0].LastModified >= DateTime.Now.AddSeconds(-60))
                {
                    List<String> recentThreeStatisticsChanges = new List<String>();

                    for (int offset = 0; offset < statistics.Count && offset < 3; offset++)
                    {
                        recentThreeStatisticsChanges.Add(statistics[offset].ToString());
                    }

                    String message = String.Join(", ", recentThreeStatisticsChanges.ToArray());

                    this.DebugWrite(String.Format("OnTaskPostStatisticsToVips: Posting statistics to VIP's: {0}", message), 2);

                    this.AdminSayToAllVips(message);
                }
            }
        }

        public void OnTaskMyrconProconTagOwnersRequest(params string[] parameters)
        {
            // I don't think tasks are fired when a plugin is disabled, but this is here in case they enable the plugin
            // then disable it and the tasks are still fired.
            if (this._isEnabled == true)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        this.DebugWrite("OnTaskMyrconProconTagOwnersRequest: Checking for tag owner list updates..", 2);

                        XmlDocument document = new XmlDocument();
                        document.Load("https://myrcon.com/procon/streams/tag_owners/format/xml");

                        this.DebugWrite("OnTaskMyrconProconTagOwnersRequest: Fetch completed.", 2);

                        foreach (XmlElement tagOwner in document.GetElementsByTagName("tag_owner"))
                        {
                            XmlNodeList tagOwnerNames = tagOwner.GetElementsByTagName("name");

                            if (tagOwnerNames.Count > 0)
                            {
                                XmlNode tagOwnerName = tagOwnerNames.Item(0);

                                if (tagOwnerName != null && tagOwnerName.InnerText.Length > 0 && this.VipNames.Contains(tagOwnerName.InnerText) == false)
                                {
                                    this.VipNames.Add(tagOwnerName.InnerText);
                                }
                            }
                        }

                        this.DebugWrite(String.Format("OnTaskMyrconProconTagOwnersRequest: List updated. Now protecting {0} VIP names", this.VipNames.Count), 2);
                    }
                    catch (Exception e)
                    {
                        this.ConsoleException("OnTaskMyrconProconTagOwnersRequest: " + e.Message);
                    }
                });
            }
        }

        public void OnTaskMyrconProconTagHuntersRequest(params string[] parameters)
        {
            // I don't think tasks are fired when a plugin is disabled, but this is here in case they enable the plugin
            // then disable it and the tasks are still fired.
            if (this._isEnabled == true)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        this.DebugWrite("OnTaskMyrconProconTagHuntersRequest: Checking for tag hunter list updates..", 2);

                        XmlDocument document = new XmlDocument();
                        document.Load("https://myrcon.com/procon/streams/tag_hunters/format/xml");

                        this.DebugWrite("OnTaskMyrconProconTagHuntersRequest: Fetch completed.", 2);

                        foreach (XmlElement tagHunter in document.GetElementsByTagName("tag_hunter"))
                        {
                            XmlNodeList tagHunterNames = tagHunter.GetElementsByTagName("name");
                            XmlNodeList tagHunterGuids = tagHunter.GetElementsByTagName("ea_guid");

                            if (tagHunterNames.Count > 0)
                            {
                                XmlNode tagHunterName = tagHunterNames.Item(0);

                                if (tagHunterName != null && tagHunterName.InnerText.Length > 0 && this.HunterNames.Contains(tagHunterName.InnerText) == false)
                                {
                                    this.HunterNames.Add(tagHunterName.InnerText);
                                }
                            }

                            if (tagHunterGuids.Count > 0)
                            {
                                XmlNode tagHunterGuid = tagHunterGuids.Item(0);

                                if (tagHunterGuid != null && tagHunterGuid.InnerText.Length > 0 && this.HunterGuids.Contains(tagHunterGuid.InnerText) == false)
                                {
                                    this.HunterGuids.Add(tagHunterGuid.InnerText);
                                }
                            }
                        }

                        this.DebugWrite(String.Format("OnTaskMyrconProconTagHuntersRequest: List updated. Now annoying {0} names and {1} GUID's", this.HunterNames.Count, this.HunterGuids.Count), 2);

                        foreach (KeyValuePair<String, CPlayerInfo> player in this.FrostbitePlayerInfoList)
                        {
                            // Poke any tag hunters right now.
                            this.CheckAndAnnoyPlayer(player.Key);

                            // Make sure they are not in a squad.
                            this.CheckAndMovePlayerToNeutralSquad(player.Key, player.Value.TeamID);
                        }
                    }
                    catch (Exception e)
                    {
                        this.ConsoleException("OnTaskMyrconProconTagHuntersRequest: " + e.Message);
                    }
                });
            }
        }

        public String FormatMessage(String msg, MessageType type)
        {
            String prefix = "[^b" + GetPluginName() + "^n] ";

            if (type.Equals(MessageType.Warning))
                prefix += "^1^bWARNING^0^n: ";
            else if (type.Equals(MessageType.Error))
                prefix += "^1^bERROR^0^n: ";
            else if (type.Equals(MessageType.Exception))
                prefix += "^1^bEXCEPTION^0^n: ";

            return prefix + msg;
        }


        public void LogWrite(String msg)
        {
            ExecuteCommand("procon.protected.pluginconsole.write", msg);
        }

        public void ConsoleWrite(String msg, MessageType type)
        {
            LogWrite(FormatMessage(msg, type));
        }

        public void ConsoleWrite(String msg)
        {
            ConsoleWrite(msg, MessageType.Normal);
        }

        public void ConsoleWarn(String msg)
        {
            ConsoleWrite(msg, MessageType.Warning);
        }

        public void ConsoleError(String msg)
        {
            ConsoleWrite(msg, MessageType.Error);
        }

        public void ConsoleException(String msg)
        {
            ConsoleWrite(msg, MessageType.Exception);
        }

        public void DebugWrite(String msg, int level)
        {
            if (_debugLevel >= level)
                ConsoleWrite(msg, MessageType.Normal);
        }

        public void ServerCommand(params String[] args)
        {
            List<String> list = new List<String>();

            list.Add("procon.protected.send");
            list.AddRange(args);
            ExecuteCommand(list.ToArray());
        }

        /// <summary>
        /// Changes the current protection enabled flag. If the flag is altered and it is set to true then
        /// we loop over all of the current players to check if they should be annoyed.
        /// </summary>
        /// <param name="enabled"></param>
        protected void SetProtectionEnabled(bool enabled)
        {
            // If the flag is changed
            if (this.ProtectionEnabled != enabled)
            {
                // If we are enabling protection OR we are disabling and the protection enabled cooldown has expired.
                if (enabled == true || DateTime.Now >= this.ProtectionEnabledCooldown)
                {
                    this.ProtectionEnabled = enabled;

                    // Loop over all players and issue a faux spawn.
                    if (this.ProtectionEnabled == true)
                    {
                        this.DebugWrite(String.Format("SetProtectionEnabled: Enabling protection, we have a VIP in the server and they have a positive score."), 2);

                        foreach (KeyValuePair<String, CPlayerInfo> player in this.FrostbitePlayerInfoList)
                        {
                            // Poke any tag hunters right now.
                            this.CheckAndAnnoyPlayer(player.Key);

                            // Alert any VIP's that protection has been enabled.
                            this.CheckAndInformVipByName(player.Key, 0);

                            // Make sure they are not in a squad.
                            this.CheckAndMovePlayerToNeutralSquad(player.Key, player.Value.TeamID);
                        }
                    }
                    else
                    {
                        this.DebugWrite(String.Format("SetProtectionEnabled: Disabling protection, we don't have any VIP's or the VIP's do not have a positive score."), 2);
                    }
                }
            }
        }

        /// <summary>
        /// Messages each vip in the server with a specific message
        /// </summary>
        /// <param name="message"></param>
        protected void AdminSayToAllVips(String message)
        {
            foreach (KeyValuePair<String, CPlayerInfo> player in this.FrostbitePlayerInfoList)
            {
                if (this.ProtectionEnabled == true && this.VipNames.Contains(player.Key) == true)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "[Phogue's VIP-H-A] " + message, "player", player.Key);
                }
            }
        }

        /// <summary>
        /// Tells a VIP that protection has been enabled for them.
        /// </summary>
        /// <param name="name">The name of the Vip. This is checked first before sending them a message.</param>
        /// <param name="delay">The delay in seconds before alerting the user to the enabled vip status.</param>
        protected void CheckAndInformVipByName(String name, int delay)
        {
            if (this.ProtectionEnabled == true && this.VipNames.Contains(name) == true)
            {
                this.ExecuteCommand("procon.protected.tasks.add", "VipHunterAnnoyer_CheckAndInformVipByName", delay.ToString(CultureInfo.InvariantCulture), "1", "1", "procon.protected.send", "admin.say", "[Phogue's VIP-H-A " + this.GetPluginVersion() + "] " + this.VipProtectionEnabledMessage, "player", name);
            }
        }

        /// <summary>
        /// Moves a player out of a squad if they are a hunter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="teamId"></param>
        protected void CheckAndMovePlayerToNeutralSquad(String name, int teamId)
        {
            if (this.ProtectionEnabled == true && this.PreventHunterSquads == true && this.FrostbitePlayerInfoList.ContainsKey(name) == true)
            {
                this.CheckAndMovePlayerToNeutralSquad(this.FrostbitePlayerInfoList[name], teamId);
            }
        }

        protected void CheckAndMovePlayerToNeutralSquad(CPlayerInfo player, int teamId)
        {
            if (this.ProtectionEnabled == true && this.PreventHunterSquads == true)
            {
                if (this.HunterGuids.Contains(player.GUID) == true || this.HunterNames.Contains(player.SoldierName) == true)
                {

                    teamId = teamId == 0 ? player.TeamID : teamId;

                    // The player isn't allowed to be in a squad, move them out.
                    if (player.SquadID > 0)
                    {
                        this.DebugWrite(String.Format("CheckAndMovePlayerToNeutralSquad: Moving player {0} out of their squad, back to lone wolf", player.SoldierName), 2);

                        this.ExecuteCommand("procon.protected.send", "admin.movePlayer", player.SoldierName, player.TeamID.ToString(CultureInfo.InvariantCulture), "0", "false");
                        this.ExecuteCommand("procon.protected.send", "admin.movePlayer", player.SoldierName, player.TeamID.ToString(CultureInfo.InvariantCulture), "0", "true");

                        if (this.OutputPokeMessage == true)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", "Nope - Love Phogue :)", "player", player.SoldierName);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Checks if a player is a hunter, if so it will think of something to do to them right now.
        /// </summary>
        /// <param name="name">The name of the player to check</param>
        protected void CheckAndAnnoyPlayer(String name)
        {
            if (this.ProtectionEnabled == true && this.FrostbitePlayerInfoList.ContainsKey(name) == true)
            {
                this.CheckAndAnnoyPlayer(this.FrostbitePlayerInfoList[name]);
            }
        }

        /// <summary>
        /// Checks that the player is a tag hunter, if so it will pick a random task against them. Also validates they are
        /// not in a squad.
        /// </summary>
        /// <param name="player"></param>
        protected void CheckAndAnnoyPlayer(CPlayerInfo player)
        {
            if (this.ProtectionEnabled == true)
            {
                if (this.HunterGuids.Contains(player.GUID) == true || this.HunterNames.Contains(player.SoldierName) == true)
                {
                    // Yeah, here we go.

                    // Delay before the command is executed.
                    int delay = this.Random.Next(2, player.SquadID > 0 ? 5 : 10);

                    String reason = "[Phogue's VIP Hunter Annoyer] " + this.PokeMessage;

                    // Add in some empty statistics for this player if we have not seen them yet.
                    if (this.HunterActionStatistics.ContainsKey(player.SoldierName) == false)
                    {
                        this.HunterActionStatistics.Add(player.SoldierName, new HunterActionStatistic());
                        this.HunterActionStatistics[player.SoldierName].Name = player.SoldierName;
                    }

                    if (this.Random.Next(0, 100) <= this.KickProbability)
                    {
                        // Kick - 20% chance
                        this.DebugWrite(String.Format("CheckAndAnnoyPlayer: Kick selected for player {0} with a delay of {1} second(s)", player.SoldierName, delay), 2);

                        // Kick
                        if (this.OutputPokeMessage == true)
                        {
                            this.ExecuteCommand("procon.protected.tasks.add", this.GetType().Name + "_spawn_action_timeout", delay.ToString(CultureInfo.InvariantCulture), "1", "1", "procon.protected.send", "admin.kickPlayer", player.SoldierName, reason);
                        }
                        else
                        {
                            this.ExecuteCommand("procon.protected.tasks.add", this.GetType().Name + "_spawn_action_timeout", delay.ToString(CultureInfo.InvariantCulture), "1", "1", "procon.protected.send", "admin.kickPlayer", player.SoldierName);
                        }


                        this.HunterActionStatistics[player.SoldierName].AddKick();
                    }
                    else
                    {
                        // Kill - 80% chance
                        this.DebugWrite(String.Format("CheckAndAnnoyPlayer: Kill selected for player {0} with a delay of {1} second(s)", player.SoldierName, delay), 2);

                        if (this.OutputPokeMessage == true)
                        {
                            this.ExecuteCommand("procon.protected.tasks.add", this.GetType().Name + "_spawn_action_timeout", delay.ToString(CultureInfo.InvariantCulture), "1", "1", "procon.protected.send", "admin.yell", reason, "10", "player", player.SoldierName);
                        }

                        this.ExecuteCommand("procon.protected.tasks.add", this.GetType().Name + "_spawn_action_timeout", delay.ToString(CultureInfo.InvariantCulture), "1", "1", "procon.protected.send", "admin.killPlayer", player.SoldierName);

                        this.CheckAndMovePlayerToNeutralSquad(player, player.TeamID);

                        this.HunterActionStatistics[player.SoldierName].AddKill();
                    }
                }
            }
        }

        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {
            base.OnListPlayers(players, subset);

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    if (subset.Subset == CPlayerSubset.PlayerSubsetType.All)
                    {
                        bool protectionEnabled = false;

                        foreach (KeyValuePair<String, CPlayerInfo> player in this.FrostbitePlayerInfoList)
                        {
                            if (this.VipNames.Contains(player.Key) == true)
                            {
                                if (player.Value.Score > 0)
                                {
                                    protectionEnabled = true;
                                }
                            }
                        }

                        this.SetProtectionEnabled(protectionEnabled);
                    }
                }
                catch (Exception e)
                {
                    this.ConsoleException("OnListPlayers: " + e.Message);
                }
            });
        }

        public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            this.DebugWrite(String.Format("OnPlayerSpawned: {0}", soldierName), 5);

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    this.CheckAndAnnoyPlayer(soldierName);
                }
                catch (Exception e)
                {
                    this.ConsoleException("OnPlayerSpawned: " + e.Message);
                }

            });
        }

        public override void OnPlayerJoin(string soldierName)
        {
            base.OnPlayerJoin(soldierName);

            // Tell them that VIP protection is enabled (if it is already) and that they are protected
            // (provided they are a VIP of course)
            this.CheckAndInformVipByName(soldierName, 180);

        }

        public override void OnRoundOver(int winningTeamId)
        {
            base.OnRoundOver(winningTeamId);

            // No disabling protection status for two minutes.
            this.ProtectionEnabledCooldown = DateTime.Now.AddSeconds(180);

            this.DebugWrite(String.Format("OnRoundOver: Set protection enabled cooldown to {0}", this.ProtectionEnabledCooldown), 2);
        }

        public override void OnRestartLevel()
        {
            base.OnRestartLevel();

            // No disabling protection status for two minutes.
            this.ProtectionEnabledCooldown = DateTime.Now.AddSeconds(180);

            this.DebugWrite(String.Format("OnRoundOver: Set protection enabled cooldown to {0}", this.ProtectionEnabledCooldown), 2);
        }

        public override void OnRunNextLevel()
        {
            base.OnRunNextLevel();

            // No disabling protection status for two minutes.
            this.ProtectionEnabledCooldown = DateTime.Now.AddSeconds(180);

            this.DebugWrite(String.Format("OnRoundOver: Set protection enabled cooldown to {0}", this.ProtectionEnabledCooldown), 2);
        }

        public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId)
        {
            base.OnPlayerTeamChange(soldierName, teamId, squadId);

            if (this.FrostbitePlayerInfoList.ContainsKey(soldierName) == true)
            {
                this.FrostbitePlayerInfoList[soldierName].TeamID = teamId;
                this.FrostbitePlayerInfoList[soldierName].SquadID = squadId;
            }
        }

        public override void OnPlayerSquadChange(string soldierName, int teamId, int squadId)
        {
            base.OnPlayerSquadChange(soldierName, teamId, squadId);

            if (this.FrostbitePlayerInfoList.ContainsKey(soldierName) == true)
            {
                this.FrostbitePlayerInfoList[soldierName].TeamID = teamId;
                this.FrostbitePlayerInfoList[soldierName].SquadID = squadId;
            }

            // if we're in a cooldown, chances are good we are moving maps. Ignore it so we don't spam a lot. 
            // We'll catch them when they first spawn.
            this.DebugWrite(String.Format("OnPlayerSquadChange: {0}", soldierName), 4);

            if (DateTime.Now >= this.ProtectionEnabledCooldown)
            {
                this.DebugWrite(String.Format("OnPlayerSquadChange: No cooldown in place, continue."), 4);

                if (squadId > 0)
                {
                    this.CheckAndMovePlayerToNeutralSquad(soldierName, teamId);
                }
            }
        }
    }

    public class HunterActionStatistic
    {

        /// <summary>
        /// The name of the player being targeted
        /// </summary>
        public String Name;

        /// <summary>
        /// The total number of times this player has been killed by the plugin
        /// </summary>
        public int KillsCount;

        /// <summary>
        /// The total number of times this player has been kicked by the plugin
        /// </summary>
        public int KicksCount;

        /// <summary>
        /// The last time the counters were incremented
        /// </summary>
        public DateTime LastModified;

        public HunterActionStatistic()
        {
            this.Name = String.Empty;
            this.KillsCount = 0;
            this.KicksCount = 0;

            this.LastModified = DateTime.Now;
        }

        /// <summary>
        /// Increments the kill counter and pokes this statistic
        /// </summary>
        public void AddKill()
        {
            this.KillsCount++;
            this.Poke();
        }

        /// <summary>
        /// Increments the kick counter and pokes this statistic
        /// </summary>
        public void AddKick()
        {
            this.KicksCount++;
            this.Poke();
        }

        /// <summary>
        /// Sets the last modified attribute to the time right now.
        /// </summary>
        public void Poke()
        {
            this.LastModified = DateTime.Now;
        }

        public override string ToString()
        {
            List<String> statistics = new List<String>();

            if (this.KillsCount > 1)
            {
                statistics.Add(String.Format("{0} kills", this.KillsCount));
            }
            else if (this.KillsCount == 1)
            {
                statistics.Add(String.Format("{0} kill", this.KillsCount));
            }

            if (this.KicksCount > 0)
            {
                statistics.Add(String.Format("{0} kicks", this.KicksCount));
            }
            else if (this.KicksCount == 1)
            {
                statistics.Add(String.Format("{0} kick", this.KicksCount));
            }

            return String.Format("{0} [{1}]", this.Name, String.Join(", ", statistics.ToArray()));
        }
    }
}