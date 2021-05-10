//#################################################################################################################################
// 방화문 통신
// 작성자
// 작성일
//xxx.xxx.xxx.xxx:
//
//
//#################################################################################################################################


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace fwServer
{

    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    // 이름 : StateObject
    // 기능 : 클라이언트의 연결 정보를 가지고 있는 객체
    // 날짜 : 2020-02-10 
    // 수정자 : 하현철
    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    public class StateObject
    {
        public Socket workSocket = null;                   // 연결 소켓 객체             
        public const int BufferSize = 1024;                // 버퍼사이즈
        public byte[] buffer = new byte[BufferSize];       // 버퍼
        public String userIP = null;                       // 클라이언트 IP
        public String DeviceID = null;                     // 디바이스 ID
        public String FW_LOCATION = null;
        public int numofdoor = 1;                          // 화재 구역
        public int sendflg1 = 0;                            // 신호에 대한 응답 확인
        public int sendflg2 = 0;                            // 신호에 대한 응답 확인
        public int sendflg3 = 0;                            // 신호에 대한 응답 확인
        public int sendflg4 = 0;                            // 신호에 대한 응답 확인
        public int s_Fire = 0;                             // 이전 화재 신호값 (0 : 정상 1 : 화재)
        public int f_Run = 0;                              // 화재 전송 필요( 0 : 화재신호 전송 필요 0 이외 : 화재신호 전송 필요없음)
        public int C_IN24V = 0;                            // 전압오류 메시지 중복체크 확인용
        public int C_INB24V = 0;                           // 베터리오류 메시지 중복체크 확인용
    }


    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    // 이름 : Packet
    // 기능 : JSON 데이터를 저장하기 위한 객체
    // 날짜 : 2020-02-10 
    // 수정자 : 하현철
    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    public class Packet
    {
        public string DEVICEID;                            // 방화문 ID
        public string OPCODE;                                 // OPCODE 화재시:119 Keep alive:000
        public int RSSI;                                   // LET_e 수신감도
        public int IN24V;                                  // 입력전압
        public int INB24V;                                 // 베터리 입력전압
        public string S1;                                     // 방화문 닫힘 확인 입력
        public string OUT1;                                   // 방화문 닫힘 출력
        public string S2;                                     // 방화문 닫힘 확인 입력
        public string OUT2;                                   // 방화문 닫힘 출력
        public string S3;                                     // 방화문 닫힘 확인 입력
        public string OUT3;                                   // 방화문 닫힘 출력
        public string S4;                                     // 방화문 닫힘 확인 입력
        public string OUT4;                                   // 방화문 닫힘 출력

        public Packet()
        {

        }
        public Packet(string DEVICEID, string OPCODE, int RSSI, int IN24V, int INB24V, string S1, string OUT1, string S2, string OUT2, string S3, string OUT3, string S4, string OUT4)
        {
            this.DEVICEID = DEVICEID;
            this.OPCODE = OPCODE;
            this.RSSI = RSSI;
            this.IN24V = IN24V;
            this.INB24V = INB24V;
            this.S1 = S1;
            this.OUT1 = OUT1;
            this.S2 = S2;
            this.OUT2 = OUT2;
            this.S3 = S3;
            this.OUT3 = OUT3;
            this.S4 = S4;
            this.OUT4 = OUT4;
        }
    };



    class AsynServer
    {
        public string PRO_TITLE = "TASKCON";

        //스레드 제어
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private int PORT;
        Thread ServerThread;

        public Dictionary<String, StateObject> server_data = new Dictionary<string, StateObject>();
        
        //송수신 결과를 Form 으로 넘겨주기 위한 이벤트 처리기
        public delegate void delegateProcessPacket(String text, String userIP);
        public event delegateProcessPacket ServerGetPacket;

        //public delegate void AddUserToUI(String userIP);
        //유저 추가 이벤트
        //public event AddUserToUI AddUserEvent;                
        //유저 삭제 이벤트
        //public delegate void DeleteUserUI(String userIP);
        //public event DeleteUserUI deleteUserEvent;
        
        //로그 저장
        private String path = @"D:\logFile\TASKCON";
        MessageLog wl = null;

        //DB저장
        SqlConnect sqlconn = new SqlConnect();
        string sqlQuery;

        //시작시간
        public string server_STime;

        public AsynServer(int port)
        {
            this.PORT = port;
        }


        /// <summary>
        /// 이름 : StartServer
        /// 기능 : 클라이언트와의 통신을 위해 서버 소켓을 생성하는 메서드
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        /// </summary>
        public void StartServer()
        {
            init_server();
            ServerThread = new Thread(new ThreadStart(SetupServer));                    //서버 스레드 생성
            ServerThread.IsBackground = true;
            ServerThread.Start();

            server_STime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
        }

        /// <summary>
        /// 이름 : SetupServer
        /// 기능 : 서버와 연결가능한 아이피와 포트를 설정하고 비동기적으로 클라이언트의 연결을 받아들이는 기능
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        ///  </summary>
        private void SetupServer()
        {
            
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(100);

            while (true)
            {
                allDone.Reset();        //방화문의 접속 이벤트가 발생할 경우 해당 접속이 처리될때까지 스레드를 일시정지
                serverSocket.BeginAccept(AcceptCallback, null);              
                allDone.WaitOne();      //다음 접속 이벤트 발생시까지 현재 스레드 중지
            }
        }

        /// <summary>
        /// 이름 : CloseAllSockets
        /// 기능 : 서버 종료를 위해 서버와 연결된 모든 소켓을 해제하는 메서드
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        ///  </summary>
        public void CloseAllSockets()
        {

            //서버에 등록된 방화문 리스트를 순회하며 방화문이 서버와 연결되어있을 경우 연결해제
            foreach (KeyValuePair<string, StateObject> kv in server_data)
            {
                if (kv.Value.workSocket != null)    //방화문에 소켓정보가 있는경우
                {
                    if (kv.Value.workSocket.Connected == true)      //소켓이 연결되어 있는경우
                    {
                        kv.Value.workSocket.Shutdown(SocketShutdown.Both);
                        kv.Value.workSocket.Close();
                        kv.Value.workSocket = null;
                    }
                }                
            }

            serverSocket.Close();
            ServerThread.Abort();



        }

        /// <summary>
        /// 이름 : AcceptCallback
        /// 기능 : 클라이언트의 연결을 비동기적으로 받아들이기 위한 콜백함수
        /// 인수 : IAsyncResult AR -> 해당 비동기 작업의 결과값       
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        ///  </summary>
        public void AcceptCallback(IAsyncResult AR)
        {
            allDone.Set();      //접속 처리를 위해 스레드 일시정지 해제
            
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);        //클라이언트 소켓 생성
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (NullReferenceException)
            {
                return;
            }

           
            string userIP = socket.RemoteEndPoint.ToString().Split(':')[0];

            //연결된 클라이언트가 서버에 등록되었는지 확인
            if (server_data.ContainsKey(userIP) == true)
            {
                // 연결된 클라이언트의 소켓정보가 남아있는지 확인
                if (server_data[userIP].workSocket == null)
                {
                    //방화문 리스트에 연결된 소켓정보 등록
                    server_data[userIP].workSocket = socket;
                    server_data[userIP].userIP = userIP;
                    server_data[userIP].sendflg1 = 0;
                    server_data[userIP].sendflg2 = 0;
                    server_data[userIP].sendflg3 = 0;
                    server_data[userIP].sendflg4 = 0;


                    //연결정보 DB 저장
                    sqlQuery = string.Format("update FW_STATE set " + "CONNECTION = '{0}' , LAST_RECV_TIME = '{1}' where FW_IP = '{2}'", 1, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), userIP);
                    sqlconn.commandDB(sqlQuery);




                    if (ServerGetPacket != null)
                    {
                        ServerGetPacket(" connected\r\n", userIP);
                    }

                    try
                    {                      
                        //연결된 소켓으로 비동기 데이터 수신 시작
                        server_data[userIP].workSocket.BeginReceive(server_data[userIP].buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), server_data[userIP]);
                    }
                    catch (NullReferenceException ne)
                    {
                        return;

                    }
                    catch(Exception e)
                    {
                        server_data[userIP].buffer = new byte[StateObject.BufferSize];
                        if (server_data[userIP].workSocket != null)
                        {
                            if (server_data[userIP].workSocket.Connected)
                            {
                                server_data[userIP].workSocket.Shutdown(SocketShutdown.Both);
                                server_data[userIP].workSocket.Close();
                                server_data[userIP].workSocket.Dispose();
                                server_data[userIP].workSocket = null;
                            }
                            else
                            {
                                server_data[userIP].workSocket = null;
                            }
                        }

                    }
                    
                }
                // 서버에 해당 소켓정보가 남아있으면
                else
                {   
                    //소켓정보 초기화
                    if (server_data[userIP].workSocket.Connected)
                    {
                        server_data[userIP].workSocket.Shutdown(SocketShutdown.Both);
                        server_data[userIP].workSocket.Close();
                        server_data[userIP].workSocket.Dispose();
                        server_data[userIP].workSocket = null;
                    }
                    else
                    {
                        server_data[userIP].workSocket = null;
                    }

                    sqlQuery = string.Format("update FW_STATE set " + "CONNECTION = '{0}' where FW_IP = '{1}'", 0, userIP);
                    sqlconn.commandDB(sqlQuery);

                    //소켓정보 재등록
                    server_data[userIP].workSocket = socket;
                    server_data[userIP].userIP = userIP;
                    server_data[userIP].sendflg1 = 0;
                    server_data[userIP].sendflg2 = 0;
                    server_data[userIP].sendflg3 = 0;
                    server_data[userIP].sendflg4 = 0;


                    //상태정보 업데이트
                    sqlQuery = string.Format("update FW_STATE set " + " CONNECTION = '{1}' , LAST_RECV_TIME = '{2}' where FW_IP = '{4}'", 1, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), userIP);
                    sqlconn.commandDB(sqlQuery);

                    if (ServerGetPacket != null)
                    {
                        ServerGetPacket(" connected\r\n", userIP);
                    }

                    try
                    {                        
                        // 재등록된 소켓으로 비동기 데이터 수신 시작
                        server_data[userIP].workSocket.BeginReceive(server_data[userIP].buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), server_data[userIP]);

                    }
                    catch (NullReferenceException ne)
                    {
                        return;
                            
                    }
                    catch(Exception e)
                    {
                        // 비동기 작업에서 에러가 발생하면
                        // 소켓연결 해제
                        server_data[userIP].buffer = new byte[StateObject.BufferSize];
                        if (server_data[userIP].workSocket != null)
                        {
                            if (server_data[userIP].workSocket.Connected)
                            {
                                server_data[userIP].workSocket.Shutdown(SocketShutdown.Both);
                                server_data[userIP].workSocket.Close();
                                server_data[userIP].workSocket.Dispose();
                                server_data[userIP].workSocket = null;
                            }
                            else
                            {
                                server_data[userIP].workSocket = null;
                            }
                        }

                    }
                    
                }


            }
            //서버에 등록되어 있지 않은 아이피에서 접속요청
            else
            {
                // 해당 소켓 종료
                socket.Close();
           
            }
            
        }


        /// <summary>
        /// 이름 : ReceiveCallback
        /// 기능 : 클라이언트와 비동기적으로 데이터 수신을 위한 콜백함수
        /// 인수 : IAsyncResult AR -> 해당 비동기 작업의 결과값       
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        ///  </summary>
        public void ReceiveCallback(IAsyncResult AR)
        {

            if (AR != null)
            {

                StateObject state = (StateObject)AR.AsyncState;               //비동기 작업 결과
                Socket current = state.workSocket;                            //비동기 소켓정보
                String text = String.Empty;                                   //수신한 텍스트가 저장될 변수
                Packet Packet = new Packet();                                 //패킷 구조체

                string group;
                if (state.FW_LOCATION == "권치 컬버트(DC)")
                {
                    group = "S3";
                }
                else if (state.FW_LOCATION == "사상 컬버트(FM)")
                {
                    group = "S2";

                }
                else if (state.FW_LOCATION == "가열로 컬버트(RM)")
                {
                    group = "S1";
                }
                else
                {
                    group = "S4";
                }
                     
                int received;                                                 //수신한 바이트 수
                           
                try
                {
                    received = current.EndReceive(AR);                         //수신한 바이트 수
                }
                //소켓 에러가 발생하면 해당 소켓을 종료
                catch (SocketException)
                {
                    if (ServerGetPacket != null)
                    {
                        ServerGetPacket("disconnected\r\n", state.userIP);
                    }



                    sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 닫힘(화면 닫힘 버튼 입력)");
                    sqlconn.commandDB(sqlQuery);


                    wl = new MessageLog(state.userIP, "연결 끊김 (통신 단절)");
                    wl.writeLog(path, 1);


                    sqlQuery = string.Format("update FW_STATE set " + "CONNECTION = '{0}' where FW_IP = '{1}'", 0, state.userIP);
                    sqlconn.commandDB(sqlQuery);

                    current.Close();
                    state.workSocket = null;

                    received = -1;
                }
                catch (ArgumentException AE)
                {
                     
                    //리스트에서 해당 방화문 소켓정보 초기화

                    if (server_data[state.userIP].workSocket.Connected)
                    {
                        server_data[state.userIP].workSocket.Shutdown(SocketShutdown.Both);
                        server_data[state.userIP].workSocket.Close();
                        server_data[state.userIP].workSocket.Dispose();
                        server_data[state.userIP].workSocket = null;
                    }
                    else
                    {
                        server_data[state.userIP].workSocket = null;
                    }


                    received = -2;
                }
                catch (NullReferenceException)
                {

                    //해당 exception은 비동기 작업을 위해 전달하는 socket 인수가 null일경우 발생하는 에러
                    //메시지 송수신 중 연결이 끊긴경우 발생

                    if (ServerGetPacket != null)
                    {
                        ServerGetPacket("disconnected\r\n", state.userIP);
                    }




                    sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 4, "연결 끊김 (소켓 통신 불량)");
                    sqlconn.commandDB(sqlQuery);

                    sqlQuery = string.Format("update FW_STATE set " + "CONNECTION = '{0}' where FW_IP = '{1}'", 0, state.userIP);
                    sqlconn.commandDB(sqlQuery);

                    received = -3;
                }
                catch (ObjectDisposedException)
                {
                    sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 4, "연결 끊김 (소켓 통신 불량)");
                    sqlconn.commandDB(sqlQuery);

                    sqlQuery = string.Format("update FW_STATE set " + "CONNECTION = '{0}' where FW_IP = '{1}'", 0, state.userIP);
                    sqlconn.commandDB(sqlQuery);

                    received = -4;
                }
                catch (Exception e)
                {
                    received = -5;
                }

                byte[] recBuf;

                //클라이언트로부터 데이터를 수신한 경우 데이터 처리 후 로그 저장
                //데이터를 수신하지 않으면(받은 데이터의 길이가 0일경우) 다시 수신대기 상태로 돌아감
                //전문 크기 128 byte
 
                if (received > 0)
                {

                    string sqlStr = string.Format("UPDATE FW_STATE SET LAST_RECV_TIME = '{0}' WHERE FW_IP = '{1}'", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), state.userIP);
                    sqlconn.commandDB(sqlStr);
 
                    recBuf = new byte[received];
                    Array.Copy(state.buffer, recBuf, received);
                    text = Encoding.Default.GetString(recBuf);

                    string realText;

                    if (text.Length > 5)
                    {
                        realText = text.Substring(4);
                        realText = realText.Replace(Environment.NewLine, "");
                        realText = realText.Replace(" ", "");
                    }
                    else
                    {
                        realText = "";
                    }

                    int msgSize;


                    try
                    {
                        msgSize = Int32.Parse(text.Substring(0, 4));
                    }
                    catch
                    {
                        msgSize = 9999;

                    }

                    //전문 사이즈 체크
                    if (150 < System.Text.Encoding.Default.GetByteCount(realText))
                    {
                        //json 전문 파싱
                        Packet = recvPacket(text);

                        //제이슨 파싱중 에러가 발생하지 않은경우 (자세한 에러는 PACKET.OPCODE 부분의 값에 저장, recvPacket 메서드 확인)
                        if (Packet.DEVICEID != "ERROR")
                        {
                            //전원 전압 이상시 DB에 전원 전압 상태이상 표시
                            if (Packet.IN24V < 15 && Packet.IN24V >= 0)
                            {
                                sqlQuery = string.Format("update FW_STATE set " + "  LOWVOLTAGE = '{0}' where FW_IP = '{1}' ", 1, state.userIP);
                                sqlconn.commandDB(sqlQuery);

                                if (state.C_IN24V == 0)
                                {
                                    state.C_IN24V = 1;
                                }
                            }
                            else
                            {
                                sqlQuery = string.Format("update FW_STATE set " + "  LOWVOLTAGE = '{0}' where FW_IP = '{1}' ", 0, state.userIP);
                                sqlconn.commandDB(sqlQuery);
                                state.C_IN24V = 0;
                            }


                            //베터리 전압 이상시 DB에 상태이상 표시
                            if (Packet.INB24V < 15 && Packet.IN24V >= 0)
                            {
                                sqlQuery = string.Format("update FW_STATE set " + "  LOWVOLTAGE_B = '{0}' where FW_IP = '{1}' ", 1, state.userIP);
                                sqlconn.commandDB(sqlQuery);
                                if (state.C_INB24V == 0)
                                {
                                    state.C_INB24V = 1;
                                }
                            }
                            else
                            {
                                sqlQuery = string.Format("update FW_STATE set " + "  LOWVOLTAGE_B = '{0}' where FW_IP = '{1}' ", 0, state.userIP);
                                sqlconn.commandDB(sqlQuery);
                                state.C_INB24V = 0;
                            }


                            //Keep Alive 신호 수신시
                            if (Packet.OPCODE == "000")     //OPCODE는 문자열 000으로 수신받음
                            {

                                sqlQuery = string.Format("update FW_STATE set " + "LAST_RECV_TIME = '{0}' , OPCODE = '{1}', RSSI = '{2}' , IN24V = '{3}', INB24V = '{4}', S1 = '{5}', OUT1 = '{6}' , S2 = '{7}', OUT2 = '{8}' , S3=  '{9}', OUT3 = '{10}', S4 = '{11}', OUT4 = '{12}' where FW_IP = '{13}'",
                                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Packet.OPCODE, Packet.RSSI, Packet.IN24V, Packet.INB24V, Packet.S1, Packet.OUT1, Packet.S2, Packet.OUT2, Packet.S3, Packet.OUT3, Packet.S4, Packet.OUT4, state.userIP);
                                sqlconn.commandDB(sqlQuery);


                                //플래그 수정

                                if (state.sendflg1 == 0 && state.sendflg2 == 0 && state.sendflg3 == 0 && state.sendflg4 == 0)
                                {
                                    sqlQuery = string.Format("update FW_STATE set " + "STATE = '{0}' where FW_IP = '{1}' ", 0, state.userIP);
                                    sqlconn.commandDB(sqlQuery);
                                }
                                else
                                {
                                    if (state.numofdoor == 1)
                                    {
                                        if (Packet.S1 == "00")
                                        {
                                            if (Packet.OUT1 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg1 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path, 1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg1 += 1;
                                                    }
                                                    else if (state.sendflg1 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg1 += 1;
                                                    }
                                                    else if (state.sendflg1 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg1 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S1 == "11")
                                        {
                                            if (Packet.OUT1 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg1 = 0;
                                                }
                                            }
                                        }

                                    }
                                    else if(state.numofdoor == 2)
                                    {
                                        if (Packet.S1 == "00")
                                        {
                                            if (Packet.OUT1 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg1 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path,1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg1 += 1;
                                                    }
                                                    else if (state.sendflg1 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg1 += 1;
                                                    }
                                                    else if (state.sendflg1 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg1 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S1 == "11")
                                        {
                                            if (Packet.OUT1 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg1 = 0;
                                                }
                                            }
                                        }
                                        /////////////////////////////////////////////
                                        if (Packet.S2 == "00")
                                        {
                                            if (Packet.OUT2 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg2 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path, 1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg2 += 1;
                                                    }
                                                    else if (state.sendflg2 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg2 += 1;
                                                    }
                                                    else if (state.sendflg2 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg2 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S2 == "11")
                                        {
                                            if (Packet.OUT2 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg2 = 0;
                                                }
                                            }
                                        }
                                    }
                                    else if (state.numofdoor == 3)
                                    {
                                        if (Packet.S1 == "00")
                                        {
                                            if (Packet.OUT1 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg1 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path, 1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg1 += 1;
                                                    }
                                                    else if (state.sendflg1 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg1 += 1;
                                                    }
                                                    else if (state.sendflg1 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg1 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S1 == "11")
                                        {
                                            if (Packet.OUT1 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg1 = 0;
                                                }
                                            }
                                        }
                                        //////////////////////////////////////////
                                        if (Packet.S2 == "00")
                                        {
                                            if (Packet.OUT2 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg2 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path, 1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg2 += 1;
                                                    }
                                                    else if (state.sendflg2 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg2 += 1;
                                                    }
                                                    else if (state.sendflg2 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg2 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S2 == "11")
                                        {
                                            if (Packet.OUT2 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg2 = 0;
                                                }
                                            }
                                        }
                                        //////////////////////////////////////////
                                        if (Packet.S3 == "00")
                                        {
                                            if (Packet.OUT3 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg3 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path, 1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg3 += 1;
                                                    }
                                                    else if (state.sendflg3 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg3 += 1;
                                                    }
                                                    else if (state.sendflg3 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg3 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S3 == "11")
                                        {
                                            if (Packet.OUT3 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg3 = 0;
                                                }
                                            }
                                        }
                                    }
                                    else if (state.numofdoor == 4)
                                    {
                                        if (Packet.S1 == "00")
                                        {
                                            if (Packet.OUT1 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg1 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path, 1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg1 += 1;
                                                    }
                                                    else if (state.sendflg1 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg1 += 1;
                                                    }
                                                    else if (state.sendflg1 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg1 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S1 == "11")
                                        {
                                            if (Packet.OUT1 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg1 = 0;
                                                }
                                            }
                                        }
                                        ///////////////////////////////////////////
                                        if (Packet.S2 == "00")
                                        {
                                            if (Packet.OUT2 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg2 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path, 1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg2 += 1;
                                                    }
                                                    else if (state.sendflg2 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg2 += 1;
                                                    }
                                                    else if (state.sendflg2 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg2 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S2 == "11")
                                        {
                                            if (Packet.OUT2 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg2 = 0;
                                                }
                                            }
                                        }
                                        //////////////////////////////////////////
                                        if (Packet.S3 == "00")
                                        {
                                            if (Packet.OUT3 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg3 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path, 1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg3 += 1;
                                                    }
                                                    else if (state.sendflg3 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg3 += 1;
                                                    }
                                                    else if (state.sendflg3 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg3 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S3 == "11")
                                        {
                                            if (Packet.OUT3 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg3 = 0;
                                                }
                                            }
                                        }
                                        /////////////////////////////////////////
                                        if (Packet.S4 == "00")
                                        {
                                            if (Packet.OUT4 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 1)
                                                {
                                                    if (state.sendflg4 == 2)
                                                    {
                                                        sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                                                        sqlconn.commandDB(sqlQuery);

                                                        wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                                                        wl.writeLog(path, 1);

                                                        sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                                                        sqlconn.commandDB(sqlQuery);

                                                        state.sendflg4 += 1;
                                                    }
                                                    else if (state.sendflg4 == 1)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                        state.sendflg4 += 1;
                                                    }
                                                    else if (state.sendflg4 == 0)
                                                    {
                                                        send(state.userIP, sendPacket2("119"));
                                                    }
                                                }
                                                else if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg4 = 0;
                                                }
                                            }
                                        }
                                        else if (Packet.S4 == "11")
                                        {
                                            if (Packet.OUT4 == "11")
                                            {
                                                if (getFireValue(state.userIP) == 0)
                                                {
                                                    send(state.userIP, sendPacket2("000"));
                                                    state.sendflg4 = 0;
                                                }
                                            }
                                        }
                                    }
                          
                                }

                            }

                            //화재 신호에 대한 응답 메시지 수신시
                            else if (Packet.OPCODE == "119")
                            {
                                sqlQuery = string.Format("update FW_STATE set " + "LAST_RECV_TIME = '{0}' , OPCODE = '{1}', RSSI = '{2}' , IN24V = '{3}', INB24V = '{4}', S1 = '{5}', OUT1 = '{6}' , S2 = '{7}', OUT2 = '{8}' , S3=  '{9}', OUT3 = '{10}', S4 = '{11}', OUT4 = '{12}' where FW_IP = '{13}'",
                                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Packet.OPCODE, Packet.RSSI, Packet.IN24V, Packet.INB24V, Packet.S1, Packet.OUT1, Packet.S2, Packet.OUT2, Packet.S3, Packet.OUT3, Packet.S4, Packet.OUT4, state.userIP);
                                sqlconn.commandDB(sqlQuery);

                                //방화문 수동조작한 경우
                                if (Packet.OUT1 == "00")      
                                {
                                    if (getFireValue(state.userIP) != 1)
                                    {
                                        //현장에서 수동으로 방화문을 연 경우
                                        if (Packet.S1 == "00")
                                        {
                                            sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 열림(수동)");
                                            sqlconn.commandDB(sqlQuery);

                                            wl = new MessageLog(state.userIP, "방화문 열림(수동)");
                                            wl.writeLog(path, 1);

                                        }
                                        //현장에서 수동으로 방화문을 닫은 경우
                                        else if (Packet.S1 == "11")
                                        {
                                            sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 닫힘(수동)");
                                            sqlconn.commandDB(sqlQuery);

                                            state.sendflg1 = 0;

                                            wl = new MessageLog(state.userIP, "방화문 닫힘(수동)");
                                            wl.writeLog(path, 1);
                                        }
                                    }
                                }
                                else if (Packet.OUT1 == "11")
                                {
                                    if (getFireValue(state.userIP) == 0)
                                    {
                                        send(state.userIP, sendPacket2("000"));
                                        state.sendflg1 = 0;
                                    }
                                    else if(getFireValue(state.userIP) == 1)
                                    {
                                        if (Packet.S1 == "11")
                                        {
                                            if (state.sendflg1 != 0)
                                            {
                                                sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 닫힘(정상)");
                                                sqlconn.commandDB(sqlQuery);

                                                wl = new MessageLog(state.userIP, "방화문 닫힘(정상)");
                                                wl.writeLog(path, 1);
                                            }
                                            state.sendflg1 = 0;
                                        }
                                    }
                                }
                                /////////////////////////////////////////////////////////
                                if (Packet.OUT2 == "00")
                                {
                                    if (getFireValue(state.userIP) != 1)
                                    {
                                        //현장에서 수동으로 방화문을 연 경우
                                        if (Packet.S2 == "00")
                                        {
                                            sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 열림(수동)");
                                            sqlconn.commandDB(sqlQuery);

                                            wl = new MessageLog(state.userIP, "방화문 열림(수동)");
                                            wl.writeLog(path, 1);

                                        }
                                        //현장에서 수동으로 방화문을 닫은 경우
                                        else if (Packet.S2 == "11")
                                        {
                                            sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 닫힘(수동)");
                                            sqlconn.commandDB(sqlQuery);

                                            state.sendflg2 = 0;

                                            wl = new MessageLog(state.userIP, "방화문 닫힘(수동)");
                                            wl.writeLog(path, 1);
                                        }
                                    }
                                }
                                else if (Packet.OUT2 == "11")
                                {
                                    if (getFireValue(state.userIP) == 0)
                                    {
                                        send(state.userIP, sendPacket2("000"));
                                        state.sendflg2 = 0;
                                    }
                                    else if (getFireValue(state.userIP) == 1)
                                    {
                                        if (Packet.S2 == "11")
                                        {
                                            if (state.sendflg2 != 0)
                                            {
                                                sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 닫힘(정상)");
                                                sqlconn.commandDB(sqlQuery);

                                                wl = new MessageLog(state.userIP, "방화문 닫힘(정상)");
                                                wl.writeLog(path, 1);
                                            }
                                            state.sendflg2 = 0;
                                        }
                                    }
                                }
                                /////////////////////////////////////////////////////////
                                if (Packet.OUT3 == "00")
                                {
                                    if (getFireValue(state.userIP) != 1)
                                    {
                                        //현장에서 수동으로 방화문을 연 경우
                                        if (Packet.S3 == "00")
                                        {
                                            sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 열림(수동)");
                                            sqlconn.commandDB(sqlQuery);

                                            wl = new MessageLog(state.userIP, "방화문 열림(수동)");
                                            wl.writeLog(path, 1);

                                        }
                                        //현장에서 수동으로 방화문을 닫은 경우
                                        else if (Packet.S3 == "11")
                                        {
                                            sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 닫힘(수동)");
                                            sqlconn.commandDB(sqlQuery);

                                            state.sendflg3 = 0;

                                            wl = new MessageLog(state.userIP, "방화문 닫힘(수동)");
                                            wl.writeLog(path, 1);
                                        }
                                    }
                                }
                                else if (Packet.OUT3 == "11")
                                {
                                    if (getFireValue(state.userIP) == 0)
                                    {
                                        send(state.userIP, sendPacket2("000"));
                                        state.sendflg3 = 0;
                                    }
                                    else if (getFireValue(state.userIP) == 1)
                                    {
                                        if (Packet.S3 == "11")
                                        {
                                            if (state.sendflg3 != 0)
                                            {
                                                sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 닫힘(정상)");
                                                sqlconn.commandDB(sqlQuery);

                                                wl = new MessageLog(state.userIP, "방화문 닫힘(정상)");
                                                wl.writeLog(path, 1);
                                            }
                                            state.sendflg3 = 0;
                                        }
                                    }
                                }
                                //
                                if (Packet.OUT4 == "00")
                                {
                                    if (getFireValue(state.userIP) != 1)
                                    {
                                        //현장에서 수동으로 방화문을 연 경우
                                        if (Packet.S4 == "00")
                                        {
                                            sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 열림(수동)");
                                            sqlconn.commandDB(sqlQuery);

                                            wl = new MessageLog(state.userIP, "방화문 열림(수동)");
                                            wl.writeLog(path, 1);

                                        }
                                        //현장에서 수동으로 방화문을 닫은 경우
                                        else if (Packet.S4 == "11")
                                        {
                                            sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 닫힘(수동)");
                                            sqlconn.commandDB(sqlQuery);

                                            state.sendflg4 = 0;

                                            wl = new MessageLog(state.userIP, "방화문 닫힘(수동)");
                                            wl.writeLog(path, 1);
                                        }
                                    }
                                }
                                else if (Packet.OUT4 == "11")
                                {
                                    if (getFireValue(state.userIP) == 0)
                                    {
                                        send(state.userIP, sendPacket2("000"));
                                        state.sendflg4 = 0;
                                    }
                                    else if (getFireValue(state.userIP) == 1)
                                    {
                                        if (Packet.S4 == "11")
                                        {
                                            if (state.sendflg4 != 0)
                                            {
                                                sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2, "방화문 닫힘(정상)");
                                                sqlconn.commandDB(sqlQuery);

                                                wl = new MessageLog(state.userIP, "방화문 닫힘(정상)");
                                                wl.writeLog(path, 1);
                                            }
                                            state.sendflg4 = 0;
                                        }
                                    }
                                }
                            }
                        }

                    } //if (text.Substring(0, 4) == "0130")
                    else
                    {
                        state.buffer = new byte[StateObject.BufferSize];
                        received = 0;

                        if (msgSize == 9999)
                        {
                            if (text != "\n")
                            {
                                wl = new MessageLog(state.userIP, text + " 전문수신실패");
                                wl.writeLog(path, 4);
                            }
                        }
                        else
                        {
                            wl = new MessageLog(state.userIP, text + " 전문 길이 이상(" + text.Length + "byte)");
                            wl.writeLog(path, 4);
                        }


                    }

                    if (ServerGetPacket != null)
                    {
                        ServerGetPacket(text + "\r\n", state.userIP);
                    }

                    try
                    {
                        // 다음 전문 수신을 위해 비동기 수신 재개
                        current.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), server_data[state.userIP]);
                    }
                    catch (Exception e)
                    {
                        state.buffer = new byte[StateObject.BufferSize];
                        //해당 비동기 작업에서 오류 발생시 소켓 초기화
                        if (server_data[state.userIP].workSocket != null)
                        {
                            if (server_data[state.userIP].workSocket.Connected)
                            {
                                server_data[state.userIP].workSocket.Shutdown(SocketShutdown.Both);
                                server_data[state.userIP].workSocket.Close();
                                server_data[state.userIP].workSocket.Dispose();
                                server_data[state.userIP].workSocket = null;
                            }
                            else
                            {
                                server_data[state.userIP].workSocket = null;
                            }
                        }

                    }

                }
                else
                {
                   
                    state.buffer = new byte[StateObject.BufferSize];

                    try
                    {
                        // 다음 전문 수신을 위해 비동기 수신 재개
                        current.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), server_data[state.userIP]);
                    }
                    catch (Exception e)
                    {
                        state.buffer = new byte[StateObject.BufferSize];

                        //해당 비동기 작업에서 오류 발생시 소켓 초기화
                        if (server_data[state.userIP].workSocket != null)
                        {
                            if (server_data[state.userIP].workSocket.Connected)
                            {
                                server_data[state.userIP].workSocket.Shutdown(SocketShutdown.Both);
                                server_data[state.userIP].workSocket.Close();
                                server_data[state.userIP].workSocket.Dispose();
                                server_data[state.userIP].workSocket = null;
                            }
                            else
                            {
                                server_data[state.userIP].workSocket = null;
                            }
                        }

                    }

                }
            }                      
        }

        /// <summary>
        /// 이름 : send
        /// 기능 : 클라이언트에게 실제 전문을 전송하는 메서드
        /// 인수 : userIP -> 메시지를 송신할 방화문 IP, text -> 전문 
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        ///  </summary>
        public void send(String userIP, String text)
        {
            //메시지를 보낼 방화문의 ip 확인
            if (server_data.ContainsKey(userIP) != false)
            {
                StateObject state = server_data[userIP];                    //메시지를 보낼 소켓을 가지고 있는 구조체
                Socket current = state.workSocket;                          //메시지를 보낼 소켓
                Byte[] data;
                data = new Byte[StateObject.BufferSize];
                data = Encoding.Default.GetBytes(text);


                //화재 전문 전송 시
                if (text == sendPacket2("119"))
                {
                    //전송플래그 변경
                    switch (server_data[userIP].numofdoor)
                    {
                        case 1:
                            server_data[userIP].sendflg1 = 1;
                            break;
                        case 2:
                            server_data[userIP].sendflg1 = 1;
                            server_data[userIP].sendflg2 = 1;
                            break;
                        case 3:
                            server_data[userIP].sendflg1 = 1;
                            server_data[userIP].sendflg2 = 1;
                            server_data[userIP].sendflg3 = 1;
    
                            break;
                        case 4:
                            server_data[userIP].sendflg1 = 1;
                            server_data[userIP].sendflg2 = 1;
                            server_data[userIP].sendflg3 = 1;
                            server_data[userIP].sendflg4 = 1;

                            break;

                        default:
                            break;
                    }
                    sqlQuery = string.Format("update FW_STATE set " + "LASTMSG = '{0}'  where FW_IP = '{1}'", text, server_data[userIP].userIP);
                    sqlconn.commandDB(sqlQuery);
                }
                else
                {
                    sqlQuery = string.Format("update FW_STATE set " + "LASTMSG = '{0}'  where FW_IP = '{1}'", text, server_data[userIP].userIP);
                    sqlconn.commandDB(sqlQuery);
                }

                if (state.workSocket != null && state.workSocket.Connected == true)
                {

                    try
                    {
                        current.Send(data, 0, data.Length, SocketFlags.None);

                        wl = new MessageLog(state.userIP, text);
                        wl.writeLog(path, 2);                //전송메시지 로그 기록

                        server_data[userIP].f_Run = 1;
                    }
                    catch (Exception ex)
                    {


                        wl = new MessageLog(state.userIP, "메시지 전송 ERROR -"+ text);
                        wl.writeLog(path, 4);


                        return;
                    }
                }
                else
                {
                    //메시지 전송 실패시 방화문 상태 고장으로 변경(FW_STATE_   값을 2로 변경)
                    sqlQuery = string.Format("update FW_STATE set " + "STATE = '{0}' where FW_IP = '{1}' ", 2, state.userIP);
                    sqlconn.commandDB(sqlQuery);


                    if (ServerGetPacket != null)
                    {
                        ServerGetPacket(state.DeviceID + " 이 서버에 연결되어 있지 않습니다.\r\n", state.userIP);
                    }
                }

            }
        }

        /// <summary>
        /// 이름 : sendPacket
        /// 기능 : OPCODE를 가지고 전문을 생성하는 메서드
        /// 인수 : OPCODE -> 생성할 전문코드      
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        ///  </summary>

        public string sendPacket(string OPCODE,int number)
        {
            string jsonStr;                                                 //제이슨 문자열을 담을 변수

            JObject JObj = new JObject();                                   //json 변수
            JObj.Add("OPCODE", OPCODE);                                     //opcode를 json에 추가
            if(number == 1)
            {
                JObj.Add("OUT1", "11");
                JObj.Add("OUT2", "00");
                JObj.Add("OUT3", "00");
                JObj.Add("OUT4", "00");
            }
            else if (number == 2)
            {
                JObj.Add("OUT1", "00");
                JObj.Add("OUT2", "11");
                JObj.Add("OUT3", "00");
                JObj.Add("OUT4", "00");
            }
            else if (number == 3)
            {
                JObj.Add("OUT1", "00");
                JObj.Add("OUT2", "00");
                JObj.Add("OUT3", "11");
                JObj.Add("OUT4", "00");
            }
            else if (number == 4)
            {
                JObj.Add("OUT1", "00");
                JObj.Add("OUT2", "00");
                JObj.Add("OUT3", "00");
                JObj.Add("OUT4", "11");
            }
            jsonStr = JObj.ToString();                                      //json을 문자열로 변환
            jsonStr = jsonStr.Replace(Environment.NewLine, "");             //Trim
            jsonStr = jsonStr.Replace(" ", "");
            //전송로그기록
            return jsonStr;                                                 //문자열 반환
        }

        /// <summary>
        /// 이름 : recvPacket
        /// 기능 : 수신한 JSON 전문을 파싱하여 구조체에 등록하는 메서드
        /// 인수 : text -> 수신한 JSON 전문      
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        ///  </summary>


        public string sendPacket2(string OPCODE)
        {

            string jsonStr;                                                 //제이슨 문자열을 담을 변수

            JObject JObj = new JObject();                                   //json 변수
            JObj.Add("OPCODE", OPCODE);

            jsonStr = JObj.ToString();
            jsonStr = jsonStr.Replace(Environment.NewLine, "");
            jsonStr = jsonStr.Replace(" ", "");
                        
            return jsonStr;
       
        }


        public Packet recvPacket(string text)
        {
            try
            {
                Packet packet;                                                  //값을 담을 구조체 호출
                string jsonText = text.Substring(4, text.Length - 4);           //문자열의 BODY부분 추출
                JObject JObj = JObject.Parse(jsonText);                         //String을 JSON으로 파싱

                JToken DEVICEID = JObj["DEVICEID"];
                JToken OPCODE = JObj["OPCODE"];
                JToken RSSI = JObj["RSSI"];
                JToken IN24V = JObj["IN24V"];
                JToken INB24V = JObj["INB24V"];
                JToken S1 = JObj["S1"];
                JToken OUT1 = JObj["OUT1"];
                JToken S2 = JObj["S2"];
                JToken OUT2 = JObj["OUT2"];
                JToken S3 = JObj["S3"];
                JToken OUT3 = JObj["OUT3"];
                JToken S4 = JObj["S4"];
                JToken OUT4 = JObj["OUT4"];



                //구조체 packet에 수신한 데이터 저장
                packet = new Packet(DEVICEID.ToString(),
                                   OPCODE.ToString(),
                                   (int)RSSI,
                                   (int)IN24V,
                                   (int)INB24V,
                                   S1.ToString(),
                                   OUT1.ToString(),
                                   S2.ToString(),
                                   OUT2.ToString(),
                                   S3.ToString(),
                                   OUT3.ToString(),
                                   S4.ToString(),
                                   OUT4.ToString());

                return packet;                                                //구조체 리턴
            }
            //2020-07-14 메시지 수신시 발생하는 에러구문을 분류하여 처리
            catch (JsonReaderException)
            {
                Packet packet = new Packet("ERROR", "NOTJSON", 0, 0, 0, "0", "0", "0", "0", "0", "0", "0", "0"); //수신 메시지가 잘려서 Json포멧이 아닐경우
                return packet;
            }
            catch (ArgumentNullException)
            {
                Packet packet = new Packet("ERROR", "NULL", 0, 0, 0, "0", "0", "0", "0", "0", "0", "0", "0"); //수신메시지가 null인경우
                return packet;
            }
            catch (FormatException)
            {
                Packet packet = new Packet("ERROR", "WRONGFORMAT", 0, 0, 0, "0", "0", "0", "0", "0", "0", "0", "0"); //json 포멧안의 데이터 형식이 틀릴경우
                return packet;
            }
            catch (Exception)
            {
                Packet packet = new Packet("ERROR", "CHECKLOG", 0, 0, 0, "0", "0", "0", "0", "0", "0", "0", "0"); // 기타 다른 에러( 로그화면 확인 필요)
                return packet;
            }
        }

        /// <summary>
        /// 이름 : checkTimeOUT
        /// 기능 : 방화문으로부터 5분간 메시지 수신이 없는 경우 해당 방화문의 연결을 끊는 메서드
        /// 인수 : text -> 수신한 JSON 전문      
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        /// 비고 : 2020-07-08 온라인 테스트 중 현장 작업자의 요청으로 기존 1:30에서 5:00으로 타임아웃 대기시간 증가
        ///  </summary>
        //온라인 테스트 체크 필!!
        public void checkTimeOUT(object sender, EventArgs e)
        {
            //온라인 테스트 체크 필!!

            //Dictionary순회하며 타임아웃 검사
            string sql = string.Format("SELECT FW_IP, LAST_RECV_TIME, CONNECTION FROM FW_STATE");
            DataSet ds = sqlconn.readData(sql);

            foreach (DataRow r in ds.Tables[0].Rows)
            {
                if ((int)r.ItemArray[2] != 0)
                {
                    string fw_ip = string.Format("{0}", r.ItemArray[0]);
                    DateTime last_recv_time;
                    if (r.ItemArray[1].ToString() != "")
                    {
                        last_recv_time = (DateTime)r.ItemArray[1];


                        //DateTime curr_time = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        DateTime curr_time = DateTime.Now;

                        TimeSpan dateDiff = curr_time - last_recv_time;     //현재 시간과 마지막 메시지 수신시간의 차를 구함

                        int diffDay = dateDiff.Days;
                        int diffHous = dateDiff.Hours;
                        int diffMin = dateDiff.Minutes;
                        int diffSec = dateDiff.Seconds;

                        // 5분이 넘는다면
                        if (diffMin > 5 || diffHous > 0 || diffDay > 0)
                        {
                            //해당 소켓 연결 해제
                            try
                            {
                                if (server_data[fw_ip].workSocket.Connected)
                                {
                                    server_data[fw_ip].workSocket.Shutdown(SocketShutdown.Both);
                                    server_data[fw_ip].workSocket.Close();
                                    server_data[fw_ip].workSocket.Dispose();
                                    server_data[fw_ip].workSocket = null;
                                }
                                else
                                {
                                    server_data[fw_ip].workSocket = null;
                                }
                            }
                            catch (NullReferenceException)
                            {
                                server_data[fw_ip].workSocket = null;
                            }

                            sqlQuery = string.Format("update FW_STATE set " + "CONNECTION = '{0}' where FW_IP = '{1}'", 0, fw_ip);
                            sqlconn.commandDB(sqlQuery);


                        }
                        // 01초를 체크하기 위한 로직 
                        else if (diffMin == 5)
                        {
                            if (diffSec > 01)
                            {
                                try
                                {
                                    if (server_data[fw_ip].workSocket.Connected)
                                    {
                                        server_data[fw_ip].workSocket.Shutdown(SocketShutdown.Both);
                                        server_data[fw_ip].workSocket.Close();
                                        server_data[fw_ip].workSocket.Dispose();
                                        server_data[fw_ip].workSocket = null;
                                    }
                                    else
                                    {
                                        server_data[fw_ip].workSocket = null;
                                    }
                                }
                                catch (NullReferenceException)
                                {
                                    server_data[fw_ip].workSocket = null;
                                }

                                sqlQuery = string.Format("update FW_STATE set " + "CONNECTION = '{0}' where FW_IP = '{1}'", 0, fw_ip);
                                sqlconn.commandDB(sqlQuery);

                            }
                        }
                    }
                }
            
            }
        }

        /// <summary>
        /// 이름 : init_server
        /// 기능 : 서버가 사용하는 자원을 초기화 시키는 메서드
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        /// </summary>
        private void init_server()
        {
            server_data.Clear();

            //DB에서 방화문 정보 검색해서 클라이언트 배열에 저장
            sqlQuery = String.Format("SELECT FW_IP, DEVICEID,FW_LOCATION FROM FW_LIST");
            DataSet ds = sqlconn.readData(sqlQuery);


            if (ds != null)
            {
                if (ds.Tables.Count != 0)
                {
                    foreach (DataRow r in ds.Tables[0].Rows)
                    {
                        StateObject state = new StateObject();

                        if (server_data.ContainsKey(string.Format("{0}", r.ItemArray[0])) != true)
                        {
                            server_data.Add(string.Format("{0}", r.ItemArray[0]), state);
                            server_data[string.Format("{0}", r.ItemArray[0])].DeviceID = string.Format("{0}", r.ItemArray[1]);
                            server_data[string.Format("{0}", r.ItemArray[0])].FW_LOCATION = string.Format("{0}", r.ItemArray[2]);
                        }
                        else
                        {
                            server_data[string.Format("{0}", r.ItemArray[0])].numofdoor += 1;
                        }

                    }
                }
            }

        }

        public void chk_state_wall(string PS_S, string PS_O, string group, StateObject state)
        {
            //방화문이 열림인 경우
            if (PS_S == "00")
            {
                if (PS_O == "11")
                {
                    if(getFireValue(state.userIP) == 1)
                    {
                        if(state.sendflg1 == 2)
                        {
                            sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group, state.userIP, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 3, "방화문 고장 또는 문고임");
                            sqlconn.commandDB(sqlQuery);

                            wl = new MessageLog(state.userIP, "방화문 고장 또는 문고임");
                            wl.writeLog(path, 1);

                            sqlQuery = string.Format("update FW_STATE SET STATE = '2' where FW_IP = '{0}'", state.userIP);
                            sqlconn.commandDB(sqlQuery);

                            state.sendflg1 += 1;
                        }
                        else if(state.sendflg1 == 1)
                        {
                            send(state.userIP, sendPacket2("119"));
                            state.sendflg1 += 1;
                        }
                        else if(state.sendflg1 == 0)
                        {
                            send(state.userIP, sendPacket2("119"));
                        }
                    }
                    else if(getFireValue(state.userIP) == 0)
                    {
                        send(state.userIP, sendPacket2("000"));
                        state.sendflg1 = 0;
                    }
                }
            }
            else if(PS_S == "11")
            {
                if(PS_O == "11")
                {
                    if (getFireValue(state.userIP) == 0)
                    {
                        send(state.userIP, sendPacket2("000"));
                        state.sendflg1 = 0;
                    }
                }
            }
        }

        public int getFireValue(string fw_ip)
        {
            string sql = string.Format("SELECT [FW_IP],[FW_LOCATION],[FIRE] FROM [dbo].[FW_FIREDETECT] WHERE [FW_IP] = '{0}'",fw_ip);
            DataSet ds = sqlconn.readData(sql);

            return Int32.Parse(ds.Tables[0].Rows[0].ItemArray[2].ToString());
        }
    }
   
}

