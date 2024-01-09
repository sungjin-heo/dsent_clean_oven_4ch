using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Device.CMT;
using Util;

namespace DaesungEntCleanOven4.Equipment
{
    internal class MeasureDataUpdateEventArgs : EventArgs
    {
        public MeasureDataUpdateEventArgs(int Id, double Temperature, double EMF, double Rpm)
        {
            this.DeviceId = Id;
            this.SensorTemperature = Temperature;
            this.SensorEMF = EMF;
            this.O2ConcentrationPpm = Rpm;
        }
        public int DeviceId { get; private set; }
        public double SensorTemperature { get; private set; }
        public double SensorEMF { get; private set; }
        public double O2ConcentrationPpm { get; private set; }
    }

    internal class AnalyzerConnectionStateEventArgs : EventArgs
    {
        public AnalyzerConnectionStateEventArgs(int Id, bool State)
        {
            this.DeviceId = Id;
            this.ConnectState = State;
        }
        public int DeviceId { get; private set; }
        public bool ConnectState { get; private set; }
    }

    class MeasureValue
    {
        public double SensorTemperature { get; set; }
        public double SensorEMF { get; set; }
        public double O2ConcentrationPpm { get; set; }
    }

    internal class O2Analyzer : Device.CMT.Analyzer
    {
        static object SyncKey = new object();
        

        // ADDRESS.
        public readonly byte ADDR_TARGET_SENSOR_TMP = 0x1E;
        public readonly byte ADDR_ALARM_LIMIT = 0x49;
        public readonly byte ADDR_SENSOR_MALFUNC_ALARM = 0x4B;
        public readonly byte ADDR_SENSOR_PWR = 0x4C;
        public readonly byte ADDR_MAIN_ALARM_CTL = 0x4D;
        public readonly byte ADDR_SENSOR_TMP = 0x51;
        public readonly byte ADDR_SENSOR_EMF = 0x57;
        public readonly byte ADDR_O2_CONCENTRATION_PPM = 0x59;
        public readonly byte ADDR_O2_CONCENTRATION_PERC = 0x5B;
        public readonly byte ADDR_O2_CONCENTRATION_LOG = 0x5C;

        protected ushort __TargetSensorTemperature;
        protected ushort __SensorMalfunctionAlarm;
        protected ushort __SensorPower;
        protected ushort __MainAlarmControl;
        protected int __ReplyErrorCount;
        
