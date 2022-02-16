using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven4.Equipment;

namespace DaesungEntCleanOven4.ViewModel
{
    class RegRelay : ItemViewModel<bool>
    {
        public RegRelay(Device.Yokogawa.PLC.YokogawaSequenceEth plc, string Name, string ReadAddr)
           : this(plc, Name, ReadAddr, null, null)
        {
            
        }
        public RegRelay(Device.Yokogawa.PLC.YokogawaSequenceEth plc, string Name, string ReadAddr, int? Offset)
            : this(plc, Name, ReadAddr, null, Offset)
        {
        }
        public RegRelay(Device.Yokogawa.PLC.YokogawaSequenceEth plc, string Name, string ReadAddr, string WriteAddr)
            : this(plc, Name, ReadAddr, WriteAddr, null)
        {
            this.ReadAddress = ReadAddr;
            this.WriteAddress = WriteAddr;
        }
        public RegRelay(Device.Yokogawa.PLC.YokogawaSequenceEth plc, string Name, string ReadAddr, string WriteAddr, int? Offset)
            : base(Name)
        {
            this.plc = plc;
            this.LatencyQueryItems = (plc as CleanOven).Channel.LatencyQueryItems;
            this.CleanOvenLatencyQueryInterval = (plc as CleanOven).Channel.CleanOvenLatencyQueryInterval;
            this.ReadAddress = ReadAddr;
            this.WriteAddress = WriteAddr;
            this.Offset = Offset;
        }
        public override bool Value
        {
            get { return base.Value; }
            set
            {
                if (IsReadOnly)
                {
                    base.Value = value;
                }
                else
                {
                    try
                    {
                        string Response = string.Empty;                        
                        Response = (string)plc.WriteBit(WriteAddress, value);
                        if (!Response.Contains("OK"))
                            throw new Exception(string.Format("Return Error for WriteBit(),  WriteAddress : {0}, WriteValue : {1}", WriteAddress, value));

                        if (LatencyQueryItems.Contains(this.Name))
                            System.Threading.Thread.Sleep(CleanOvenLatencyQueryInterval);

                        if (!string.IsNullOrEmpty(ReadAddress))
                        {
                            Response = (string)plc.ReadBit(ReadAddress, 1);
                            if (!Response.Contains("OK"))
                                throw new Exception(string.Format("Return Error for ReadBit(),  ReadAddress : {0}, Count : 1", ReadAddress));
                            base.Value = Response[4] == '1';
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Dispatch("e", ex.Message);
                    }
                }
            }
        }
        public string ReadAddress { get; protected set; }
        public string WriteAddress { get; protected set; }
        public int? Offset { get; set; }
        public override string FormattedValue
        {
            get { return base.Value ? "1" : "0"; }
            set
            {
                throw new NotImplementedException();
            }
        }
        public bool IsReadOnly { get { return string.IsNullOrEmpty(WriteAddress) && !string.IsNullOrEmpty(ReadAddress); } }
        public static explicit operator bool(RegRelay relay) { return relay.Value; }
        public void UpdateOnly(bool value)
        {
            base.Value = value;
        }

        // 4채널 확장을 위해 어쩔수 없이....ㅠㅠ
        private Device.Yokogawa.PLC.YokogawaSequenceEth plc { get; set; }
        private int CleanOvenLatencyQueryInterval { get; set; }
        private string[] LatencyQueryItems { get; set; }
    }
}
