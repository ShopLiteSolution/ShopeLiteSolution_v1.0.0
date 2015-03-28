using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;

namespace ShopLiteModule
{
    class ReaderConnection
    {
        public const int ConReader_Msk = 0x01;
        public const int SetPwr_Msk = 0x02;
        public const int SetFreq_Msk = 0x04;

        public SyncedSet<string> existTags;
        
        public int status;

        private Reader.ReaderMethod reader;
        private ReaderSetting m_curSetting = new ReaderSetting();

        private InventoryBuffer m_curInventoryBuffer = new InventoryBuffer();
        private OperateTagBuffer m_curOperateTagBuffer = new OperateTagBuffer();
        private OperateTagISO18000Buffer m_curOperateTagISO18000Buffer = new OperateTagISO18000Buffer();

        private bool m_bInventory = false;
        private bool m_bLockTab = false;
        private int m_nTotal = 0;

        private byte[] m_btAryData = new byte[10];

        public ReaderConnection()
        {
            status = 0;
            existTags = new SyncedSet<string>();
            existTags.Added += new AddEventHandler(newTagDetected);
            reader = new Reader.ReaderMethod();

            reader.AnalyCallback = AnalyData;
            reader.ReceiveCallback = ReceiveData;
            reader.SendCallback = SendData;

            ConnectTcp();
            waitForStatus(ConReader_Msk);

            SetOutputPower(DefaultSettings.OutputPower);
            waitForStatus(SetPwr_Msk);

            SetFrequencyRegion(DefaultSettings.SignalStrength, 0, 52);
            waitForStatus(SetFreq_Msk);
        }

        public void waitForStatus(int mask)
        {
            while ((status & mask) == 0)
            {
                Thread.Sleep(500);
            }
        }

        public void DisconnectTcp()
        {
            reader.SignOut();
            System.Console.WriteLine("reader signout");
        }
        public void ConnectTcp()
        {
            try
            {
                string strException = string.Empty;
                IPAddress ipAddress = IPAddress.Parse(DefaultSettings.ipAddress);
                int nPort = Convert.ToInt32(DefaultSettings.ipPort);

                int nRet = reader.ConnectServer(ipAddress, nPort, out strException);
                if (nRet != 0)
                {
                    string strLog = "Connect reader failed, due to: " + strException;
                    Console.WriteLine(strLog);
                    return;
                }
                else
                {
                    string strLog = "Reader connect succeeded: " + ipAddress.ToString() + ":" + nPort.ToString();
                    System.Console.WriteLine(strLog);
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }

            status |= ConReader_Msk;
        }
        private void ResetReader()
        {
            int nRet = reader.Reset(m_curSetting.btReadId);
            if (nRet != 0)
            {
                Console.Out.WriteLine("Reset reader failed");
            }
            else
            {
                Console.Out.WriteLine("Reset reader succeeded");
            }
        }
        private void SetOutputPower(string txtOutputPower)
        {
            try
            {
                if (txtOutputPower.Length != 0)
                {
                    reader.SetOutputPower(m_curSetting.btReadId, Convert.ToByte(txtOutputPower));
                    m_curSetting.btOutputPower = Convert.ToByte(txtOutputPower);
                }
            }
            catch (System.Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }

        }

        private void SetFrequencyRegion(string rdbRegion, int cmbFrequencyStart, int cmbFrequencyEnd)
        {
            try
            {
                byte btRegion = 0x00;
                byte btStartFreq = 0x00;
                byte btEndFreq = 0x00;

                int nStartIndex = cmbFrequencyStart;
                int nEndIndex = cmbFrequencyEnd;
                if (nEndIndex < nStartIndex)
                {
                    Console.Out.WriteLine("Spectrum definition is not valid, please see communication protocol sepcification");
                    return;
                }

                if (rdbRegion == "fcc")
                {
                    btRegion = 0x01;
                    btStartFreq = Convert.ToByte(nStartIndex + 7);
                    btEndFreq = Convert.ToByte(nEndIndex + 7);
                }
                else if (rdbRegion == "etsi")
                {
                    btRegion = 0x02;
                    btStartFreq = Convert.ToByte(nStartIndex);
                    btEndFreq = Convert.ToByte(nEndIndex);
                }
                else if (rdbRegion == "chn")
                {
                    btRegion = 0x03;
                    btStartFreq = Convert.ToByte(nStartIndex + 43);
                    btEndFreq = Convert.ToByte(nEndIndex + 43);
                }
                else
                {
                    return;
                }

                reader.SetFrequencyRegion(m_curSetting.btReadId, btRegion, btStartFreq, btEndFreq);
                m_curSetting.btRegion = btRegion;
                m_curSetting.btFrequencyStart = btStartFreq;
                m_curSetting.btFrequencyEnd = btEndFreq;


            }
            catch (System.Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
        }

        public void RealTimeInventory()
        {
            try
            {
                m_curInventoryBuffer.ClearInventoryPar();
                m_curInventoryBuffer.btRepeat = Convert.ToByte(1);
                m_curInventoryBuffer.bLoopCustomizedSession = false;
                m_curInventoryBuffer.lAntenna.Add(0x00);
                //TODO: m_curInventoryBuffer.lAntenna.Add(0x01);

                m_bInventory = true;
                m_curInventoryBuffer.bLoopInventory = true;
                m_curInventoryBuffer.bLoopInventoryReal = true;
                m_curInventoryBuffer.ClearInventoryRealResult();
                //clear item list
                //lvRealList.Items.Clear();
                m_nTotal = 0;

                byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                m_curSetting.btWorkAntenna = btWorkAntenna;

            }
            catch (System.Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }


        }


        private void AnalyData(Reader.MessageTran msgTran)
        {
            if (msgTran.PacketType != 0xA0)
            {
                return;
            }
            switch (msgTran.Cmd)
            {
                case 0x74:
                    ProcessSetWorkAntenna(msgTran);
                    break;
                case 0x76:
                    ProcessSetOutputPower(msgTran);
                    break;
                case 0x78:
                    ProcessSetFrequencyRegion(msgTran);
                    break;
                case 0x89:
                case 0x8B:
                    ProcessInventoryReal(msgTran);
                    break;
                default:
                    Console.Out.WriteLine("Unsupported command: " + msgTran.Cmd);
                    break;
            }
        }

        private void ProcessSetOutputPower(Reader.MessageTran msgTran)
        {
            string strCmd = "Set RF output power ";
            string strErrorCode = string.Empty;
            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    Console.Out.WriteLine(strCmd);
                    status |= SetPwr_Msk;
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "Unknown error";
            }

            string strLog = strCmd + "failed , due to: " + strErrorCode;
            Console.Out.WriteLine(strLog);
        }

        private void ProcessSetFrequencyRegion(Reader.MessageTran msgTran)
        {
            string strCmd = "Set RF spectrum ";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    Console.Out.WriteLine(strCmd);
                    status |= SetFreq_Msk;
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "Unknown error";
            }

            string strLog = strCmd + "failed , due to: " + strErrorCode;
            Console.Out.WriteLine(strLog);
        }

