using DataService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace XGTDriver
{
    [Description("XGT FENET Ethernet protocol")]
    public sealed class XGTFENETDriver : IPLCDriver, IMultiReadWrite 
    {
        #region
        public int PDU
        {
            
            get { return 255; } 
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
                        //dv.Start = (st / 16) * 16;//???????????????????
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
                        //dv.Start = (st / 16) * 16;//???????????????????
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
                        dv.ByteOrder = ByteOrder.Network;
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
                        dv.ByteOrder = ByteOrder.Network;
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
        private int _timeout = 1000;

        private Socket tcpSynCl;
        private byte[] tcpSynClBuffer = new byte[0xFF];

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

        string _ip;
        public string ServerName
        {
            get { return _ip; }
            set { _ip = value; }
        }

        public bool IsClosed
        {
            get
            {
                return tcpSynCl == null || tcpSynCl.Connected == false;
            }
        }

        public int TimeOut
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        List<IGroup> _grps = new List<IGroup>(20);
        public IEnumerable<IGroup> Groups
        {
            get { return _grps; }
        }

        IDataServer _server;
        public IDataServer Parent
        {
            get { return _server; }
        }
        public XGTFENETDriver(IDataServer server, short id, string name)
        {
            _id = id;
            _name = name;
            _server = server;
        }

        public bool Connect()
        {
            int port = 2004;
            // check if available
            Ping p = new Ping();
            PingReply pingReplay = p.Send(_ip);
            if (pingReplay.Status != IPStatus.Success)
            {
                throw new Exception();
            }

            try
            {
                if (tcpSynCl != null)
                    tcpSynCl.Close();
                //IPAddress ip = IPAddress.Parse(_ip);
                // ----------------------------------------------------------------
                // Connect synchronous client
                if (_timeout <= 0) _timeout = 1000;
                tcpSynCl = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tcpSynCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                tcpSynCl.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 2000);
                tcpSynCl.Connect(_ip, port);
                return true;
            }
            catch (SocketException error)
            {
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(error.Message));
                return false;
            }
        }
     public IGroup AddGroup(string name, short id, int updateRate, float deadBand = 0f, bool active = false)
        {
            NetShortGroup grp = new NetShortGroup(id, name, updateRate, active, this);
            _grps.Add(grp);
            return grp;
        }

        public bool RemoveGroup(IGroup grp)
        {
            grp.IsActive = false;
            return _grps.Remove(grp);
        }

        public void Dispose()
        {
            if (tcpSynCl != null)
            {
                if (tcpSynCl.Connected)
                {
                    try { tcpSynCl.Shutdown(SocketShutdown.Send); }
                    catch { }
                    tcpSynCl.Close();
                }
                tcpSynCl = null;
            }
            foreach (IGroup grp in _grps)
            {
                grp.Dispose();
            }
            _grps.Clear();
        }

           object _async = new object();

        private byte[] WriteSyncData(byte[] write_data)
        {
            short id = BitConverter.ToInt16(write_data, 0);
              
            {
                lock (_async)
                {
                    try
                    {
                        tcpSynCl.Send(write_data, 0, write_data.Length, SocketFlags.None);//是否存在lock的问题？
                        int result = tcpSynCl.Receive(tcpSynClBuffer, 0, 0xFF, SocketFlags.None);
 
                        return tcpSynClBuffer;
                    }
                    catch (SocketException ex)
                    {
                        CallException(id, write_data[7], ex.Message);
                    }
                }
            }
            return null;
        }
        //**********************************
        //* Extract a header from a packet
        //**********************************
        public List<byte> Extract(byte[] packet, int length)
        {

            List<byte> m_EncapsulatedData = new List<byte>();
            string txtValue = string.Empty;
            string Value3 = string.Empty;
            byte[] Data = null;

            // Read Response
            if (packet[20] == 0x55)
            {

                switch (packet[22])
                {

                    case 0: // Bit X
                        for (int i = 32; i < packet.Length; i++)
                        {
                            m_EncapsulatedData.Add(packet[i]);
                        }
                        break;
                    case 1: //Byte B
                        txtValue =XGTFENETMessage. GetValStr(packet, 20 + 12, 1);
                        Data = (Conversion.HexToBytes(txtValue));

                        break;
                    case 2: //Word
                        txtValue = XGTFENETMessage.GetValStr(packet, 20 + 12, 2);
                        Data = (Conversion.HexToBytes(txtValue));
                        break;
                    case 3: //DWord
                        txtValue = XGTFENETMessage.GetValStr(packet, 20 + 12, 4);

                        Data = (Conversion.HexToBytes(txtValue));

                        break;
                    case 4: //LWord
                        txtValue = XGTFENETMessage.GetValStr(packet, 20 + 12, 8);
                        Data = (Conversion.HexToBytes(txtValue));

                        break;
                    case 20:
                        Data = XGTFENETMessage.ToStringArray(packet, packet.Length);

                        break;
                }

                for (int i = 0; i < Data.Length; i++)
                {
                    m_EncapsulatedData.Add(Data[i]);
                }

            }
            return m_EncapsulatedData;

        }


        public byte[] WriteSingleCoils(int id, int startAddress, bool OnOff)
        {
          
            byte[] frame = XGTFENETMessage.WriteSingleCoilMessage(Convert.ToByte(id),Convert.ToString( startAddress), OnOff);

            return WriteSyncData(frame);
        }

        public byte[] WriteMultipleCoils(int id, int startAddress, ushort numBits, byte[] values)
        {
            throw new NotImplementedException();
        }

        public byte[] WriteSingleRegister(int id, int startAddress, byte[] values)
        {
            byte[] frame = XGTFENETMessage.WriteSingleRegisterMessage(Convert.ToByte(id), Convert.ToString(startAddress), values);

            return WriteSyncData(frame);
        }

        public byte[] WriteMultipleRegister(int id, int startAddress, byte[] values)
        {
            throw new NotImplementedException();
        }

        internal void CallException(int id, byte function, string  exception)
        {
            if (tcpSynCl == null) return;
            if (OnError != null)
                OnError(this, new IOErrorEventArgs(exception));
        }

        public byte[] ReadBytes(DeviceAddress address, ushort size)
        {
            int area = address.DBNumber;
            //return area < 3 ? WriteSyncData(CreateReadHeader(address.Area, address.Start * 16, (ushort)(16 * size), (byte)area))
            //    : WriteSyncData(CreateReadHeader(address.Area, address.Start, size, (byte)area));
            return null;
        }

        public ItemData<int> ReadInt32(DeviceAddress address)
        {
            byte[] data = WriteSyncData(XGTFENETMessage.ReadHoldingRegistersMessage((byte)address.Area,Convert.ToString(address.Start), (ushort)address.DBNumber));
            
            if ( Extract(data, data.Length).ToArray() == null)
                return new ItemData<int>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<int>(IPAddress.HostToNetworkOrder(BitConverter.ToInt32(Extract(data, data.Length).ToArray(), 0)), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<uint> ReadUInt32(DeviceAddress address)
        {
            byte[] data = WriteSyncData(XGTFENETMessage.ReadHoldingRegistersMessage((byte)address.Area,Convert.ToString(address.Start), (ushort)address.DBNumber));
            if (Extract(data, data.Length).ToArray() == null)
                return new ItemData<uint>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<uint>((uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(Extract(data, data.Length).ToArray(), 0)), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<ushort> ReadUInt16(DeviceAddress address)
        {
            byte[] data = WriteSyncData(XGTFENETMessage.ReadHoldingRegistersMessage((byte)address.Area, Convert.ToString(address.Start), (ushort)address.DBNumber));
            if (Extract(data, data.Length).ToArray() == null)
                return new ItemData<ushort>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<ushort>((ushort)IPAddress.HostToNetworkOrder(BitConverter.ToInt16(Extract(data, data.Length).ToArray(), 0)), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<short> ReadInt16(DeviceAddress address)
        {
            byte[] data = WriteSyncData(XGTFENETMessage.ReadHoldingRegistersMessage((byte)address.Area, Convert.ToString(address.Start), (ushort)address.DBNumber));
            if (Extract(data, data.Length).ToArray() == null)
                return new ItemData<short>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<short>(IPAddress.HostToNetworkOrder(BitConverter.ToInt16(Extract(data, data.Length).ToArray(), 0)), 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<byte> ReadByte(DeviceAddress address)
        {
            byte[] data = WriteSyncData(XGTFENETMessage.ReadHoldingRegistersMessage((byte)address.Area, Convert.ToString(address.Start), (ushort)address.DBNumber));
            if (Extract(data, data.Length).ToArray() == null)
                return new ItemData<byte>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<byte>(Extract(data, data.Length).ToArray()[0], 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<string> ReadString(DeviceAddress address, ushort size)
        {
            byte[] data = WriteSyncData(XGTFENETMessage.ReadHoldingRegistersMessage((byte)address.Area, Convert.ToString(address.Start), (ushort)address.DBNumber));
            if (Extract(data, data.Length).ToArray() == null)
                return new ItemData<string>(string.Empty, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<string>(Encoding.ASCII.GetString(Extract(data, data.Length).ToArray(), 0, data.Length), 0, QUALITIES.QUALITY_GOOD);//是否考虑字节序问题？
        }

        public unsafe ItemData<float> ReadFloat(DeviceAddress address)
        {
            byte[] data = WriteSyncData(XGTFENETMessage.ReadHoldingRegistersMessage((byte)address.Area, Convert.ToString(address.Start), (ushort)address.DBNumber));
            if (Extract(data, data.Length).ToArray() == null)
                return new ItemData<float>(0.0f, 0, QUALITIES.QUALITY_BAD);
            else
            {
                return new ItemData<float>(IPAddress.HostToNetworkOrder(BitConverter.ToInt32(Extract(data, data.Length).ToArray(), 0)), 0, QUALITIES.QUALITY_GOOD);
            }
        }

        public ItemData<bool> ReadBit(DeviceAddress address)
        {
            byte[] data = address.DBNumber > 2 ? WriteSyncData(XGTFENETMessage.ReadCoilStatusMessage((byte)address.Area, Convert.ToString(address.Start), (ushort)address.DBNumber)) :
                   WriteSyncData(XGTFENETMessage.ReadCoilStatusMessage((byte)address.Area,Convert.ToString(address.Start * 16 + address.Bit), (ushort)address.DBNumber));
            if (Extract(data, data.Length).ToArray() == null)
                return new ItemData<bool>(false, 0, QUALITIES.QUALITY_BAD);
            if (data.Length == 1) return new ItemData<bool>(Extract(data, data.Length).ToArray()[0] > 0, 0, QUALITIES.QUALITY_GOOD);
            unsafe
            {
                fixed (byte* p = data)
                {
                    short* p1 = (short*)p;
                    return new ItemData<bool>((*p1 & (1 << address.Bit.BitSwap()))
                        != 0, 0, QUALITIES.QUALITY_GOOD);
                }
            }
        }

        public ItemData<object> ReadValue(DeviceAddress address)
        {
            return this.ReadValueEx(address);
        }

        public int WriteBytes(DeviceAddress address, byte[] bit)
        {
            var data = address.DBNumber > 2 ? WriteMultipleRegister(address.Area, address.Start, bit)
                : WriteMultipleCoils(address.Area, address.Start, (ushort)(8 * bit.Length), bit);//应考虑到
            return data == null ? -1 : 0;
        }

        public int WriteBit(DeviceAddress address, bool bit)
        {
            if (address.DBNumber < 3)
            {
                var data = WriteSingleCoils(address.Area, address.Start + address.Bit, bit);
                return data == null ? -1 : 0;
            }
            return -1;
        }

        public int WriteBits(DeviceAddress address, byte bits)
        {
            if (address.DBNumber != 3) return -1;
            var data = WriteSingleRegister(address.Area, address.Start, new byte[] { bits });
            return data == null ? -1 : 0;
        }

        public int WriteInt16(DeviceAddress address, short value)
        {
            if (address.DBNumber != 3) return -1;
            var data = WriteSingleRegister(address.Area, address.Start, BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
            return data == null ? -1 : 0;
        }

        public int WriteUInt16(DeviceAddress address, ushort value)
        {
            if (address.DBNumber != 3) return -1;
            var data = WriteSingleRegister(address.Area, address.Start, BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)value)));
            return data == null ? -1 : 0;
        }

        public int WriteUInt32(DeviceAddress address, uint value)
        {
            if (address.DBNumber != 3) return -1;
            var data = WriteMultipleRegister(address.Area, address.Start, BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)value)));
            return data == null ? -1 : 0;
        }

        public int WriteInt32(DeviceAddress address, int value)
        {
            if (address.DBNumber != 3) return -1;
            var data = WriteMultipleRegister(address.Area, address.Start, BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)));
            return data == null ? -1 : 0;
        }

        public int WriteFloat(DeviceAddress address, float value)
        {
            if (address.DBNumber != 3) return -1;
            var data = WriteMultipleRegister(address.Area, address.Start, BitConverter.GetBytes((int)value));
            return data == null ? -1 : 0;
        }

        public int WriteString(DeviceAddress address, string str)
        {
            if (address.DBNumber != 3) return -1;
            var data = WriteMultipleRegister(address.Area, address.Start, Encoding.ASCII.GetBytes(str));
            return data == null ? -1 : 0;
        }

        public int WriteValue(DeviceAddress address, object value)
        {
            return this.WriteValueEx(address, value);
        }

        public event IOErrorEventHandler OnError;

        public int Limit
        {
            get { return 60; }
        }

        public ItemData<Storage>[] ReadMultiple(DeviceAddress[] addrsArr)
        {
            return this.PLCReadMultiple(new NetShortCacheReader(), addrsArr);
        }

        public int WriteMultiple(DeviceAddress[] addrArr, object[] buffer)
        {
            return this.PLCWriteMultiple(new NetShortCacheReader(), addrArr, buffer, Limit);
        }
    }
    public sealed class XGTFENETMessage 
    {
        public   enum DataType
        {
            Input = 0x50,
            Output = 0x51,
            Marker = 0x4D,
            DataBlock = 0x44,
            Timer = 29,
            Counter = 28
        }
        public   enum Command
        {
            Read = 0x54,
            Write = 0x58,
            Status = 0xB0

        }
        public   enum VarType
        {
            Bit = 0x0,
            Byte = 0x1,
            Word = 0x2,
            DWord = 0x3,
            LWord = 0x4,
            Continuous = 0x14,
            Int,
            DInt,
            Real,
            String,
            Timer,
            Counter
        }
        public   enum variable
        {
            Bit = 0x58,
            Byte = 0x42,
            Word = 0x57,
            DWord = 0x44,
            LWord = 0x4C
        }
        #region Read
        public static byte[] Read(string strVar, int iDataType, byte[] dataToWrite)
        {
            byte[] TxRead = null;
            TxRead = new byte[19];
            TxRead[0] = (byte)Microsoft.VisualBasic.Strings.Asc("L");
            TxRead[1] = (byte)Microsoft.VisualBasic.Strings.Asc("S");
            TxRead[2] = (byte)Microsoft.VisualBasic.Strings.Asc("I");
            TxRead[3] = (byte)Microsoft.VisualBasic.Strings.Asc("S");
            TxRead[4] = (byte)Microsoft.VisualBasic.Strings.Asc("-");
            TxRead[5] = (byte)Microsoft.VisualBasic.Strings.Asc("X");
            TxRead[6] = (byte)Microsoft.VisualBasic.Strings.Asc("G");
            TxRead[7] = (byte)Microsoft.VisualBasic.Strings.Asc("T");

            TxRead[8] = 0; //Reserved
            TxRead[9] = 0;

            TxRead[10] = 0; //PLC_Info
            TxRead[11] = 0;

            TxRead[12] = 0xA0; //CPU_Info

            TxRead[13] = 0x33; //Source_of_Frame

            TxRead[14] = 0; //CByte(Invoke_ID) 'Invoke ID
            TxRead[15] = 0;


            int Length = 0;
            int i = 0;

            Length = strVar.Length;

            if (!(Length > 0))
            {
                return null;
            }


            Array.Resize(ref TxRead, 30 + Length);

            TxRead[16] = (byte)(10 + Length); //Length (2)

            TxRead[17] = 0;

            TxRead[18] = 3; //Fenet_Position(1)

            TxRead[19] = ByteCheckSum(TxRead, 0, 18);

            TxRead[20] = 0x54; //Command (2)
            TxRead[21] = 0;

            TxRead[22] = (byte)iDataType; //DataType (2)
            TxRead[23] = 0;
            TxRead[24] = 0; //Reserved area (2)
            TxRead[25] = 0;
            TxRead[26] = 1; //Variable Number (2)
            TxRead[27] = 0;

            TxRead[28] = (byte)Length; //Variable Length
            TxRead[29] = 0;

            for (i = 0; i < Length; i++)
            {
                TxRead[30 + i] = (byte)Microsoft.VisualBasic.Strings.Asc(strVar.Substring(i, 1));
            }


            return TxRead;
        }

        /// <summary>
        /// ReadBytes
        /// </summary>
        /// <param name="DataType"></param>
        /// <param name="DB"></param>
        /// <param name="StartByteAdr"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static byte[] ReadBytes(DataType DataType, int DB, int StartByteAdr, int count)
        {
            //ReadBytes
            List<byte> FullPacket = new List<byte>();
            string txtValue = string.Empty;
            string Value3 = string.Empty;

            byte[] TxRead = null;
            byte[] bytes = new byte[(2 * count)];
            FullPacket.Clear();

            try
            {
                // first create the header
                TxRead = new byte[20];
                TxRead[0] = 76;
                TxRead[1] = 83;
                TxRead[2] = 73;
                TxRead[3] = 83;
                TxRead[4] = 45;
                TxRead[5] = 88;
                TxRead[6] = 71;
                TxRead[7] = 84;
                TxRead[8] = 0;
                TxRead[9] = 0;
                TxRead[10] = 0;
                TxRead[11] = 0;
                TxRead[12] = 160;
                TxRead[13] = 51;
                TxRead[14] = 0;
                TxRead[15] = 0;
                TxRead[16] = (byte)(10 + 8);
                TxRead[17] = 0;
                TxRead[18] = 3;
                TxRead[19] = ByteCheckSum(TxRead, 0, 18);
                FullPacket.AddRange(TxRead);
                FullPacket.Add(Convert.ToByte(Command.Read));
                FullPacket.Add(0);
                FullPacket.Add(Convert.ToByte(VarType.Continuous));
                FullPacket.Add(0);
                FullPacket.AddRange(new byte[] { 0, 0, 1, 0, 6, 0, 37 });
                FullPacket.Add(Convert.ToByte(DataType));
                FullPacket.Add(Convert.ToByte(variable.Byte));
                FullPacket.AddRange(Encoding.ASCII.GetBytes(string.Format("{0:X3}", 2 * StartByteAdr)));
                FullPacket.Add((byte)count);
                FullPacket.Add(0);




            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            return FullPacket.ToArray();
        }
        #endregion

        #region Write


        /// <summary>
        /// Write
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="numberOfElements"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] Write(string variable, int numberOfElements, string value)
        {
            string[] DataAsArray = { value };
            object objValue = "";
            List<byte> dataPacket = new List<byte>();
            string txt = variable.ToUpper();
            txt = txt.Replace(" ", ""); // Leerzeichen entfernen
            string strVar = "";
            byte[] x = null;
            strVar = variable.ToUpper();
            try
            {
                switch (txt.Substring(1, 2))
                {
                    case "DW":
                        x = new byte[4];
                        for (int i = 0; i < DataAsArray.Length; i++)
                        {
                            BitConverter.GetBytes(short.Parse(DataAsArray[i])).CopyTo(x, 0);
                        }
                        Array.Reverse(x);
                        return WriteVariable(strVar, x, 2);
                    case "DD":
                        // Eingangsbyte
                        x = new byte[8];
                        for (int i = 0; i < DataAsArray.Length; i++)
                        {
                            BitConverter.GetBytes(short.Parse(DataAsArray[i])).CopyTo(x, 0);
                        }

                        return WriteVariable(strVar, x, 8);
                    case "MB":
                    case "MW":
                    case "MD":
                    case "MX":
                        short temp = 0;
                        // Merkerdoppelwort
                        if (value == "true" || value == "True")
                        {
                            temp = 1;
                        }
                        else
                        {
                            temp = 0;
                        }
                        byte[] data = Conversion.HexToBytes(string.Format("{0:X8}", temp));
                        return WriteVariable(strVar, data, 2);
                    default:
                        break;
                }
                return dataPacket.ToArray();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                return null;
            }
        }
        /// <summary>
        /// Write
        /// </summary>
        /// <param name="strVar"></param>
        /// <param name="strValue"></param>
        /// <param name="iDataType"></param>
        /// <returns></returns>
        public static byte[] WriteVariable(string strVar, byte[] strValue, int iDataType)
        {

            byte[] TxWrite = null;
            try
            {



                TxWrite = new byte[19];
                //
                TxWrite[0] = (byte)Microsoft.VisualBasic.Strings.Asc("L");
                TxWrite[1] = (byte)Microsoft.VisualBasic.Strings.Asc("S");
                TxWrite[2] = (byte)Microsoft.VisualBasic.Strings.Asc("I");
                TxWrite[3] = (byte)Microsoft.VisualBasic.Strings.Asc("S");
                TxWrite[4] = (byte)Microsoft.VisualBasic.Strings.Asc("-");
                TxWrite[5] = (byte)Microsoft.VisualBasic.Strings.Asc("X");
                TxWrite[6] = (byte)Microsoft.VisualBasic.Strings.Asc("G");
                TxWrite[7] = (byte)Microsoft.VisualBasic.Strings.Asc("T");

                TxWrite[8] = 0; //Reserved
                TxWrite[9] = 0;

                TxWrite[10] = 0; //PLC_Info
                TxWrite[11] = 0;

                TxWrite[12] = 0xA0; //CPU_Info

                TxWrite[13] = 0x33; //Source_of_Frame

                TxWrite[14] = 0; //Invoke ID
                TxWrite[15] = 0;

                int iVarLength = 0;
                int i = 0;
                int iDataSize = 0;
                int iInDataLen = 0;
                int iInDataByte = 0;
                int iRemLen = 0;
                string strHexa = "";
                string strRem = "";


                iVarLength = strVar.Length;
                iDataSize = GetDataSize(iDataType);
                iInDataLen = strValue.Length;
                iInDataByte = (iInDataLen + 1) / 2;

                if (!(iVarLength > 0))
                {
                    return null;
                }

                if (!(iInDataByte > 0))
                {
                    return null;
                }

                //If (iInDataByte > iDataSize) Then
                //    Return Nothing
                //End If

                Array.Resize(ref TxWrite, 32 + iVarLength + iDataSize);

                TxWrite[16] = (byte)(12 + iVarLength + iDataSize); //Length (2)

                TxWrite[17] = 0;


                TxWrite[18] = 3; //Fenet_Position(1)

                TxWrite[19] = ByteCheckSum(TxWrite, 0, 18);

                TxWrite[20] = 0x58; //0x58 (2)
                TxWrite[21] = 0;

                TxWrite[22] = (byte)iDataType; //DataType (2)
                TxWrite[23] = 0;
                TxWrite[24] = 0; //Reserved area (2)
                TxWrite[25] = 0;
                TxWrite[26] = 1; //Variable Number (2)
                TxWrite[27] = 0;
                TxWrite[28] = (byte)iVarLength; //Variable Length
                TxWrite[29] = 0;

                for (i = 0; i < iVarLength; i++)
                {
                    TxWrite[30 + i] = (byte)Microsoft.VisualBasic.Strings.Asc(strVar.Substring(i, 1));
                }

                TxWrite[30 + iVarLength] = (byte)iDataSize;
                TxWrite[31 + iVarLength] = 0;

                iRemLen = iInDataLen;
                strRem = strValue.Length.ToString();

                i = 0;

                Array.Reverse(strValue);
                while (iRemLen > 0)
                {
                    strHexa = (strValue.Length - 2).ToString();
                    TxWrite[32 + iVarLength + i] = strValue[i];
                    i = i + 1;
                    iRemLen = iRemLen - int.Parse(strHexa);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return TxWrite;


        }
        #endregion
        #region Shared Function
        public static byte ByteCheckSum(byte[] Buff, int iStart, int iEnd)
        {


            int i = 0;
            int CheckSum = 0;

            for (i = iStart; i <= iEnd; i++)
            {
                CheckSum = CheckSum + Buff[i];
                if (CheckSum > 255)
                {
                    CheckSum = CheckSum - 256;
                }
            }

            return (byte)CheckSum;


        }
        public static int GetDataType(string strVar)
        {
            int tempGetDataType = 0;
            string strType = "";

            strType = strVar.Substring(2, 1);

            switch (strType)
            {
                case "X":
                    tempGetDataType = 0;
                    break;
                case "B":
                    tempGetDataType = 1;
                    break;
                case "W":
                    tempGetDataType = 2;
                    break;
                case "D":
                    tempGetDataType = 3;
                    break;
                case "L":
                    tempGetDataType = 4;
                    break;
            }
            return tempGetDataType;
        }
        public static int GetDataSize(int iDataType)
        {
            int tempGetDataSize = 0;

            switch (iDataType)
            {
                case 0:
                    tempGetDataSize = 0;
                    break;
                case 1:
                    tempGetDataSize = 1;
                    break;
                case 2:
                    tempGetDataSize = 2;
                    break;
                case 3:
                    tempGetDataSize = 4;
                    break;
                case 4:
                    tempGetDataSize = 8;
                    break;
            }

            return tempGetDataSize;
        }
        public static string GetValStr(byte[] Buff, int iStart, int iDataSize)
        {


            string strVal = "";
            string strByteVal = "";
            int i = 0;

            for (i = 0; i < iDataSize; i++)
            {
                strByteVal = Convert.ToString(Buff[i + iStart], 16).ToUpper();
                if (strByteVal.Length == 1)
                {
                    strByteVal = "0" + strByteVal;
                }
                strVal = strByteVal + strVal;
            }

            return strVal;


        }
        #endregion



        #region Functions.

        public static byte[] ReadCoilStatusMessage(byte slaveAddress, string startAddress, ushort nuMBErOfPoints)
        {
            return ReadBytes(DataType.Marker, Convert.ToInt16(slaveAddress), int.Parse(startAddress), Convert.ToInt16(nuMBErOfPoints));
        }

        public static byte[] ReadInputStatusMessage(byte slaveAddress, string startAddress, ushort nuMBErOfPoints)
        {
            return ReadBytes(DataType.Input, Convert.ToInt16(slaveAddress), int.Parse(startAddress), Convert.ToInt16(nuMBErOfPoints));
        }

        public static byte[] ReadHoldingRegistersMessage(byte slaveAddress, string startAddress, ushort nuMBErOfPoints)
        {
            return ReadBytes(DataType.DataBlock, Convert.ToInt16(slaveAddress), int.Parse(startAddress), Convert.ToInt16(nuMBErOfPoints));
        }

        public static byte[] ReadInputRegistersMessage(byte slaveAddress, string startAddress, ushort nuMBErOfPoints)
        {
            return ReadBytes(DataType.DataBlock, Convert.ToInt16(slaveAddress), int.Parse(startAddress), Convert.ToInt16(nuMBErOfPoints));
        }

        public static  byte[] WriteSingleCoilMessage(byte slaveAddress, string startAddress, bool value)
        {
            string values = string.Empty;
            if (value == true)
            {
                values = "true";

            }
            else
            {
                values = "false";

            }
            return Write(startAddress, 1, values);
        }

        public static byte[] WriteMultipleCoilsMessage(byte slaveAddress, string startAddress, bool[] values)
        {

            return null;
        }

        public static byte[] WriteSingleRegisterMessage(byte slaveAddress, string startAddress, byte[] values)
        {
            string value = Encoding.ASCII.GetString(values);
            return Write(startAddress, 1, value);
        }

        public static byte[] WriteMultipleRegistersMessage(byte slaveAddress, string startAddress, byte[] values)
        {

            return null;
        }

        #endregion
        public static string[] ToStringArray(string value, int Length)
        {
            int startIndex = 0;
            string[] bytes = new string[Length];
            try
            {
                for (int cnt = 0; cnt < bytes.Length; cnt++)
                {
                    bytes[cnt] = value.Substring(startIndex, 4);
                    startIndex += 4;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return bytes;
        }
        public static byte[] ToStringArray(byte[] value, int Length)
        {
            byte[] bytes = new byte[(2 * Length)];
            byte[] bReceive = new byte[(31 + bytes.Length)];
            try
            {
                value.CopyTo(bReceive, 0);
                int r = 0;

                for (int cnt = 32; cnt < bReceive.Length; cnt++)
                {

                    bytes[r] = bReceive[cnt];
                    r += 1;

                    if (r >= bytes.Length)
                    {
                        break;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }




            return bytes;
        }

    }
}
