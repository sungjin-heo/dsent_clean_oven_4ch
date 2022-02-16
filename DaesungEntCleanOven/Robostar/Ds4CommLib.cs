using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Mp.Helpers;


namespace DsFourChamberLib.DataComm
{
    /// <summary>
    /// 기본
    /// </summary>
    public class CommonData
    {
		public DateTime Time { get; set; }
		public int MessageSeqNo { get; set; }
		public bool IsEFEM { get; set; } // EFEM이 보낸 메시지임
		public string DebugMessage { get; set; } = "";
	}

	/// <summary>
	/// EFEM 상태
	/// </summary>
	public class EfemCommDataStatus : CommonData
	{
		// EFEM
		public bool IsRobotConnected { get; set; }
		public bool IsRobotBusy { get; set; }
		public bool IsRobotOrigined { get; set; }
		public bool HasRobotError { get; set; }
		public bool HasPanel1 { get; set; }
		public bool HasPanel2 { get; set; }

		// LPM
		public bool IsLPM1Loaded { get; set; }
		public bool IsLPM2Loaded { get; set; }
		public bool IsLPM3Loaded { get; set; }
		public bool IsLPM4Loaded { get; set; }

		// CIM
		public bool IsHostConnected { get; set; }
		public bool IsJobReserved { get; set; }
		public bool IsJobNow { get; set; }

		// Chamber
		public EfemCommDataPortData Port1 { get; set; }
		public EfemCommDataPortData Port2 { get; set; }
		public EfemCommDataPortData Port3 { get; set; }
		public EfemCommDataPortData Port4 { get; set; }

		/// <summary>
		/// #cons
		/// </summary>
		public EfemCommDataStatus()
		{
			Port1 = new EfemCommDataPortData();
			Port2 = new EfemCommDataPortData();
			Port3 = new EfemCommDataPortData();
			Port4 = new EfemCommDataPortData();
		}
	}

	/// <summary>
	/// chamber 데이터
	/// </summary>
	public class EfemCommDataPortData
	{
		public string RecipeName { get; set; } = "";
		public string LotID { get; set; } = "";
		public string CarrierID { get; set; } = "";
		public string SlotMap { get; set; } = "";
		public int PanelCount { get; set; } = 0;
		public string[] PanelIDList { get; set; } = new string[6];

		public bool IsStarted { get; set; }
		public bool IsAborted { get; set; }
		public bool IsFinished { get; set; }

		/// <summary>
		/// #cons
		/// </summary>
		public EfemCommDataPortData()
		{
			for (int i = 0; i < PanelIDList.Length; ++i)
			{
				PanelIDList[i] = string.Format("PANEL ID{0}", (i + 1));
			}
		}

	}

	/// <summary>
	/// 패널 공급
	/// </summary>
	public class EfemCommDataSupplyPanel : CommonData
	{
		public string PanelLotID { get; set; } = "";
		public string PanelCarrierID { get; set; } = "";
		public string PanelRecipeName { get; set; } = "";
		public string PanelID { get; set; } = "";
		public int PanelSlotNo { get; set; } = 0;
		public int PanelPortNo { get; set; } = 0;
	}

	/// <summary>
	/// message
	/// </summary>
	public enum eEfemMessage
	{
		None,

		ReqResetAlarm,			// 알람 해제 요청
		ReqRecipeList,			// 레시피 리스트 요청
		ReqStatus,				// 상태정보 요청

		PreCheckChamberReady,	// 챔버 작업가능한지 요청
		ReqOpenChamberDoor,		// 챔버 도어 열기요청
		ReqCloseChamberDoor,	// 챔버 도어 닫기요청
		ReqPrepareBakeProcess,	// 작업 준비 요청
		ReqStartTransfer,		// 패널 이송준비 요청
		ReqCompleteTransfer,	// 패널 이송완료 요청
		ReqStartBakeProcess,	// bake 프로세스 시작 요청
		ReqAbortBakeProcess,	// bake 프로세스 중단 요청
	}

	/// <summary>
	/// event
	/// </summary>
	public enum eEfemEvent
	{
		// event
		StartPutPanelEvent,
		FinishPutPanelEvent,
		StopPutPanelEvent,
		StartGetPanelEvent,
		FinishGetPanelEvent,
		StopGetPanelEvent,
	}

	/// <summary>
	/// message
	/// </summary>
	public enum eBakeOvenAck
	{
		OK = 0,
		NG = 1,
	}

	/// <summary>
	/// event
	/// </summary>
	public enum eOvenEvent
	{
		None,

		Alarm,
		DoorOpenComplete,
		DoorCloseComplete,
		ProcessStart,
		ProcessComplete,
	}

	/// <summary>
	/// request
	/// </summary>
	public class EfemRequest : CommonData
	{
		public int ChamberNo { get; set; } // [1-4]
		public eEfemMessage Req { get; set; }

		// parameter for prepare, start, abort
		public string LotID { get; set; } = "";
		public string CarrierID { get; set; } = "";
		public string RecipeName { get; set; } = "";
		public int RecipeNo { get; set; }

		// parameter for put, get panel
		public string PanelID { get; set; } = "";
		public int PanelSlotNo { get; set; } = 0;
		public int PanelPortNo { get; set; } = 0;
	}

	/// <summary>
	/// event
	/// </summary>
	public class EfemEvent : CommonData
	{
		public eEfemEvent EventID { get; set; }

