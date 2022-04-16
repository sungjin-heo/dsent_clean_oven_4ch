using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace DaesungEntCleanOven4.Robostar.ViewModel
{
    internal class EfemServer : DevExpress.Mvvm.BindableBase
    {
        private bool __IsServerConnected;

        public EfemServer()
        {
            this.Model = new DsFourChamberLib.DataComm.EfemCommDataStatus();
            this.SupplyModel = new DsFourChamberLib.DataComm.EfemCommDataSupplyPanel();
            this.PortDatas = new EfemPortDataViewModel[4];
            for (int i = 0; i < 4; i++)
            {
                this.PortDatas[i] = new EfemPortDataViewModel();
            }
        }
        
        DsFourChamberLib.DataComm.EfemCommDataStatus Model;
        DsFourChamberLib.DataComm.EfemCommDataSupplyPanel SupplyModel;

        public string ServerIpAddress { get; set; }
        public int ServerTcpPort { get; set; }
        public bool IsServerConnected
        {
            get => __IsServerConnected;
            set
            {
                __IsServerConnected = value;
                RaisePropertiesChanged("IsServerConnected");
            }
        }

        // 서버 및 로봇 상태 정보.
        public bool IsRobotConnected => Model.IsRobotConnected;
        public bool IsRobotBusy => Model.IsRobotBusy;
        public bool IsRobotOrigined => Model.IsRobotOrigined;
        public bool HasRobotError => Model.HasRobotError;
        public bool HasPanel1 => Model.HasPanel1;
        public bool HasPanel2 => Model.HasPanel2;
        public bool IsLPM1Loaded => Model.IsLPM1Loaded;
        public bool IsLPM2Loaded => Model.IsLPM2Loaded;
        public bool IsLPM3Loaded => Model.IsLPM3Loaded;
        public bool IsLPM4Loaded => Model.IsLPM4Loaded;
        public bool IsHostConnected => Model.IsHostConnected;
        public bool IsJobReserved => Model.IsJobReserved;
        public bool IsJobNow => Model.IsJobNow;

        // 포트 1~4 상태 정보.
        public EfemPortDataViewModel[] PortDatas { get; private set; }

        // 패널 공급 정보
        public string PanelLotID => SupplyModel.PanelLotID;
        public string PanelCarrierID => SupplyModel.PanelCarrierID;
        public string PanelRecipeName => SupplyModel.PanelRecipeName;
        public string PanelID => SupplyModel.PanelID;
        public int PanelSlotNo => SupplyModel.PanelSlotNo;
        public int PanelPortNo => SupplyModel.PanelPortNo;

        public void Update(DsFourChamberLib.DataComm.EfemCommDataStatus data)
        {
            this.Model = data;
            PortDatas[0].IsLoaded = data.IsLPM1Loaded;
            PortDatas[1].IsLoaded = data.IsLPM2Loaded;
            PortDatas[2].IsLoaded = data.IsLPM3Loaded;
            PortDatas[3].IsLoaded = data.IsLPM4Loaded;
            PortDatas[0].Update(data.Port1);
            PortDatas[1].Update(data.Port2);
            PortDatas[2].Update(data.Port3);
            PortDatas[3].Update(data.Port4);

            PropertyInfo[] props = typeof(EfemServer).GetProperties();
            IEnumerable<string> q = from p in props select p.Name;
            RaisePropertiesChanged(q.ToArray());
        }
        public void Update(DsFourChamberLib.DataComm.EfemCommDataSupplyPanel data)
        {
            this.SupplyModel = data;
            RaisePropertiesChanged(
                "PanelLotID",
                "PanelCarrierID",
                "PanelRecipeName",
                "PanelID",
                "PanelSlotNo",
                "PanelPortNo");
        }
    }

    internal class EfemPortDataViewModel : DevExpress.Mvvm.BindableBase
    {
        public EfemPortDataViewModel()
        {
            this.Model = new DsFourChamberLib.DataComm.EfemCommDataPortData();
        }

        DsFourChamberLib.DataComm.EfemCommDataPortData Model;
        public bool IsLoaded { get; set; }
        public string RecipeName => Model.RecipeName;
        public string LotID => Model.LotID;
        public string CarrierID => Model.CarrierID;
        public string SlotMap => Model.SlotMap;
        public int PanelCount => Model.PanelCount;
        public string[] PanelList => Model.PanelIDList;
        public bool IsStarted => Model.IsStarted;
        public bool IsAborted => Model.IsAborted;
        public bool IsFinished => Model.IsFinished;
        public void Update(DsFourChamberLib.DataComm.EfemCommDataPortData data)
        {
            this.Model = data;
            PropertyInfo[] props = typeof(EfemPortDataViewModel).GetProperties();
            IEnumerable<string> q = from p in props select p.Name;
            RaisePropertiesChanged(q.ToArray());
        }
    }
}
