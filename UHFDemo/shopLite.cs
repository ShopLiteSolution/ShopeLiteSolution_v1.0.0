using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.ComponentModel;
using System.Threading;
using System.Data;

namespace UHFDemo
{
    class shopLite : R2000UartDemo
    {
        private Reader.ReaderMethod reader;
        private ReaderSetting m_curSetting = new ReaderSetting();
        private InventoryBuffer m_curInventoryBuffer = new InventoryBuffer();
        private OperateTagBuffer m_curOperateTagBuffer = new OperateTagBuffer();
        private OperateTagISO18000Buffer m_curOperateTagISO18000Buffer = new OperateTagISO18000Buffer();

        //盘存操作前，需要先设置工作天线，用于标识当前是否在执行盘存操作
        private bool m_bInventory = false;
        //标识是否统计命令执行时间，当前仅盘存命令需要统计时间
        private bool m_bReckonTime = false;
        //实时盘存锁定操作
        private bool m_bLockTab = false;
        //ISO18000标签连续盘存标识
        private bool m_bContinue = false;
        //是否显示串口监控数据
        private bool m_bDisplayLog = false;
        //记录ISO18000标签循环写入次数
        private int m_nLoopTimes = 0;
        //记录ISO18000标签写入字符数
        private int m_nBytes = 0;
        //记录ISO18000标签已经循环写入次数
        private int m_nLoopedTimes = 0;
        //实时盘存次数
        private int m_nTotal = 0;
        //列表更新频率
        private int m_nRealRate = 20;
        //记录快速轮询天线参数
        private byte[] m_btAryData=new byte[10];
        //记录快速轮询总次数
        private int m_nSwitchTotal = 0;
        private int m_nSwitchTime = 0;

        public shopLite ()
        {
            reader = new Reader.ReaderMethod();
            reader.AnalyCallback = AnalyData;
            reader.ReceiveCallback = ReceiveData;
            reader.SendCallback = SendData;
            System.Console.Out.WriteLine("intialize reader method");


            ConnectTcp();
            ResetReader();
            SetOutputPower("20");
            //Thread.Sleep(10000);
            SetFrequencyRegion("fcc", 0, 52); // 0-52 is the index of frequency range
            //Thread.Sleep(6000);
        }

        public void shopLite_load(object sender, DoWorkEventArgs e)
        {
            System.Console.Out.WriteLine("RealTimeInventory");
            for (int i = 0; i < 10; i++) {
                RealTimeInventory();
                Thread.Sleep(3000); //NTBF
            }


            System.Console.Out.WriteLine("end of RealTimeInventory");

        }

        private void DisconnectTcp()
        {
            //处理断开Tcp连接读写器
            reader.SignOut();
            System.Console.WriteLine("reader signout");
        }

        public void ConnectTcp()
        {
            try
            {
                //处理Tcp连接读写器

                //ipIpServer.IpAddressStr = "192.168.0.178";
                //txtTcpPort.Text = "4001";

                string strException = string.Empty;
                //IPAddress ipAddress = IPAddress.Parse(ipIpServer.IpAddressStr);
                IPAddress ipAddress = IPAddress.Parse("192.168.0.178");
                //int nPort = Convert.ToInt32(txtTcpPort.Text);
                int nPort = Convert.ToInt32("4001");

                int nRet = reader.ConnectServer(ipAddress, nPort, out strException);
                if (nRet != 0)
                {
                    string strLog = "Connect reader failed, due to: " + strException;
                    //WriteLog(lrtxtLog, strLog, 1);
                    System.Console.WriteLine(strLog);

                    return;
                }
                else
                {
                    string strLog = "Reader connected  " + ipAddress.ToString() + "@" + nPort.ToString();
                    //WriteLog(lrtxtLog, strLog, 0);
                    System.Console.WriteLine(strLog);
                }

                //处理界面元素是否有效
                //SetFormEnable(true);
                //btnConnectTcp.Enabled = false;
                //btnDisconnectTcp.Enabled = true;

                //设置按钮字体颜色
                //btnConnectTcp.ForeColor = Color.Black;
                //btnDisconnectTcp.ForeColor = Color.Indigo;
                //SetButtonBold(btnConnectTcp);
                //SetButtonBold(btnDisconnectTcp);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex);
                //MessageBox.Show(ex.Message);
            }

        }

