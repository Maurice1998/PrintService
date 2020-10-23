using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace HttpPrint
{
    class ConfigAppSettings
    {
        #region API函数声明

        [DllImport("kernel32")]//返回0表示失败，非0为成功
        private static extern long WritePrivateProfileString(string section, string key,
            string val, string filePath);

        [DllImport("kernel32")]//返回取得字符串缓冲区的长度
        private static extern long GetPrivateProfileString(string section, string key,
            string def, StringBuilder retVal, int size, string filePath);
        #endregion
        public string ReadIniData(string Section, string Key, string NoText, string iniFilePath)//读取INI文件
        {
            string str = System.Environment.CurrentDirectory;//获取当前文件目录
            //ini文件路径
            string str1 = "" + str + "\\config.ini";
            if (File.Exists("" + str1 + ""))
            {
                StringBuilder temp = new StringBuilder(1024);
                GetPrivateProfileString(Section, Key, NoText, temp, 1024, iniFilePath);
                return temp.ToString();
            }
            else
            {
                return String.Empty;
            }
        }
    }
}
