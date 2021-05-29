using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents
{

    public class Seeder
    {
        public string soldierName;
        public int teamid;
        private bool active_flag = false;
        private bool onlineStatus = false;
        private double current_score = 0;
        private double previous_score = 0;
        private DateTime time_of_most_recent_score_increase = DateTime.Now.AddHours(-1.00);
        private DateTime time_of_last_activity = DateTime.Now.AddHours(-1.0);

        public void update_activity_time()
        {
            // seeder did something that qualifies as being active, update the time_of_last_activity
            time_of_last_activity = DateTime.Now;
        }
        public double getMinutesSinceLastActivity()
        {
            TimeSpan dt = DateTime.Now - time_of_last_activity;
            return dt.TotalMinutes;
        }
        public void updateScore(double newscore, enumBoolYesNo use4idle)
        {
            if (newscore > current_score)
            {
                time_of_most_recent_score_increase = DateTime.Now;
                if (use4idle == enumBoolYesNo.Yes)
                {
                    update_activity_time();
                }
            }
            previous_score = current_score;
            current_score = newscore;
        }
        public DateTime getTimeOfLastScoreIncrease() { return time_of_most_recent_score_increase; }
        public double getMinutesSinceLastScoreIncrease()
        {
            TimeSpan dt = DateTime.Now - time_of_most_recent_score_increase;
            return dt.TotalMinutes;
        }
        public void setActive() { active_flag = true; }
        public void setInactive() { active_flag = false; }
        public bool checkIsActive() { return active_flag; }
        public bool checkIsOnline() { return onlineStatus; }
        public void setOnline() { onlineStatus = true; }
        public void setOffline() { onlineStatus = false; }


    }
    public class Team
    {
        public int TeamID;
        public double Score_Starting;
        public double Score_Winning;
        public double Score_Current;
        public double getPercentRoundComplete()
        {
            double pct_done = 0.0;
            if ((Score_Winning - Score_Starting) == 0)
            {
                pct_done = 0.0;
            }
            else
            {
                pct_done = (Score_Current - Score_Starting) / (Score_Winning - Score_Starting);
            }
            pct_done = pct_done * 100.0;
            return pct_done;
        }
    }




    public class BalanceServerSeeders : PRoConPluginAPI, IPRoConPluginInterface
    {
        enum gameType { Rush, CTF, Conquest, SquadDeathMatch, SquadRush, TeamDeathMatch, GunMaster, Scavenger, TankSuperiority, AirSuperiority, Other };
        private double percent_of_round_complete = 0.0;
        private List<Team> teamList;
        private List<string> seederNames;
        private List<Seeder> SeederList;
        private double kickLimit_pctRoundLeft = 10.0;
        private double kickLimit_waitTime_at_roundstart = 2.0;
        private double kickLimit_CTF_timelimit = 15.0;
        private string curr_gameMode;
        private string rushLogic = "Attackers";
        private string nonRushLogic = "Player_Based";
        private DateTime roundStartTime = DateTime.Now;
        private TimeSpan roundElapsedTime;
        private gameType gameMode = gameType.Other;
        private enumBoolYesNo simulateOnly = enumBoolYesNo.No;
        private enumBoolYesNo autoAddDeleteSeeders = enumBoolYesNo.No;
        private enumBoolYesNo idleCheck_chat = enumBoolYesNo.Yes;
        private enumBoolYesNo idleCheck_score = enumBoolYesNo.Yes;
        private enumBoolYesNo idleCheck_spawn = enumBoolYesNo.Yes;
        private enumBoolYesNo kick_idleSeeders_flag = enumBoolYesNo.No;
        private double kick_idleTimeMin = 10.0;
        private int kick_minPlayerCount = 30;
        private string kick_reason = "You were kicked because you were idle and the server is populated.";
        private bool pluginenabled = false;
        private double time_since_activity = 5;
        private int debugPrintLevel = 1;
        private int balanceMethod = 1;
        private int team1Score = 0;
        private int team2Score = 0;
        private int team1count = 0;
        private int team2count = 0;
        private int team1RealPlayerCount = 0;
        private int team2RealPlayerCount = 0;
        private int team1InactiveSeederCount = 0;
        private int team2InactiveSeederCount = 0;
        private int team1_minus_team2_inactiveSeederCount = 0;
        private int team1_minus_team2_realPlayerCount = 0;
        private int team1_minus_team2_score = 0;
        public BalanceServerSeeders()
        {
            this.seederNames = new List<string>();
            this.seederNames.Add("SeederSoldier1");
            this.seederNames.Add("SeederSoldier2");
            this.SeederList = new List<Seeder>();
            writeMsgToPluginConsole(3, "Plugin constructor calling new List<Team>");
            this.teamList = new List<Team>();
            writeMsgToPluginConsole(3, string.Format("Plugin constructor Team List has {0} entries", teamList.Count));
        }
        public string GetPluginName()
        {
            return "BalanceServerSeeders";
        }
        public string GetPluginVersion()
        {
            return "4.0.0.4";
        }
        public string GetPluginAuthor()
        {
            return "Flyswamper";
        }
        public string GetPluginWebsite()
        {
            return "www.phogue.net/forumvb/showthread.php?3992-BF3-BalanceServerSeeders(1-0-1-5-21-Apr-2012)-Distribute-server-seeding-players>BalanceServerSeeders";
        }
        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>
<p>Balances pre-designated server-seeding accounts between Team 1 and Team2</p>


<h2>Settings</h2>
<blockquote><h4>Rush Mode Logic&lt;Attackers/Defenders&gt;</h4>Determines whether the extra seeder, when there is an odd number of them, gets put ont he attacker or defender team for Rush mode.</blockquote>

<blockquote><h4>Non-Rush Mode Logic&lt;Player_Based / Ticket_Based&gt;</h4>Determines whether the extra seeder, when there is an odd number of them, gets placed on the team with more players or the team with more tickets for non Rush modes.</blockquote>

<blockquote><h4>Debug_Level  &lt;value (0-10)&gt;</h4>This will increase/decrease the amount of information that gets written to the plugin console window.  A value of 0 (zero) should eliminate all output from the plugin.  A value of 10 provides maximum output.</blockquote>


<blockquote><h4>Seeder Idle/Active detection for seeder balancing</h4>Specify the number of minutes since last detected seeder activity for plugin to assume seeder is idle (and therefore included in seeder balancing)
Use the yes/no options to specify events which should be monitored to look for seeder activity.
</blockquote>

<blockquote><h4>Controls for kicking idle seeders when server is populated</h4>Specify whether you want this plugin to kick seeders when the server gets populated (yes/no).  If kicking is enabled (yes), you also specify the player count the server is considered populated enough to start kicking idle seeders.  Also can specify a number of minutes that the seeders need to be idle before being eligible for kicking (this can be a different/longer time than the time whether to include them in balancing). </blockquote>
<blockquote><h4>Seeders  &lt;soldiernames one per line&gt;</h4>Enter the soldiernames (without clan tags)for each of the accounts you wish to designate as a seeder that this plugin should balance/manage.</blockquote>



<h2>Development</h2>
<h3>Changelog</h3>
<h4>Version 4.0.0.4  21-April-2014 (EBassie)</h4>
<ul>
<li>Fixed Seeder balancing</li>
</ul>
<h4>Version 4.0.0.3  18-Oct-2013</h4>
<ul>
<li>Fixed bug that was preventing balancing for several game modes</li>
</ul>
<h4>Version 4.0.0.2  13-Oct-2013</h4>
<ul>
<li>Fixed bug that was deleting unlisted seeders if they were inactive, now deletes them when they go offline instead (as it should)</li>
</ul>


<h4>Version 4.0.0.1  11-Oct-2013</h4>
<ul>
<li>Minor bug fixes and tweaks</li>
</ul>

<h4>Version 4.0.0.0  10-Oct-2013</h4>
<ul>
<li>Adding option to automatically add players to the seeders list as they join and remove them as they leave the server</li>
<li>Added simulate only option to turn off actual balancing and kicking, so you can watch plugin console messages for debugging only</li>
</ul>

<h4>Version 3.6.0.0  19-Sept-2013</h4>
<ul>
<li>Adding ability to disable kicking near end of rounds</li>
</ul>

<h4>Version 3.5.0.0  3-May-2013</h4>
<ul>
<li>Add logic to detect CTF mode and force player-count based logic when CTF detected since tickets are not reported to Procon in CTF</li>
</ul>


<h4>Version 3.4.0.0  31-Mar-2013</h4>
<ul>
<li>Change the event that triggers a check for kicking idle seeders to avoid problem with CTF mode</li>
</ul>

<h4>Version 3.3.0.0  4-Jan-2013</h4>
<ul>
<li>Add ability for admin to select extra seeder going to attackers or defenders for Rush Mode</li>
<li>Changed user input to be more intuitive (Player_Based or Ticket_Based, instead of method 1 or 2)</li>
</ul>


<h4>Version 3.2.0.0  28-Dec-2012</h4>
<ul>
<li>Added code that should force any inactive seeders to always be forced into squad 0 (no squad)</li>
</ul>

<h4>Version 3.1.0.0  7-Sep-2012</h4>
<ul>
<li>Add custom reason for kick message</li>
<li>Add 4 digits back to the version number so the plugin shows up in Phogue's usage reports</li>
<li>Fix some typo's in the help screen</li>
</ul>

<h4>Version 3.0  5-Sep-2012</h4>
<ul>
<li>Added functionality that will kick seeders when server gets populated</li>
</ul>

<h4>Version 2.0.3.1  2-Sep-2012</h4>
<ul>
<li>Fix a bug that prevented saving of plugin settings</li>
<li>Adjust some of the debug output.</li>
</ul>
<h4>Version 2.0.3.0   29-Aug-2012</h4>
<ul>
<li>Improved idle/active detection for seeders.  Activity now detected (optionally) by score increase, spawn events, or chat events</li>
<li>Begin using yes/no for admin-user configuration settings where appropriate</li>
</ul>

<h4>Version 2.0.2.3   16-Aug-2012</h4>
<ul>
<li>Bug fixing and lots of cleanup of the diagnostic output.</li>
<li>Added additional debug levels </li>
</ul>

<h4>Version 2.0.2.2   1--Aug-2012</h4>
<ul>
<li>Add and cleanup some diagnostic output messages, trying to track a bug with others testing for me.</li>
</ul>

<h4>Version 2.0.2.1   8-Aug-2012</h4>
<ul>
<li>Revise code to eliminate loops.  Only move one seeder per check.  Plugin will take longer to acheive balance but less risk of endless loop coding errors</li>
</ul>

<h4>Version 2.0.2.0   5-Aug-2012</h4>
<ul>
<li>This is a significant rewrite of code and logic and (unfortunately) I can't test it myself.  Likely to have bugs is high!</li>
<li>Added ability for user to select balancing based on two methods.  Player or Score count</li>
</ul>

<h4>Version 2.0.1.1   15-July-2012</h4>
<ul>
<li>Minor bugfix/change.  Adjusted loop/logic that executes the player move to another team.</li>
<li>Added more diagnostic output (use level 5 to see it all)</li>
</ul>

<h4>Version 2.0.1.0   14-July-2012</h4>
<ul>
<li>Revised code so that it would detect whether rush mode is active.</li>
<li>If Rush mode is active and an odd number of inactive seeders are online, the extra seeder should now be forced to the defenders team</li>
<li>If mode is NOT Rush and an odd number of inactive seeders are online, the extra seeder should now be forced to team 1. This *should* help keep non-seeder player counts more balanced when the server population is low.</li>
</ul>

<h4>Version 2.0.0.4   28-April-2012</h4>
<ul>
<li>Bugfix - plugin was remembering seeders MAX score instead of last score.</li>
</ul>

<h4>Version 2.0.0.3   28-April-2012</h4>
<ul>
<li>Simplify code and make it easier to follow (but still same basic logic)</li>
<li>Improve the formatting of the debug output to the plugin console window</li>
</ul>

<h4>Version 2.0.0.2   21-April-2012</h4>
<ul>
<li>Bug fix related to player not detected as going offline</li>
</ul>


<h4>Version 2.0.0.1   21-April-2012</h4>
<ul>
<li>Completely re-coded the logic to properly handle active/idle detection better</li>
<li>Improved the details info sheet within the plugin (what you are reading now)</li>
</ul>

<h4>Versions 1.0.1.3,  1.0.1.4,  and 1.0.1.5   xx-March-2012</h4>
<ul>
<li>These were failed attempts at quick-fixing the code to use a time-since last score to detect if a seeder was active or not.</li>
</ul>


<h4>Version 1.0.1.2   11-March-2012</h4>
<ul>
<li>Renamed plugin to BalanceServerSeeders because it does a much better job of describing what the plugin does!</li>
<li>Fix a minor typo</li>
</ul>

<h4>Version 1.0.1.1    7-March-2012</h4>
<ul>
<li>Fix a couple of issues where tet written to plugin console log was incorrect.  Did not affection functionality</li>
<li>Fix a minor typo</li>
</ul>

<h4>Version 1.0.0.6    6-March-2012</h4>
<ul>
<li>Make sure full player list is received before taking action</li>
<li>Implemented code suggestions from stealth (thanks!)</li>
<li>Added debug-output levels</li>
<li>Added logic to consider seeder as active if he has non-zero score</li>
<li>Changed plugin name from CKeepCampersApart to CKeepSeedersApart</li>
</ul>
";

        }
        public void OnPluginEnable()
        {
            this.pluginenabled = true;
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBalanceServerSeeders ^2Enabled!");
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBalanceServerSeeders ^2 calling new list<team>");
            writeMsgToPluginConsole(3, "OnPlugin Enable section calling new List<Team>");
            this.teamList = new List<Team>();
            writeMsgToPluginConsole(3, string.Format("OnPluginEnable Team List has {0} entries", teamList.Count));
        }
        public void OnPluginDisable()
        {
            this.pluginenabled = false;
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBalanceServerSeeders ^1Disabled =(");
        }
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBalanceServerSeeders ^2Loaded!");
            this.RegisterEvents(this.GetType().Name, "OnPluginLoaded",
                                                    "OnPlayerJoin",
                                                    "OnListPlayers",
                                                    "OnPlayerLeft",
                                                    "OnServerInfo",
                                                    "OnPlayerSpawned",
                                                    "OnGlobalChat",
                                                    "OnTeamChat",
                                                    "OnSquadChat",
                                                    "OnLevelLoaded"
                                                    );
            gameMode = gameType.Other;  // default to other       
        }
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            /* autoAddDeleteSeeders */
            lstReturn.Add(new CPluginVariable("1. Plugin Controls|Simulate moves and kicks only(debug mode=yes, active mode=no", typeof(enumBoolYesNo), this.simulateOnly));
            lstReturn.Add(new CPluginVariable("1. Plugin Controls|Treat all players as seeders", typeof(enumBoolYesNo), this.autoAddDeleteSeeders));
            lstReturn.Add(new CPluginVariable("1. Plugin Controls|Debug Level(0-10 higher is more info)", debugPrintLevel.GetType(), this.debugPrintLevel));
            lstReturn.Add(new CPluginVariable("2. Seeders|Seeders(one per line)", typeof(string[]), this.seederNames.ToArray()));
            lstReturn.Add(new CPluginVariable("3. Balancing Logic|Rush mode logic - place extra seeder on", "enum.proconBalanceSeedersRushMethod(Attackers|Defenders)", this.rushLogic));
            lstReturn.Add(new CPluginVariable("3. Balancing Logic|Non-Rush mode logic", "enum.proconBalanceSeedersNonRushMethod(Player_Based|Ticket_Based)", nonRushLogic));
            lstReturn.Add(new CPluginVariable("4. Idle detection logic|Number of minutes since last activity for seeder to be considered idle", this.time_since_activity.GetType(), this.time_since_activity));
            lstReturn.Add(new CPluginVariable("4. Idle detection logic|Use increase in score to detect seeder activity", typeof(enumBoolYesNo), this.idleCheck_score));
            lstReturn.Add(new CPluginVariable("4. Idle detection logic|Use spawn events to detect seeder activity", typeof(enumBoolYesNo), this.idleCheck_spawn));
            lstReturn.Add(new CPluginVariable("4. Idle detection logic|Use chat events to detect seeder activity", typeof(enumBoolYesNo), this.idleCheck_chat));
            lstReturn.Add(new CPluginVariable("5. Kick logic/controls|Kick idle seeders when server is populated?", typeof(enumBoolYesNo), this.kick_idleSeeders_flag));
            lstReturn.Add(new CPluginVariable("5. Kick logic/controls|Minutes that seeder must be idle to be kicked?", this.kick_idleTimeMin.GetType(), this.kick_idleTimeMin));
            lstReturn.Add(new CPluginVariable("5. Kick logic/controls|Server player count to begin kicking idle seeders?", this.kick_minPlayerCount.GetType(), this.kick_minPlayerCount));
            lstReturn.Add(new CPluginVariable("5. Kick logic/controls|Do not kick seeders if this percent (or lower) of round is left (0-100)", this.kickLimit_pctRoundLeft.GetType(), this.kickLimit_pctRoundLeft));
            lstReturn.Add(new CPluginVariable("5. Kick logic/controls|Do not kick seeders until round is at least how many minutes old?", this.kickLimit_waitTime_at_roundstart.GetType(), this.kickLimit_waitTime_at_roundstart));
            lstReturn.Add(new CPluginVariable("5. Kick logic/controls|For CTF mode only, do not kick seeder after round timer reaches how many minutes?", this.kickLimit_CTF_timelimit.GetType(), this.kickLimit_CTF_timelimit));
            lstReturn.Add(new CPluginVariable("5. Kick logic/controls|Message displayed as reason for the kick?", this.kick_reason.GetType(), this.kick_reason));

            return lstReturn;
        }
        public List<CPluginVariable> GetPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Simulate moves and kicks only(debug mode=yes, active mode=no", typeof(enumBoolYesNo), this.simulateOnly));
            lstReturn.Add(new CPluginVariable("Treat all players as seeders", typeof(enumBoolYesNo), this.autoAddDeleteSeeders));
            lstReturn.Add(new CPluginVariable("Seeders(one per line)", typeof(string[]), this.seederNames.ToArray()));
            lstReturn.Add(new CPluginVariable("Debug Level(0-10 higher is more info)", debugPrintLevel.GetType(), debugPrintLevel));
            lstReturn.Add(new CPluginVariable("Rush mode logic - place extra seeder on", "enum.proconBalanceSeedersRushMethod(Attackers|Defenders)", this.rushLogic));
            lstReturn.Add(new CPluginVariable("Non-Rush mode logic", "enum.proconBalanceSeedersNonRushMethod(Player_Based|Ticket_Based)", nonRushLogic));
            lstReturn.Add(new CPluginVariable("Number of minutes since last activity for seeder to be considered idle", this.time_since_activity.GetType(), this.time_since_activity));
            lstReturn.Add(new CPluginVariable("Use increase in score to detect seeder activity", typeof(enumBoolYesNo), this.idleCheck_score));
            lstReturn.Add(new CPluginVariable("Use spawn events to detect seeder activity", typeof(enumBoolYesNo), this.idleCheck_spawn));
            lstReturn.Add(new CPluginVariable("Use chat events to detect seeder activity", typeof(enumBoolYesNo), this.idleCheck_chat));
            lstReturn.Add(new CPluginVariable("Kick idle seeders when server is populated?", typeof(enumBoolYesNo), this.kick_idleSeeders_flag));
            lstReturn.Add(new CPluginVariable("Minutes that seeder must be idle to be kicked?", this.kick_idleTimeMin.GetType(), this.kick_idleTimeMin));
            lstReturn.Add(new CPluginVariable("Server player count to begin kicking idle seeders?", this.kick_minPlayerCount.GetType(), this.kick_minPlayerCount));
            lstReturn.Add(new CPluginVariable("Do not kick seeders if this percent (or lower) of round is left (0-100)", this.kickLimit_pctRoundLeft.GetType(), this.kickLimit_pctRoundLeft));
            lstReturn.Add(new CPluginVariable("Do not kick seeders until round is at least how many minutes old?", this.kickLimit_waitTime_at_roundstart.GetType(), this.kickLimit_waitTime_at_roundstart));
            lstReturn.Add(new CPluginVariable("For CTF mode only, do not kick seeder after round timer reaches how many minutes?", this.kickLimit_CTF_timelimit.GetType(), this.kickLimit_CTF_timelimit));
            lstReturn.Add(new CPluginVariable("Message displayed as reason for the kick?", this.kick_reason.GetType(), this.kick_reason));

            return lstReturn;
        }
        public void SetPluginVariable(string strVariable, string strValue)
        {
            writeMsgToPluginConsole(1, string.Format("User setting change requested.  strVariable: {0}, strValue: {1}", strVariable, strValue));
            int result = 0;
            double resultTime = 5;
            double result_pct = 10;

            if ((strVariable.CompareTo("Simulate moves and kicks only(debug mode=yes, active mode=no") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.simulateOnly = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }


            if ((strVariable.CompareTo("Treat all players as seeders") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.autoAddDeleteSeeders = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            if ((strVariable.CompareTo("For CTF mode only, do not kick seeder after round timer reaches how many minutes?") == 0) && double.TryParse(strValue, out resultTime) == true)
            {
                writeMsgToPluginConsole(1, string.Format("Setting kickLimit_CTF_timelimit to {0}", resultTime));
                this.kickLimit_CTF_timelimit = resultTime;
            }

            if ((strVariable.CompareTo("Do not kick seeders until round is at least how many minutes old?") == 0) && double.TryParse(strValue, out resultTime) == true)
            {
                writeMsgToPluginConsole(1, string.Format("Setting kickLimit_waitTime_at_roundstart to {0}", resultTime));
                this.kickLimit_waitTime_at_roundstart = resultTime;
            }

            if ((strVariable.CompareTo("Do not kick seeders if this percent (or lower) of round is left (0-100)") == 0) && double.TryParse(strValue, out result_pct) == true)
            {
                writeMsgToPluginConsole(1, string.Format("Setting kickLimit_pctRoundLeft to {0}", result_pct));
                this.kickLimit_pctRoundLeft = result_pct;
            }

            if (strVariable.CompareTo("Rush mode logic - place extra seeder on") == 0)
            {
                this.rushLogic = strValue;
            }
            if (strVariable.CompareTo("Non-Rush mode logic") == 0)
            {
                this.nonRushLogic = strValue;
            }



            if (strVariable.CompareTo("Message displayed as reason for the kick?") == 0)
            {
                this.kick_reason = strValue;
            }
            if (strVariable.CompareTo("Seeders(one per line)") == 0)
            {
                this.seederNames = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                rebuild_SeederList_detailed();

            }
            if ((strVariable.CompareTo("Debug Level(0-10 higher is more info)") == 0) && int.TryParse(strValue, out result) == true)
            {
                if ((result >= 0) && (result <= 10))
                {
                    writeMsgToPluginConsole(1, string.Format("Setting debug_level to {0}", result));
                    debugPrintLevel = result;
                }
                else
                {
                    writeMsgToPluginConsole(1, string.Format("{0} is not a valid debug level.", result));

                }
            }

            if ((strVariable.CompareTo("Number of minutes since last activity for seeder to be considered idle") == 0) && double.TryParse(strValue, out resultTime) == true)
            {
                writeMsgToPluginConsole(1, string.Format("Setting time_since_activity to {0}", resultTime));
                this.time_since_activity = resultTime;
            }
            /* if ((strVariable.CompareTo("Method 1 or 2 (1=player based, 2=score based)") == 0) && int.TryParse(strValue, out result) == true)
             {
                
                 if ((result == 1) || (result == 2))
                 {
                     writeMsgToPluginConsole(1, string.Format("Setting balanceMethod to {0}", result));
                     this.balanceMethod = result;
                 }
                 else
                 {
                     writeMsgToPluginConsole(1, string.Format("{0} is not a valid balance method", result));
                 }
             }*/
            if ((strVariable.CompareTo("Use increase in score to detect seeder activity") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.idleCheck_score = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            if ((strVariable.CompareTo("Use spawn events to detect seeder activity") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.idleCheck_spawn = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            if ((strVariable.CompareTo("Use chat events to detect seeder activity") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.idleCheck_chat = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            if ((strVariable.CompareTo("Kick idle seeders when server is populated?") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.kick_idleSeeders_flag = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            if ((strVariable.CompareTo("Minutes that seeder must be idle to be kicked?") == 0) && double.TryParse(strValue, out resultTime) == true)
            {
                writeMsgToPluginConsole(1, string.Format("Setting time since activity for seeder to initiate kick to {0}", resultTime));
                this.kick_idleTimeMin = resultTime;
            }
            if ((strVariable.CompareTo("Server player count to begin kicking idle seeders?") == 0) && int.TryParse(strValue, out result) == true)
            {

                writeMsgToPluginConsole(1, string.Format("Setting player count for seeder kicking to beign at {0}", result));
                this.kick_minPlayerCount = result;
            }


        }
        private void rebuild_SeederList_detailed()
        {
            // wipes out the seeder list and re-creates it, used when user changes inputs 
            SeederList.Clear();
            for (int i = 0; i < seederNames.Count; i++)
            {
                Seeder newSeeder = new Seeder();
                newSeeder.soldierName = seederNames[i];
                newSeeder.setInactive();
                newSeeder.teamid = 0;
                newSeeder.setOffline();
                newSeeder.updateScore(0, idleCheck_score);
                SeederList.Add(newSeeder);
                writeMsgToPluginConsole(1, string.Format("Building initial seederlist. Adding {0}", seederNames[i]));
            }
        }
        private int countIdleOnlineSeeders(int team_num)
        {
            // return the number of idle and online seeders on the specified team
            int idleonlinecount = 0;
            writeMsgToPluginConsole(6, string.Format("Counting inactive seeders for team {0}", team_num));
            for (int ndxSeeder = 0; ndxSeeder < SeederList.Count; ndxSeeder++)
            {
                bool move_flag = true;     // assume it's a match, check and correct if not

                if (SeederList[ndxSeeder].checkIsActive() == true) { move_flag = false; }            // don't count if not active
                if (SeederList[ndxSeeder].checkIsOnline() == false) { move_flag = false; }   // don't count if not online
                if (SeederList[ndxSeeder].teamid != team_num) { move_flag = false; }       // don't count if not the correct team

                if (move_flag)
                {
                    idleonlinecount++;
                    writeMsgToPluginConsole(2, string.Format("{0} is inactive seeder on team {1}", SeederList[ndxSeeder].soldierName, team_num));
                }

            }
            writeMsgToPluginConsole(1, string.Format("Counted {0} inactive seeders on team {1}", idleonlinecount, team_num));
            return idleonlinecount;
        }

        public virtual void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)  // BF3
        {
            writeMsgToPluginConsole(3, "OnLevelLoaded event detected");
            teamList.Clear();     // empty the team list
            roundStartTime = DateTime.Now;        //update the time for the start of the round
            roundElapsedTime = DateTime.Now - roundStartTime;
            writeMsgToPluginConsole(3, string.Format("New round started at time {0}", roundStartTime));
        }
        public void OnPlayerJoin(string strSoldierName)
        {
            writeMsgToPluginConsole(2, string.Format("OnPlayerJoin event for player {0} ", strSoldierName));
            int SeederAlreadyInList = 0;
            writeMsgToPluginConsole(7, string.Format("{0} joined", strSoldierName.ToString()));
            for (int ndx = 0; ndx < SeederList.Count; ndx++)
            {
                if (string.Compare(SeederList[ndx].soldierName, strSoldierName) == 0)
                {
                    SeederAlreadyInList = 1;
                    SeederList[ndx].setOnline();
                    writeMsgToPluginConsole(1, string.Format("Seeder {0} has joined the server", SeederList[ndx].soldierName));
                    SeederList[ndx].update_activity_time();         // update activity time if seeder just joined server to avoid kicking or balancing them before they have a chance to play
                }
            }
            if ((SeederAlreadyInList == 0) && (this.autoAddDeleteSeeders == enumBoolYesNo.Yes))
            {
                // this player wasn't already a seeder, add him if plugin is configured to do so.
                Seeder newSeeder = new Seeder();
                newSeeder.soldierName = strSoldierName;
                newSeeder.setActive();
                newSeeder.setOnline();
                newSeeder.teamid = 0;
                newSeeder.updateScore(0, idleCheck_score);
                newSeeder.update_activity_time();               // initialize any newly joining player that isn't already in the seederlist from admin with a zero time since activity
                SeederList.Add(newSeeder);
                writeMsgToPluginConsole(2, string.Format("Adding newly joined player {0} as seeder", strSoldierName));

            }

        }
        public void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            writeMsgToPluginConsole(2, string.Format("OnPlayerLeft event for player {0} ", playerInfo.SoldierName));
            // need to remove seeder from online status if this is a seeder leaving
            for (int ndx = 0; ndx < SeederList.Count; ndx++)
            {
                if (string.Compare(SeederList[ndx].soldierName, playerInfo.SoldierName) == 0)
                {
                    SeederList[ndx].setOffline();
                    SeederList[ndx].setInactive();
                    SeederList[ndx].teamid = 0;
                    writeMsgToPluginConsole(1, string.Format("Seeder {0} set to offline", SeederList[ndx].soldierName));
                }
            }

            // in case auto-adding players as seeders was/is enabled, go through the list of seeders and the seedernames and 
            // delete any seederlist items that are inactive and not in seedernames

            for (int ndx = 0; ndx < SeederList.Count; ndx++)
            {
                if (SeederList[ndx].checkIsOnline() == false)
                {
                    int deleteflag = 1;     // assume we'll delete this seeder unless a match is found in the admin specified list
                    for (int i = 0; i < seederNames.Count; i++)
                    {
                        if (string.Compare(SeederList[ndx].soldierName, seederNames[i]) == 0)
                        {
                            deleteflag = 0;
                        }

                    }
                    if (deleteflag == 1)
                    {
                        writeMsgToPluginConsole(2, string.Format("Deleting offline seeder {0} from seederList", SeederList[ndx].soldierName));
                        SeederList.Remove(SeederList[ndx]);
                    }
                }
            }

        }
        public void OnServerInfo(CServerInfo csiServerInfo)
        {
            writeMsgToPluginConsole(3, "OnServerInfo event detected");
            writeMsgToPluginConsole(3, string.Format("ServerInfo Event - Team Count from server: {0}, plugin team count: {1} ", csiServerInfo.TeamScores.Count, teamList.Count));
            if (teamList.Count != csiServerInfo.TeamScores.Count)   // if the count of teams doesn't match, we'd better rebuild the local team list
            {
                // rebuild the team list
                teamList.Clear();
                writeMsgToPluginConsole(3, string.Format("Rebuilding internal team list at start of round."));
                for (int i = 0; i < csiServerInfo.TeamScores.Count; i++)
                {
                    Team newTeamitem = new Team();
                    newTeamitem.TeamID = csiServerInfo.TeamScores[i].TeamID;
                    newTeamitem.Score_Current = csiServerInfo.TeamScores[i].Score;
                    newTeamitem.Score_Winning = csiServerInfo.TeamScores[i].WinningScore;
                    newTeamitem.Score_Starting = newTeamitem.Score_Current;
                    teamList.Add(newTeamitem);
                    writeMsgToPluginConsole(3, string.Format("Adding team: {0}, Current/Starting Score: {1}, Score to Win: {2}", newTeamitem.TeamID, newTeamitem.Score_Current, newTeamitem.Score_Winning));
                    percent_of_round_complete = 0.0;
                }
            }
            else
            {   // update the scores of the teams
                for (int i = 0; i < csiServerInfo.TeamScores.Count; i++)
                {
                    teamList[i].Score_Current = csiServerInfo.TeamScores[i].Score;
                    teamList[i].Score_Winning = csiServerInfo.TeamScores[i].WinningScore;
                    writeMsgToPluginConsole(3, string.Format("Updating scores for team {0}, starting score: {1}, current score: {2}, score_to_win: {3}, fraction_of_round_complete: {4}", csiServerInfo.TeamScores[i].TeamID, teamList[i].Score_Starting, teamList[i].Score_Current, teamList[i].Score_Winning, teamList[i].getPercentRoundComplete()));
                    if (teamList[i].getPercentRoundComplete() > percent_of_round_complete)
                    {
                        percent_of_round_complete = teamList[i].getPercentRoundComplete();
                        writeMsgToPluginConsole(10, string.Format("Updating fraction of round complete: {0} ", percent_of_round_complete));
                    }
                }
            }
            writeMsgToPluginConsole(4, string.Format("Fraction of round complete: {0} ", percent_of_round_complete));
            string strcurrentGametype = csiServerInfo.GameMode.ToLower();
            writeMsgToPluginConsole(10, string.Format("Server reported game mode is: {0}", csiServerInfo.GameMode));
            gameMode = gameType.Other;      // set to other then fix back to correct value based on result

            if (strcurrentGametype.Contains("rush")) { gameMode = gameType.Rush; }
            if (strcurrentGametype.Contains("capturetheflag")) { gameMode = gameType.CTF; }
            if (strcurrentGametype.Contains("conquest")) { gameMode = gameType.Conquest; }
            if (strcurrentGametype.Contains("gunmaster")) { gameMode = gameType.GunMaster; }
            if (strcurrentGametype.Contains("scavenger")) { gameMode = gameType.Scavenger; }
            if (strcurrentGametype.Contains("squaddeathmatch")) { gameMode = gameType.SquadDeathMatch; }
            if (strcurrentGametype.Contains("squadrush")) { gameMode = gameType.SquadRush; }
            if (strcurrentGametype.Contains("tank")) { gameMode = gameType.TankSuperiority; }
            if (strcurrentGametype.Contains("airsuperiority")) { gameMode = gameType.AirSuperiority; }

            writeMsgToPluginConsole(5, string.Format("Plugin game mode set to: {0}", gameMode));

            // code never gets to setting team score if this is capture the flag more
            roundElapsedTime = DateTime.Now - roundStartTime;
            writeMsgToPluginConsole(5, string.Format("Round Elapsed Time: {0} minutes", roundElapsedTime.Minutes));
            if (gameMode != gameType.CTF)
            {
                team1Score = csiServerInfo.TeamScores[0].Score;
                team2Score = csiServerInfo.TeamScores[1].Score;
                team1_minus_team2_score = team1Score - team2Score;
                writeMsgToPluginConsole(5, string.Format("Updating team scores.  Team 1 Score: {0}, Team 2 Score:{1}", team1Score, team2Score));
            }
            else
            {
                writeMsgToPluginConsole(5, string.Format("Game mode is CTF, updating timer.  Elapsed round time: {0} minutes", roundElapsedTime.TotalMinutes));
            }
        }
        public virtual void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            // loop through seeders to see if the player that spawned is a seeder, update activity status if there is a match
            if (idleCheck_spawn == enumBoolYesNo.Yes)
            {
                for (int ndxSeeder = 0; ndxSeeder < SeederList.Count; ndxSeeder++)
                {
                    if (string.Compare(soldierName, SeederList[ndxSeeder].soldierName) == 0)
                    {
                        writeMsgToPluginConsole(6, string.Format("Seeder {0} spawned, setting time since last activity to zero", soldierName));
                        SeederList[ndxSeeder].update_activity_time();
                    }
                }
            }
        }
        public virtual void OnGlobalChat(string speaker, string message)
        {
            // loop through seeders to see if the player that typed in chat is a seeder, update activity status if there is a match
            if (idleCheck_chat == enumBoolYesNo.Yes)
            {
                for (int ndxSeeder = 0; ndxSeeder < SeederList.Count; ndxSeeder++)
                {
                    if (string.Compare(speaker, SeederList[ndxSeeder].soldierName) == 0)
                    {
                        writeMsgToPluginConsole(6, string.Format("Seeder {0} typed in global chat, setting time since last activity to zero", speaker));
                        SeederList[ndxSeeder].update_activity_time();
                    }
                }
            }

        }
        public virtual void OnTeamChat(string speaker, string message, int teamId)
        {
            // loop through seeders to see if the player that typed in chat is a seeder, update activity status if there is a match
            if (idleCheck_chat == enumBoolYesNo.Yes)
            {
                for (int ndxSeeder = 0; ndxSeeder < SeederList.Count; ndxSeeder++)
                {
                    if (string.Compare(speaker, SeederList[ndxSeeder].soldierName) == 0)
                    {
                        writeMsgToPluginConsole(6, string.Format("Seeder {0} typed in team chat, setting time since last activity to zero", speaker));
                        SeederList[ndxSeeder].update_activity_time();
                    }
                }
            }
        }
        public virtual void OnSquadChat(string speaker, string message, int teamId, int squadId)
        {
            // loop through seeders to see if the player that typed in chat is a seeder, update activity status if there is a match
            if (idleCheck_chat == enumBoolYesNo.Yes)
            {
                for (int ndxSeeder = 0; ndxSeeder < SeederList.Count; ndxSeeder++)
                {
                    if (string.Compare(speaker, SeederList[ndxSeeder].soldierName) == 0)
                    {
                        writeMsgToPluginConsole(6, string.Format("Seeder {0} typed in squad chat, setting time since last activity to zero", speaker));
                        SeederList[ndxSeeder].update_activity_time();
                    }
                }
            }
        }
        public void addAnyMissingSeeders(List<CPlayerInfo> players)
        {
            // if there are any players in the player list that aren't already a seeder, add them to seeder list
            for (int ndx = 0; ndx < players.Count; ndx++)
            {
                int seederAlreadyFlag = 0;
                for (int i = 0; i < SeederList.Count; i++)
                {
                    if (string.Compare(SeederList[i].soldierName, players[ndx].SoldierName) == 0)
                    {
                        seederAlreadyFlag = 1;
                        i = SeederList.Count;   // get out of the loop 
                    }
                }
                if ((seederAlreadyFlag == 0) && (autoAddDeleteSeeders == enumBoolYesNo.Yes))
                {
                    // this player wasn't already a seeder, add him if plugin is configured to do so.
                    Seeder newSeeder = new Seeder();
                    newSeeder.soldierName = players[ndx].SoldierName;
                    newSeeder.setActive();
                    newSeeder.setOnline();
                    newSeeder.teamid = 0;
                    newSeeder.updateScore(0, idleCheck_score);
                    newSeeder.update_activity_time();               // initialize any newly joining player that isn't already in the seederlist from admin with a zero time since activity
                    SeederList.Add(newSeeder);
                    writeMsgToPluginConsole(2, string.Format("Adding newly joined player {0} as seeder", players[ndx].SoldierName));
                }
            }
        }
        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset cpsSubset)
        {


            if (cpsSubset.Subset != CPlayerSubset.PlayerSubsetType.All)
            {
                writeMsgToPluginConsole(5, string.Format("OnlistPlayer triggered but not full list {0}", cpsSubset));
                return;      // if we didn't get the full list, do nothing
            }
            writeMsgToPluginConsole(1, string.Format("---------"));
            writeMsgToPluginConsole(3, string.Format("Player count {0}", players.Count));

            StringBuilder plyrListString = new StringBuilder(string.Format("Player list: "));
            if (debugPrintLevel >= 9)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    plyrListString.Append(string.Format(" {0} ", players[i].SoldierName));
                }
                writeMsgToPluginConsole(9, plyrListString.ToString());
            }

            if (this.autoAddDeleteSeeders == enumBoolYesNo.Yes)
            {
                addAnyMissingSeeders(players);
            }



            // loop through each seeder, check and see if seeder is online and update the seederlist with his current info
            for (int ndxSeeder = 0; ndxSeeder < SeederList.Count; ndxSeeder++)
            {
                // set all seeders to offline, team 0, and inactive and let them get set to current values by the updated player list if a match is found
                SeederList[ndxSeeder].setOffline();
                SeederList[ndxSeeder].setInactive();
                SeederList[ndxSeeder].teamid = 0;

                writeMsgToPluginConsole(7, string.Format("Checking player list for seeder {0}", SeederList[ndxSeeder].soldierName));

                // loop through each player listed from the server
                for (int ndxPlayer = 0; ndxPlayer < players.Count; ndxPlayer++)
                {
                    // if the playername matches a seeder, update score, online status
                    writeMsgToPluginConsole(10, string.Format("Checking for name match.  (player , seeder):  {0} , {1}", players[ndxPlayer].SoldierName, SeederList[ndxSeeder].soldierName));

                    if (string.Compare(SeederList[ndxSeeder].soldierName, players[ndxPlayer].SoldierName) == 0)
                    {
                        SeederList[ndxSeeder].setOnline();
                        SeederList[ndxSeeder].updateScore(players[ndxPlayer].Score, idleCheck_score);
                        SeederList[ndxSeeder].teamid = players[ndxPlayer].TeamID;

                        StringBuilder outputString = new StringBuilder(string.Format("Seeder: {0}", SeederList[ndxSeeder].soldierName));
                        outputString.Append(string.Format("Team: {0}, Current Score: {1},", players[ndxPlayer].TeamID, players[ndxPlayer].Score));

                        double minutesSinceActivity = SeederList[ndxSeeder].getMinutesSinceLastActivity();
                        outputString.Append(string.Format("Minutes since last activity: {0},", minutesSinceActivity));
                        if (minutesSinceActivity <= time_since_activity)
                        {
                            SeederList[ndxSeeder].setActive();
                            outputString.Append(string.Format(", status: Active"));
                        }
                        else
                        {
                            SeederList[ndxSeeder].setInactive();
                            outputString.Append(string.Format(", status: Inactive"));

                            // seeder is inactive, lets make sure seeder is in squad 0 (no squad)... not doing a team move here, just enforcing squad = 0 when inactive
                            if (players[ndxPlayer].SquadID > 0)
                            {
                                int current_team = players[ndxPlayer].TeamID;
                                if (simulateOnly == enumBoolYesNo.No)
                                {
                                    this.ExecuteCommand("procon.protected.send", "admin.movePlayer", players[ndxPlayer].SoldierName, Convert.ToString(current_team), "0", "true");
                                    writeMsgToPluginConsole(1, string.Format("Moving inactive seeder {0} from Squad {1} to Squad 0", players[ndxPlayer].SoldierName, players[ndxPlayer].SquadID)); return;
                                }
                                else
                                {
                                    writeMsgToPluginConsole(1, string.Format("Simulated Moving inactive seeder {0} from Squad {1} to Squad 0", players[ndxPlayer].SoldierName, players[ndxPlayer].SquadID)); return;
                                }
                            }
                        }


                        writeMsgToPluginConsole(7, outputString.ToString());
                    }
                }
            }


            // count the total players on each team
            team1count = 0;
            team2count = 0;
            for (int ndxPlayer = 0; ndxPlayer < players.Count; ndxPlayer++)
            {
                if (players[ndxPlayer].TeamID == 1) { team1count++; }
                if (players[ndxPlayer].TeamID == 2) { team2count++; }
                if ((players[ndxPlayer].TeamID != 1) && (players[ndxPlayer].TeamID != 2))
                {
                    writeMsgToPluginConsole(7, string.Format("Player {0} found but not on team 1 or 2, is on team: {1}", players[ndxPlayer].SoldierName, players[ndxPlayer].TeamID));
                }
            }
            writeMsgToPluginConsole(2, string.Format("Player count - Team 1: {0}, Team 2: {1}, Team 1+2: {2}, All:{3}", team1count, team2count, team1count + team2count, players.Count));

            // count the inactive seeders
            team1InactiveSeederCount = countIdleOnlineSeeders(1);
            team2InactiveSeederCount = countIdleOnlineSeeders(2);
            writeMsgToPluginConsole(1, string.Format("Inactive Seeder count - Team 1: {0}, Team 2: {1} ", team1InactiveSeederCount, team2InactiveSeederCount));

            // count the players that aren't idle seeders
            team1RealPlayerCount = team1count - team1InactiveSeederCount;
            team2RealPlayerCount = team2count - team2InactiveSeederCount;
            writeMsgToPluginConsole(1, string.Format("Real Player count - Team 1: {0}, Team 2: {1}", team1RealPlayerCount, team2RealPlayerCount));


            team1_minus_team2_inactiveSeederCount = team1InactiveSeederCount - team2InactiveSeederCount;
            team1_minus_team2_realPlayerCount = team1RealPlayerCount - team2RealPlayerCount;


            //this.updateOnlineSeedersActivityStatus();
            this.balanceIfNeeded(players);

            // check player count against threshold set to begin kicking seeders
            writeMsgToPluginConsole(5, string.Format("ServerInfo reported player count is: {0}, player kick threshold is {1}", players.Count, this.kick_minPlayerCount));
            if ((players.Count >= this.kick_minPlayerCount) && (this.kick_idleSeeders_flag == enumBoolYesNo.Yes))
            {
                writeMsgToPluginConsole(2, string.Format("Player count ({0}) exceeds threshold ({1}) to kick seeders, looking for seeder to kick", players.Count, this.kick_minPlayerCount));
                // try and find a seeder that can be kicked 
                if ((100.0 - percent_of_round_complete) > kickLimit_pctRoundLeft)
                {
                    if (roundElapsedTime.Minutes < kickLimit_waitTime_at_roundstart)
                    {
                        writeMsgToPluginConsole(2, string.Format("Kicking of seeder delayed at the start of new round for {0} minutes", kickLimit_waitTime_at_roundstart));
                    }
                    else
                    {
                        if ((roundElapsedTime.Minutes > kickLimit_CTF_timelimit) && (gameMode == gameType.CTF))
                        {
                            writeMsgToPluginConsole(2, string.Format("Kicking of seeder delayed because game mode is CTF and round time of {0} exceeds the limit set of {1}", roundElapsedTime.Minutes, kickLimit_CTF_timelimit));
                        }
                        else
                        {
                            kick_One_Seeder();
                        }
                    }
                }
                else
                {
                    writeMsgToPluginConsole(2, string.Format("Kicking of seeder delayed because percent of round left is {0} and is below threshold of {1}", 100.0 - percent_of_round_complete, kickLimit_pctRoundLeft));
                }
            }

        }

        public void kick_One_Seeder()
        {
            // go through list of seeders, use threshold time for seeder to be idle to decide if seeder can be kicked, then kick if possible
            for (int ndxSeeder = 0; ndxSeeder < SeederList.Count; ndxSeeder++)
            {
                if (SeederList[ndxSeeder].checkIsOnline())
                {
                    writeMsgToPluginConsole(6, string.Format("Seeder: {0} is online, time since last activity is: {1}", SeederList[ndxSeeder].soldierName, SeederList[ndxSeeder].getMinutesSinceLastActivity()));
                    if (SeederList[ndxSeeder].getMinutesSinceLastActivity() > this.kick_idleTimeMin)
                    {
                        writeMsgToPluginConsole(1, string.Format("Player count exceeds threshold of {0}, Seeder {1} has been idle {2} minutes and is being kicked due to server being populated", this.kick_minPlayerCount, SeederList[ndxSeeder].soldierName, SeederList[ndxSeeder].getMinutesSinceLastActivity()));
                        // seeder is online and has been inactive long enough... lets kick this one.
                        if (simulateOnly == enumBoolYesNo.No)
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", SeederList[ndxSeeder].soldierName, kick_reason);
                        }
                        else
                        {
                            writeMsgToPluginConsole(1, string.Format("Simulated kick of player: {0} occurred", SeederList[ndxSeeder].soldierName));
                        }
                        return; // exit out of this after having issued only one kick command.  Another can be kicked at next serverinfo event if needed.
                    }
                }

            }

        }
        public void balanceIfNeeded(List<CPlayerInfo> players)
        {

            writeMsgToPluginConsole(5, string.Format("Seeder balancing needed"));
            writeMsgToPluginConsole(5, string.Format("GameMode Detected: " + gameMode));

            /*	
                        EBASSIE EDIT: Changed Switch to IF / ELSE statement

                        switch (gameMode)
                        {					
                            case gameType.CTF:
                                writeMsgToPluginConsole(5, string.Format("Game type is CTF, using player count based logic"));
                                if (team1_minus_team2_inactiveSeederCount < -1)
                                {
                                    // too many seeders on team 2, move one to team 1
                                    writeMsgToPluginConsole(1, string.Format("Team 2 has excess idle seeders, moving one to team 1"));
                                    moveOneSeeder(2, 1);
                                }
                                if ((team1_minus_team2_inactiveSeederCount == -1) && (team1_minus_team2_realPlayerCount > 1))
                                {
                                    // extra seeder on team 2, team 2 disadvantaged, move a seeder to team 1
                                    writeMsgToPluginConsole(1, string.Format("Extra seeder is on team 2, team 2 has fewer real players (disadvantaged), moving a seeder from team 2 to team 1"));
                                    moveOneSeeder(2, 1);
                                }
                                if ((team1_minus_team2_inactiveSeederCount == 1) && (team1_minus_team2_realPlayerCount < -1))
                                {
                                    // team1 has extra seeder and is disadvantaged, move a seeder to team 2
                                    writeMsgToPluginConsole(1, string.Format("Extra seeder is on team 1, team 1 has fewer real players (disadvantaged), moving a seeder from team 1 to team 2"));
                                    moveOneSeeder(1, 2);
                                }
                                if (team1_minus_team2_inactiveSeederCount > 1)
                                {
                                    // team 1 has too many seeders, move one to team2
                                    writeMsgToPluginConsole(1, string.Format("Team 1 has excess idle seeders, moving a seeder from team 1 to team 2"));
                                    moveOneSeeder(1, 2);
                                }
                                break;

                            case gameType.Rush:
                                if (rushLogic.CompareTo("Defenders") == 0)    // preference is for extra seeder to be on defenders
                                {
                                    if (team2InactiveSeederCount > (team1InactiveSeederCount + 1))  // if team2 (defenders) has a difference of more than 1, move seeders to attackers (team 1)
                                    {
                                        writeMsgToPluginConsole(1, " Rush mode - detected too many seeders on defender side (team2)");
                                        writeMsgToPluginConsole(1, string.Format(" Team1InactiveSeederCount: {0}, Team2InactiveSeederCount: {1}", team1InactiveSeederCount, team2InactiveSeederCount));
                                        moveOneSeeder(2, 1);           // moveSeeders (int fromTeam, int toTeam, int numSeedersToMove)
                                    }
                                    if (team1InactiveSeederCount > team2InactiveSeederCount)      // if team1 (attackers) has more seeders than defenders, move them till equal or defenders has one more
                                    {
                                        writeMsgToPluginConsole(1, " Rush mode - detected too many seeders on attackers side (team1)");
                                        writeMsgToPluginConsole(1, string.Format(" Team1InactiveSeederCount: {0}, Team2InactiveSeederCount: {1}", team1InactiveSeederCount, team2InactiveSeederCount));
                                        moveOneSeeder(1, 2);
                                    }
                                }
                                if (rushLogic.CompareTo("Attackers") == 0)    // preference is for extra seeder to be on attackers
                                {
                                    if (team1InactiveSeederCount > (team2InactiveSeederCount + 1))  // if team1 (attackers) has a difference of more than 1, move seeders to defenders (team 2)
                                    {
                                        writeMsgToPluginConsole(1, " Rush mode - detected too many seeders on attackers side (team1)");
                                        writeMsgToPluginConsole(1, string.Format(" Team1InactiveSeederCount: {0}, Team2InactiveSeederCount: {1}", team1InactiveSeederCount, team2InactiveSeederCount));
                                        moveOneSeeder(1, 2);           // moveSeeders (int fromTeam, int toTeam, int numSeedersToMove)
                                    }
                                    if (team2InactiveSeederCount > team1InactiveSeederCount)      // if team2 (defenders) has more seeders, move them until equal or attackers has one more
                                    {
                                        writeMsgToPluginConsole(1, " Rush mode - detected too many seeders on defenders side (team2)");
                                        writeMsgToPluginConsole(1, string.Format(" Team1InactiveSeederCount: {0}, Team2InactiveSeederCount: {1}", team1InactiveSeederCount, team2InactiveSeederCount));
                                        moveOneSeeder(2, 1);
                                    }
                                }
                                break;

                            case gameType.GunMaster:
                            case gameType.Scavenger:
                            case gameType.SquadDeathMatch:
                            case gameType.SquadRush:
                            case gameType.TankSuperiority:
                            case gameType.AirSuperiority:
                            case gameType.Other:

                                // if code gets to here, this is not rush mode
                                writeMsgToPluginConsole(7, string.Format("Mode is NOT rush.  Using balanceMethod: {0}", nonRushLogic));

                                if (nonRushLogic.CompareTo("Player_Based") == 0)        // player count method
                                {
                                    if (team1_minus_team2_inactiveSeederCount < -1)
                                    {
                                        // too many seeders on team 2, move one to team 1
                                        writeMsgToPluginConsole(1, string.Format("Team 2 has excess idle seeders, moving one to team 1"));
                                        moveOneSeeder(2, 1);
                                    }
                                    if ((team1_minus_team2_inactiveSeederCount == -1) && (team1_minus_team2_realPlayerCount > 1))
                                    {
                                        // extra seeder on team 2, team 2 disadvantaged, move a seeder to team 1
                                        writeMsgToPluginConsole(1, string.Format("Extra seeder is on team 2, team 2 has fewer real players (disadvantaged), moving a seeder from team 2 to team 1"));
                                        moveOneSeeder(2, 1);
                                    }
                                    if ((team1_minus_team2_inactiveSeederCount == 1) && (team1_minus_team2_realPlayerCount < -1))
                                    {
                                        // team1 has extra seeder and is disadvantaged, move a seeder to team 2
                                        writeMsgToPluginConsole(1, string.Format("Extra seeder is on team 1, team 1 has fewer real players (disadvantaged), moving a seeder from team 1 to team 2"));
                                        moveOneSeeder(1, 2);
                                    }
                                    if (team1_minus_team2_inactiveSeederCount > 1)
                                    {
                                        // team 1 has too many seeders, move one to team2
                                        writeMsgToPluginConsole(1, string.Format("Team 1 has excess idle seeders, moving a seeder from team 1 to team 2"));
                                        moveOneSeeder(1, 2);
                                    }
                                }
                                if (nonRushLogic.CompareTo("Ticket_Based") == 0)        // ticket count method
                                {
                                    if (team1_minus_team2_inactiveSeederCount < -1)
                                    {
                                        // too many seeders on team 2, move one to team 1
                                        writeMsgToPluginConsole(1, string.Format("Team 2 has excess idle seeders, moving a seeder from team 2 to team 1"));
                                        moveOneSeeder(2, 1);
                                    }
                                    if ((team1_minus_team2_inactiveSeederCount == -1) && (team1_minus_team2_score > 0))
                                    {
                                        // extra seeder on team 2, team 2 disadvantaged, move a seeder to team 1
                                        writeMsgToPluginConsole(1, string.Format("Team 2 has the extra seeder, Team 2 has a lower score(disadvantaged), moving a seeder from team 2 to team 1"));
                                        moveOneSeeder(2, 1);
                                    }

                                    if ((team1_minus_team2_inactiveSeederCount == 1) && (team1_minus_team2_score < 0))
                                    {
                                        // team1 has extra seeder and is disadvantaged, move a seeder to team 2
                                        writeMsgToPluginConsole(1, string.Format("Team 1 has the extra seeder, Team 1 has a lower score(disadvantaged), moving a seeder from team 1 to team 2"));
                                        moveOneSeeder(1, 2);
                                    }
                                    if (team1_minus_team2_inactiveSeederCount > 1)
                                    {
                                        // team 1 has too many seeders, move one to team2
                                        writeMsgToPluginConsole(1, string.Format("Team 1 excess idle seeders, moving a seeder from team 1 to team 2"));
                                        moveOneSeeder(1, 2);
                                    }
                                }
                                break;
                        }
                        */

            if (gameMode == gameType.CTF)
            {
                writeMsgToPluginConsole(5, string.Format("Game type is CTF, using player count based logic"));
                if (team1_minus_team2_inactiveSeederCount < -1)
                {
                    // too many seeders on team 2, move one to team 1
                    writeMsgToPluginConsole(1, string.Format("Team 2 has excess idle seeders, moving one to team 1"));
                    moveOneSeeder(2, 1);
                }
                if ((team1_minus_team2_inactiveSeederCount == -1) && (team1_minus_team2_realPlayerCount > 1))
                {
                    // extra seeder on team 2, team 2 disadvantaged, move a seeder to team 1
                    writeMsgToPluginConsole(1, string.Format("Extra seeder is on team 2, team 2 has fewer real players (disadvantaged), moving a seeder from team 2 to team 1"));
                    moveOneSeeder(2, 1);
                }
                if ((team1_minus_team2_inactiveSeederCount == 1) && (team1_minus_team2_realPlayerCount < -1))
                {
                    // team1 has extra seeder and is disadvantaged, move a seeder to team 2
                    writeMsgToPluginConsole(1, string.Format("Extra seeder is on team 1, team 1 has fewer real players (disadvantaged), moving a seeder from team 1 to team 2"));
                    moveOneSeeder(1, 2);
                }
                if (team1_minus_team2_inactiveSeederCount > 1)
                {
                    // team 1 has too many seeders, move one to team2
                    writeMsgToPluginConsole(1, string.Format("Team 1 has excess idle seeders, moving a seeder from team 1 to team 2"));
                    moveOneSeeder(1, 2);
                }
            }
            else if (gameMode == gameType.Rush)
            {
                if (rushLogic.CompareTo("Defenders") == 0)    // preference is for extra seeder to be on defenders
                {
                    if (team2InactiveSeederCount > (team1InactiveSeederCount + 1))  // if team2 (defenders) has a difference of more than 1, move seeders to attackers (team 1)
                    {
                        writeMsgToPluginConsole(1, " Rush mode - detected too many seeders on defender side (team2)");
                        writeMsgToPluginConsole(1, string.Format(" Team1InactiveSeederCount: {0}, Team2InactiveSeederCount: {1}", team1InactiveSeederCount, team2InactiveSeederCount));
                        moveOneSeeder(2, 1);           // moveSeeders (int fromTeam, int toTeam, int numSeedersToMove)
                    }
                    if (team1InactiveSeederCount > team2InactiveSeederCount)      // if team1 (attackers) has more seeders than defenders, move them till equal or defenders has one more
                    {
                        writeMsgToPluginConsole(1, " Rush mode - detected too many seeders on attackers side (team1)");
                        writeMsgToPluginConsole(1, string.Format(" Team1InactiveSeederCount: {0}, Team2InactiveSeederCount: {1}", team1InactiveSeederCount, team2InactiveSeederCount));
                        moveOneSeeder(1, 2);
                    }
                }
                if (rushLogic.CompareTo("Attackers") == 0)    // preference is for extra seeder to be on attackers
                {
                    if (team1InactiveSeederCount > (team2InactiveSeederCount + 1))  // if team1 (attackers) has a difference of more than 1, move seeders to defenders (team 2)
                    {
                        writeMsgToPluginConsole(1, " Rush mode - detected too many seeders on attackers side (team1)");
                        writeMsgToPluginConsole(1, string.Format(" Team1InactiveSeederCount: {0}, Team2InactiveSeederCount: {1}", team1InactiveSeederCount, team2InactiveSeederCount));
                        moveOneSeeder(1, 2);           // moveSeeders (int fromTeam, int toTeam, int numSeedersToMove)
                    }
                    if (team2InactiveSeederCount > team1InactiveSeederCount)      // if team2 (defenders) has more seeders, move them until equal or attackers has one more
                    {
                        writeMsgToPluginConsole(1, " Rush mode - detected too many seeders on defenders side (team2)");
                        writeMsgToPluginConsole(1, string.Format(" Team1InactiveSeederCount: {0}, Team2InactiveSeederCount: {1}", team1InactiveSeederCount, team2InactiveSeederCount));
                        moveOneSeeder(2, 1);
                    }
                }
            }
            else
            {
                // if code gets to here, this is not rush mode
                writeMsgToPluginConsole(7, string.Format("Mode is NOT rush.  Using balanceMethod: {0}", nonRushLogic));

                if (nonRushLogic.CompareTo("Player_Based") == 0)        // player count method
                {
                    if (team1_minus_team2_inactiveSeederCount < -1)
                    {
                        // too many seeders on team 2, move one to team 1
                        writeMsgToPluginConsole(1, string.Format("Team 2 has excess idle seeders, moving one to team 1"));
                        moveOneSeeder(2, 1);
                    }
                    if ((team1_minus_team2_inactiveSeederCount == -1) && (team1_minus_team2_realPlayerCount > 1))
                    {
                        // extra seeder on team 2, team 2 disadvantaged, move a seeder to team 1
                        writeMsgToPluginConsole(1, string.Format("Extra seeder is on team 2, team 2 has fewer real players (disadvantaged), moving a seeder from team 2 to team 1"));
                        moveOneSeeder(2, 1);
                    }
                    if ((team1_minus_team2_inactiveSeederCount == 1) && (team1_minus_team2_realPlayerCount < -1))
                    {
                        // team1 has extra seeder and is disadvantaged, move a seeder to team 2
                        writeMsgToPluginConsole(1, string.Format("Extra seeder is on team 1, team 1 has fewer real players (disadvantaged), moving a seeder from team 1 to team 2"));
                        moveOneSeeder(1, 2);
                    }
                    if (team1_minus_team2_inactiveSeederCount > 1)
                    {
                        // team 1 has too many seeders, move one to team2
                        writeMsgToPluginConsole(1, string.Format("Team 1 has excess idle seeders, moving a seeder from team 1 to team 2"));
                        moveOneSeeder(1, 2);
                    }
                }
                if (nonRushLogic.CompareTo("Ticket_Based") == 0)        // ticket count method
                {
                    if (team1_minus_team2_inactiveSeederCount < -1)
                    {
                        // too many seeders on team 2, move one to team 1
                        writeMsgToPluginConsole(1, string.Format("Team 2 has excess idle seeders, moving a seeder from team 2 to team 1"));
                        moveOneSeeder(2, 1);
                    }
                    if ((team1_minus_team2_inactiveSeederCount == -1) && (team1_minus_team2_score > 0))
                    {
                        // extra seeder on team 2, team 2 disadvantaged, move a seeder to team 1
                        writeMsgToPluginConsole(1, string.Format("Team 2 has the extra seeder, Team 2 has a lower score(disadvantaged), moving a seeder from team 2 to team 1"));
                        moveOneSeeder(2, 1);
                    }

                    if ((team1_minus_team2_inactiveSeederCount == 1) && (team1_minus_team2_score < 0))
                    {
                        // team1 has extra seeder and is disadvantaged, move a seeder to team 2
                        writeMsgToPluginConsole(1, string.Format("Team 1 has the extra seeder, Team 1 has a lower score(disadvantaged), moving a seeder from team 1 to team 2"));
                        moveOneSeeder(1, 2);
                    }
                    if (team1_minus_team2_inactiveSeederCount > 1)
                    {
                        // team 1 has too many seeders, move one to team2
                        writeMsgToPluginConsole(1, string.Format("Team 1 excess idle seeders, moving a seeder from team 1 to team 2"));
                        moveOneSeeder(1, 2);
                    }
                }
                //break;
            }

        }
        private void moveOneSeeder(int moveFromTeam, int moveToTeam)
        {
            writeMsgToPluginConsole(2, string.Format("Attempting to move an inactive seeder from team {0} to team {1}", moveFromTeam, moveToTeam));

            // move one seeder from  moveFromTeam to moveToTeam

            for (int i = 0; i < SeederList.Count; i++)
            {
                //writeMsgToPluginConsole(1, string.Format("Checking seeders: {0}", SeederList[i].soldierName));

                if ((SeederList[i].teamid == moveFromTeam) && (SeederList[i].checkIsActive() == false))     // find an inactive seeder on the movefromTeam
                {
                    if (simulateOnly == enumBoolYesNo.No)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.movePlayer", SeederList[i].soldierName, Convert.ToString(moveToTeam), "0", "true");
                        writeMsgToPluginConsole(1, string.Format("Moving seeder {0} from Team {1} to Team {2}", SeederList[i].soldierName, moveFromTeam, moveToTeam));
                    }
                    else
                    {
                        writeMsgToPluginConsole(2, string.Format("Simulated move of player {0} occurred", SeederList[i].soldierName));
                    }

                    return;
                }
                else
                {
                    string onOfflineStatus = "offline";
                    if (SeederList[i].checkIsOnline())
                    {
                        onOfflineStatus = "online";
                    }
                    string activityStatus = "idle";
                    if (SeederList[i].checkIsActive()) { activityStatus = "active"; }

                    writeMsgToPluginConsole(6, string.Format("Not Moving Seeder {0} on team: {1}, {2}, {3}", SeederList[i].soldierName, onOfflineStatus, activityStatus, SeederList[i].teamid));

                }
            }

        }
        public void writeMsgToPluginConsole(int debugLevel, string message)
        {
            //this.ExecuteCommand("procon.protected.pluginconsole.write", string.Format("BalanceServerSeeders: debugLevel {0} debugPrintlevel: {1}", debugLevel, debugPrintLevel));
            if (debugLevel <= debugPrintLevel)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", string.Format("BalanceServerSeeders: {0}", message));
            }
        }

    }
}