using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

namespace PRoConEvents
{
    public class EndOfRoundStats : PRoConPluginAPI, IPRoConPluginInterface3
    {
        #region Plugin variable declarations and assignments

        private float m_fBestKDR;

        private int m_iLongestKillDistance;
        private int m_iLongestHeadshotDistance;
        private int m_iMostDeaths;

        private string m_strBestKDR;
        private string m_strFirstBlood;
        private string m_strFirstKnife;
        private string m_strLongestHeadshot;
        private string m_strLongestKill;
        private string m_strLastBlood;
        private string m_strLastKnife;
        private string m_strMostDeaths;

        public EndOfRoundStats()
        {
            this.m_fBestKDR = 0F;
            this.m_iLongestHeadshotDistance = 0;
            this.m_iLongestKillDistance = 0;
            this.m_iMostDeaths = 0;
            this.m_strBestKDR = "Nobody!";
            this.m_strFirstBlood = "Nobody!";
            this.m_strFirstKnife = "Nobody!";
            this.m_strLongestHeadshot = "Nobody!";
            this.m_strLongestKill = "Nobody!";
            this.m_strLastBlood = "Nobody!";
            this.m_strLastKnife = "Nobody!";
            this.m_strMostDeaths = "Nobody!";
        }
        #endregion

        #region Plugin information

        public string GetPluginName()
        {
            return "End of Round Stats";
        }

        public string GetPluginVersion()
        {
            return "v0.05";
        }

        public string GetPluginAuthor()
        {
            return "blactionhero";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net/";
        }

        public string GetPluginDescription()
        {
            return @"
			
<h2><u>Description</u></h2>

<p>End of Round Stats displays the following info in the chatbox at the end of each round: Best KDR, Longest Kill, Longest Headshot, Most Deaths, First Blood, First Knife, Last Blood, and Last Knife.

";
        }

        #endregion

        #region PRoCon variables

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnPluginEnable", "OnListPlayers", "OnLevelStarted", "OnPlayerKilled", "OnRoundOver", "OnPluginDisable");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {

        }

        #endregion

        #region PRoCon events

        public void OnPluginEnable() { }

        public void OnLevelStarted()
        {
            this.ExecuteCommand("procon.protected.send", "admin.say", "End of Round Stats - Previous round:", "all");
            this.ExecuteCommand("procon.protected.send", "admin.say", "First Blood: " + m_strFirstBlood + ", First Knife: " + m_strFirstKnife, "all");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Last Blood: " + m_strLastBlood + ", Last Knife: " + m_strLastKnife, "all");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Best KDR: " + m_strBestKDR + " (" + m_fBestKDR.ToString() + "), Most Deaths: " + m_strMostDeaths + " (" + m_iMostDeaths.ToString() + " lol)", "all");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Longest Headshot: " + m_strLongestHeadshot + " (" + m_iLongestHeadshotDistance.ToString() + "m), Longest Kill: " + " (" + m_iLongestKillDistance.ToString() + "m)", "all");

            m_fBestKDR = 0F;
            m_iLongestHeadshotDistance = 0;
            m_iLongestKillDistance = 0;
            m_iMostDeaths = 0;
            m_strBestKDR = "Nobody!";
            m_strFirstBlood = "Nobody!";
            m_strFirstKnife = "Nobody!";
            m_strLongestHeadshot = "Nobody!";
            m_strLongestKill = "Nobody!";
            m_strLastBlood = "Nobody!";
            m_strLastKnife = "Nobody!";
            m_strMostDeaths = "Nobody!";
        }

        public void OnPluginDisable()
        {
            this.OnLevelStarted();
        }

        public void OnRoundOver(int iWinningTeamID)
        {
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");

            this.ExecuteCommand("procon.protected.pluginconsole.write", "First Blood: " + m_strFirstBlood + ", First Knife: " + m_strFirstKnife);
            this.ExecuteCommand("procon.protected.pluginconsole.write", "Last Blood: " + m_strLastBlood + ", Last Knife: " + m_strLastKnife);
            this.ExecuteCommand("procon.protected.pluginconsole.write", "Best KDR: " + m_strBestKDR + " (" + m_fBestKDR.ToString() + "), Most Deaths: " + m_strMostDeaths + " (" + m_iMostDeaths.ToString() + " lol)");
            this.ExecuteCommand("procon.protected.pluginconsole.write", "Longest Headshot: " + m_strLongestHeadshot + " (" + m_iLongestHeadshotDistance.ToString() + "m), Longest Kill: " + " (" + m_iLongestKillDistance.ToString() + "m)");


            this.ExecuteCommand("procon.protected.send", "admin.say", "First Blood: " + m_strFirstBlood + ", First Knife: " + m_strFirstKnife, "all");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Last Blood: " + m_strLastBlood + ", Last Knife: " + m_strLastKnife, "all");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Best KDR: " + m_strBestKDR + " (" + m_fBestKDR.ToString() + "), Most Deaths: " + m_strMostDeaths + " (" + m_iMostDeaths.ToString() + " lol)", "all");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Longest Headshot: " + m_strLongestHeadshot + " (" + m_iLongestHeadshotDistance.ToString() + "m), Longest Kill: " + " (" + m_iLongestKillDistance.ToString() + "m)", "all");

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (cpiPlayer.Kdr > m_fBestKDR)
                    {
                        m_fBestKDR = cpiPlayer.Kdr;
                        m_strBestKDR = cpiPlayer.SoldierName;
                    }

                    if (cpiPlayer.Deaths > m_iMostDeaths)
                    {
                        m_iMostDeaths = cpiPlayer.Deaths;
                        m_strMostDeaths = cpiPlayer.SoldierName;
                    }
                }
            }
        }

        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            Weapon weaponUsed = this.GetWeaponDefines()[kKillerVictimDetails.DamageType];
            string weaponUsedName = this.GetLocalized(weaponUsed.Name, String.Format("global.Weapons.{0}", kKillerVictimDetails.DamageType.ToLower()));
            CPlayerInfo Killer = kKillerVictimDetails.Killer;

            m_strLastBlood = Killer.SoldierName;

            if (kKillerVictimDetails.Headshot)
            {
                if (m_strLongestHeadshot == "Nobody!")
                {
                    m_strLongestHeadshot = Killer.SoldierName;
                    m_iLongestHeadshotDistance = System.Convert.ToInt16(kKillerVictimDetails.Distance);
                }

                else
                {
                    if (kKillerVictimDetails.Distance > m_iLongestHeadshotDistance)
                    {
                        m_iLongestHeadshotDistance = System.Convert.ToInt16(kKillerVictimDetails.Distance);
                        m_strLongestHeadshot = Killer.SoldierName;
                    }
                }
            }

            else
            {
                if (m_strLongestKill == "Nobody!")
                {
                    m_strLongestKill = Killer.SoldierName;
                    m_iLongestKillDistance = System.Convert.ToInt16(kKillerVictimDetails.Distance);
                }

                else
                {
                    if (kKillerVictimDetails.Distance > m_iLongestKillDistance)
                    {
                        m_iLongestKillDistance = System.Convert.ToInt16(kKillerVictimDetails.Distance);
                        m_strLongestKill = Killer.SoldierName;
                    }
                }
            }

            if (m_strFirstBlood == "Nobody!")
            {
                m_strFirstBlood = Killer.SoldierName;
            }

            if (weaponUsedName == "Combat Knife")
            {
                m_strLastKnife = Killer.SoldierName;

                if (m_strFirstKnife == "Nobody!")
                {
                    m_strFirstKnife = Killer.SoldierName;
                }
            }
        }
    }
}
#endregion