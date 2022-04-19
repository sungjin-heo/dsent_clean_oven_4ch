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
		/// <summary>
		/// 메시지 생성시간
		/// </summary>
		public DateTime Time { get; set; }

		/// <summary>
		/// 메시지 일련번호 (하나씩 증가)
		/// </summary>
		public int MessageSeqNo { get; set; }
		
		/// <summary>
		/// Sender 설정, True - EFEM이 보냄, False - EQP가 보냄
		/// </summary>
		public bool IsEFEM { get; set; }

		/// <summary>
		/// 디버그용 메시지.
		/// </summary>
		public string DebugMessage { get; set; } = "";


		/// <summary>
		/// #cons
		/// </summary>
		public CommonData()
		{
			Time = DateTime.Now;
		}

	}

	/// <summary>
	/// EFEM 상태
	/// </summary>
	public class EfemCommDataStatus : CommonData
	{
		// EFEM
		// 로봇 상태
		public bool IsRobotConnected { get; set; }		// 로봇 연결상태
		public bool IsRobotBusy { get; set; }			// 로봇 동작상태 
		public bool IsRobotOrigined { get; set; }		// 로봇 원점동작 완료 여부
		public bool HasRobotError { get; set; }			// 로봇 에러 유무
		public bool HasPanel1 { get; set; }				// 로봇 ARM 1번 패널 유무
		public bool HasPanel2 { get; set; }				// 로봇 ARM 2번 패널 유무

		// LPM
		public bool IsLPM1Loaded { get; set; }			// LPM1 로딩 상태
		public bool IsLPM2Loaded { get; set; }			// LPM2 로딩 상태
		public bool IsLPM3Loaded { get; set; }			// LPM3 로딩 상태
		public bool IsLPM4Loaded { get; set; }			// LPM4 로딩 상태

		// CIM
		public bool IsHostConnected { get; set; }		// EAP연결상태
		public bool IsJobReserved { get; set; }			// 작업 예약 상태
		public bool IsJobNow { get; set; }				// 작업중 여부

		// PORTS
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
	/// LPM 포트 데이터
	/// </summary>
	public class EfemCommDataPortData
	{
		public string RecipeName { get; set; } = "";		// 레시피 이름
		public string LotID { get; set; } = "";				// LOT
		public string CarrierID { get; set; } = "";			// CID
		public string SlotMap { get; set; } = "";			// 작업 슬롯맵
		public int PanelCount { get; set; } = 0;			// 패널 개수
		public string[] PanelIDList { get; set; } = new string[6];		// 패널 이름 리스트

		public bool IsStarted { get; set; }					// 시작 여부
		public bool IsAborted { get; set; }					// 중단 여부
		public bool IsFinished { get; set; }				// 완료 여부

		/// <summary>
		/// #cons
		/// </summary>
		public EfemCommDataPortData()
		{
// 			for (int i = 0; i < PanelIDList.Length; ++i)
// 			{
// 				PanelIDList[i] = "PANEL ID{0}".Fmt(i + 1);
// 			}
		}
	}

	/// <summary>
	/// 패널 공급 시
	/// </summary>
	public class EfemCommDataSupplyPanel : CommonData
	{
		public string PanelLotID { get; set; } = "";			// Lot
		public string PanelCarrierID { get; set; } = "";		// CID
		public string PanelRecipeName { get; set; } = "";		// Recipe
		public string PanelID { get; set; } = "";				// Panel ID
		public int PanelSlotNo { get; set; } = 0;				// Slot
		public int PanelPortNo { get; set; } = 0;				// Port
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
		/// <summary>
		/// 패널 공급 시작
		/// </summary>
		StartPutPanelEvent,

		/// <summary>
		/// 패널 공급 완료
		/// </summary>
		FinishPutPanelEvent,

		/// <summary>
		/// 패널 공급 중단
		/// </summary>
		StopPutPanelEvent,


		/// <summary>
		/// 패널 배출 시작
		/// </summary>
		StartGetPanelEvent,

		/// <summary>
		/// 패널 배출 완료
		/// </summary>
		FinishGetPanelEvent,

		/// <summary>
		/// 패널 배출 중단
		/// </summary>
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
	/// request
	/// </summary>
	public enum eBakeOvenMessage
	{
		None,
		
		ReqStatus,				// EFEM 상태 정보 요청
	}

	/// <summary>
	/// event
	/// </summary>
	public enum eOvenEvent
	{
		None,

		Alarm,
		DoorOpenComplete,		// 챔버 도어 열림
		DoorCloseComplete,		// 챔버 도어 닫힘
		ProcessStart,			// 챔버 프로세스 시작
		ProcessComplete,		// 챔버 프로세스 완료
		ProcessAbort,			// 챔버 프로세스 중단
	}

	/// <summary>
	/// request
	/// </summary>
	public class EfemRequest : CommonData
	{
		/// <summary>
		/// 명령어
		/// </summary>
		public eEfemMessage Req { get; set; }

		/// <summary>
		/// 챔버 번호 [1-4]
		/// </summary>
		public int ChamberNo { get; set; }

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
		/// <summary>
		/// 이벤트
		/// </summary>
		public eEfemEvent EventID { get; set; }

		// paramter for chamber
		public int ChamberNo { get; set; }

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
	/// request
	/// </summary>
	public class BakeOvenRequest : CommonData
	{
		public eBakeOvenMessage Req { get; set; }

		/// <summary>
		/// 챔버 번호 [1-4]
		/// </summary>
		public int ChamberNo { get; set; }
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
		public const int MaxAlarm = 100;

		// alarm
		public bool[] AlarmBits { get; set; }

        public bool IsInitOperation { get; set; }   // 초기운전
        public bool IsRun { get; set; }             // 운전
        public bool IsStopping { get; set; }        // 종료중
        public bool IsStop { get; set; }            // 정지
        public bool IsAutoTune { get; set; }        // 오토튜닝

        // interface
        public bool IsReady { get; set; }			// 작업 시작 준비됨 => 정지 상태이면서 알람이 없는 상태.
// 		public bool IsPrepared { get; set; }		// 작업 준비 완료 (패널 투입 가능) => 협의하여 삭제하기로 함.
		public bool IsAlarm { get; set; }
		public bool IsDoorOpenAvailable { get; set; }
		public bool IsDoorOpen { get; set; }
		public bool IsDoorClosed { get; set; }

		// recipe
		public string RecipeName { get; set; }			// pattern no.
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

		/// <summary>
		/// #cons
		/// </summary>
		public BakeOvenCommDataChamberStatusData()
		{
			AlarmBits = new bool[MaxAlarm];
		}
	}

	/// <summary>
	/// event
	/// </summary>
	public class BakeOvenEventData : CommonData
	{
		public int ChamberNo { get; set; } // [1-4]
		public eOvenEvent EventID { get; set; }

		public string RecipeName { get; set; } = "";
	}

	/// <summary>
	/// recipe list
	/// </summary>
	public class BakeOvenRecipeList : CommonData
	{
		public List<BakeOvenRecipe> Recipes { get; set; } = new List<BakeOvenRecipe>();
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
// 			Segments = new BakeOvenSegmentData[2];
// 			Segments[0] = new BakeOvenSegmentData();
// 			Segments[1] = new BakeOvenSegmentData();
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

	public static class Ds4CommHelper
	{
		public static void RegisterDataObject(Dictionary<string, Type> table)
		{
			table[nameof(EfemCommDataStatus)] = typeof(EfemCommDataStatus);
			table[nameof(EfemCommDataSupplyPanel)] = typeof(EfemCommDataSupplyPanel);
			table[nameof(EfemRequest)] = typeof(EfemRequest);
			table[nameof(EfemEvent)] = typeof(EfemEvent);
			table[nameof(BakeOvenRequest)] = typeof(BakeOvenRequest);
			table[nameof(BakeOvenResponse)] = typeof(BakeOvenResponse);
			table[nameof(BakeOvenCommDataStatus)] = typeof(BakeOvenCommDataStatus);
			table[nameof(BakeOvenEventData)] = typeof(BakeOvenEventData);
			table[nameof(BakeOvenRecipeList)] = typeof(BakeOvenRecipeList);
			table[nameof(BakeOvenRecipe)] = typeof(BakeOvenRecipe);
		}
	}
}
