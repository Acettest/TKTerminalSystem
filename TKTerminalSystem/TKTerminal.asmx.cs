using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Services;

namespace TKTerminalSystem
{
    /// <summary>
    /// TKTerminal 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。
    // [System.Web.Script.Services.ScriptService]
    public class TKTerminal : System.Web.Services.WebService
    {
        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        #region MD5加密方式

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public string PASSWORDMD5(string password)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bytes = new byte[16];
            System.Text.ASCIIEncoding asc = new System.Text.ASCIIEncoding();
            bytes = md5.ComputeHash(asc.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        #endregion MD5加密方式

        [WebMethod(Description = "登陆系统")]
        public void LogIn(string username, string password)
        {
            password = PASSWORDMD5(password);
            string sql = @"select userinfo.id,userinfo.UserName,userinfo.RealName,
userrole.id as UserRoleId,userrole.RoleName from userinfo,userrole WHERE userinfo.UserRoleID=userrole.ID and userinfo.username='" + username + "' and userinfo.UserPassword='" + password + "';";
            DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sql).Tables[0];
            int result = 1;
            if (dt.Rows.Count > 0)
            {
                result = 0;//成功
            }
            else
            {
                result = 1;//登陆失败
            }
            string resultStr = JsonConvert.SerializeObject(result);
            Context.Response.Charset = "UTF-8";
            Context.Response.ContentEncoding = System.Text.Encoding.GetEncoding("UTF-8");
            Context.Response.Write(resultStr);
            Context.Response.End();
        }

        [WebMethod(Description = "查看专线清单")]
        public string ShowLines(string lineType)
        {
            string tableName = GetLineTypeGuardTable(lineType);
            string sql = "select * from " + tableName;
            DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sql).Tables[0];
            return DataTable2Json(dt);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="lineTypeName">eg：互联网专线</param>
        /// <param name="area">eg:省公司</param>
        /// <returns></returns>
        [WebMethod(Description = "查看历史测试记录")]
        public string ShowTestHistory(string username,string lineTypeName,string area)
        {
            string sqlList = string.Format("select * from instanttest_task where IF(ISNULL(City),'',City)='{0}' and linetypename='{1}' and UserName = '{2}'",area,lineTypeName,username);
            DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnLog, sqlList).Tables[0];
            return DataTable2Json(dt);
        }

        [WebMethod(Description = "互联网Http测试")]
        public string InternetHttp()
        {
            string baseAddress = "http://localhost:9000/";
            WebClient web = new WebClient();
            WebHeaderCollection headers = new WebHeaderCollection();
            headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded; charset=utf-8";
            web.Headers = headers;

            return string.Empty;
        }

        [WebMethod(Description = "互联网Ping测试")]
        public string InternetPing()
        {
            return string.Empty;
        }

        [WebMethod(Description = "互联网DNS测试")]
        public string InternetDns()
        {
            return string.Empty;
        }

