using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mp.Lib.IO
{
    public delegate void MpMessageReceiveHandler(object m);

    /// <summary>
    /// message client using tcp client
    /// </summary>
    public class MpMessageClient
	{
		// version:1.1
		private static byte[] MessageHeader = { 0xFF, 0xFE, 0xEF, 0xFF };


		/// <summary>
		/// ip
		/// </summary>
		public string IPAddress { get; protected set; } = "127.0.0.1";

		/// <summary>
		/// port
		/// </summary>
		public int TcpPortNo { get; protected set; }



		/// <summary>
		/// 보낸 메시지 개수
		/// </summary>
		public int SendMessageCount { get; protected set; }

		/// <summary>
		/// 받은 메시지 개수
		/// </summary>
		public int RecvMessageCount { get; protected set; }

		/// <summary>
		/// error count
		/// </summary>
		public int ErrorCount { get; protected set; }

		/// <summary>
		/// 시도회수
		/// </summary>
		public int ConnectTryCount { get; protected set; }

		/// <summary>
		/// error
		/// </summary>
		public string LastErrorMessage { get; protected set; }

		/// <summary>
		/// 연결확인 주기, 단위(초)
		/// </summary>
		public double ConnectionCheckPeriod { get; set; } = 1;

		/// <summary>
		/// thread 주기, 단위(msec)
		/// </summary>
		public int ThreadSleepTime { get; set; } = 10;

		/// <summary>
		/// 재연결 딜레이, 초
		/// </summary>
		public int ConnectionRetryDelay { get; set; } = 5;


		// thread
		public bool IsRun { get; protected set; }
		protected Thread CommThread { get; set; }
		private int ThreadSeqNo { get; set; }

		// socket
		protected Socket MySocket { get; set; }
		protected NetworkStream MyStream { get; set; }
		public bool IsConnected { get; protected set; }
		protected DateTime LastCheckConnectionTime { get; set; }

		// lock object
		private object SendLock = new object();
		private object RecvLock = new object();


		// type mapper
		public Dictionary<string, Type> MessageTypes { get; protected set; }
		public event MpMessageReceiveHandler MessageReceiveEvent;
		public event Action<MpMessageClient, bool> ClientConnectionChangedEvent;
		public event Action<MpMessageClient, string> ReceiveErrorEvent;

		public MpMessageClient(string ip, int port)
		{
			IPAddress = ip;
			TcpPortNo = port;
			MessageTypes = new Dictionary<string, Type>();
		}
		public bool Start()
		{
			if (CommThread != null && CommThread.IsAlive)
				return false;

			LastCheckConnectionTime = DateTime.MinValue;
			SendMessageCount = 0;
			RecvMessageCount = 0;
			ErrorCount = 0;
			ConnectTryCount = 0;
			LastErrorMessage = "";
			ThreadSleepTime = Math.Max(10, ThreadSleepTime);
			ConnectionCheckPeriod = Math.Max(0.2, ConnectionCheckPeriod);
			IsConnected = false;

			IsRun = true;
			CommThread = new Thread(DoComm);
			CommThread.IsBackground = true;
			CommThread.Name = string.Format("Mp.Msg.Client{0}", ++ThreadSeqNo);
			CommThread.Start();
			return true;
		}
		public void Stop()
		{
			IsRun = false;
		}
		private unsafe void DoComm()
		{
			// var
			var last_try_conn = DateTime.MinValue;
			var need_delay = true;

			// loop
			while (IsRun)
			{
				if (need_delay)
				{
					Thread.Sleep(ThreadSleepTime);
				}
				need_delay = true;
				var now = DateTime.Now;

				// check recv
				if (IsConnected)
				{
					var chk_elasped = now.Subtract(LastCheckConnectionTime).TotalSeconds;
					if (chk_elasped >= ConnectionCheckPeriod)
					{
						if (MySocket == null || MyStream == null)
						{
							IsConnected = false;
							continue;
						}

						// connection check
						lock (SendLock)
						lock (RecvLock)
						{
							try
							{
								if (MySocket.Poll(1, SelectMode.SelectRead) && MySocket.Available == 0)
								{
									Thread.Sleep(10);
									if (MySocket.Poll(1, SelectMode.SelectRead) && MySocket.Available == 0)
									{
										CloseSockets();
										continue;
									}
								}
								LastCheckConnectionTime = now;
							}
							catch (Exception ex)
							{
								ErrorCount++;
								LastErrorMessage = "Error:" + ex.Message;
								ReceiveErrorEvent?.Invoke(this, LastErrorMessage);
							}
						}
					}

					// receive
					lock (RecvLock)
					{
						try
						{
							if (MySocket.Available < 4)
								continue;

							// read sig
							var failure = false;
							for (int i = 0; i < MessageHeader.Length; ++i)
							{
								var ch = MessageHeader[i];
								var n = MyStream.ReadByte();
								if (ch != n)
								{
									failure = true;
									break;
								}
							}

							if (failure)
								continue;


							

							// read len
							var tmo = Stopwatch.StartNew();
							var buf1 = new byte[4];
							if (MyStream.Read(buf1, 0, 4) != 4)
							{
								continue;
							}
							var len = BitConverter.ToInt32(buf1, 0);
							var data = new byte[len];
							var cnt = 0;
							while (cnt < len)
							{
								var n = MyStream.Read(data, cnt, data.Length - cnt);
								if (n <= 0)
								{
									Thread.Sleep(50);
								}
								else
								{
									tmo.Restart();
									cnt += n;
								}

								// timeout
								if (tmo.ElapsedMilliseconds > 10000)
								{
									failure = true;
									break;
								}
							}

							if (failure)
								continue;

                            string Xml = Encoding.UTF8.GetString(data);
                            Log.Logger.Dispatch("i", Xml);

                            // read
                            using (var ms = new MemoryStream(data))
							using (var r = new BinaryReader(ms))
							{
								var typename = r.ReadString();
								if (!MessageTypes.ContainsKey(typename))
								{
									ErrorCount++;
									LastErrorMessage = "Not found type:" + typename;
									ReceiveErrorEvent?.Invoke(this, LastErrorMessage);
									continue;
								}

								// conv
								object obj = null;
								try
								{
									var t = MessageTypes[typename];
									var s = new XmlSerializer(t);
									obj = s.Deserialize(ms);
									RecvMessageCount++;
									need_delay = false;
								}
								catch (Exception ex)
								{
									ErrorCount++;
									LastErrorMessage = "ConvertError:" + ex.Message;
									ReceiveErrorEvent?.Invoke(this, LastErrorMessage);
								}

								// call
								try
								{
									MessageReceiveEvent?.Invoke(obj);
								}
								catch (Exception ex)
								{
									ErrorCount++;
									LastErrorMessage = "InvokeError:" + ex.Message;
									ReceiveErrorEvent?.Invoke(this, LastErrorMessage);
								}
							}
						}
						catch (Exception ex)
						{
							ErrorCount++;
							LastErrorMessage = "RecvError:" + ex.Message;
							ReceiveErrorEvent?.Invoke(this, LastErrorMessage);
						}

					}
				}
				// make a connection
				else if (last_try_conn == DateTime.MinValue || now.Subtract(last_try_conn).TotalSeconds > ConnectionRetryDelay)
				{
					if (IPAddress != null && IPAddress.Length > 0 && TcpPortNo > 0)
					{
						lock (RecvLock)
						lock (SendLock)
						{
							try
							{
								var ip = System.Net.IPAddress.Parse(IPAddress);
								var ep = new IPEndPoint(ip, TcpPortNo);
								var sock = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
								ConnectTryCount++;
								Trace.WriteLine(string.Format("MpMessage - Try to connect({0}) : {1}:{2}", ConnectTryCount, IPAddress, TcpPortNo));
								sock.Connect(ep);
								Trace.WriteLine(string.Format("MpMessage is Connected: {0}:{1}", IPAddress, TcpPortNo));
								MySocket = sock;
								sock.SendBufferSize = 1048576;
								sock.ReceiveBufferSize = 1048576;
								MyStream = new NetworkStream(sock);
								LastCheckConnectionTime = DateTime.Now;
								IsConnected = true;

								// connected
								ClientConnectionChangedEvent?.Invoke(this, true);
							}
							catch (SocketException ex) 
							{  
								ErrorCount++;
								LastErrorMessage = "Error:" + ex.Message;
								ReceiveErrorEvent?.Invoke(this, LastErrorMessage);
							}
							catch (Exception ex)
							{
								ErrorCount++;
								LastErrorMessage = "Error:" + ex.Message;
								ReceiveErrorEvent?.Invoke(this, LastErrorMessage);
							}
						}
						last_try_conn = DateTime.Now;
					}
				}
			}

			// close
			IsRun = false;
			CloseSockets();
		}
		protected void CloseSockets()
		{
			lock (SendLock)
			lock (RecvLock)
			{
				try
				{
					// disconnected event
					if (IsConnected)
					{
						ClientConnectionChangedEvent?.Invoke(this, false);
					}

					IsConnected = false;
					if (MyStream != null)
					{
						MyStream.Close();
						MyStream = null;
					}
					if (MySocket != null)
					{
						MySocket.Close();
						MySocket = null;
					}
				}
				catch (Exception ex)
				{
					ErrorCount++;
					LastErrorMessage = "Error:" + ex.Message;
					ReceiveErrorEvent?.Invoke(this, LastErrorMessage);
				}
			}
		}
		public bool Send(object message)
		{
			if (!IsConnected)
			{
				LastErrorMessage = "Not Connected.";
				return false;
			}

			lock (SendLock)
			{
				if (!IsConnected)
				{
					LastErrorMessage = "Not Connected.";
					return false;
				}

				try
				{
					var m = message;
					if (m == null)
					{
						LastErrorMessage = "Send(null)";
						return false;
					}

					// conv
					byte[] arr = null;
					using (var wms = new MemoryStream())
					using (var w = new BinaryWriter(wms))
					{
						var t = m.GetType();
						w.Write(t.Name);

						var ser = new XmlSerializer(t);
						using (var ms = new MemoryStream())
						{
							ser.Serialize(ms, m);
							w.Write(ms.ToArray());
						}
						w.Flush();
						arr = wms.ToArray();
					}

					// send
					MyStream.Write(MessageHeader, 0, MessageHeader.Length);
					MyStream.Write(BitConverter.GetBytes((int)arr.Length), 0, 4);
					MyStream.Write(arr, 0, arr.Length);
					MyStream.Flush();
					SendMessageCount++;

                    string Xml = Encoding.UTF8.GetString(arr);
					Log.Logger.Dispatch("i", Xml);
					return true;
				}
				catch (Exception ex)
				{
					ErrorCount++;
					LastErrorMessage = "Error:" + ex.Message;
					ReceiveErrorEvent?.Invoke(this, LastErrorMessage);
					return false;
				}
			}
        }
	}
}