        public O2Analyzer(string portName, int baudRate, byte Id)
          : base(portName, baudRate, Id)
        {
//             for (int i = 0; i < MeasurementValueCache.Length; i++)
//                 this.MeasurementValueCache[i] = new MeasureValue();
        }
        public ushort TargetSensorTemperature
        {
            get { return __TargetSensorTemperature; }
            set
            {
                try
                {
                    if (value < 0 || value > 1000)
                        throw new ArgumentOutOfRangeException("TargetSensorTemperature");

                    SessionMessage Message = MakeMessage(CMD_WRITE, ADDR_TARGET_SENSOR_TMP, value);
                    if (Message != null)
                    {
                        var Response = Send(Message);
                        if (Response == null)
                            throw new Exception("Response Message is null");

                        if (Response.Data != null && Response.Data.Length == 8)
                            if (Response.Data[0] == Id && Response.Data[3] == ADDR_TARGET_SENSOR_TMP)
                                __Tracer.TraceInfo("Received a Reply Message");
                    }
                }
                catch (Exception ex)
                {
                    __Tracer.TraceError("Exception is Occured while to Set TargetSensorTemperature : " + ex.Message);
                }
            }
        }
        public ushort SensorMalfunctionAlarm
        {
            get { return __SensorMalfunctionAlarm; }
            set
            {
                try
                {
                    // 0 : Normal
                    // 1 : Sensor Malfunction
                    if (value < 0 || value > 1)
                        throw new ArgumentOutOfRangeException("SensorMalfunctionAlarm");

                    SessionMessage Message = MakeMessage(CMD_WRITE, ADDR_SENSOR_MALFUNC_ALARM, value);
                    if (Message != null)
                    {
                        var Response = Send(Message);
                        if (Response == null)
                            throw new Exception("Response Message is null");

                        if (Response.Data != null && Response.Data.Length == 8)
                            if (Response.Data[0] == Id && Response.Data[3] == ADDR_SENSOR_MALFUNC_ALARM)
                                __Tracer.TraceInfo("Received a Reply Message");
                    }
                }
                catch (Exception ex)
                {
                    __Tracer.TraceError("Exception is Occured while to Set SensorMalfunctionAlarm : " + ex.Message);
                }
            }
        }
        public ushort SensorPower
        {
            get { return __SensorPower; }
            set
            {
                try
                {
                    // 0 : OFF
                    // 1 : ON
                    if (value < 0 || value > 1)
                        throw new ArgumentOutOfRangeException("SensorPower");

                    SessionMessage Message = MakeMessage(CMD_WRITE, ADDR_SENSOR_PWR, value);
                    if (Message != null)
                    {
                        var Response = Send(Message);
                        if (Response == null)
                            throw new Exception("Response Message is null");

                        if (Response.Data != null && Response.Data.Length == 8)
                            if (Response.Data[0] == Id && Response.Data[3] == ADDR_SENSOR_PWR)
                                __Tracer.TraceInfo("Received a Reply Message");
                    }
                }
                catch (Exception ex)
                {
                    __Tracer.TraceError("Exception is Occured while to Set SensorPower : " + ex.Message);
                }
            }
        }
        public ushort MainAlarmControl
        {
            get { return __MainAlarmControl; }
            set
            {
                try
                {
                    // 0 : NONE (deactivate)
                    // 1 : Alarm ON (activate)
                    // 2 : Alarm State
                    if (value < 0 || value > 2)
                        throw new ArgumentOutOfRangeException("MainAlarmControl");

                    SessionMessage Message = MakeMessage(CMD_WRITE, ADDR_MAIN_ALARM_CTL, value);
                    if (Message != null)
                    {
                        var Response = Send(Message);
                        if (Response == null)
                            throw new Exception("Response Message is null");

                        if (Response.Data != null && Response.Data.Length == 8)
                            if (Response.Data[0] == Id && Response.Data[3] == ADDR_MAIN_ALARM_CTL)
                                __Tracer.TraceInfo("Received a Reply Message");
                    }
                }
                catch (Exception ex)
                {
                    __Tracer.TraceError("Exception is Occured while to Set MainAlarmControl : " + ex.Message);
                }
            }
        }
        public ushort AlarmLimitState { get; protected set; }       // 0 : None, 1 : Lower Limit Alarm, 2 : Upper Limit Alarm
        public double SensorTemperature { get; protected set; }
        public double SensorEMF { get; protected set; }
        public double O2ConcentrationPpm { get; protected set; }
        public double O2ConcentrationPercentage { get; protected set; }
        public double O2ConcentrationLog { get; protected set; }
        public event EventHandler<MeasureDataUpdateEventArgs> MeasureDataUpdated;
        public event EventHandler<AnalyzerConnectionStateEventArgs> ConnectionStateChanged;
        public override bool Open()
        {
            if (base.Open())
            {
                OnConnected();
            }
            return base.IsOpen;
        }
      
