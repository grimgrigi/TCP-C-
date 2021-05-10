using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;


namespace fwServer
{
    //폼간 데이터 송수신을 위한 이벤트핸들러
    public delegate void DataGetEventHandler(List<string> ip);


    public partial class serverHMI : Form
    {
        

        AsynServer fwServer;
        public Timer test_checked = new Timer();
        Timer checkedFire = new Timer();
        Timer db_show_timer = new Timer();
        Timer check_open_timer = new Timer();
        Timer del_log = new Timer();

        MessageLog wl = null;
        string path = @"D:\logFile\TASKCON";
       
        SqlConnect sqlconn = new SqlConnect();
        string sqlQuery;
                      
        List<object> fireList = new List<object>();
        //###################################################################################################
        // 이벤트 관련 부분은 server에서 전달받은 데이터를 폼에 표시하기위해 대리자를 통해 전달하는 부분입니다.
        // 미리 정의한 이벤트가 서버에서 발생하면 서버는 userIP 혹은 msg를 Form 에 전달합니다.
        // Form은 전달받은 인수를 가지고 Form의 컨트롤을 스레드로부터 안전한 방식으로 변경합니다.
        //###################################################################################################
        #region 이벤트 관련
        delegate void delegateProcessPacket(String text, String userIP);    //발생한 이벤트를 전달받음
        public void DelegateProcessPacket(String text, String userIP)
        {

            //스레드로부터 안전한 호출
            if (InvokeRequired)
            {
                try{
                delegateProcessPacket c = new delegateProcessPacket(DelegateProcessPacket);
                Invoke(c, new object[] { text, userIP });        //스레드로부터 안전하게 실제 컨트롤을 변경하는 코드
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
            else
            {
                ProcessPacket(text, userIP);    //실제 컨트롤을 변경하는 코드
            }
        }

        //수신받은 메시지를 표시하는 메소드
        private void ProcessPacket(String text, String userIP)
        {
            if (StopDisplay.Checked != true)
            {
                bool msgSelectFlag = false;

                foreach (var item in userBox.CheckedItems)
                {
                    string[] chkip = item.ToString().Split('(');
                    if (userIP != null)
                    {
                        if (chkip[0].Trim() == userIP.Trim())
                        {
                            msgSelectFlag = true;
                        }
                    }
                }
                if (msgSelectFlag == true)
                {
                    msgViewer.AppendText(userIP + " : " + text);
                }
            }
            
        }

        delegate void AddUserToUI(String userIP);               //발생한 이벤트를 전달받음

        public void addUserToUI(String userIP)
        {
            if (InvokeRequired)                                 //컨트롤의 변경이 서버에서 일어났는지 경우
            {
                AddUserToUI c = new AddUserToUI(addUserToUI);   //AsynServer클래스에서 받은 userIP를 addUserToUI에 전달합니다.
                Invoke(c, new object[] { userIP });             //스레드로부터 안전하게 실제 컨트롤을 변경하는 코드
            }
            else
            {
                AddUser(userIP);                                //실제 컨트롤을 변경하는 코드
            }
        }

        //리스트박스에 유저 추가

        private void AddUser(String userIP)
        {
            userBox.Items.Add(userIP);           
        }

        delegate void DeleteUserUI(String userIP);                           //발생한 이벤트를 전달받음
        public void deleteUserUI(String userIP)
        {
            if (InvokeRequired)                                              //컨트롤의 변경이 서버에서 일어났는지 경우
            {
                DeleteUserUI c = new DeleteUserUI(deleteUserUI);             //AsynServer클래스에서 받은 userIP를 DeleteUserUI에 전달합니다.
                Invoke(c, new object[] { userIP });                          //스레드로부터 안전하게 실제 컨트롤을 변경하는 코드
            }                                                                
            else
            {
                DeleteUser(userIP);                                          //실제 컨트롤을 변경하는 코드
            }                                                                
        }

        //ListBox에서 유저 삭제

        private void DeleteUser(String userIP)
        {
            userBox.Items.Remove(userIP);
        }
        #endregion




        public serverHMI()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 이름 : serverHMI_Load
        /// 기능 : Form을 생성하고 이벤트 및 타이머를 등록합니다. 서버를 동작시킵니다.
        /// 인수 : 이벤트      
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철
        ///  </summary>
        private void serverHMI_Load(object sender, EventArgs e)
        {
            //초기화면 설정
            int portNum = 9000;

            msgViewer.Visible = true;
            dataGridView1.Visible = false;

            //서버시작
            fwServer = new AsynServer(portNum);

            fwServer.StartServer();
            //서버로 부터 받은 데이터를 폼에 표시하기 위한 이벤트 등록
                //fwServer.AddUserEvent += addUserToUI;                                        //접속유저추가
            fwServer.ServerGetPacket += DelegateProcessPacket;                           //메시지 로그
                //fwServer.deleteUserEvent += deleteUserUI;                                    //접속유저 삭제

            sqlQuery = String.Format("SELECT [FW_IP],MAX([FW_LOCATION]) FROM FW_LIST GROUP BY [FW_IP]");
            DataSet ds = sqlconn.readData(sqlQuery);

            //리스트에 방화문 입력 (DB select)
            if (ds != null)
            {
                if (ds.Tables.Count != 0)
                {
                    foreach (DataRow r in ds.Tables[0].Rows)
                    {
                        AddUser(string.Format("{0} ({1})", r.ItemArray[0], r.ItemArray[1]));
                    }
                }

            }
            //타이머등록
            
            //수동 문닫힘 확인을 위한 타이머
            check_open_timer.Interval = 1 * 1000;
            check_open_timer.Tick += check_open_timer_Tick;
            check_open_timer.Start();

            //타임아웃 감지를 위한 타이머
            test_checked.Interval = 15 * 1000;                                           //타이머 주기
            test_checked.Tick += fwServer.checkTimeOUT;                                  //작동함수
            test_checked.Start();

            //화재감지 타이머
            checkedFire.Interval = 10 * 1000;                                            //타이머 주기
            checkedFire.Tick += CheckedFire_Tick;                                        //작동함수
            checkedFire.Start();

            del_log.Interval = 24 * 60 * 60 * 1000;
            del_log.Tick += Del_log_Tick;
            del_log.Start();



        }

        private void Del_log_Tick(object sender, EventArgs e)
        {

            try

            {
                DirectoryInfo di = new DirectoryInfo(string.Format(path));
                //삭제할 경로에 파일들이 존재한다면

                if (di.Exists)

                {
                    FileInfo[] files = di.GetFiles();

                    //생성된지 1주일 된 파일 지우기 위한 날짜 지정

                    string date = DateTime.Today.AddDays(-14).ToString("yyyy-MM-dd");



                    foreach (FileInfo file in files)

                    {

                        //파일의 마지막 쓰여진 시간과 date 날짜와 비교

                        if (date.CompareTo(file.LastWriteTime.ToString("yyyy-MM-dd")) > 0)

                        {

                            //만약 마지막으로 쓰여진 시간이 1주일 지난 파일들이라면 

                            //확장자가 .log인 파일들 지워라

                            if (System.Text.RegularExpressions.Regex.IsMatch(file.Name, ".txt"))

                            {
                                File.Delete(di + "\\" + file.Name);
                            }

                        }

                    }

                }

            }

            catch (Exception ex)
            {
                return;
            }


        }






        /// <summary>
        /// 이름 : serverHMI_FormClosing
        /// 기능 : 우측 상단의 종료버튼 클릭시 동작하는 메서드
        /// 인수 : 이벤트     
        /// 날짜 : 2020-07-02 
        /// 수정자 : 하현철

        ///  </summary>
        private void serverHMI_FormClosing(object sender, FormClosingEventArgs e)
        {
            //
            if (MessageBox.Show("종료 버튼을 클릭하면 프로그램이 종료 됩니다. 정말 종료하시겠습니까?", "ALRAM", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                //서버와 연결된 모든 소켓을 닫음
                if (fwServer != null)
                {
                    fwServer.CloseAllSockets();

                }
            }
        }
        /// 이름 : check_open_timer_Tick
        /// 기능 : HMI에서 문열림 신호를 받으면 처리하는 타이머
        /// 인수 : 타이머
        /// 날짜 : 2020-07-15
        /// 수정자 : 하현철
 

        void check_open_timer_Tick(object sender, EventArgs e)
        {
            
            //string sql = string.Format("SELECT FW_IP,MAX(OPENCLOSE) FROM FW_LIST GROUP BY FW_IP");
            string sql = string.Format("SELECT FW_IP,OPENCLOSE,NUMBER FROM FW_LIST ORDER BY FW_IP");
            DataSet ds = sqlconn.readData(sql);
            foreach (DataRow r in ds.Tables[0].Rows)
            {
                if ((int)r.ItemArray[1] == 1)
                {


                    fwServer.send(string.Format("{0}",r.ItemArray[0]), fwServer.sendPacket("119",Int32.Parse(r.ItemArray[2].ToString())));

                    sql = string.Format("UPDATE FW_LIST SET OPENCLOSE = 0 WHERE FW_IP = '{0}' AND NUMBER = '{1}' ", string.Format("{0}", r.ItemArray[0]), string.Format(r.ItemArray[2].ToString()));

                    sqlconn.commandDB(sql);

                    string group;
                    if (fwServer.server_data[string.Format("{0}", r.ItemArray[0])].FW_LOCATION == "권치 컬버트(DC)")
                    {
                        group = "S3";
                    }
                    else if (fwServer.server_data[string.Format("{0}", r.ItemArray[0])].FW_LOCATION == "사상 컬버트(FM)")
                    {
                        group = "S2";

                    }
                    else if (fwServer.server_data[string.Format("{0}", r.ItemArray[0])].FW_LOCATION == "가열로 컬버트(RM)")
                    {
                        group = "S1";

                    }
                    else
                    {
                        group = "S4";

                    }

                    sqlQuery = string.Format("INSERT INTO FW_ALRAM" + "(FW_GROUP, FW_IP , TIME ,MSG_CODE, MSG_LOG ) Values ( '{0}' , '{1}' , '{2}','{3}', '{4}' )", group,string.Format("{0}",r.ItemArray[0]), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2 ,"방화문 닫힘(화면 닫힘 버튼 입력)");
                    sqlconn.commandDB(sqlQuery);

                    wl = new MessageLog(string.Format("{0}", r.ItemArray[0]),"방화문 닫힘(화면 닫힘 버튼 입력)");
                    wl.writeLog(path, 1);
                }
            }
        }

        //해당 메소드는 테스트를 위한 메소드입니다.
        //화재 감지 
        public void CheckedFire_Tick(object sender, EventArgs e)
        {

            string sql = string.Format("SELECT [FW_IP],[FW_LOCATION] ,[FIRE] FROM [dbo].[FW_FIREDETECT]");
            DataSet ds = sqlconn.readData(sql);

            foreach (DataRow r in ds.Tables[0].Rows)
            {
                if (Int32.Parse(r.ItemArray[2].ToString()) == 1)
                {
                    fwServer.send(r.ItemArray[0].ToString(), fwServer.sendPacket2("119"));
                }
                                        
            }
          
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        // 이름 : sendJson_btn_Click
        // 기능 : sendJson버튼을 클릭했을 때 발생하는 이벤트 처리기 입니다.
        // 날짜 : 2020-02-10 
        // 수정자 : 하현철
        // 비고 : 해당 메소드는 테스트용 임시 코드입니다. 추후 보강이 필요합니다.
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::




        private void ClearBtn_Click(object sender, EventArgs e)
        {
            msgViewer.Clear();
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        // 이름 : DB_Show_Click
        // 기능 : 방화문 현재상태 버튼을 클릭했을 때 발생하는 이벤트 처리기 입니다.
        // 날짜 : 2020-07-13
        // 수정자 : 하현철
        // 내용 : 방화문 현재상태 버튼을 출력할시 DB에서 각 방화문 개소의 상태정보를 읽어와 표시하는 
        //        화면으로 전환합니다.
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void DB_Show_Click(object sender, EventArgs e)
        {
            msgViewer.Visible = false;
            dataGridView1.Visible = true;
            refresh.Visible = true;

            DB_Show.Visible = false;
            Main.Visible = true;

            string sql = string.Format("SELECT {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18} FROM FW_STATE"
                            , "FW_IP"
                            , "FW_GROUP"
                            , "OPCODE"
                            , "RSSI"
                            , "IN24V"
                            , "INB24V"
                            , "S1"
                            , "OUT1"
                            , "S2"
                            , "OUT2"
                            , "S3"
                            , "OUT3"
                            , "S4"
                            , "OUT4"
                            , "LOWVOLTAGE"
                            , "LOWVOLTAGE_B"
                            , "LAST_RECV_TIME"
                            , "CONNECTION"
                            , "STATE"
                          );


            DataTable dt = sqlconn.GetDBTable(sql);
            dataGridView1.DataSource = dt;
            
          

            db_show_timer.Interval = 4 * 1000;
            db_show_timer.Tick += db_show_timer_Tick;
            db_show_timer.Start();

            

        }

        //2020-07-13방화문 현재상태 화면을 갱신하는 타이머 동작 함수입니다.
        void db_show_timer_Tick(object sender, EventArgs e)
        {
            int i = 0;
            string sql = string.Format("SELECT {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16} FROM FW_STATE"
                            , "OPCODE"
                            , "RSSI"
                            , "IN24V"
                            , "INB24V"
                            , "S1"
                            , "OUT1"
                            , "S2"
                            , "OUT2"
                            , "S3"
                            , "OUT3"
                            , "S4"
                            , "OUT4"
                            , "LOWVOLTAGE"
                            , "LOWVOLTAGE_B"
                            , "LAST_RECV_TIME"
                            , "CONNECTION"
                            , "STATE"
                          );

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            DataSet ds = sqlconn.readData(sql);

            foreach (DataRow r in ds.Tables[0].Rows)
            {
                for (int itemCount = 0; itemCount < r.ItemArray.Length; itemCount++)
                {
                    dataGridView1[itemCount + 2, i].Value = r.ItemArray[itemCount];
                }
                i++;
            }

           
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        // 이름 : Main_Click
        // 기능 : 전문수신화면 버튼을 클릭할시 동작하는 이벤트 처리기입니다.
        // 날짜 : 2020-07-02
        // 수정자 : 하현철
        // 내용 : 전문 수신 화면 버튼을 클릭할시 화면을 전문수신모니터링 화면으로 전환합니다. 
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Main_Click(object sender, EventArgs e)
        {
            Main.Visible = false;
            DB_Show.Visible = true;

            dataGridView1.Visible = false;
            msgViewer.Visible = true;
            refresh.Visible = false;
            db_show_timer.Stop();
          
        }


        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        // 이름 : refresh_Click
        // 기능 : 새로고침버튼을 클릭했을 때 발생하는 이벤트 처리기 입니다.
        // 날짜 : 2020-07-13
        // 수정자 : 하현철
        // 내용 : 방화문 상태 정보를 DB에서 새로 읽어와 화면에 갱신합니다
        //        
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void refresh_Click(object sender, EventArgs e)
        {
            int i = 0;
            string sql = string.Format("SELECT {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16} FROM FW_STATE"
 
                            , "OPCODE"
                            , "RSSI"
                            , "IN24V"
                            , "INB24V"
                            , "S1"
                            , "OUT1"
                            , "S2"
                            , "OUT2"
                            , "S3"
                            , "OUT3"
                            , "S4"
                            , "OUT4"
                            , "LOWVOLTAGE"
                            , "LOWVOLTAGE_B"
                            , "LAST_RECV_TIME"
                            , "CONNECTION"
                            , "STATE"
                          );

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            DataSet ds = sqlconn.readData(sql);

            foreach (DataRow r in ds.Tables[0].Rows)
            {
                for (int itemCount = 0; itemCount < r.ItemArray.Length; itemCount++)
                {
                    dataGridView1[itemCount + 2, i].Value = r.ItemArray[itemCount];
                }
                i++;
            }


        }


        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        // 이름 : serverHMI_FormClosing_1
        // 기능 : 관리 프로그램에서 종료버튼을 누를 시 이벤트 처리기입니다.
        // 날짜 : 2020-08-06
        // 수정자 : 하현철
        // 내용 : 관리프로그램에서 프로그램을 종료할 경우 종료사유에 TaskManagerClosing를 입력하고 프로그램이
        //        트레이 아이콘으로 가지 않고 종료됨
        //        
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

        private void serverHMI_FormClosing_1(object sender, FormClosingEventArgs e)
        {
           
            if (e.CloseReason.ToString() == "TaskManagerClosing")
            {
    
                if (fwServer != null)
                {
                    fwServer.CloseAllSockets();
                }
                trayIcon.Visible = false;

                sqlQuery = string.Format("UPDATE SERVER_RESOURCE SET REASON = '{0}' WHERE START_TIME = '{1}' AND TASK_NAME = '{2}'", "TaskManagerClosing", fwServer.server_STime, fwServer.PRO_TITLE);
                sqlconn.commandDB(sqlQuery);
    
                this.Dispose();
                Application.Exit();
            
                
            }
            else
            {
                e.Cancel = true;
                this.Visible = false;
                //트레이아이콘 동적 변경 코드 - (추후 알림에 재사용)
                //trayIcon.Icon = TASKCON.Properties.Resources.Icon1;
            }
        }

        //2020-07-16 트레이 아이콘 더블클릭시 화면 디스플레이
        private void trayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Activate();
            this.Visible = true;
            this.ShowIcon = true;

        }


        private void Close_Click(object sender, EventArgs e)
        {
    
    
            if (fwServer != null)
            {
                fwServer.CloseAllSockets();
            }
            trayIcon.Visible = false;
    
            this.Dispose();
            Application.Exit();
    
           
        }



        private void userBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index == 0 && e.NewValue.ToString() == "Checked")
            {
                for (int i = 1; i < userBox.Items.Count; i++)
                {
                    userBox.SetItemCheckState(i, CheckState.Checked);
                }
            }
            else if (e.Index == 0 && e.NewValue.ToString() == "Unchecked")
            {
                for (int i = 1; i < userBox.Items.Count; i++)
                {
                    userBox.SetItemCheckState(i, CheckState.Unchecked);
                }
            }
        }


    }

}
