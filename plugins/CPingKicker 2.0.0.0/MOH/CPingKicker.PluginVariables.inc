
        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables() {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Communication|Send privately to player", typeof(enumBoolYesNo), this.m_enSendKicksPrivately));
            lstReturn.Add(new CPluginVariable("Communication|Delay between message and kick", typeof(int), this.m_iDelayBetweenMessageAndKick));

            if (this.m_enSendKicksPrivately == enumBoolYesNo.Yes) {
                lstReturn.Add(new CPluginVariable("Communication|Private kick message", this.m_strKickingMessagePrivate.GetType(), this.m_strKickingMessagePrivate));
            }
            else {
                lstReturn.Add(new CPluginVariable("Communication|Public kick message", this.m_strKickingMessagePublic.GetType(), this.m_strKickingMessagePublic));
            }

            lstReturn.Add(new CPluginVariable("Kick Thresholds|Kick player if average ping >", typeof(int), this.m_iMaximumAveragePing));
            lstReturn.Add(new CPluginVariable("Kick Thresholds|over a period of (seconds)", typeof(int), this.m_iOverPeriodInSeconds));

            lstReturn.Add(new CPluginVariable("Additional updates (Higher accuracy, higher bandwidth)|Request more data from the server", typeof(enumBoolYesNo), this.m_enRequestMoreDataFromServer));
            if (this.m_enRequestMoreDataFromServer == enumBoolYesNo.Yes) {
                lstReturn.Add(new CPluginVariable("Additional updates (Higher accuracy, higher bandwidth)|Get additional updates every (seconds)", typeof(int), this.m_iGetAdditionalDataSeconds));
            }

            //lstReturn.Add(new CPluginVariable("Whitelist options|Kick account holders", typeof(enumBoolYesNo), this.m_enKickAccountHolders));
			
            lstReturn.Add(new CPluginVariable("Whitelist options|White List", typeof(string[]), this.pingWhiteList.ToArray()));
			
            //lstReturn.Add(new CPluginVariable("Testing options (will be removed once tested properly)|Debug mode", typeof(enumBoolOnOff), this.m_enRunInDebugMode));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables() {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Send privately to player", typeof(enumBoolYesNo), this.m_enSendKicksPrivately));
            lstReturn.Add(new CPluginVariable("Delay between message and kick", typeof(int), this.m_iDelayBetweenMessageAndKick));
            lstReturn.Add(new CPluginVariable("Private kick message", this.m_strKickingMessagePrivate.GetType(), this.m_strKickingMessagePrivate));
            lstReturn.Add(new CPluginVariable("Public kick message", this.m_strKickingMessagePublic.GetType(), this.m_strKickingMessagePublic));

            lstReturn.Add(new CPluginVariable("Kick player if average ping >", typeof(int), this.m_iMaximumAveragePing));
            lstReturn.Add(new CPluginVariable("over a period of (seconds)", typeof(int), this.m_iOverPeriodInSeconds));

            lstReturn.Add(new CPluginVariable("Request more data from the server", typeof(enumBoolYesNo), this.m_enRequestMoreDataFromServer));
            lstReturn.Add(new CPluginVariable("Get additional updates every (seconds)", typeof(int), this.m_iGetAdditionalDataSeconds));

            //lstReturn.Add(new CPluginVariable("Kick account holders", typeof(enumBoolYesNo), this.m_enKickAccountHolders));
            lstReturn.Add(new CPluginVariable("White List", typeof(string[]), this.pingWhiteList.ToArray()));

            //lstReturn.Add(new CPluginVariable("Debug mode", typeof(enumBoolOnOff), this.m_enRunInDebugMode));

            return lstReturn;
        }