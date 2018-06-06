using System.Configuration;

namespace TKTerminalSystem
{
    public static class ConnConfig
    {
        /// <summary>
        ///ConnConfig 的摘要说明
        /// </summary>
        public static string DBConnConfig = ConfigurationManager.AppSettings["DBConnConfig"].ToString();//配置库的链接字符串
        public static string DBConnLog = ConfigurationManager.AppSettings["DBConn_Log"].ToString();//日志库的链接字符串
        public static string reportdb2 = ConfigurationManager.AppSettings["reportdb2"].ToString();//日志库的链接字符串
        public static string SysTitle = ConfigurationManager.AppSettings["SysTitle"].ToString();//日志库的链接字符串
        public static string WebAPI = ConfigurationManager.AppSettings["WebAPI"].ToString();//webAPI接口
    }
}