        private void ReceiveData(byte[] btAryReceiveData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btAryReceiveData, 0, btAryReceiveData.Length);
                System.Console.WriteLine(strLog);
            }
        }

        private void SendData(byte[] btArySendData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btArySendData, 0, btArySendData.Length);
                System.Console.WriteLine(strLog);
            }
        }

        private void SetOutputPower(string txtOutputPower)
        {
            try
            {
                System.Console.Out.WriteLine("Setting output power");
                if (txtOutputPower.Length != 0)
                {
                    reader.SetOutputPower(m_curSetting.btReadId, Convert.ToByte(txtOutputPower));
                    m_curSetting.btOutputPower = Convert.ToByte(txtOutputPower);
                }
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show(ex.Message);
                System.Console.Out.WriteLine(ex.Message);
            }

        }

        private void SetFrequencyRegion(string rdbRegion, int cmbFrequencyStart, int cmbFrequencyEnd)
        {
            try
            {
                System.Console.Out.WriteLine("Setting Frequency Region");
                byte btRegion = 0x00;
                byte btStartFreq = 0x00;
                byte btEndFreq = 0x00;

                int nStartIndex = cmbFrequencyStart;
                int nEndIndex = cmbFrequencyEnd;
                if (nEndIndex < nStartIndex)
                {
                    System.Console.Out.WriteLine("Spectrum definition is not valid, please see communication protocol sepcification");
                    return;
                }

                if (rdbRegion ==  "fcc")
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
                //MessageBox.Show(ex.Message);
                System.Console.Out.WriteLine(ex.Message);
            }
        }

        private void ResetReader()
        {
            int nRet = reader.Reset(m_curSetting.btReadId);
            if (nRet != 0)
            {
                string strLog = "Reset reader failed";
                System.Console.Out.WriteLine(strLog);
            }
            else
            {
                string strLog = "Reset reader";
                System.Console.Out.WriteLine(strLog);
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
                case 0x69:
                   // ProcessSetProfile(msgTran);
                    break;
                case 0x6A:
                    //ProcessGetProfile(msgTran);
                    break;
                case 0x71:
                    //ProcessSetUartBaudrate(msgTran);
                    break;
                case 0x72:
                    //ProcessGetFirmwareVersion(msgTran);
                    break;
                case 0x73:
                   // ProcessSetReadAddress(msgTran);
                    break;
                case 0x74:
                    ProcessSetWorkAntenna(msgTran);
                    break;
                case 0x75:
                    ////////////ProcessGetWorkAntenna(msgTran);
                    break;
                case 0x76:
                    ProcessSetOutputPower(msgTran);
                    break;
                case 0x77:
                    //////////////////ProcessGetOutputPower(msgTran);
                    break;
                case 0x78:
                    ////////////////ProcessSetFrequencyRegion(msgTran);
                    break;
                case 0x79:
                    ///////////////////ProcessGetFrequencyRegion(msgTran);
                    break;
                case 0x7A:
                   // ProcessSetBeeperMode(msgTran);
                    break;
                case 0x7B:
                    //ProcessGetReaderTemperature(msgTran);
                    break;
                case 0x7C:
                    //ProcessSetDrmMode(msgTran);
                    break;
                case 0x7D:
                   // ProcessGetDrmMode(msgTran);
                    break;
                case 0x7E:
                    //ProcessGetImpedanceMatch(msgTran);
                    break;
                case 0x60:
                   // ProcessReadGpioValue(msgTran);
                    break;
                case 0x61:
                   // ProcessWriteGpioValue(msgTran);
                    break;
                case 0x62:
                    //ProcessSetAntDetector(msgTran);
                    break;
                case 0x63:
                   // ProcessGetAntDetector(msgTran);
                    break;
                case 0x67:
                    //ProcessSetReaderIdentifier(msgTran);
                    break;
                case 0x68:
                    //ProcessGetReaderIdentifier(msgTran);
                    break;

                case 0x80:
                   ////////////////////// ProcessInventory(msgTran);
                    break;
                case 0x81:
                    //ProcessReadTag(msgTran);
                    break;
                case 0x82:
                    //ProcessWriteTag(msgTran);
                    break;
                case 0x83:
                    //ProcessLockTag(msgTran);
                    break;
                case 0x84:
                   // ProcessKillTag(msgTran);
                    break;
                case 0x85:
                   // ProcessSetAccessEpcMatch(msgTran);
                    break;
                case 0x86:
                   // ProcessGetAccessEpcMatch(msgTran);
                    break;

                case 0x89:
                case 0x8B:
                    ProcessInventoryReal(msgTran);
                    break;
                case 0x8A:
                   // ProcessFastSwitch(msgTran);
                    break;
                case 0x8D:
                  //  ProcessSetMonzaStatus(msgTran);
                    break;
                case 0x8E:
                  //  ProcessGetMonzaStatus(msgTran);
                    break;
                case 0x90:
                  //  ProcessGetInventoryBuffer(msgTran);
                    break;
                case 0x91:
                   // ProcessGetAndResetInventoryBuffer(msgTran);
                    break;
                case 0x92:
                   // ProcessGetInventoryBufferTagCount(msgTran);
                    break;
                case 0x93:
                   // ProcessResetInventoryBuffer(msgTran);
                    break;
                case 0xb0:
                   // ProcessInventoryISO18000(msgTran);
                    break;
                case 0xb1:
                   // ProcessReadTagISO18000(msgTran);
                    break;
                case 0xb2:
                   // ProcessWriteTagISO18000(msgTran);
                    break;
                case 0xb3:
                   // ProcessLockTagISO18000(msgTran);
                    break;
                case 0xb4:
                    //ProcessQueryISO18000(msgTran);
                    break;
                default:
                    break;
            }
        }

        private void ProcessSetOutputPower(Reader.MessageTran msgTran)
        {
            System.Console.Out.WriteLine("ProcessSetOutputPower");
            string strCmd = "Set RF output power ";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    //WriteLog(lrtxtLog, strCmd, 0);
                    System.Console.Out.WriteLine(strCmd);


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
            System.Console.Out.WriteLine(strLog);
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

                    //校验是否盘存操作
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

        private void ProcessInventoryReal(Reader.MessageTran msgTran)
        {
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

                //WriteLog(lrtxtLog, strLog, 1);
                System.Console.Out.WriteLine(strLog);
                RefreshInventoryReal(0x00);
                RunLoopInventroy();
            }
            else if (msgTran.AryData.Length == 7)
            {
                m_curInventoryBuffer.nReadRate = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                m_curInventoryBuffer.nDataCount = Convert.ToInt32(msgTran.AryData[3]) * 256 * 256 * 256 + Convert.ToInt32(msgTran.AryData[4]) * 256 * 256 + Convert.ToInt32(msgTran.AryData[5]) * 256 + Convert.ToInt32(msgTran.AryData[6]);

                //WriteLog(lrtxtLog, strCmd, 0);
                System.Console.Out.WriteLine(strCmd);
                RefreshInventoryReal(0x01);
                RunLoopInventroy();
            }
            else
            {
                m_nTotal++;
                int nLength = msgTran.AryData.Length;
                int nEpcLength = nLength - 4;

                //增加盘存明细表
                //if (msgTran.AryData[3] == 0x00)
                //{
                //    MessageBox.Show("");
                //}
                string strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, nEpcLength);
                System.Console.Out.WriteLine("StrEPC = " + strEPC);
                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 2);
                string strRSSI = msgTran.AryData[nLength - 1].ToString();
                //SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nLength - 1]));
                byte btTemp = msgTran.AryData[0];
                byte btAntId = (byte)((btTemp & 0x03) + 1);
                m_curInventoryBuffer.nCurrentAnt = btAntId;
                string strAntId = btAntId.ToString();

                byte btFreq = (byte)(btTemp >> 2);
                //string strFreq = GetFreqString(btFreq);

                //DataRow row = m_curInventoryBuffer.dtTagDetailTable.NewRow();
                //row[0] = strEPC;
                //row[1] = strRSSI;
                //row[2] = strAntId;
                //row[3] = strFreq;

                //m_curInventoryBuffer.dtTagDetailTable.Rows.Add(row);
                //m_curInventoryBuffer.dtTagDetailTable.AcceptChanges();

                ////增加标签表
                //DataRow[] drsDetail = m_curInventoryBuffer.dtTagDetailTable.Select(string.Format("COLEPC = '{0}'", strEPC));
                //int nDetailCount = drsDetail.Length;
                /*
                 DataRow[] drs = m_curInventoryBuffer.dtTagTable.Select(string.Format("COLEPC = '{0}'", strEPC));
                
                 if (drs.Length == 0)
                 {
                     DataRow row1 = m_curInventoryBuffer.dtTagTable.NewRow();
                     row1[0] = strPC;
                     row1[2] = strEPC;
                     row1[4] = strRSSI;
                     row1[5] = "1";
                     row1[6] = strFreq;

                     m_curInventoryBuffer.dtTagTable.Rows.Add(row1);
                     m_curInventoryBuffer.dtTagTable.AcceptChanges();
                 }
                 else
                 {
                     foreach (DataRow dr in drs)
                     {
                         dr.BeginEdit();

                         dr[4] = strRSSI;
                         dr[5] = (Convert.ToInt32(dr[5]) + 1).ToString();
                         dr[6] = strFreq;

                         dr.EndEdit();
                     }
                     m_curInventoryBuffer.dtTagTable.AcceptChanges();
                 }
                 */
                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RefreshInventoryReal(0x89);
            }
        }
        private void RefreshInventoryReal(byte btCmd)
        {
            if (this.InvokeRequired)
            {
               // RefreshInventoryRealUnsafe InvokeRefresh = new RefreshInventoryRealUnsafe(RefreshInventoryReal);
               // this.Invoke(InvokeRefresh, new object[] { btCmd });
            }
            else
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

                            if (m_curInventoryBuffer.nReadRate == 0) //读写器没有返回速度前软件测速度
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

                            /*
                            //列表用变量
                            int nEpcCount = 0;
                            int nEpcLength = m_curInventoryBuffer.dtTagTable.Rows.Count;

                            ledReal1.Text = nTagCount.ToString();
                            ledReal2.Text = nCaculatedReadRate.ToString();

                            ledReal5.Text = nTotalTime.ToString();
                            ledReal3.Text = nTotalRead.ToString();
                            ledReal4.Text = nCommandDuation.ToString();  //实际的命令执行时间
                            tbRealMaxRssi.Text = (m_curInventoryBuffer.nMaxRSSI - 129).ToString() + "dBm";
                            tbRealMinRssi.Text = (m_curInventoryBuffer.nMinRSSI - 129).ToString() + "dBm";
                            lbRealTagCount.Text = "Tag List: " + nTagCount.ToString();

                            nEpcCount = lvRealList.Items.Count;


                            if (nEpcCount < nEpcLength)
                            {
                                DataRow row = m_curInventoryBuffer.dtTagTable.Rows[nEpcLength - 1];

                                ListViewItem item = new ListViewItem();
                                item.Text = (nEpcCount + 1).ToString();
                                item.SubItems.Add(row[2].ToString());
                                item.SubItems.Add(row[0].ToString());
                                item.SubItems.Add(row[5].ToString());
                                item.SubItems.Add((Convert.ToInt32(row[4]) - 129).ToString() + "dBm");
                                item.SubItems.Add(row[6].ToString());
                                lvRealList.Items.Add(item);
                                lvRealList.Items[nEpcCount].EnsureVisible();
                            }
                            //else
                            //{
                            //    int nIndex = 0;
                            //    foreach (DataRow row in m_curInventoryBuffer.dtTagTable.Rows)
                            //    {
                            //        ListViewItem item = ltvInventoryEpc.Items[nIndex];
                            //        item.SubItems[3].Text = row[5].ToString();
                            //        nIndex++;
                            //    }
                            //}

                            //更新列表中读取的次数
                            if (m_nTotal % m_nRealRate == 1)
                            {
                                int nIndex = 0;
                                foreach (DataRow row in m_curInventoryBuffer.dtTagTable.Rows)
                                {
                                    ListViewItem item;
                                    item = lvRealList.Items[nIndex];
                                    item.SubItems[3].Text = row[5].ToString();
                                    item.SubItems[4].Text = (Convert.ToInt32(row[4]) - 129).ToString() + "dBm";
                                    item.SubItems[5].Text = row[6].ToString();

                                    nIndex++;
                                }
                            }

                            //if (ltvInventoryEpc.SelectedIndices.Count != 0)
                            //{
                            //    int nDetailCount = ltvInventoryTag.Items.Count;
                            //    int nDetailLength = m_curInventoryBuffer.dtTagDetailTable.Rows.Count;

                            //    foreach (int nIndex in ltvInventoryEpc.SelectedIndices)
                            //    {
                            //        ListViewItem itemEpc = ltvInventoryEpc.Items[nIndex];
                            //        DataRow row = m_curInventoryBuffer.dtTagDetailTable.Rows[nDetailLength - 1];
                            //        if (itemEpc.SubItems[1].Text == row[0].ToString())
                            //        {
                            //            ListViewItem item = new ListViewItem();
                            //            item.Text = (nDetailCount + 1).ToString();
                            //            item.SubItems.Add(row[0].ToString());

                            //            string strTemp = (Convert.ToInt32(row[1].ToString()) - 129).ToString() + "dBm";
                            //            item.SubItems.Add(strTemp);
                            //            byte byTemp = Convert.ToByte(row[1]);
                            //            if (byTemp > 0x50)
                            //            {
                            //                item.BackColor = Color.PowderBlue;
                            //            }
                            //            else if (byTemp < 0x30)
                            //            {
                            //                item.BackColor = Color.LemonChiffon;
                            //            }

                            //            item.SubItems.Add(row[2].ToString());
                            //            item.SubItems.Add(row[3].ToString());

                            //            ltvInventoryTag.Items.Add(item);
                            //            ltvInventoryTag.Items[nDetailCount].EnsureVisible();
                            //        }
                            //    }
                            //}
                            //else
                            //{
                            //    int nDetailCount = ltvInventoryTag.Items.Count;
                            //    int nDetailLength = m_curInventoryBuffer.dtTagDetailTable.Rows.Count;

                            //    DataRow row = m_curInventoryBuffer.dtTagDetailTable.Rows[nDetailLength - 1];
                            //    ListViewItem item = new ListViewItem();
                            //    item.Text = (nDetailCount + 1).ToString();
                            //    item.SubItems.Add(row[0].ToString());

                            //    string strTemp = (Convert.ToInt32(row[1].ToString()) - 129).ToString() + "dBm";
                            //    item.SubItems.Add(strTemp);
                            //    byte byTemp = Convert.ToByte(row[1]);
                            //    if (byTemp > 0x50)
                            //    {
                            //        item.BackColor = Color.PowderBlue;
                            //    }
                            //    else if (byTemp < 0x30)
                            //    {
                            //        item.BackColor = Color.LemonChiffon;
                            //    }

                            //    item.SubItems.Add(row[2].ToString());
                            //    item.SubItems.Add(row[3].ToString());

                            //    ltvInventoryTag.Items.Add(item);
                            //    ltvInventoryTag.Items[nDetailCount].EnsureVisible();
                            //}

                            */
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
            if (this.InvokeRequired)
            {
                RunLoopInventoryUnsafe InvokeRunLoopInventory = new RunLoopInventoryUnsafe(RunLoopInventroy);
                this.Invoke(InvokeRunLoopInventory, new object[] { });
            }
            else
            {
                //校验盘存是否所有天线均完成
                if (m_curInventoryBuffer.nIndexAntenna < m_curInventoryBuffer.lAntenna.Count - 1 || m_curInventoryBuffer.nCommond == 0)
                {
                    if (m_curInventoryBuffer.nCommond == 0)
                    {
                        m_curInventoryBuffer.nCommond = 1;

                        if (m_curInventoryBuffer.bLoopInventoryReal)
                        {
                            //m_bLockTab = true;
                            //btnInventory.Enabled = false;
                            if (m_curInventoryBuffer.bLoopCustomizedSession)//自定义Session和Inventoried Flag 
                            {
                                reader.CustomizedInventory(m_curSetting.btReadId, m_curInventoryBuffer.btSession, m_curInventoryBuffer.btTarget, m_curInventoryBuffer.btRepeat);
                            }
                            else //实时盘存
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
                //校验是否循环盘存
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

        private void RealTimeInventory()
        {
            try
            {
                m_curInventoryBuffer.ClearInventoryPar();

                //repeat per commands 
                
                string textRealRound = "1";
                m_curInventoryBuffer.btRepeat = Convert.ToByte(textRealRound);

/*
                if (textRealRound.Text.Length == 0)
                {
                    MessageBox.Show("Please input repeat per command ");
                    return;
                }
                m_curInventoryBuffer.btRepeat = Convert.ToByte(textRealRound.Text);

                if (cbRealSession.Checked == true)
                {
                    if (cmbSession.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select a session ID");
                        return;
                    }
                    if (cmbTarget.SelectedIndex == -1)
                    {
                        MessageBox.Show("Please select an inventoried flag");
                        return;
                    }
                    m_curInventoryBuffer.bLoopCustomizedSession = true;
                    m_curInventoryBuffer.btSession = (byte)cmbSession.SelectedIndex;
                    m_curInventoryBuffer.btTarget = (byte)cmbTarget.SelectedIndex;
                }
                else
                {
                    m_curInventoryBuffer.bLoopCustomizedSession = false;
                }
                */
                m_curInventoryBuffer.bLoopCustomizedSession = false;

                //NTBF (need to be fixed)
                //cmbSession.SelectedIndex = 0;
                //cmbTarget.SelectedIndex = 0;
                //m_curInventoryBuffer.btSession = (byte)cmbSession.SelectedIndex;
                //m_curInventoryBuffer.btTarget = (byte)cmbTarget.SelectedIndex;

                //Add antennas
                m_curInventoryBuffer.lAntenna.Add(0x00);
                /*
                if (cbRealWorkant1.Checked)
                {
                    m_curInventoryBuffer.lAntenna.Add(0x00);
                }
                if (cbRealWorkant2.Checked)
                {
                    m_curInventoryBuffer.lAntenna.Add(0x01);
                }
                if (cbRealWorkant3.Checked)
                {
                    m_curInventoryBuffer.lAntenna.Add(0x02);
                }
                if (cbRealWorkant4.Checked)
                {
                    m_curInventoryBuffer.lAntenna.Add(0x03);
                }
                if (m_curInventoryBuffer.lAntenna.Count == 0)
                {
                    MessageBox.Show("Please select at least one antenna ");
                    return;
                }
                 */
                //默认循环发送命令
                if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_bInventory = false;
                    m_curInventoryBuffer.bLoopInventory = false;
                    //btRealTimeInventory.BackColor = Color.WhiteSmoke;
                    //btRealTimeInventory.ForeColor = Color.DarkBlue;
                    //btRealTimeInventory.Text = "Inventory ";
                    return;
                }
                else
                {
                    /*
                    //ISO 18000-6B盘存是否正在运行
                    if (m_bContinue)
                    {
                        if (MessageBox.Show("ISO 18000-6B inventory is ongoing, stop it? ", "Note ", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
                        {
                            return;
                        }
                        else
                        {
                            btnInventoryISO18000_Click(sender, e);
                            return;
                        }
                    }
                    */
                    m_bInventory = true;
                    m_curInventoryBuffer.bLoopInventory = true;
                    //btRealTimeInventory.BackColor = Color.DarkBlue;
                    //btRealTimeInventory.ForeColor = Color.White;
                    //btRealTimeInventory.Text = "Stop";
                }

                m_curInventoryBuffer.bLoopInventoryReal = true;

                m_curInventoryBuffer.ClearInventoryRealResult();
                //lvRealList.Items.Clear();
                //lvRealList.Items.Clear();
                //tbRealMaxRssi.Text = "0";
                //tbRealMinRssi.Text = "0";
                m_nTotal = 0;


                byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                m_curSetting.btWorkAntenna = btWorkAntenna;

            }
            catch (System.Exception ex)
            {
                //MessageBox.Show(ex.Message);
                System.Console.Out.WriteLine(ex.Message);
            }


        }

    }
}