        protected override void MonitorFunc(object State)
        {
            int[] comm_chk_try_cnt = new int[4];

            __Sp.DiscardInBuffer();
            CancellationToken Token = (CancellationToken)State;
            while (!Token.IsCancellationRequested && IsOpen)
            {
                if (Monitor.TryEnter(SyncKey, 3000))
                {
                    try
                    {
                        // 4개 채널 데이터 쿼리...
                        for (int i = 0; i < 4; i++)
                        {
                            try
                            {
                                double Temp = .0, Emf = .0, Rpm = .0;
                                SessionMessage Message;

                                // 하나의 세션에서 4개 채널에 대한 데이터를 받는다.
                                // 메세지 디코더에서 패킷의 시작을 구분하기 위한 유일한 수단은 디바이스 아이디. 따라서 현재 요청하는 디바이스 아이디를 전역변수로 설정.
                                // 메세지 디코더에서 전역변수의 값에 해당하는 디바이스 식별자를 이용해 패킷의 시작점 구분.
                                Device.CMT.Analyzer.RequestedDeviceId = i + 1;

                                // GET TEMPERATURE.
                                Message = MakeMessage((byte)(i + 1), CMD_READ, ADDR_SENSOR_TMP);
                                Comm.IMessage Response = Send(Message);
                                Log.Logger.Dispatch("i", "02 sensor temp : {0}", Response);
                                if (Response == null)
                                {
                                    throw new Exception("02 sensor temp's return value null");
                                }
                                if (Response != null && Response.Data.Length == 9)
                                {
                                    Array.Reverse(Response.Data, 3, 2);
                                    Array.Reverse(Response.Data, 5, 2);
                                    Temp = BitConverter.ToInt32(Response.Data, 3) * 0.1;
                                }

                                Thread.Sleep(300);

                                // GET EMF.
                                Message = MakeMessage((byte)(i + 1), CMD_READ, ADDR_SENSOR_EMF);
                                Response = Send(Message);
                                Log.Logger.Dispatch("i", "02 sensor emf : {0}", Response);
                                if (Response == null)
                                {
                                    throw new Exception("02 sensor emf's return value null");
                                }
                                if (Response != null && Response.Data.Length == 9)
                                {
                                    Array.Reverse(Response.Data, 3, 2);
                                    Array.Reverse(Response.Data, 5, 2);
                                    Emf = BitConverter.ToInt32(Response.Data, 3) * 0.01;
                                }

                                Thread.Sleep(300);

                                // GET RPM.
                                Message = MakeMessage((byte)(i + 1), CMD_READ, ADDR_O2_CONCENTRATION_PPM);
                                Response = Send(Message);
                                Log.Logger.Dispatch("i", "02 sensor ppm : {0}", Response);
                                if (Response == null)
                                {
                                    throw new Exception("02 sensor ppm's return value null");
                                }
                                if (Response != null && Response.Data.Length == 9)
                                {
                                    Array.Reverse(Response.Data, 3, 2);
                                    Array.Reverse(Response.Data, 5, 2);
                                    Rpm = BitConverter.ToInt32(Response.Data, 3) * 0.01;
                                }

                                MeasureDataUpdated?.Invoke(this, new MeasureDataUpdateEventArgs(i, Temp, Emf, Rpm));
                                ConnectionStateChanged?.Invoke(this, new AnalyzerConnectionStateEventArgs(i, true));
                                comm_chk_try_cnt[i] = 0;
                            }
                            catch (Exception ex)
                            {
                                Log.Logger.Dispatch("e", ex.Message);
                                if (ex.Message.Contains("return value null"))
                                {
                                    comm_chk_try_cnt[i]++;
                                    if (comm_chk_try_cnt[i] >= 3)
                                    {
                                        ConnectionStateChanged?.Invoke(this, new AnalyzerConnectionStateEventArgs(i, false));
                                        comm_chk_try_cnt[i] = 0;
                                    }
                                }
                            }

                            // 채널 데이터 요청 딜레이 : 500 ms
                            Thread.Sleep(300);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Dispatch("e", "Exception is Occured while to Update Analyzer Data : " + ex.Message);
                    }
                    finally
                    {
                        Monitor.Exit(SyncKey);
                    }
                }
                Thread.Sleep(1000);
            }
            if (!IsOpen && !Token.IsCancellationRequested)
            {
                OnDisConnected();
            }
        }
        protected SessionMessage MakeMessage(byte Cmd, byte Addr, ushort? value = null)
        {
            try
            {
                if (Cmd == CMD_READ)
                {
                    byte addrCnt = 1;
                    if (Addr == ADDR_SENSOR_TMP || Addr == ADDR_SENSOR_EMF || Addr == ADDR_O2_CONCENTRATION_PPM)
                    {
                        addrCnt = 2;
                    }

                    byte[] Bytes = new byte[8];
                    Bytes[0] = Id;
                    Bytes[1] = Cmd;
                    Bytes[2] = 0x0;
                    Bytes[3] = Addr;
                    Bytes[4] = 0x0;
                    Bytes[5] = addrCnt;
                    byte[] Crc = BitConverter.GetBytes(CRC16.MODBUS(Bytes, 0, 6));
                    Bytes[6] = Crc[0];
                    Bytes[7] = Crc[1];
                    return new SessionMessage(Bytes);
                }
                else if (Cmd == CMD_WRITE)
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException("Setting Value");
                    }

                    byte[] Bytes = new byte[11];
                    Bytes[0] = Id;
                    Bytes[1] = Cmd;
                    Bytes[2] = 0x0;
                    Bytes[3] = Addr;
                    Bytes[4] = 0x0;
                    Bytes[5] = 0x1;
                    Bytes[6] = 0x2;
                    byte[] Tmp = BitConverter.GetBytes((ushort)value);
                    Tmp.Reverse();
                    Array.Copy(Tmp, 0, Bytes, 7, Tmp.Length);
                    byte[] Crc = BitConverter.GetBytes(CRC16.MODBUS(Bytes, 0, 6));
                    Bytes[9] = Crc[0];
                    Bytes[10] = Crc[1];
                    return new SessionMessage(Bytes);
                }
            }
            catch (Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to MakeMessage in O2Anaylzer : " + ex.Message);
            }
            return null;
        }
        protected SessionMessage MakeMessage(byte DevId, byte Cmd, byte Addr, ushort? value = null)
        {
            try
            {
                if (Cmd == CMD_READ)
                {
                    byte addrCnt = 1;
                    if (Addr == ADDR_SENSOR_TMP || Addr == ADDR_SENSOR_EMF || Addr == ADDR_O2_CONCENTRATION_PPM)
                    {
                        addrCnt = 2;
                    }

                    byte[] Bytes = new byte[8];
                    Bytes[0] = DevId;
                    Bytes[1] = Cmd;
                    Bytes[2] = 0x0;
                    Bytes[3] = Addr;
                    Bytes[4] = 0x0;
                    Bytes[5] = addrCnt;
                    byte[] Crc = BitConverter.GetBytes(CRC16.MODBUS(Bytes, 0, 6));
                    Bytes[6] = Crc[0];
                    Bytes[7] = Crc[1];
                    return new SessionMessage(Bytes);
                }
                else if (Cmd == CMD_WRITE)
                {
                    if (value == null)
                        throw new ArgumentNullException("Setting Value");

                    byte[] Bytes = new byte[11];
                    Bytes[0] = DevId;
                    Bytes[1] = Cmd;
                    Bytes[2] = 0x0;
                    Bytes[3] = Addr;
                    Bytes[4] = 0x0;
                    Bytes[5] = 0x1;
                    Bytes[6] = 0x2;
                    byte[] Tmp = BitConverter.GetBytes((ushort)value);
                    Tmp.Reverse();
                    Array.Copy(Tmp, 0, Bytes, 7, Tmp.Length);
                    byte[] Crc = BitConverter.GetBytes(CRC16.MODBUS(Bytes, 0, 6));
                    Bytes[9] = Crc[0];
                    Bytes[10] = Crc[1];
                    return new SessionMessage(Bytes);
                }
            }
            catch (Exception ex)
            {
                __Tracer.TraceError("Exception is Occured while to MakeMessage in O2Anaylzer : " + ex.Message);
            }
            return null;
        }
    }
}