		// parameter for prepare, start, abort
		public string PanelLotID { get; set; } = "";
		public string PanelCarrierID { get; set; } = "";
		public string PanelRecipeName { get; set; } = "";

		// parameter for put, get panel
		public string PanelID { get; set; } = "";
		public int PanelSlotNo { get; set; } = 0;
		public int PanelPortNo { get; set; } = 0;
	}

	/// <summary>
	/// response
	/// </summary>
	public class BakeOvenResponse : CommonData
	{
		public int ChamberNo { get; set; }
		public eEfemMessage Req { get; set; }
		public eBakeOvenAck Ack { get; set; }
		public string ErrorCode { get; set; } = "";

		/// <summary>
		/// #cons
		/// </summary>
		public BakeOvenResponse()
		{
		}
	}

	/// <summary>
	/// 대성ENT 챔버 상태
	/// </summary>
	public class BakeOvenCommDataStatus : CommonData
	{
		public bool IsInitOperation { get; set; } // 초기운전
		public bool IsRun { get; set; } // 운전
		public bool IsStopping { get; set; } // 종료중
		public bool IsStop { get; set; } // 정지
		public bool IsAutoTune { get; set; } // 오토튜닝

		public BakeOvenCommDataChamberStatusData Chamber1 { get; set; }
		public BakeOvenCommDataChamberStatusData Chamber2 { get; set; }
		public BakeOvenCommDataChamberStatusData Chamber3 { get; set; }
		public BakeOvenCommDataChamberStatusData Chamber4 { get; set; }

		/// <summary>
		/// #cons
		/// </summary>
		public BakeOvenCommDataStatus()
		{
			Chamber1 = new BakeOvenCommDataChamberStatusData();
			Chamber2 = new BakeOvenCommDataChamberStatusData();
			Chamber3 = new BakeOvenCommDataChamberStatusData();
			Chamber4 = new BakeOvenCommDataChamberStatusData();
		}
	}

	/// <summary>
	/// Oven의 상태 정보
	/// </summary>
	public class BakeOvenCommDataChamberStatusData
	{
        private const int maxAlarm = 100;

        // alarm
        public bool[] AlarmBits { get; set; }

		// interface
		public bool IsReady { get; set; } // 작업 시작 준비됨
		public bool IsPrepared { get; set; } // 작업 준비 완료 (패널 투입 가능)
		public bool IsRun { get; set; }
		public bool IsAlarm { get; set; }
		public bool IsDoorOpenAvailable { get; set; }
		public bool IsDoorOpen { get; set; }
		public bool IsDoorClosed { get; set; }

		// recipe
		public string RecipeName { get; set; } // pattern no.
		public int SequenceTotal { get; set; }
		public int SequenceNo { get; set; }
		public double ProcessStartTime { get; set; } // sec
		public double ProcessTotalTime { get; set; } // sec
		public double SequenceStartTime { get; set; } // sec
		public double SequenceToatlTime { get; set; } // sec

		// FDC
		public double TempPV { get; set; }
		public double TempSV { get; set; }
		public double TempMV { get; set; }
		public double ChmPressPV { get; set; }
		public double ChmPressSV { get; set; }
		public double ChmPressMV { get; set; }
		public double ChmOverTemp { get; set; }
		public double HeaterOverTemp { get; set; }
		public double ChmFilterPV { get; set; }
		public double MotorChmPV { get; set; }
		public double MotorChmSV { get; set; }
		public double MotorChmMV { get; set; }
		public double MfcSV { get; set; }
		public double MfcPV { get; set; }
		public double MfcMV { get; set; }
		public double InsideTemp1 { get; set; }
		public double InsideTemp2 { get; set; }
		public double O2Temp { get; set; }
		public double O2EMF { get; set; }
		public double O2PPM { get; set; }
        public static int MaxAlarm => MaxAlarm1;
        public static int MaxAlarm1 => maxAlarm;

        /// <summary>
        /// #cons
        /// </summary>
        public BakeOvenCommDataChamberStatusData()
		{
			AlarmBits = new bool[MaxAlarm];
		}
	}

	public class BakeOvenEventData : CommonData
	{
		public int ChamberNo { get; set; } // [1-4]
		public eOvenEvent EventID { get; set; }

		public string RecipeName { get; set; } = "";
	}


	/// <summary>
	/// recipe
	/// </summary>
	public class BakeOvenRecipe : CommonData
	{
		public int RecipeNo { get; set; }
		public string RecipeName { get; set; } = "";
		public BakeOvenSegmentData[] Segments { get; set; }


		/// <summary>
		/// #cons
		/// </summary>
		public BakeOvenRecipe()
		{
			Segments = new BakeOvenSegmentData[2];
			Segments[0] = new BakeOvenSegmentData();
			Segments[1] = new BakeOvenSegmentData();
		}
	}

	/// <summary>
	/// sequence
	/// </summary>
	public class BakeOvenSegmentData
	{ 
		public int SegmentNo { get; set; }
		public double Temp { get; set; }
		public double ChmPress { get; set; }
		public double MotorChm { get; set; }
		public double MotorCooling { get; set; }
		public double MFC { get; set; }
		public double Time { get; set; }
        public int TS1 { get; set; }
        public int TS2 { get; set; }
	}
}
