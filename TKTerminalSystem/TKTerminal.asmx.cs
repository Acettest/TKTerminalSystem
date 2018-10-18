using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Services;
using System.Linq;
namespace TKTerminalSystem
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class TKTerminal : WebService
    {
        [WebMethod(Description = "即时拨测")]
        public void ImmediateTest(string lineTypeName, string particularTestType, string testItemUrl, string testLine, string testItemNameTemp, string username)
        {
            ResponseJson(GetImmediateTest(lineTypeName, particularTestType, testItemUrl, testLine, testItemNameTemp, username));
        }

        private string GetImmediateTest(string lineTypeName, string particularTestType, string testItemUrl, string testLine, string testItemNameTemp, string username)
        {
            try
            {
                string GuardTable = GetLineTypeGuardTable(lineTypeName);
                string sqlLineAndTerminal = "SELECT terminal.`Name`," + GuardTable + ".LineName ," + GuardTable + ".City from " + GuardTable + ",terminal WHERE " + GuardTable + ".TerminalID=terminal.ID;";
                DataTable dtLineAndTerminal = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLineAndTerminal).Tables[0];
                string[] obj = new string[6]
                {
                    particularTestType,
                    "_",
                    username,
                    null,
                    null,
                    null
                };
                DateTime now = DateTime.Now;
                obj[3] = now.ToString("yyyyMMdd");
                obj[4] = "_";
                now = DateTime.Now;
                obj[5] = now.ToString("HHmmss");
                string TaskName = string.Concat(obj);
                string TestItemName2 = "";
                string TestCounts = "1";
                if (lineTypeName == "短彩专线" || lineTypeName == "语音专线")
                {
                    string lineMatch = "SELECT `TechquickLine`,`EastcomquickLine` FROM `eastcomlinematch`";
                    DataTable lineMatchTable = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, lineMatch).Tables[0];
                    DataRow[] matchlineRow = lineMatchTable.Select("EastcomquickLine='" + testLine + "'");
                    if (matchlineRow.Length == 0)
                    {
                        string err4 = "所测专线不在系统支持范围！";
                        return "{\"Success\":false,\"Message\":" + err4 + ",\"Results\":[]}";
                    }
                    testLine = matchlineRow[0]["TechquickLine"].ToString().Trim();
                }
                DataRow[] line = dtLineAndTerminal.Select("LineName='" + testLine + "'");
                if (line.Length != 0)
                {
                    string city = line[0]["City"].ToString().Trim();
                    now = DateTime.Now;
                    string TestDate = now.ToString("yyyy-MM-dd HH:mm:ss");
                    string TestState = "0";
                    StringBuilder sbJson = new StringBuilder();
                    sbJson.Append("{\"LineItems\":[");
                    DataTable dt2 = null;
                    if (lineTypeName == "短彩专线")
                    {
                        string sqlLine3 = "select LineName,MasIP as ip,LineKey from guard_smsmms where linename = '" + testLine + "';";
                        dt2 = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLine3).Tables[0];
                    }
                    else if (lineTypeName == "语音专线")
                    {
                        string sqlLine2 = "select LineName,ImsIP as ip,LineKey from guard_voice where linename = '" + testLine + "';";
                        dt2 = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLine2).Tables[0];
                    }
                    else
                    {
                        string sqlLine = "select LineName,LineKey from line_config where linename = '" + testLine + "';";
                        dt2 = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLine).Tables[0];
                    }
                    string LineName = testLine.Trim();
                    string Line_IP = string.Empty;
                    string Line_IP_Key = string.Empty;
                    string LineKey = string.Empty;
                    if (dt2.Rows.Count > 0)
                    {
                        LineKey = dt2.Select("LineName='" + LineName + "'")[0]["LineKey"].ToString().Trim();
                    }
                    if (lineTypeName == "短彩专线")
                    {
                        Line_IP_Key = "MasIP";
                        if (dt2.Rows.Count > 0)
                        {
                            Line_IP = dt2.Select("LineName='" + LineName + "'")[0]["ip"].ToString().Trim();
                        }
                    }
                    else if (lineTypeName == "语音专线")
                    {
                        Line_IP_Key = "ImsIP";
                        if (dt2.Rows.Count > 0)
                        {
                            Line_IP = dt2.Select("LineName='" + LineName + "'")[0]["ip"].ToString().Trim();
                        }
                    }
                    sbJson.Append("{");
                    sbJson.Append("\"LineTestItem\":[");
                    sbJson.Append("{\"TestItemName\":\"");
                    sbJson.Append(testItemNameTemp);
                    sbJson.Append("\",");
                    sbJson.Append("\"TestItemUrl\":\"");
                    sbJson.Append(testItemUrl);
                    sbJson.Append("\"");
                    sbJson.Append("}");
                    sbJson.Append("],");
                    TestItemName2 = testItemNameTemp + "(" + testItemUrl + ")";
                    sbJson.Append("\"LineName\":\"");
                    sbJson.Append(LineName);
                    sbJson.Append("\",");
                    sbJson.Append("\"City\":\"");
                    sbJson.Append(city);
                    sbJson.Append("\",");
                    sbJson.Append("\"LineKey\":\"");
                    sbJson.Append(LineKey);
                    sbJson.Append("\",\"TerminalName\":\"");
                    string TerminalName = string.Empty;
                    DataRow[] dr = dtLineAndTerminal.Select("LineName='" + LineName + "'");
                    if (dr.Length != 0)
                    {
                        TerminalName = dr[0]["Name"].ToString();
                    }
                    sbJson.Append(TerminalName);
                    sbJson.Append("\"");
                    if (!string.IsNullOrEmpty(Line_IP))
                    {
                        sbJson.Append(",");
                        sbJson.Append("\"");
                        sbJson.Append(Line_IP_Key);
                        sbJson.Append("\":\"");
                        sbJson.Append(Line_IP);
                        sbJson.Append("\"");
                    }
                    sbJson.Append("}");
                    sbJson.Append("],");
                    sbJson.Append("\"TaskName\":\"");
                    sbJson.Append(TaskName);
                    sbJson.Append("\",");
                    sbJson.Append("\"TestType\":\"");
                    sbJson.Append(particularTestType);
                    sbJson.Append("\",");
                    sbJson.Append("\"TestTimes\":\"");
                    sbJson.Append(TestCounts);
                    sbJson.Append("\"");
                    sbJson.Append("}");
                    string sql = "insert into instanttest_task (lineTypeName,TaskName,TestTypeName,TestLineList,TestItemName,TestCounts,username,TestDate,TestState,City) values('" + lineTypeName + "','" + TaskName + "','" + particularTestType + "','" + testLine + "','" + TestItemName2 + "','" + TestCounts + "','" + username + "','" + TestDate + "','" + TestState + "','" + city + "');";
                    WebClient web2 = new WebClient();
                    try
                    {
                        WebHeaderCollection headers = new WebHeaderCollection();
                        headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded; charset=GB2312";
                        web2.Headers = headers;
                        string url = string.Format(ConnConfig.WebAPI + "ImmediateTest");
                        string data = "TaskContent=" + base.Server.UrlEncode(sbJson.ToString());
                        string result4 = web2.UploadString(url, "POST", data);
                        string ja = JsonConvert.DeserializeObject(result4).ToString();
                        JObject jsonObj = JObject.Parse(ja);
                        if (!(jsonObj["Result"].ToString().Trim().ToLower() == "true"))
                        {
                            string err2 = "后台引擎接收失败，请重新尝试！";
                            LogToMysql(err2);
                            return "{\"Success\":false,\"Message\":" + err2 + ",\"Results\":[]}";
                        }
                        int insertResult = MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnConfig, sql);
                        bool flag = true;
                        while (flag)
                        {
                            result4 = Encoding.UTF8.GetString(web2.DownloadData(ConnConfig.WebAPI + "/ImmediateTest/" + TaskName));
                            result4 = result4.Trim('"');
                            StringBuilder sb = new StringBuilder();
                            string[] parts = result4.Split(new char[7]
                            {
                                ' ',
                                '\n',
                                '\t',
                                '\r',
                                '\f',
                                '\v',
                                '\\'
                            }, StringSplitOptions.RemoveEmptyEntries);
                            int size = parts.Length;
                            for (int i = 0; i < size; i++)
                            {
                                sb.AppendFormat("{0}", parts[i]);
                            }
                            result4 = sb.ToString();
                            State stateObj = JsonConvert.DeserializeObject<State>(result4);
                            switch (stateObj.state)
                            {
                                case "1":
                                    flag = false;
                                    break;
                                case "-1":
                                    return "{\"Success\":false,\"Message\":任务失败，请检查参数是否错误，并稍后再试,\"Results\":[]}";
                                case "-2":
                                    return "{\"Success\":false,\"Message\":后台引擎异常，请稍后再试,\"Results\":[]}";
                            }
                            Thread.Sleep(1000);
                        }
                        string logTable = GetTestTypeLogTable(particularTestType);
                        string resultSql = "select * from " + logTable + " where TaskName = '" + TaskName + "';";
                        switch (particularTestType)
                        {
                            case "Ping测试":
                                resultSql = "select `TestTime`,`LineName`,`IP`,`Ping32AvgCost`,`Ping32AvgLost`,`NetJitter32` from " + logTable + " where TaskName = '" + TaskName + "';";
                                break;
                            case "网页测试":
                                resultSql = "select `TestTime`,`LineName`,`URL`,`TimeCost90p`,`Ping32AvgCost`,`Ping32AvgLost`,`NetJitter32` from " + logTable + " where TaskName = '" + TaskName + "';";
                                break;
                            case "DNS测试":
                                resultSql = "select `TestTime`,`LineName`,`ResolverCostTime` from " + logTable + " where TaskName = '" + TaskName + "';";
                                break;
                            case "语音测试":
                                resultSql = "select `TestTime`,`Ping32AvgCost`,`Ping32AvgLost`,`NetJitter32`,`DialSuccessRate` from " + logTable + " where TaskName = '" + TaskName + "';";
                                break;
                            case "短彩测试":
                                resultSql = "select `TestTime`,`IPAddress`,`TraceRoute`,`SiteName` from " + logTable + " where TaskName = '" + TaskName + "';";
                                break;
                        }
                        DataTable resultTable = MySqlHelper.ExecuteDataset(ConnConfig.DBConnLog, resultSql).Tables[0];
                        LogToMysql(DataTable2Json(resultTable));
                        return "{\"Success\":true,\"Message\":\"操作成功\",\"Results\":" + DataTable2Json(resultTable) + "}";
                    }
                    catch (Exception)
                    {
                        string err = "后台引擎接收失败，请重新尝试！";
                        LogToMysql(err);
                        return "{\"Success\":false,\"Message\":" + err + ",\"Results\":[]}";
                    }
                    finally
                    {
                        if (web2 != null)
                        {
                            web2.Dispose();
                            web2 = null;
                        }
                    }
                }
                string err3 = "所测专线不在系统支持范围！";
                return "{\"Success\":false,\"Message\":" + err3 + ",\"Results\":[]}";
            }
            catch (Exception ex)
            {
                return "{\"Success\":false,\"Message\":" + ex.Message + ",\"Results\":[]}";
            }
        }

        [WebMethod(Description = "查看历史测试记录")]
        public void ShowTestHistory(string lineTypeName)
        {
            ResponseJson(GetTestHistory(lineTypeName));
        }

        private string GetTestHistory(string lineTypeName)
        {
            try
            {
                string sqlList = string.Format("select `LineTypeName`,`TestTypeName`,`TestLineList`,`TestDate`,`TestState` from instanttest_task where linetypename='{0}'", lineTypeName);
                DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlList).Tables[0];
                return "{\"Success\":true,\"Message\":\"操作成功\",\"Results\":" + DataTable2Json(dt) + "}";
            }
            catch (Exception ex)
            {
                return "{\"Success\":false,\"Message\":" + ex.Message + ",\"Results\":[]}";
            }
        }

        [WebMethod(Description = "获取即时测试历史结果")]
        public void GetPreviousResults(string particularTestType)
        {
            ResponseJson(PreviousResults(particularTestType));
        }

        private string PreviousResults(string particularTestType)
        {
            try
            {
                string logTable = GetTestTypeLogTable(particularTestType);
                string resultSql = "select * from " + logTable;
                switch (particularTestType)
                {
                    case "Ping测试":
                        resultSql = "select `TestTime`,`LineName`,`IP`,`Ping32AvgCost`,`Ping32AvgLost`,`NetJitter32` from " + logTable + ";";
                        break;
                    case "网页测试":
                        resultSql = "select `TestTime`,`LineName`,`URL`,`TimeCost90p`,`Ping32AvgCost`,`Ping32AvgLost`,`NetJitter32` from " + logTable + ";";
                        break;
                    case "DNS测试":
                        resultSql = "select `TestTime`,`LineName`,`ResolverCostTime` from " + logTable + ";";
                        break;
                    case "语音测试":
                        resultSql = "select `TestTime`,`Ping32AvgCost`,`Ping32AvgLost`,`NetJitter32`,`DialSuccessRate` from " + logTable + ";";
                        break;
                    case "短彩测试":
                        resultSql = "select `TestTime`,`IPAddress`,`TraceRoute`,`SiteName` from " + logTable + ";";
                        break;
                }
                DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnLog, resultSql).Tables[0];
                return "{\"Success\":true,\"Message\":\"操作成功\",\"Results\":" + DataTable2Json(dt) + "}";
            }
            catch (Exception ex)
            {
                return "{\"Success\":false,\"Message\":" + ex.Message + ",\"Results\":[]}";
            }
        }

        private string DataTable2Json(DataTable dt)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                foreach (DataColumn column in dt.Columns)
                {
                    result.Add(column.ColumnName, row[column].ToString());
                }
                list.Add(result);
            }
            return JsonConvert.SerializeObject(list);
        }

        private string GetLineTypeGuardTable(string LineType)
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

        private void LogToMysql(string logmessage)
        {
            string logSql = "INSERT INTO `WSLog` (`Time`,`Log`) VALUES (now(),'" + logmessage + "')";
            MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnLog, logSql);
        }

        public string PASSWORDMD5(string password)
        {
            MD5 md5 = MD5.Create();
            byte[] bytes2 = new byte[16];
            ASCIIEncoding asc = new ASCIIEncoding();
            bytes2 = md5.ComputeHash(asc.GetBytes(password));
            return Convert.ToBase64String(bytes2);
        }

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

        /*
         * 测试类型
任务名称
失效时间
测试时间点
测试轮数
专线名称
被测项目名称
被测项目地址

         */

        /// <summary>
        /// 建立巡检任务
        /// </summary>
        /// <param name="testType">测试类型 eg:Ping测试</param>
        /// <param name="taskName">任务名称 eg:A日常游戏巡检</param>
        /// <param name="stopTime">失效时间 eg:2018-10-18 14:38:13</param>
        /// <param name="times">测试时间点 eg:1,3,5,7,9,11,13,15,17,19,21,23</param>
        /// <param name="count">测试轮数 eg:1</param>
        /// <param name="lineNames">专线名称 eg:合肥民创中心互联网专线</param>
        /// <param name="ipByName">序列化的字典值，测试项目名称：测试项目地址 eg:地下城与勇士江苏二区:180.96.59.195</param>
        /// <returns>执行情况</returns>
        private string PostScheduleTask(string testType, string taskName,string stopTime,string times, string count, string lineNames,string ipByNameStr)
        {
            #region 参数解析与验证

            List<string> testTypes = new List<string>(ConnConfig.TestTypes);
            if(!testTypes.Contains(testType))
            {
                return OutputStandardization("false", "测试类型非支持类型");
            }

            if(!DateTime.TryParse(stopTime,out DateTime re))
            {
                return OutputStandardization("false", "任务失效时间错误，无法转换成日期格式");
            }


            if(times.Split(',').Select(n => int.TryParse(n, out int resTimes)).Contains(false))
            {
                return OutputStandardization("false", "测试时间点错误，含有除数字之外的非法字符");
            }
             
            
            if(!int.TryParse(count,out int resCount))
            {
                return OutputStandardization("false", "轮询次数错误，含有除数字之外的非法字符");
            }

            var ipByName  = JsonConvert.DeserializeObject<Dictionary<string,string>>(ipByNameStr);
            foreach(var nametoip in ipByName)
            {
                IPAddress.TryParse(nametoip.Value, out IPAddress resIP);
            }

            var testLines = new List<string>(lineNames.Trim().Split(','));
            
            foreach (var line in testLines)
            {
                if(testType.Equals("语音测试")||testType.Equals("短彩测试"))
                {
                    string lineMatch = "SELECT `TechquickLine`,`EastcomquickLine` FROM `eastcomlinematch`";
                    DataTable lineMatchTable = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, lineMatch).Tables[0];
                    DataRow[] matchlineRow = lineMatchTable.Select("EastcomquickLine='" + line + "'");
                    if (matchlineRow.Length == 0)
                    {
                        return OutputStandardization("false", "所测专线不在系统支持范围");
                    }
                }
                
            }
            
            

            #endregion
        }


            private void ResponseJson(string code)
        {
            base.Context.Response.Charset = "GB2312";
            base.Context.Response.ContentEncoding = Encoding.GetEncoding("GB2312");
            base.Context.Response.Write(code);
            base.Context.Response.End();
        }

        private string OutputStandardization(string opResult,string opMessage,string TaskResult ="")
        {
            return "{\"Success\":"+ opResult + ",\"Message\":"+ opMessage + ",\"Results\":["+ TaskResult + "]}";
        }
    }
}
