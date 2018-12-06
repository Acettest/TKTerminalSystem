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

        [WebMethod(Description = "新建任务")]
        public void BuildScheduleTask(string testType, string taskName, string stopTime, string times, string count,  string lineNames, string ipByNameStr)
        {
            ResponseJson(PostScheduleTask(testType, taskName, stopTime, times, count, lineNames, ipByNameStr));
        }

                [WebMethod(Description = "删除任务")]
        public void DeleteScheduleTask(string taskName)
        {
             ResponseJson(DeleteTask(taskName));
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


        /// <summary>
        /// 建立巡检任务
        /// </summary>
        /// <param name="testType">测试类型 eg:Ping测试</param>
        /// <param name="taskName">任务名称 eg:A日常游戏巡检</param>
        /// <param name="stopTime">失效时间 eg:2018-10-18 14:38:13</param>
        /// <param name="times">测试时间点 eg:1,3,5,7,9,11,13,15,17,19,21,23</param>
        /// <param name="count">测试轮数 eg:1</param>
        /// <param name="taskStatus">任务启动状态 1为启用，0为禁用</param>
        /// <param name="lineNames">专线名称 eg:合肥民创中心互联网专线</param>
        /// <param name="ipByName">序列化的字典值，测试项目名称：测试项目地址 eg:{"mshengaiyiqi.com":"www.iqiyi.com","mshengbaidu.com":"www.baidu.com"}</param>
        /// <returns>执行情况</returns>
        private string PostScheduleTask(string testType, string taskName, string stopTime, string times, string count,  string lineNames, string ipByNameStr,string taskStatus="1")
        {
            try
            {
                #region 参数验证
                List<string> testTypes = new List<string>(ConnConfig.TestTypes);
                if (!testTypes.Contains(testType))
                {
                    return OutputStandardization("false", string.Format("测试类型{0}非支持类型", testType));
                }

                DateTime re;
                if (!DateTime.TryParse(stopTime, out re))
                {
                    return OutputStandardization("false", "任务失效时间错误，无法转换成日期格式");
                }

                int resTimes;
                if (times.Split(',').Select(n => int.TryParse(n, out resTimes)).Contains(false))
                {
                    return OutputStandardization("false", "测试时间点错误，含有除数字之外的非法字符");
                }

                int resCount;
                if (!int.TryParse(count, out resCount))
                {
                    return OutputStandardization("false", string.Format("轮询次数{0}错误，含有除数字之外的非法字符", count));
                }

                if ((taskStatus.Trim() != "0") && (taskStatus.Trim() != "1"))
                {
                    return OutputStandardization("false", string.Format("任务状态{0}错误，0为禁用，1为启用", taskStatus));
                }
                //转换被测ip
                
                var ipByName = JsonConvert.DeserializeObject<Dictionary<string, string>>(ipByNameStr);

                //foreach (var nametoip in ipByName)
                //{
                //    bool isUrl = false;
                //    foreach (var iporurlChar in nametoip.Value)
                //    {
                //        if (Char.IsLetter(iporurlChar))
                //        {
                //            isUrl = true;
                //            break;
                //        }
                //    }
                //    if (!isUrl)
                //    {
                //        IPAddress resIP;
                //        if (!IPAddress.TryParse(nametoip.Value, out resIP))
                //        {
                //            return OutputStandardization("false", string.Format("被测地址{0}既非url，也非IP", nametoip.Value));
                //        }
                //    }
                //}

                #region 检查任务名是否合法
                if (string.IsNullOrWhiteSpace(taskName))
                {
                    return OutputStandardization("false", string.Format("任务名称{0}不可为空，请重新分配任务名", taskName));
                }
                string sqlCheck = "select id from task_config where TaskName='" + taskName + "'";
                DataSet dsCheck = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlCheck);
                if (dsCheck.Tables[0].Rows.Count > 0)
                {
                    return OutputStandardization("false", string.Format("任务名称{0}已存在，请重新分配任务名", taskName));
                }
                #endregion
                //获取所测专线
                var testLines = new List<string>(lineNames.Trim().Split(','));
                List<string> eastCom2LocalLine = new List<string>();//短彩和语音匹配的拨测系统内的专线
                foreach (var line in testLines)
                {
                    if (testType.Equals("语音测试") || testType.Equals("短彩测试"))
                    {
                        string lineMatch = "SELECT `TechquickLine`,`EastcomquickLine` FROM `eastcomlinematch`";
                        DataTable lineMatchTable = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, lineMatch).Tables[0];
                        DataRow[] matchlineRow = lineMatchTable.Select("EastcomquickLine='" + line + "'");
                        if (matchlineRow.Length == 0)
                        {
                            return OutputStandardization("false", string.Format("所测专线{0}不在系统支持范围", line));
                        }
                        else
                        {
                            eastCom2LocalLine.Add(matchlineRow[0][0].ToString().Trim());
                        }
                    }
                    else
                    {
                        eastCom2LocalLine.Add(line.Trim());
                    }
                }

                if (testLines.Contains("平台侧") && testLines.Count() > 1)
                {
                    return OutputStandardization("false", string.Format("同一个任务中，所测专线信息不能既包含“平台侧”，又包含其他专线"));
                }
                #endregion
                int isProvinceCompanyTask = 0;//1 为是省平台任务，0为非省平台任务 针对数据库中IsPorL字段
                if (testLines.Contains("平台侧"))
                {
                    isProvinceCompanyTask = 1;
                }

                string lineTableSql = @"select  LineTypeName,GuardInfoTable from linetype where LineTypeTestType like ";//末尾有空格
                if (testType.Trim().Equals("Ping测试") || testType.Trim().Equals("网页测试") || testType.Trim().Equals("DNS测试"))
                {
                    lineTableSql =string.Format("{0}'%Ping测试、网页测试%'",lineTableSql);
                }
                else
                {
                    lineTableSql = string.Format("{0}'%{1}%'",lineTableSql,testType);
                }
                var lineInfoRes = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, lineTableSql).Tables[0].Rows[0];
                string lineType = lineInfoRes["LineTypeName"].ToString();
                var tableLineBelongsTo = lineInfoRes["GuardInfoTable"].ToString();

                StringBuilder sb = new StringBuilder();
                //Remark 0 表示东信公司，给东信公司做的接口
                sb.Append("insert into task_config (TaskName,TestType,TaskEndTime,");
                sb.Append("TestTimes,TestHours,TaskStatus,Remark,TestTypeAlias,LineType,IsPorL) values");
                sb.Append(" ('" + taskName.Trim() + "','" + testType.Trim() + "','" + stopTime.Trim()
                    + "','" + count.Trim() + "','" + times.Trim() + "'," + taskStatus.Trim()
                    + ",'" + "0" + "','" + string.Empty + "','" + lineType.Trim() + "','" + isProvinceCompanyTask + "');");
                int TaskInsert = MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnConfig, sb.ToString());

                //UserOperateLogInsert.Insert("巡检任务", "基本信息入库", "配置语句：" + sb.ToString());
                string sqlTaskID = "select id from task_config where TaskName='" + taskName + "'";
                DataSet dsTaskID = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlTaskID);
                if (dsTaskID.Tables[0].Rows.Count > 0)
                {

                    string TaskID = dsTaskID.Tables[0].Rows[0]["id"].ToString();
                    foreach (var line in eastCom2LocalLine)
                    {
                        string lineconfigSql = string.Format("select A.ID, A.LineName,B.LineType,A.TerminalID,A.City,A.LineKey from {0} as A left join terminal as B on A.TerminalID = B.ID where  LineName like '%{1}%' ", tableLineBelongsTo, line);
                        var lineConfigRes = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, lineconfigSql).Tables[0].Rows[0];
                        //string insertLineConfigSql = string.Format("insert into taskline_config (ID,LineName,LineType,TerminalID,City,LineKey) values ('{0}','{1}','{2}','{3}','{4}','{5}')", Guid.NewGuid().ToString(), lineConfigRes[1], lineConfigRes[2], lineConfigRes[3], lineConfigRes[4], lineConfigRes[5]);
                        string insertTaskLineConfig = string.Format("insert into taskline_config (`TerminalID`,`LineID`,`TaskID`,`LineTable`,`LineName`) values ('{0}','{1}','{2}','{3}','{4}')", lineConfigRes[3], lineConfigRes[0], TaskID, tableLineBelongsTo, line);
                        MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnConfig, insertTaskLineConfig);


                    }

                    StringBuilder sbLineAndItem = new StringBuilder();
                    int num = 0;
                    foreach (var nameToIP in ipByName)
                    {
                        //因为语音测试和短彩测试因为某种原因在数据库中保存的字段是反的，所以在此处进行特殊处理
                        string testItemName = (testType.Equals("语音测试") || testType.Equals("短彩测试")) ? nameToIP.Value : nameToIP.Key;
                        string testItemUrl = (testType.Equals("语音测试") || testType.Equals("短彩测试")) ? nameToIP.Key : nameToIP.Value;
                        string testItemEngine = "ping,tracert";
                        string testItemRemark = string.Empty;
                        sbLineAndItem.Append("insert into testitem_config (TaskID,TestItemName");
                        sbLineAndItem.Append(",TestItemUrl,TestItemEngine,TestItemRemark,IsPorL) values");
                        sbLineAndItem.Append("(" + TaskID.Trim() + ",'" + testItemName.Trim() + "'");
                        sbLineAndItem.Append(",'" + testItemUrl.Trim() + "'");
                        sbLineAndItem.Append(",'" + testItemEngine.Trim() + "'");
                        sbLineAndItem.Append(",'" + testItemRemark + "'");
                        sbLineAndItem.Append(",'" + isProvinceCompanyTask + "');");

                        num++;
                        if (num > 10)
                        {
                            MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnConfig, sbLineAndItem.ToString());
                            sbLineAndItem = new StringBuilder();
                        }
                    }

                    MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnConfig, sbLineAndItem.ToString());
                    sbLineAndItem = new StringBuilder();

                    return OutputStandardization("true", string.Format("新建任务{0}成功", taskName));
                }
                else
                {
                    return OutputStandardization("false", string.Format("新建任务{0}在任务保存时失败", taskName));
                }
            }
            catch (Exception ex)
            {
                return OutputStandardization("false", string.Format("新建任务{0}失败，原因{1}", taskName, ex.ToString()));
            }
        }

        private string DeleteTask(string taskName)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                string sqlTaskID = "select id from task_config where TaskName='" + taskName.Trim() + "'";
                DataSet dsTaskID = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlTaskID);
                if (dsTaskID.Tables[0].Rows.Count == 0)
                {
                    return OutputStandardization("false", string.Format("删除任务{0}失败，该任务在系统中无记录", taskName));
                }
                string TaskID = dsTaskID.Tables[0].Rows[0]["id"].ToString();

                //删除任务信息
                sb.Append("delete from task_config where id=");
                sb.Append(TaskID);
                sb.Append(";");
                //删除任务和专线关系表
                sb.Append("delete from taskline_config where taskid=");
                sb.Append(TaskID);
                sb.Append(";");
                //删除任务网元信息表
                sb.Append("delete from testitem_config where taskid=");
                sb.Append(TaskID);
                sb.Append(";");
                MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnConfig, sb.ToString());
                return OutputStandardization("true", string.Format("任务{0}删除成功", taskName));
            }catch(Exception ex)
            {
                return OutputStandardization("false", string.Format("删除任务{0}失败，原因：{1}", taskName,ex.ToString()));
            }
        }

        private void ResponseJson(string code)
        {
            base.Context.Response.Charset = "GB2312";
            base.Context.Response.ContentEncoding = Encoding.GetEncoding("GB2312");
            base.Context.Response.Write(code);
            base.Context.Response.End();
        }

        private string OutputStandardization(string opResult, string opMessage, string TaskResult = "")
        {
            return "{\"Success\":" + opResult + ",\"Message\":" + opMessage + ",\"Results\":[" + TaskResult + "]}";
        }
    }
}
