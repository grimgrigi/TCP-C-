using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace fwServer
{

    class MessageLog
    {
        String date = null;                              //날짜
        String clientIP = null;                          //클라이언트 명
        String msg = null;                               //메시지
        Boolean clientState;

        public MessageLog(String clientIP)
        {
            this.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.clientIP = clientIP;
        }

        public MessageLog(String clientIP, String msg)
        {
            this.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.clientIP = clientIP;
            this.msg = msg;
        }

        public MessageLog(String clientIP, String msg, Boolean clientState)
        {
            this.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.clientIP = clientIP;
            this.msg = msg;
            this.clientState = clientState;
        }


        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        // 이름 : writeLog
        // 인수 : path - 로그가 기록될 디렉토리경로
        //        flag - 로그의 종류 (1 - 접속기록 2 - 송신메시지 3 - 수신메시지 4 - 에러)
        // 기능 : 텍스트파일에 로그 메시지를 기록
        // 날짜 : 2020-02-10 
        // 수정자 : 하현철
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

        public void writeLog(String path, int flag)
        {            

            DirectoryInfo dirInfo = new DirectoryInfo(@"D:\logFile");
            if (!dirInfo.Exists) dirInfo.Create();           

            dirInfo = new DirectoryInfo(path);                   //로그가 기록될 디렉토리 경로
            if (!dirInfo.Exists) dirInfo.Create();                             //해당 경로에 폴더가 없으면 폴더 생성



            string txtFile = null;
            switch (flag)
            {
                case 1:                                                        //접속 로그
                    txtFile = path + string.Format(@"\{0}-{1} Alram.txt",DateTime.Now.Month, DateTime.Now.Day);
                    break;
                case 2:                                                        //송신 메시지 로그
                    txtFile = path + string.Format(@"\{0}-{1} FWCOMMU.txt", DateTime.Now.Month, DateTime.Now.Day);
                    break;
                case 4:                                                        //에러 로그
                    txtFile = path + string.Format(@"\{0}-{1} ERROR.txt", DateTime.Now.Month, DateTime.Now.Day);
                    break;
                default:
                    txtFile = path + string.Format(@"\{0}-{1} ERROR.txt", DateTime.Now.Month, DateTime.Now.Day);
                    break;
            }

            try
            {
                using (FileStream fileStream = new FileStream(txtFile, FileMode.Append, FileAccess.Write, FileShare.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.Default))
                    {

                        sw.WriteLine(this.date + "  " + this.clientIP + "    " + this.msg.Replace(Environment.NewLine, " "));
                        sw.Flush();
                        sw.Close();
                        fileStream.Close();
                    }
                }
            }
            catch (System.IO.IOException IOE)       //에러 발생시 해당 에러를 ErrorLog.txt에 기록
            {
                

                path = string.Format(@"D:\logFile\TASKCON");
                using (FileStream fileStream = new FileStream(txtFile, FileMode.Append, FileAccess.Write, FileShare.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.Default))
                    {

                        sw.WriteLine(this.date + "  " + this.clientIP + "  " + IOE.ToString());

                        sw.Flush();
                        sw.Close();
                        fileStream.Close();
                    }
                }

                return;
            }
        }
    }
}