        private Dictionary<string, string> GetWhitePhoneNumbers()
        {
            Dictionary<string, string> whitePhoneNumbers = new Dictionary<string, string>();
            string sqlTask = @"select id,name,phone from phonewhitelist  order by CONVERT(name USING gbk) asc";
            DataSet dsTask = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlTask);
            for (int index = 0; index < dsTask.Tables[0].Rows.Count; index++)
            {
                string name = dsTask.Tables[0].Rows[index]["name"].ToString().Trim();
                string phone = dsTask.Tables[0].Rows[index]["phone"].ToString().Trim();
                //string id = dsTask.Tables[0].Rows[index]["id"].ToString().Trim();
                whitePhoneNumbers.Add(name, phone);
            }
            return whitePhoneNumbers;
        }

        [WebMethod(Description = "语音测试")]
        public string Voice()
        {
            return string.Empty;
        }

        [WebMethod(Description = "短信测试")]
        public string Mas()
        {
            StringBuilder sbJson = new StringBuilder();
            return string.Empty;
        }

        #region 根据专线类型获取即时拨测日志表
        /// <summary>
        /// 获取测试日志
        /// </summary>
        /// <param name="testType">eg:Ping测试</param>
        /// <returns></returns>
        [WebMethod(Description = "获取测试日志")]       
        public string GetTestLog(string testType)
        {
            string logTable = GetTestTypeLogTable(testType);
            if (logTable != string.Empty)
            {
                string sql = "select * from " + logTable;//具体数据后期再更新
                DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnLog, sql).Tables[0];
                return DataTable2Json(dt);
            }
            else
            {
                return "测试类型错误";
            }
        }
        #endregion

        private string DataTable2Json(DataTable dt)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            foreach (DataRow dr in dt.Rows)
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                foreach (DataColumn dc in dt.Columns)
                {
                    result.Add(dc.ColumnName, dr[dc].ToString());
                }
                list.Add(result);
            }
            return JsonConvert.SerializeObject(list);
        }

        /// <summary>
        /// 根据专线类型获取即时拨测日志表
        /// </summary>
        /// <param name="TestType"></param>
        /// <returns></returns>
        private static string GetTestTypeLogTable(string TestType)
        {
            string result = string.Empty;
            string sql = "select InstantLogTable from testtype where TypeName='" + TestType + "';";
            DataSet ds = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sql);
            if (ds.Tables[0].Rows.Count > 0)
            {
                result = ds.Tables[0].Rows[0]["InstantLogTable"].ToString().Trim();
            }
            return result;
        }

        #endregion 根据专线类型获取即时拨测日志表

        #region 根据专线类型获取档案信息表

        /// <summary>
        /// 根据专线类型获取档案信息表
        /// </summary>
        /// <param name="LineType"></param>
        /// <returns></returns>
        [WebMethod]
        public string GetLineTypeGuardTable(string LineType)
        {
            string result = string.Empty;
            string sql = "select GuardInfoTable from linetype where LineTypeName='" + LineType + "';";
            DataSet ds = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sql);
            if (ds.Tables[0].Rows.Count > 0)
            {
                result = ds.Tables[0].Rows[0]["GuardInfoTable"].ToString().Trim();
            }
            return result;
        }

        #endregion 根据专线类型获取档案信息表

        /// <summary>
        ///
        /// </summary>
        /// <param name="particularTestType">eg:ping测试</param>
        /// <param name="lineTypeName">eg:短彩专线</param>
        /// <param name="lineTypeName">eg:测试专线名称</param>
        /// <param name="testItemNameTemp">eg:测试项目名称</param>
        /// <param name="username">用户名，用于生成任务名称</param>
        [WebMethod(Description = "All")]
        public string All(string lineTypeName, string particularTestType, string testItemUrl, string testLineList, string testItemNameTemp, string username)
        {
            //string lineTypeName = Request.QueryString["lineTypeName"].Trim();
            string GuardTable = GetLineTypeGuardTable(lineTypeName);
            string sqlLineAndTerminal = "SELECT terminal.`Name`," + GuardTable + ".LineName ," + GuardTable + ".City "
            + "from " + GuardTable + ",terminal WHERE " + GuardTable + ".TerminalID=terminal.ID;";
            DataTable dtLineAndTerminal = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLineAndTerminal).Tables[0];

            string TaskName = particularTestType + "_" + username//Session["username"].ToString() + "_"
                + DateTime.Now.ToString("yyyyMMdd") + "_" + DateTime.Now.ToString("HHmmss");
            string TestTypeName = particularTestType;
            string TestItemName = "";
            string TestCounts = "1";
            DataRow[] line = dtLineAndTerminal.Select("LineName='" + testLineList + "'");
            string city = line[0]["City"].ToString().Trim();

            //string username = Session["username"].ToString();
            string TestDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string TestState = "0";

            #region 任务参数组合
            StringBuilder sbJson = new StringBuilder();
            switch (lineTypeName)
            {
                default:
                    #region
                    sbJson.Append("{\"LineItems\":[");
                    string[] arrayLine = testLineList.Split(',');
                    DataTable dt = null;
                    if (lineTypeName == "短彩专线")
                    {
                        string sqlLine1 = "select LineName,MasIP as ip,LineKey from guard_smsmms where linename in('" +
                        testLineList.Replace(",", "','") + "');";
                        dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLine1).Tables[0];
                    }
                    else if (lineTypeName == "语音专线")
                    {
                        string sqlLine1 = "select LineName,ImsIP as ip,LineKey from guard_voice where linename in('" +
                        testLineList.Replace(",", "','") + "');";
                        dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLine1).Tables[0];
                    }
                    else
                    {
                        string sqlLine1 = "select LineName,LineKey from line_config where linename in('" +
                        testLineList.Replace(",", "','") + "');";
                        dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLine1).Tables[0];
                    }
                    for (int index = 0; index < arrayLine.Length; index++)
                    {
                        string LineName = arrayLine[index].Trim();
                        string Line_IP = string.Empty;
                        string Line_IP_Key = string.Empty;
                        //string testItemNameTemp = string.Empty;
                        string LineKey = string.Empty;
                        if (dt.Rows.Count > 0)
                        {
                            LineKey = dt.Select("LineName='" + LineName + "'")[0]["LineKey"].ToString().Trim();
                        }
                        if (lineTypeName == "短彩专线")
                        {
                            Line_IP_Key = "MasIP";
                            //testItemNameTemp = this.ddlHM.SelectedValue.Trim();//短信的测试号码
                            if (dt.Rows.Count > 0)
                            {
                                Line_IP = dt.Select("LineName='" + LineName + "'")[0]["ip"].ToString().Trim();
                            }
                        }
                        else if (lineTypeName == "语音专线")
                        {
                            Line_IP_Key = "ImsIP";
                            if (dt.Rows.Count > 0)
                            {
                                Line_IP = dt.Select("LineName='" + LineName + "'")[0]["ip"].ToString().Trim();
                            }
                            //testItemNameTemp = this.tbItemName.Text.Trim();//语音的测试号码
                        }
                        else
                        {
                            //testItemNameTemp = this.tbItemName.Text.Trim();//即时测试的第三个文本框
                        }
                        sbJson.Append("{");
                        sbJson.Append("\"LineTestItem\":[");
                        sbJson.Append("{\"TestItemName\":\""); sbJson.Append(testItemNameTemp); sbJson.Append("\",");
                        sbJson.Append("\"TestItemUrl\":\""); sbJson.Append(testItemUrl); sbJson.Append("\"");
                        sbJson.Append("}");
                        sbJson.Append("],");
                        TestItemName = testItemNameTemp + "(" + testItemUrl + ")";
                        sbJson.Append("\"LineName\":\""); sbJson.Append(LineName); sbJson.Append("\",");
                        sbJson.Append("\"City\":\""); sbJson.Append(city); sbJson.Append("\",");
                        sbJson.Append("\"LineKey\":\""); sbJson.Append(LineKey); sbJson.Append("\",\"TerminalName\":\"");
                        string TerminalName = string.Empty;
                        DataRow[] dr = dtLineAndTerminal.Select("LineName='" + LineName + "'");
                        if (dr.Length > 0)
                        {
                            TerminalName = dr[0]["Name"].ToString();
                        }
                        sbJson.Append(TerminalName);
                        sbJson.Append("\"");
                        if (!string.IsNullOrEmpty(Line_IP))
                        {
                            sbJson.Append(",");
                            sbJson.Append("\""); sbJson.Append(Line_IP_Key); sbJson.Append("\":\"");
                            sbJson.Append(Line_IP); sbJson.Append("\"");
                        }
                        sbJson.Append("}");
                        if (index + 1 != arrayLine.Length)
                        {
                            sbJson.Append(",");
                        }
                    }
                    sbJson.Append("],");
                    sbJson.Append("\"TaskName\":\""); sbJson.Append(TaskName); sbJson.Append("\",");
                    sbJson.Append("\"TestType\":\""); sbJson.Append(TestTypeName); sbJson.Append("\",");
                    sbJson.Append("\"TestTimes\":\""); sbJson.Append(TestCounts); sbJson.Append("\"");
                    sbJson.Append("}");
                    #endregion 任务参数组合
                    break;
            }
            #endregion          
            string sql = "insert into instanttest_task (lineTypeName,TaskName,TestTypeName,testLineList,"
            + "TestItemName,TestCounts,username,TestDate,TestState,City) values"
            + "('" + lineTypeName + "','" + TaskName + "','" + TestTypeName + "','" + testLineList + "','"
            + TestItemName + "','" + TestCounts + "','" + username + "','" + TestDate + "','" + TestState + "','" + city + "');";
            //QLog.SendLog("【连通性验证】" + this.hfLineType.Value.Trim() + "：" + sbJson.ToString());

            #region 发送任务到接口
            WebClient web = new WebClient();
            try
            {
                WebHeaderCollection headers = new WebHeaderCollection();
                headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded; charset=GB2312";
                web.Headers = headers;
                string url = string.Format(ConnConfig.WebAPI + "ImmediateTest");
                string data = "TaskContent=" + this.Server.UrlEncode(sbJson.ToString());
                string result = web.UploadString(url, "POST", data);
                string ja = JsonConvert.DeserializeObject(result).ToString();
                JObject jsonObj = JObject.Parse((ja));
                if (jsonObj["Result"].ToString().Trim().ToLower() == "true")
                {
                    int insertResult = MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnConfig, sql);
                    bool flag = true;
                    while (flag)
                    {
                        result = Encoding.UTF8.GetString(web.DownloadData(ConnConfig.WebAPI + "/ImmediateTest/" + TaskName));
                        result = result.Trim('"');
                        StringBuilder sb = new StringBuilder();
                        string[] parts = result.Split(new char[] { ' ', '\n', '\t', '\r', '\f', '\v', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        int size = parts.Length;
                        for (int i = 0; i < size; i++)
                            sb.AppendFormat("{0}", parts[i]);
                        result = sb.ToString();
                        State stateObj = JsonConvert.DeserializeObject<State>(result);
                        switch (stateObj.state)
                        {
                            case "1":
                                flag = false;
                                break;

                            case "-1":
                                return "测试失败";

                            case "-2":
                                return "测试异常";
                        }
                        Thread.Sleep(1000);
                    }
                    string logTable = GetTestTypeLogTable(particularTestType);
                    string resultSql = "select * from " + logTable + " where TaskName = '" + TaskName + "';";//具体字段后期再协调
                    DataTable resultTable = MySqlHelper.ExecuteDataset(ConnConfig.DBConnLog, resultSql).Tables[0];
                    LogToMysql(DataTable2Json(resultTable));
                    return DataTable2Json(resultTable);
                }
                else
                {
                    //return "后台引擎接收失败，请重新尝试！";
                    string err = "后台引擎接收失败，请重新尝试！";
                    LogToMysql(err);
                    return err;
                }
            }
            catch (Exception)
            {
                //return err.ToString();
                string err = "后台引擎接收失败，请重新尝试！";
                LogToMysql(err);
                return err;
                //QLog.SendLog("【连通性验证】异常：" + err.Message.ToString() + err.ToString());
                //ScriptManager.RegisterStartupScript(this.Page, this.GetType(), "action", "alert('后台引擎接收异常，请重新尝试！');", true);
            }
            finally
            {
                if (web != null)
                {
                    web.Dispose();
                    web = null;
                }
            }
            #endregion
            //UserOperateLogInsert.Insert("连通性验证", "进行验证", "配置语句：" + sql.ToString());//操作日志
        }
        void LogToMysql(string logmessage)
        {
            string logSql = "INSERT INTO `WSLog` (`Time`,`Log`) VALUES (now(),'"+logmessage+"')";
            MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnLog, logSql);
        }
    }

    
    internal class State
    {
        public string state { get; set; }
        public List<Message> msglist { get; set; }
    }

    //{\"IMTime\":\"2018-06-06  09:40:38\",\"LineName\":\"合肥民创中心探针\",\"IMMsg\":\"弢 始测试Ping即时任务.\"},
    internal class Message
    {
        public string IMTime { get; set; }
        public string LineName { get; set; }
        public string IMMsg { get; set; }
    }
}