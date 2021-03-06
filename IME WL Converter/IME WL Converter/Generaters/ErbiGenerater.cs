﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Studyzy.IMEWLConverter.Entities;
using Studyzy.IMEWLConverter.Helpers;

namespace Studyzy.IMEWLConverter.Generaters
{
    /*
     * 二字词：取每个字的前两位编码。例如“计算”取“JP”+“SQ”，即：“JPSQ”。
　　三字词：取第一字的前二位编码和最后两个字的第一码。例如“计算机”取“JPSJ”。
　　四字词：取每个字的第一码。例如“兴高采烈”取“XGCL”。
　　多字词（四字以上词）：取前三字和最后一字的第一码（前三末一）。
     */

    public abstract class ErbiGenerater : IWordCodeGenerater
    {
        /// <summary>
        /// 二笔的编码可能是一字多码的
        /// </summary>
        private Dictionary<char, IList<string>> erbiDic;
        /// <summary>
        /// 1是现代二笔，2是音形，3是超强二笔，4是青松二笔
        /// </summary>
        protected abstract int DicColumnIndex { get; }

        protected Dictionary<char, IList<string>> ErbiDic
        {
            get
            {
                if (erbiDic == null)
                {
                    //该字典包含4种编码，1是现代二笔，2是音形，3是超强二笔，4是青松二笔
                    string txt = Dictionaries.Erbi;

                    erbiDic = new Dictionary<char, IList<string>>();
                    foreach (string line in txt.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] arr = line.Split('\t');
                        if (arr[0].Length == 0)
                        {
                            continue;
                        }
                        char word = arr[0][0];
                        string code = arr[DicColumnIndex];
                        if (code == "")
                        {
                            code = arr[1];
                        }
                        var codes = code.Split(' '); //code之间空格分割
                        erbiDic[word] = new List<string>(codes);
                    }
                    OverrideDictionary(erbiDic);
                }
                return erbiDic;
            }
        }
        /// <summary>
        /// 读取外部的字典文件，覆盖系统默认字典
        /// </summary>
        /// <param name="dictionary"></param>
        protected virtual void OverrideDictionary(IDictionary<char, IList<string>> dictionary)
        {
            var fileContent = FileOperationHelper.ReadFile("mb.txt");
            if (fileContent != "")
            {
                foreach (string line in fileContent.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] arr = line.Split('\t');
                    if (arr[0].Length == 0)
                    {
                        continue;
                    }
                    char word = arr[0][0];
                    string code = arr[1];
                    var codes = code.Split(' ');
                    dictionary[word] = new List<string>(codes);//强行覆盖现有字典
                }
            }
        }

        public bool IsBaseOnOldCode { get { return true; } }
        #region IWordCodeGenerater Members

        public bool Is1Char1Code
        {
            get { return false; }
        }

        public string GetDefaultCodeOfChar(char str)
        {
            return ErbiDic[str][0];
        }

        public IList<string> GetCodeOfString(string str, string charCodeSplit = "")
        {
            IList<string> pinYin =  pinyinGenerater.GetCodeOfString(str);
            var codes = GetErbiCode(str, pinYin);
            var result = CollectionHelper.Descartes(codes);
            return result;
        }
        private static PinyinGenerater pinyinGenerater = new PinyinGenerater();
        public IList<string> GetCodeOfChar(char str)
        {
            return ErbiDic[str];
        }


        public bool Is1CharMutiCode
        {
            get { return true; }
        }

        #endregion
        public IList<string> GetCodeOfWordLibrary(WordLibrary wl, string charCodeSplit = "")
        {
            IList<string> pinYin = null;
            if (wl.CodeType == CodeType.Pinyin)
            {
                pinYin = wl.PinYin;
            }
            else
            {
                //生成拼音
                pinYin = pinyinGenerater.GetCodeOfString(wl.Word);
            }
            var codes = GetErbiCode(wl.Word, pinYin);
            if (codes == null)
                return null;
            var result = CollectionHelper.Descartes(codes);
            return result;
        }


        protected virtual IList<IList<string>> GetErbiCode(string str, IList<string> py)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            var codes = new List<IList<string>>();

         
            try
            {
                if (str.Length == 1)
                {
                    codes.Add(Get1CharCode(str[0], py[0]));
                }
                else if (str.Length == 2)//各取2码
                {
                    codes.Add(Get1CharCode(str[0], py[0]));
                    codes.Add(Get1CharCode(str[1], py[1]));
                }
                else if (str.Length == 3)
                {
                    codes.Add(Get1CharCode(str[0], py[0]));
                    codes.Add(new List<string>() { py[1][0].ToString() });
                    codes.Add(new List<string>() { py[2][0].ToString() });
                }
                else
                {
                    codes.Add(new List<string>() { py[0][0].ToString() + py[1][0] + py[2][0] + py[str.Length - 1][0] });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
            return codes;
        }
        /// <summary>
        /// 获得一个字的二笔码
        /// </summary>
        /// <param name="c"></param>
        /// <param name="py"></param>
        /// <returns></returns>
        protected IList<string> Get1CharCode(char c,string py)
        {
            var result = new List<string>();
            var codes = ErbiDic[c];
            foreach ( var code in codes)
            {
                result.Add(py[0].ToString()+code[0]);
            }
            return result;
        }
    }
}