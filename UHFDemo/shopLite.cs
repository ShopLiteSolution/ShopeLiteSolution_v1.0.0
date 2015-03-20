using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.ComponentModel;

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
        private bool m_bDisplayLog = true;
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
            //SetFrequencyRegion("fcc", 0, 52); // 0-52 is the index of frequency range
        }

        public void shopLite_load(object sender, DoWorkEventArgs e)
        {

            System.Console.Out.WriteLine("end of load");

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
                    /////////////////ProcessSetWorkAntenna(msgTran);
                    break;
                case 0x75:
                    ////////////ProcessGetWorkAntenna(msgTran);
                    break;
                case 0x76:
                    //ProcessSetOutputPower(msgTran);

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
                  //  ProcessInventoryReal(msgTran);
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



    }
}
