﻿using MySql.Data.MySqlClient;
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

        #region WebService公开方法

        #region 即时测试
        /// <summary>
        /// 即时测试
        /// </summary>
        /// <param name="particularTestType">eg:ping测试</param>
        /// <param name="lineTypeName">eg:短彩专线</param>
        /// <param name="lineTypeName">eg:测试专线名称</param>
        /// <param name="testItemNameTemp">eg:测试项目名称</param>
        /// <param name="username">用户名，用于生成任务名称</param>
        [WebMethod(Description = "即时拨测")]
        public string ImmediateTest(string lineTypeName, string particularTestType, string testItemUrl, string testLine, string testItemNameTemp, string username)
        {
            try
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
                DataRow[] line = dtLineAndTerminal.Select("LineName='" + testLine + "'");
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
                        string[] arrayLine = testLine.Split(',');
                        DataTable dt = null;
                        if (lineTypeName == "短彩专线")
                        {
                            string sqlLine1 = "select LineName,MasIP as ip,LineKey from guard_smsmms where linename in('" +
                            testLine.Replace(",", "','") + "');";
                            dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLine1).Tables[0];
                        }
                        else if (lineTypeName == "语音专线")
                        {
                            string sqlLine1 = "select LineName,ImsIP as ip,LineKey from guard_voice where linename in('" +
                            testLine.Replace(",", "','") + "');";
                            dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlLine1).Tables[0];
                        }
                        else
                        {
                            string sqlLine1 = "select LineName,LineKey from line_config where linename in('" +
                            testLine.Replace(",", "','") + "');";
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

                #endregion 即时拨测
                string sql = "insert into instanttest_task (lineTypeName,TaskName,TestTypeName,testLine,"
                + "TestItemName,TestCounts,username,TestDate,TestState,City) values"
                + "('" + lineTypeName + "','" + TaskName + "','" + TestTypeName + "','" + testLine + "','"
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
                        return "{\"Success\":true,\"Message\":\"操作成功\",\"Results\":[{" + DataTable2Json(resultTable) + "}]}";
                    }
                    else
                    {
                        //return "后台引擎接收失败，请重新尝试！";
                        string err = "后台引擎接收失败，请重新尝试！";
                        LogToMysql(err);
                        return "{\"Success\":false,\"Message\":" + err + ",\"Results\":[]}";
                    }
                }
                catch (Exception)
                {
                    //return err.ToString();
                    string err = "后台引擎接收失败，请重新尝试！";
                    LogToMysql(err);
                    return "{\"Success\":false,\"Message\":" + err + ",\"Results\":[]}";
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
                #endregion 发送任务到接口
            }
            catch (Exception ex)
            {
                return "{\"Success\":false,\"Message\":" + ex.Message + ",\"Results\":[]}";
            }
            //UserOperateLogInsert.Insert("连通性验证", "进行验证", "配置语句：" + sql.ToString());//操作日志
        }
        #endregion

        #region 登陆系统
        [WebMethod(Description = "登陆系统")]
        public string LogIn(string username, string password)
        {
            try
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
                return "{\"Success\":true,\"Message\":\"操作成功\",\"Results\":[{" + resultStr + "}]}";
                /*
                Context.Response.Charset = "UTF-8";
                Context.Response.ContentEncoding = System.Text.Encoding.GetEncoding("UTF-8");
                Context.Response.Write("{\"Success\":true,\"Message\":\"操作成功\",\"Results\":[{" + resultStr + "}]}");
                Context.Response.End();
                */
            }
            catch (Exception ex)
            {
                return "{\"Success\":false,\"Message\":" + ex.Message + ",\"Results\":[]}";
            }
        }
        #endregion

        #region 查看专线清单
        [WebMethod(Description = "查看专线清单")]
        public string ShowLines(string lineType)
        {
            try
            {
                string tableName = GetLineTypeGuardTable(lineType);
                string sql = "select * from " + tableName;
                DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sql).Tables[0];
                return "{\"Success\":true,\"Message\":\"操作成功\",\"Results\":[{" + DataTable2Json(dt) + "}]}";

            }
            catch (Exception ex)
            {
                return "{\"Success\":false,\"Message\":" + ex.Message + ",\"Results\":[]}";
            }

        }
        #endregion

        #region 获取短信白名单
        /// <summary>
        /// 获取短信白名单
        /// </summary>
        /// <returns></returns>
        [WebMethod(Description = "获取短信白名单")]
        public string GetWhitePhoneNumbers()
        {
            try
            {
                Dictionary<string, string> whitePhoneNumbers = new Dictionary<string, string>();
                string sqlTask = @"select id,name,phone from phonewhitelist  order by CONVERT(name USING gbk) asc";
                DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnConfig, sqlTask).Tables[0];
                return "{\"Success\":true,\"Message\":\"操作成功\",\"Results\":[{" + DataTable2Json(dt) + "}]}";//;
            }
            catch (Exception ex)
            {
                return "{\"Success\":false,\"Message\":" + ex.Message + ",\"Results\":[]}";
            }
        }
        #endregion 获取短信白名单

        #region 查看测试历史记录
        /// <summary>
        /// 获取即时测试的历史纪录
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="lineTypeName">eg：互联网专线</param>
        /// <param name="area">eg:省公司</param>
        /// <returns></returns>
        [WebMethod(Description = "查看历史测试记录")]
        public string ShowTestHistory(string username, string lineTypeName, string area)
        {
            try
            {
                string sqlList = string.Format("select * from instanttest_task where IF(ISNULL(City),'',City)='{0}' and linetypename='{1}' and UserName = '{2}'", area, lineTypeName, username);
                DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnLog, sqlList).Tables[0];
                return "{\"Success\":true,\"Message\":\"操作成功\",\"Results\":[{" + DataTable2Json(dt) + "}]}";
            }
            catch (Exception ex)
            {
                return "{\"Success\":false,\"Message\":" + ex.Message + ",\"Results\":[]}";
            }
        }
        #endregion 

        #region 获取拨测历史结果
        /// <summary>
        /// 获取历史拨测结果
        /// </summary>
        /// <param name="particularTestType">Ping测试</param>
        /// <param name="lineTypeName"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        [WebMethod(Description = "获取历史拨测结果")]
        public string GetPreviousResults(string particularTestType)
        {
            try
            {
                string logTable = GetTestTypeLogTable(particularTestType);
                string resultSql = "select * from " + logTable;//具体字段后期再协调
                DataTable dt = MySqlHelper.ExecuteDataset(ConnConfig.DBConnLog, resultSql).Tables[0];
                return "{\"Success\":true,\"Message\":\"操作成功\",\"Results\":[{" + DataTable2Json(dt) + "}]}";
            }
            catch (Exception ex)
            {
                return "{\"Success\":false,\"Message\":" + ex.Message + ",\"Results\":[]}";
            }
        }
        #endregion

        #endregion

        #region 辅助方法

        #region DataTable转化成Json
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
        #endregion DataTable转化成Json

        #region 根据专线类型获取档案信息表
        /// <summary>
        /// 根据专线类型获取档案信息表,eg传入“互联网专线”得到保存互联网专线的表
        /// </summary>
        /// <param name="LineType">eg:互联网专线</param>
        /// <returns></returns>
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
        #endregion 根据专线类型获取档案信息表

        #region 向数据库中写日志

        private void LogToMysql(string logmessage)
        {
            string logSql = "INSERT INTO `WSLog` (`Time`,`Log`) VALUES (now(),'" + logmessage + "')";
            MySqlHelper.ExecuteNonQuery(ConnConfig.DBConnLog, logSql);
        }

        #endregion

        #region MD5加密方式
        /// <summary>
        /// MD5加密,数据库中保存的是密码加密后的字符串
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

        #region 获取即时拨测日志表
        /// <summary>
        /// 根据专线类型获取即时拨测日志表
        /// </summary>
        /// <param name="TestType">eg:互联网测试</param>
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

        #endregion
    }
}