        private void ProcessSetWorkAntenna(Reader.MessageTran msgTran)
        {
            int intCurrentAnt = 0;
            intCurrentAnt = m_curSetting.btWorkAntenna + 1;
            string strCmd = "Successfully set working antenna, current working antenna : Ant " + intCurrentAnt.ToString();

            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);

                    if (m_bInventory)
                    {
                        RunLoopInventroy();
                    }
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "Unknown error";
            }

            string strLog = strCmd + "failed , due to: " + strErrorCode;
            //WriteLog(lrtxtLog, strLog, 1);

            if (m_bInventory)
            {
                m_curInventoryBuffer.nCommond = 1;
                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RunLoopInventroy();
            }
        }

        private void ProcessInventoryReal(Reader.MessageTran msgTran) {
            string strCmd = "";
            if (msgTran.Cmd == 0x89)
            {
                strCmd = "Real time mode inventory ";
            }
            if (msgTran.Cmd == 0x8B)
            {
                strCmd = "Customized Session and Inventoried Flag inventory ";
            }
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "failed, due to： " + strErrorCode;

                Console.Out.WriteLine(strLog);
                RefreshInventoryReal(0x00);
                RunLoopInventroy();
            }
            else if (msgTran.AryData.Length == 7)
            {
                m_curInventoryBuffer.nReadRate = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                m_curInventoryBuffer.nDataCount = Convert.ToInt32(msgTran.AryData[3]) * 256 * 256 * 256 +
                    Convert.ToInt32(msgTran.AryData[4]) * 256 * 256 + Convert.ToInt32(msgTran.AryData[5]) * 256 + Convert.ToInt32(msgTran.AryData[6]);

                Console.Out.WriteLine(strCmd);
                RefreshInventoryReal(0x01);
                RunLoopInventroy();
            }
            else
            {
                m_nTotal++;
                int nLength = msgTran.AryData.Length;
                int nEpcLength = nLength - 4;

                string strEPC = CCommondMethod.ByteArrayToStringNoSpace(msgTran.AryData, 3, nEpcLength);
                Console.Out.WriteLine("StrEPC = " + strEPC);
                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 2);
                string strRSSI = msgTran.AryData[nLength - 1].ToString();
                byte btTemp = msgTran.AryData[0];
                byte btAntId = (byte)((btTemp & 0x03) + 1);
                m_curInventoryBuffer.nCurrentAnt = btAntId;
                string strAntId = btAntId.ToString();

                byte btFreq = (byte)(btTemp >> 2);

                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RefreshInventoryReal(0x89);
            }
        }

        private void RefreshInventoryReal(byte btCmd)
        {
            /*if (this.InvokeRequired)
            {
                // RefreshInventoryRealUnsafe InvokeRefresh = new RefreshInventoryRealUnsafe(RefreshInventoryReal);
                // this.Invoke(InvokeRefresh, new object[] { btCmd });
            }*/
            //else
            {
                switch (btCmd)
                {
                    case 0x89:
                    case 0x8B:
                        {
                            int nTagCount = m_curInventoryBuffer.dtTagTable.Rows.Count;
                            int nTotalRead = m_nTotal;// m_curInventoryBuffer.dtTagDetailTable.Rows.Count;
                            TimeSpan ts = m_curInventoryBuffer.dtEndInventory - m_curInventoryBuffer.dtStartInventory;
                            int nTotalTime = ts.Minutes * 60 * 1000 + ts.Seconds * 1000 + ts.Milliseconds;
                            int nCaculatedReadRate = 0;
                            int nCommandDuation = 0;

                            if (m_curInventoryBuffer.nReadRate == 0) 
                            {
                                if (nTotalTime > 0)
                                {
                                    nCaculatedReadRate = (nTotalRead * 1000 / nTotalTime);
                                }
                            }
                            else
                            {
                                nCommandDuation = m_curInventoryBuffer.nDataCount * 1000 / m_curInventoryBuffer.nReadRate;
                                nCaculatedReadRate = m_curInventoryBuffer.nReadRate;
                            }
                        }
                        break;
                    case 0x00:
                    case 0x01:
                        {
                            m_bLockTab = false;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private delegate void RunLoopInventoryUnsafe();
        private void RunLoopInventroy()
        {
 /*           if (this.InvokeRequired)
            {
                RunLoopInventoryUnsafe InvokeRunLoopInventory = new RunLoopInventoryUnsafe(RunLoopInventroy);
                this.Invoke(InvokeRunLoopInventory, new object[] { });
            }*/ //TODO: Check
            /*if (!Dispatcher.CheckAccess())
            {
                RunLoopInventoryUnsafe InvokeRunLoopInventory = new RunLoopInventoryUnsafe(RunLoopInventroy);
                Dispatcher.Invoke(InvokeRunLoopInventory, arg);
            }*/
            //else
            {
                if (m_curInventoryBuffer.nIndexAntenna < m_curInventoryBuffer.lAntenna.Count - 1 || m_curInventoryBuffer.nCommond == 0)
                {
                    if (m_curInventoryBuffer.nCommond == 0)
                    {
                        m_curInventoryBuffer.nCommond = 1;

                        if (m_curInventoryBuffer.bLoopInventoryReal)
                        {
                            if (m_curInventoryBuffer.bLoopCustomizedSession)
                            {
                                reader.CustomizedInventory(m_curSetting.btReadId, m_curInventoryBuffer.btSession, m_curInventoryBuffer.btTarget, m_curInventoryBuffer.btRepeat);
                            }
                            else
                            {
                                reader.InventoryReal(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);

                            }
                        }
                        else
                        {
                            if (m_curInventoryBuffer.bLoopInventory)
                                reader.Inventory(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);
                        }
                    }
                    else
                    {
                        m_curInventoryBuffer.nCommond = 0;
                        m_curInventoryBuffer.nIndexAntenna++;

                        byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                        reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                        m_curSetting.btWorkAntenna = btWorkAntenna;
                    }
                }
                else if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_curInventoryBuffer.nIndexAntenna = 0;
                    m_curInventoryBuffer.nCommond = 0;

                    byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                    reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                    m_curSetting.btWorkAntenna = btWorkAntenna;
                }
            }
        }


        private void ReceiveData(byte[] btAryReceiveData)
        {
            string strLog = CCommondMethod.ByteArrayToString(btAryReceiveData, 0, btAryReceiveData.Length);
            //Console.Out.WriteLine(strLog);
        }

        private void SendData(byte[] btArySendData)
        {
            string strLog = CCommondMethod.ByteArrayToString(btArySendData, 0, btArySendData.Length);
            //Console.Out.WriteLine(strLog);
        }
        
        private void newTagDetected(object sender, SetAddEventArgs e) {
            Console.Out.WriteLine("new tag is detected");
        }
    }
}
