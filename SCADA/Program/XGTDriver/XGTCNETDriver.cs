using DataService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace XGTDriver
{
    [Description("XGT CNET Protocol")]
    public sealed class XGTCNETDriver : IPLCDriver
    {
        #region :IDriver
        //从站地址
        short _id;
        public short ID
        {
            get
            {
                return _id;
            }
        }

        string _name;
        public string Name
        {
            get
            {
                return _name;
            }
        }

        string _port = "COM1";
        [Category("串口设置"), Description("串口号")]
        public string PortName
        {
            get { return _port; }
            set { _port = value; }
        }

        public bool IsClosed
        {
            get
            {
                return _serialPort == null || _serialPort.IsOpen == false;
            }
        }

        private int _timeOut = 3000;
        [Category("串口设置"), Description("通迅超时时间")]
        public int TimeOut
        {
            get { return _timeOut; }
            set { _timeOut = value; }
        }


        private int _baudRate = 9600;
        [Category("串口设置"), Description("波特率")]
        public int BaudRate
        {
            get { return _baudRate; }
            set { _baudRate = value; }
        }
        //   private SerialPort _serialPort;

        private int _dataBits = 8;
        [Category("串口设置"), Description("数据位")]
        public int DataBits
        {
            get { return _dataBits; }
            set { _dataBits = value; }
        }
        private StopBits _stopBits = StopBits.One;
        [Category("串口设置"), Description("停止位")]
        public StopBits StopBits
        {
            get { return _stopBits; }
            set { _stopBits = value; }
        }

        private Parity _parity = Parity.None;
        [Category("串口设置"), Description("奇偶校验")]
        public Parity Parity
        {
            get { return _parity; }
            set { _parity = value; }
        }

        List<IGroup> _grps = new List<IGroup>();
        public IEnumerable<IGroup> Groups
        {
            get { return _grps; }
        }

        IDataServer _server;
        public IDataServer Parent
        {
            get { return _server; }
        }

        public bool Connect()
        {
            try
            {
                if (_timeOut <= 0) _timeOut = 1000;
                if (_serialPort == null)
                    _serialPort = new SerialPort(_port);
                _serialPort.ReadTimeout = _timeOut;
                _serialPort.WriteTimeout = _timeOut;
                _serialPort.BaudRate = _baudRate;
                _serialPort.DataBits = _dataBits;
                _serialPort.Parity = _parity;
                _serialPort.StopBits = _stopBits;
                _serialPort.Open();
                return true;
            }
            catch (IOException error)
            {
                if (OnError != null)
                {
                    OnError(this, new IOErrorEventArgs(error.Message));
                }
                return false;
            }
        }

        public IGroup AddGroup(string name, short id, int updateRate, float deadBand = 0f, bool active = false)
        {
            ShortGroup grp = new ShortGroup(id, name, updateRate, active, this);
            _grps.Add(grp);
            return grp;
        }

        public bool RemoveGroup(IGroup grp)
        {
            grp.IsActive = false;
            return _grps.Remove(grp);
        }
        public event IOErrorEventHandler OnError;
        #endregion
        public XGTCNETDriver(IDataServer server, short id, string name)
        {
            _id = id;
            _name = name;
            _server = server;
        }

        private SerialPort _serialPort;
        #region Write
        public int WriteSingleCoils(int id, int startAddress, bool OnOff)
        {
            byte[] frame = XGTCnetMessage.WriteSingleCoilMessage(Convert.ToByte(id), Convert.ToString(startAddress), OnOff);
            lock (_async)
            {
                _serialPort.Write(frame, 0, frame.Length);
                int numBytesRead = 0;
                var frameBytes = new byte[5];
                while (numBytesRead != frameBytes.Length)
                    numBytesRead += _serialPort.Read(frameBytes, numBytesRead, frameBytes.Length - numBytesRead);
                var slave = frameBytes[0];
                var code = frameBytes[1];
                if (code == 0x85)//错误则只需读5字节
                {
                    var errorcode = frameBytes[2];
                    if (OnError != null)
                    {
                       // OnError(this, new IOErrorEventArgs(Modbus.GetErrorString(errorcode)));
                    }
                    Thread.Sleep(10);
                    return -1;
                }
                else//正确需8字节
                {
                    numBytesRead = 0;
                    while (numBytesRead < 3)
                        numBytesRead += _serialPort.Read(frameBytes, numBytesRead, 3 - numBytesRead);
                    Thread.Sleep(10);
                    return 0;
                }
            }
        }
        #region WriteSingleRegister:0x06
        public int WriteSingleRegister(int id, int startAddress, byte[] values)
        {
            
            lock (_async)
            {
                byte[] frame = XGTCnetMessage.WriteSingleRegisterMessage(Convert.ToByte(id), Convert.ToString(startAddress), values);
                _serialPort.Write(frame, 0, frame.Length);
                Thread.Sleep(100);
                
                int numBytesRead = 0;
                var frameBytes = new byte[5];
                while (numBytesRead != frameBytes.Length)
                    numBytesRead += _serialPort.Read(frameBytes, numBytesRead, frameBytes.Length - numBytesRead);
                var slave = frameBytes[0];
                var code = frameBytes[1];
                if (code == 0x85)//错误则只需读5字节
                {
                    var errorcode = frameBytes[2];
                    if (OnError != null)
                    {
                       // OnError(this, new IOErrorEventArgs(Modbus.GetErrorString(errorcode)));
                    }
                    Thread.Sleep(10);
                    return -1;
                }
                else//正确需8字节
                {
                    numBytesRead = 0;
                    while (numBytesRead < 3)
                        numBytesRead += _serialPort.Read(frameBytes, numBytesRead, 3 - numBytesRead);
                    Thread.Sleep(10);
                    return 0;
                }
            }
        }
        #endregion

           #endregion

        #region  :IPLCDriver
        public int PDU
        {
             get { return 255; } 
        }
        private string _serverName = "unknown";
        public string ServerName
        {
            get { return _serverName; }
            set { _serverName = value; }
        }

        public DeviceAddress GetDeviceAddress(string address)
        {
            DeviceAddress dv = DeviceAddress.Empty;
            if (string.IsNullOrEmpty(address))
                return dv;
            var sindex = address.IndexOf(':');
            if (sindex > 0)
            {
                int slaveId;
                if (int.TryParse(address.Substring(0, sindex), out slaveId))
                    dv.Area = slaveId;
                address = address.Substring(sindex + 1);
            }
            switch (address[0])
            {
                case 'M':
                    {
                        dv.DBNumber = 0;
                        int st;
                        int.TryParse(address, out st);
                        dv.Bit = (byte)(st % 16);
                        st /= 16;
                        dv.Start = st;
                        dv.Bit--;
                    }
                    break;
                case 'P':
                    {
                        dv.DBNumber = 1;
                        int st;
                        int.TryParse(address.Substring(1), out st);
                        dv.Bit = (byte)(st % 16);
                        st /= 16;
                        dv.Start = st;
                        dv.Bit--;
                    }
                    break;
                case 'D':
                    {
                        int index = address.IndexOf('.');
                        dv.DBNumber = 4;
                        if (index > 0)
                        {
                            dv.Start = int.Parse(address.Substring(1, index - 1));
                            dv.Bit = byte.Parse(address.Substring(index + 1));
                        }
                        else
                            dv.Start = int.Parse(address.Substring(1));
                        dv.Start--;
                        dv.Bit--;
                        dv.ByteOrder = ByteOrder.BigEndian;
                    }
                    break;
                case '3':
                    {
                        int index = address.IndexOf('.');
                        dv.DBNumber = 3;
                        if (index > 0)
                        {
                            dv.Start = int.Parse(address.Substring(1, index - 1));
                            dv.Bit = byte.Parse(address.Substring(index + 1));
                        }
                        else
                            dv.Start = int.Parse(address.Substring(1));
                        dv.Start--;
                        dv.Bit--;
                        dv.ByteOrder = ByteOrder.BigEndian;
                    }
                    break;
            }
            return dv;
        }

        public string GetAddress(DeviceAddress address)
        {
            return string.Empty;
        }
        #endregion
       
       
        #region :IReaderWriter
        object _async = new object();
        public byte[] ReadBytes(DeviceAddress address, ushort size)
        {
            var func = (byte)address.DBNumber;
           string tempStrg = string.Empty;
           string buffReceiver = string.Empty;
            try
            {
               
                 lock (_async)
                {
                    switch (func)
                    {
                        case 0:

                            byte[] frame = XGTCnetMessage.ReadCoilStatusMessage(Convert.ToByte(address.Area), "MB", Convert.ToString(address.Start), size);
                            _serialPort.Write(frame, 0, frame.Length);
                            Thread.Sleep(100);
                        
                            break;
                        case 1:
                            byte[] frame1 = XGTCnetMessage.ReadInputStatusMessage(Convert.ToByte(address.Area), "PB", Convert.ToString(address.Start), size);
                            _serialPort.Write(frame1, 0, frame1.Length);
                            Thread.Sleep(100);
                            break;
                        case 4:

                            byte[] frame4 = XGTCnetMessage.ReadHoldingRegistersMessage(Convert.ToByte(address.Area), "DW", Convert.ToString(address.Start), size);
                            _serialPort.Write(frame4, 0, frame4.Length);
                            Thread.Sleep(100);
                           
                            break;
                        default:
                            break;
                    }
                    if (this._serialPort.BytesToRead >= 10)
                    {
                        buffReceiver = this._serialPort.ReadExisting();
                        this._serialPort.DiscardInBuffer();

                    }
                    if (buffReceiver != null)
                    {
                    tempStrg = buffReceiver.Substring(1, buffReceiver.Length - 2);
                    tempStrg = tempStrg.Remove(0, 9);
                    return Conversion.HexToBytes(tempStrg);
                    }
                  
                  

                }
                return null;
            }
            catch (Exception e)
            {
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(e.Message));
                return null;
            }
        }

        public ItemData<int> ReadInt32(DeviceAddress address)
        {
            byte[] bit = ReadBytes(address, 2);
            return bit == null ? new ItemData<int>(0, 0, QUALITIES.QUALITY_BAD) :
                new ItemData<int>(BitConverter.ToInt32(bit, 0), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<uint> ReadUInt32(DeviceAddress address)
        {
            byte[] bit = ReadBytes(address, 2);
            return bit == null ? new ItemData<uint>(0, 0, QUALITIES.QUALITY_BAD) :
                new ItemData<uint>(BitConverter.ToUInt32(bit, 0), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<ushort> ReadUInt16(DeviceAddress address)
        {
            byte[] bit = ReadBytes(address, 1);
            return bit == null ? new ItemData<ushort>(0, 0, QUALITIES.QUALITY_BAD) :
                new ItemData<ushort>(BitConverter.ToUInt16(bit, 0), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<short> ReadInt16(DeviceAddress address)
        {
            byte[] bit = ReadBytes(address, 1);
            return bit == null ? new ItemData<short>(0, 0, QUALITIES.QUALITY_BAD) :
                new ItemData<short>(BitConverter.ToInt16(bit, 0), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<byte> ReadByte(DeviceAddress address)
        {
            byte[] bit = ReadBytes(address, 1);
            return bit == null ? new ItemData<byte>(0, 0, QUALITIES.QUALITY_BAD) :
                 new ItemData<byte>(bit[0], 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<string> ReadString(DeviceAddress address, ushort size)
        {
            byte[] bit = ReadBytes(address, size);
            return bit == null ? new ItemData<string>(string.Empty, 0, QUALITIES.QUALITY_BAD) :
                new ItemData<string>(Encoding.ASCII.GetString(bit), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<float> ReadFloat(DeviceAddress address)
        {
            byte[] bit = ReadBytes(address, 2);
            return bit == null ? new ItemData<float>(0f, 0, QUALITIES.QUALITY_BAD) :
                new ItemData<float>(BitConverter.ToSingle(bit, 0), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<bool> ReadBit(DeviceAddress address)
        {
            byte[] bit = ReadBytes(address, 1);
            return bit == null ? new ItemData<bool>(false, 0, QUALITIES.QUALITY_BAD) :
                 new ItemData<bool>((bit[0] & (1 << (address.Bit))) > 0, 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<object> ReadValue(DeviceAddress address)
        {
            return this.ReadValueEx(address);
        }

        public int WriteBytes(DeviceAddress address, byte[] bit)
        {
            throw new NotImplementedException();
        }

        public int WriteBit(DeviceAddress address, bool bit)
        {
            return WriteSingleCoils(address.Area, address.Start + address.Bit, bit);
        }

        public int WriteBits(DeviceAddress address, byte bits)
        {
            return WriteSingleRegister(address.Area, address.Start, new byte[] { bits });
        }

        public int WriteInt16(DeviceAddress address, short value)
        {
            return WriteSingleRegister(address.Area, address.Start, BitConverter.GetBytes(value));
        }

        public int WriteUInt16(DeviceAddress address, ushort value)
        {
            return WriteSingleRegister(address.Area, address.Start, BitConverter.GetBytes(value));
        }

        public int WriteUInt32(DeviceAddress address, uint value)
        {
            throw new NotImplementedException();
        }

        public int WriteInt32(DeviceAddress address, int value)
        {
            throw new NotImplementedException();
        }

        public int WriteFloat(DeviceAddress address, float value)
        {
            throw new NotImplementedException();
        }

        public int WriteString(DeviceAddress address, string str)
        {
            throw new NotImplementedException();
        }

        public int WriteValue(DeviceAddress address, object value)
        {
            return this.WriteValueEx(address, value);
        }

        #endregion

        #region : IDisposable

        public void Dispose()
        {
            foreach (IGroup grp in _grps)
            {
                grp.Dispose();
            }
            _grps.Clear();
            _serialPort.Close();
        }
        #endregion
    }
    public sealed  class XGTCnetMessage
    {
        public static int commCountNeed;
        public static int commCountCurr;
        public static byte[] commRecvBuf = new byte[MAX_RECV_BUF + 1];
        public static byte[] commSendBuf = new byte[MAX_SEND_BUF + 1];
        public static int commCountSend;
        public static  int SOH = 0x1;
        public static  int STX = 0x2; // start of text
        public static  int ETX = 0x3; // end of text
        public static  int EOT = 0x4; // end of transmission
        public static  int ENQ = 0x5; // enquiry
        public static  int ACK = 0x6; // acknowledge
        public static  int LF = 0xA; // line feed
        public static  int CR = 0xD; // carriage return
        public static  int DLE = 0x10;
        public static  int NAK = 0x15; // negative acknowledge
        public static  int MAX_SEND_BUF = 256;
        public static  int MAX_RECV_BUF = 1024;
        public static byte ByteCheckSum(string strData)
        {
            int i = 0;
            int CheckSum = 0;
            int Length = 0;

            Length = strData.Length;

            CheckSum = 0;
            for (i = 1; i <= Length; i++)
            {
                CheckSum = CheckSum + Microsoft.VisualBasic.Strings.Asc(strData.Substring(i - 1, 1));
                if (CheckSum > 255)
                {
                    CheckSum = CheckSum - 256;
                }
            }

            return Convert.ToByte(CheckSum);
        }
        // BYTE DATA¸ 
        public static string ByteToHexStr(byte byData)
        {

            string strHex = "";

            strHex = Convert.ToString(byData, 16).ToUpper();

            if (strHex.Length < 2)
            {
                strHex = "0" + strHex;
            }

            return strHex;

        }

        public enum DTYPE
        {
            DATA_TYPE_BIT,
            DATA_TYPE_WORD,
            DATA_TYPE_DWORD
        } // WORD ' WORD

        public static int GetDataType(string Type, ref int data_type)
        {
            if (string.Compare(Type, "PW") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_WORD);
            }
            else if (string.Compare(Type, "MW") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_WORD);
            }
            else if (string.Compare(Type, "LW") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_WORD);
            }
            else if (string.Compare(Type, "KW") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_WORD);
            }
            else if (string.Compare(Type, "FW") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_WORD);
            }
            else if (string.Compare(Type, "TW") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_WORD);
            }
            else if (string.Compare(Type, "CW") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_WORD);
            }
            else if (string.Compare(Type, "DW") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_WORD);
            }
            else if (string.Compare(Type, "SW") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_WORD);

            }
            else if (string.Compare(Type, "PX") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_BIT);
            }
            else if (string.Compare(Type, "MX") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_BIT);
            }
            else if (string.Compare(Type, "LX") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_BIT);
            }
            else if (string.Compare(Type, "KX") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_BIT);
            }
            else if (string.Compare(Type, "FX") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_BIT);
            }
            else if (string.Compare(Type, "TX") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_BIT);
            }
            else if (string.Compare(Type, "CX") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_BIT);

            }
            else if (string.Compare(Type, "PD") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_DWORD);
            }
            else if (string.Compare(Type, "MD") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_DWORD);
            }
            else if (string.Compare(Type, "LD") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_DWORD);
            }
            else if (string.Compare(Type, "KD") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_DWORD);
            }
            else if (string.Compare(Type, "FD") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_DWORD);
            }
            else if (string.Compare(Type, "TD") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_DWORD);
            }
            else if (string.Compare(Type, "CD") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_DWORD);
            }
            else if (string.Compare(Type, "DD") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_DWORD);
            }
            else if (string.Compare(Type, "SD") == 0)
            {
                data_type = Convert.ToInt32(DTYPE.DATA_TYPE_DWORD);
            }
            else
            {
                return 0;
            }

            return 1;
        }

        public static byte[] Read(int station, ref string type, int address, int size, int datatype, ref int CountSend, ref int CountNeed)
        {
            byte[] commSendBuf = new byte[MAX_SEND_BUF + 1];
            string imsi = new string(new char[80]);

            commSendBuf[0] = (byte) ENQ; // STX
            Encoding.ASCII.GetBytes(string.Format("{0:X2}", station)).CopyTo(commSendBuf, 1); // status
            Encoding.ASCII.GetBytes("R").CopyTo(commSendBuf, 3); // status
            Encoding.ASCII.GetBytes("SB").CopyTo(commSendBuf, 4); // mode
            imsi = string.Format("%{0}{1:D4}", type, address);
            Encoding.ASCII.GetBytes(string.Format("{0:X2}", imsi.Length)).CopyTo(commSendBuf, 6);
            Encoding.ASCII.GetBytes(string.Format("{0}", imsi)).CopyTo(commSendBuf, 8);
            commCountSend = 8 + imsi.Length;
            Encoding.ASCII.GetBytes(string.Format("{0:X2}", size)).CopyTo(commSendBuf, commCountSend); // module no
            commCountSend += 2;
            commSendBuf[commCountSend] =(byte) EOT; // STX
            commCountSend += 1;
            CountSend = commCountSend;
            byte[] SendBuf = new byte[commCountSend];
            Array.Copy(commSendBuf, 0, SendBuf, 0, SendBuf.Length);

            if (datatype == 1)
            {
                commCountNeed = 13 + size * 4; // STX+COMMAND+STATION+ADDRESS+SIZE+DATA+CRC+ETX
            }
            else
            {
                commCountNeed = 13 + size * 8; // STX+COMMAND+STATION+ADDRESS+SIZE+DATA+CRC+ETX
            }
            CountNeed = commCountNeed;
            return SendBuf;
        }
        public static byte[] Write(byte slaveAddress, ushort startAddress, string functionCode, byte[] values)
        {
            string DataBlockName = functionCode + startAddress;
            short temp = 0;
            byte Bcc = 0;
            int iBcc = 0;
            string sBcc = "";
            string frame = string.Empty;
            switch (functionCode)
            {
                case "MX":
                    if (values[0] == 0xFF)
                    {
                        temp = 1;
                    }
                    else
                    {
                        temp = 0;
                    }
                    frame += string.Format("{0:X2}", slaveAddress);
                    frame += string.Format("WSS01{0}%{1}", string.Format("{0:X2}", DataBlockName.Length + 1), DataBlockName);
                    //frame += string.Format("{0:X2}", startAddress);
                    frame += string.Format("{0:X2}", temp);


                    // BCC
                    iBcc = 5 + ByteCheckSum(frame) + 4;
                    if (iBcc > 255)
                    {
                        iBcc = iBcc - 256;
                    }
                    Bcc = Convert.ToByte(iBcc);
                    sBcc = ByteToHexStr(Bcc);

                    break;
                case "DW":
                    string Data = string.Empty;
                    foreach (byte item in values)
                    {
                        Data += String.Format("{0:X2} ", item);
                    }

                    frame += string.Format("{0:X2}", slaveAddress);
                    frame += string.Format("WSB{0}%{1}", string.Format("{0:X2}", DataBlockName.Length + 1), DataBlockName);
                    frame += "01" + Data.Replace(" ", "");

                    // BCC
                    iBcc = 5 + ByteCheckSum(frame) + 4;
                    if (iBcc > 255)
                    {
                        iBcc = iBcc - 256;
                    }
                    Bcc = Convert.ToByte(iBcc);
                    sBcc = ByteToHexStr(Bcc);

                    break;
                default:
                    break;
            }
            return Encoding.ASCII.GetBytes((char)5 + frame + (char)4 + sBcc);

        }


        public static byte[] ReadCoilStatusMessage(byte slaveAddress, string Type, string startAddress, ushort nuMBErOfPoints)
        {
            int commCountNeed = 0;
            int commCountSend = 0;
            int data_type2 = 0;
            int num = GetDataType(Type, ref data_type2);

            return Read(slaveAddress, ref Type, ushort.Parse(startAddress), nuMBErOfPoints, data_type2, ref commCountSend, ref commCountNeed);
        }

        public static byte[] ReadInputStatusMessage(byte slaveAddress, string Type, string startAddress, ushort nuMBErOfPoints)
        {
            int commCountNeed = 0;
            int commCountSend = 0;
            int data_type2 = 0;
            int num = GetDataType(Type, ref data_type2);

            return Read(slaveAddress, ref Type, ushort.Parse(startAddress), nuMBErOfPoints, data_type2, ref commCountSend, ref commCountNeed);
        }

        public static byte[] ReadHoldingRegistersMessage(byte slaveAddress, string Type, string startAddress, ushort nuMBErOfPoints)
        {
            int commCountNeed = 0;
            int commCountSend = 0;
            int data_type2 = 0;
            int num = GetDataType(Type, ref data_type2);

            return  Read(slaveAddress, ref Type, ushort.Parse(startAddress), nuMBErOfPoints, data_type2, ref commCountSend, ref commCountNeed);
        }

        public byte[] ReadInputRegistersMessage(byte slaveAddress, string Type, string startAddress, ushort nuMBErOfPoints)
        {
            int commCountNeed = 0;
            int commCountSend = 0;
            int data_type2 = 0;
            int num = GetDataType(Type, ref data_type2);

            return  Read(slaveAddress, ref Type, ushort.Parse(startAddress), nuMBErOfPoints, data_type2, ref commCountSend, ref commCountNeed);
        }

        public static byte[] WriteSingleCoilMessage(byte slaveAddress, string startAddress, bool value)
        {
            byte[] values = new byte[2];
            if (value == true)
            {
                values[0] = 0xFF;
                values[1] = 0x00;
            }
            else
            {
                values[0] = 0x00;
                values[1] = 0x00;
            }
            return Write(slaveAddress, ushort.Parse(startAddress), "MX", values);
        }

        public  static byte[] WriteSingleRegisterMessage(byte slaveAddress, string startAddress, byte[] values)
        {
            string Data = string.Empty;
            //if (strValue(0).IndexOf("-", 0, System.StringComparison.InvariantCultureIgnoreCase) >= 0)
            //{
            //    Data = string.Format("{0:X4}", Convert.ToString(Convert.ToInt64(strValue(0)), 16).ToUpper());
            //    DataNo = Data.Substring(4, 4);
            //}
            //else
            //{
            //    DataNo = string.Format("{0:X4}", Convert.ToInt32(strValue(0)));
            //}

            return Write(slaveAddress, ushort.Parse(startAddress), "DW", values);
        }
    }
    public class Conversion
    {

        public static byte[] HexToBytes(string hex)
        {
            if (hex == null)
                throw new ArgumentNullException("The data is null");

            if (hex.Length % 2 != 0)
                throw new FormatException("Hex Character Count Not Even");

            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return bytes;
        }
    }
}
