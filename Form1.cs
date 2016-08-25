using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Tesseract;
using System.IO;
using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Emgu.CV.Util;
using Emgu.Util;

namespace EmguTesseract
{
    public partial class Form1 : Form
    {
        string path;
        string fileName;
        decimal succeed = 0;
        decimal failed = 0;
        decimal rate = 0;
        int count = 0;

        
        bool USE_AdaptiveThreshold = false;
        int AdaptiveblockSize = 35;

        int nBlockSize = 1;
        int scale = 1;
        int medianBlurBlurKsize = 3;

        int dgGrayValue = 158; //值越小，擦除力度越大；当值大于255时, 无法过滤任何点
        int maxNearPoints = 4; //值越大，擦除力度越大
        int testTimes = 300;
        //scale = 1; USE_AdaptiveThreshold = false && BlurKsize = 5, rate = %46
        //scale = 2; USE_AdaptiveThreshold = false && BlurKsize = 9, rate = %40 ?
        //scale 3, USE_AdaptiveThreshold False, BlurKsize 13, succeed 504, failed 496, rate 50.40%
        //scale 3, USE_AdaptiveThreshold False, BlurKsize 15, 350 samples, rete 43.5%
        //scale: 3, USE_AdaptiveThreshold? False, BlurKsize: 11, succeed: 458, failed: 542, rate: 45.80%
        //scale: 3, USE_AdaptiveThreshold? true, blocksize:5 BlurKsize: 11, succeed: 34, failed: 244, rate: 12.23%
        //scale: 4, USE_AdaptiveThreshold? false, BlurKsize: 17, succeed: 297, failed: 329, rate: 47.44%
        //scale: 4, USE_AdaptiveThreshold? false, BlurKsize: 15, succeed: 160, failed: 202, rate: 44.20%
        //scale: 4, USE_AdaptiveThreshold? False, BlurKsize: 19, succeed: 457, failed: 543, rate: 45.70%
        //using 100 trained data, scale: 3, USE_AdaptiveThreshold? False, BlurKsize: 13, succeed: 415, failed: 585, rate: 41.50%
        //using 200 trained data,scale: 3, USE_AdaptiveThreshold? False, BlurKsize: 13, succeed: 504, failed: 496, rate: 50.40%
        //using 300 trained data,scale: 3, USE_AdaptiveThreshold? False, BlurKsize: 13, succeed: 365, failed: 441, rate: 45.29%
        //using eng data, scale:1,USE_AdaptiveThreshold? False; RemoveBlock 2; ClearNoise:255; no medianBlur, succeed: 295, failed: 221, rate: 57.17%
        //using 300 trained data, scale:1,USE_AdaptiveThreshold? False; RemoveBlock 2; ClearNoise:255; no medianBlur, succeed: 109, failed: 111, rate: 49.55%
        //using 300 trained data, ClearNoiseByVolidPoint ONLY, dgGrayValue 80;  maxNearPoints 4, succeed: 38, failed: 33, rate: 53.52%
        //using eng data, ClearNoiseByVolidPoint ONLY, dgGrayValue 80;  maxNearPoints 4, succeed:192, failed: 108, rate: 64.00%
        //using eng data, ClearNoiseByVolidPoint ONLY, dgGrayValue 150;  maxNearPoints 4, test 30, rate: 70.00%
        //using eng data, ClearNoiseByVolidPoint ONLY, dgGrayValue 158;  maxNearPoints 4, test 30, rate: 60.00%


        public Form1()
        {
            InitializeComponent();
            path = System.Environment.CurrentDirectory + "\\" + "codePictures";
            System.IO.Directory.CreateDirectory(path);
        }

        public bool downloadImage(string url)
        {
            try
            {
                WebRequest requestPic = WebRequest.Create(url);
                WebResponse responsePic = requestPic.GetResponse();
                Image webImage = Image.FromStream(responsePic.GetResponseStream()); // Error
                fileName = "vCode" + System.DateTime.Now.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo) + ".jpg";
                webImage.Save(path + "\\" + fileName);
            }
            catch (Exception ex)
            {
                setLogT(0, "download image error: " + ex.Message );
                return false;
            }
            return true;
        }


        public string processOCR(string file)
        {
            string result;
            try{

        //        Image<Gray, byte> raw = new Image<Gray, byte>(file);
       //         raw = raw.Resize(scale, Emgu.CV.CvEnum.Inter.Linear);

                //need to be removed
        //        raw.Save(path + "\\" + "bigRaw" + fileName); //for testing



                Bitmap bm = new Bitmap(Image.FromFile(file));
                setImage1(new Image<Gray, byte>(bm));
                // 水平垂直切割
                //           ImageProcessor.CutVerticality(bm, 1);
                //           ImageProcessor.CutHorizontally(bm, 1);

                // 去掉规模小于指定值的连通子图
    //            ImageProcessor.RemoveBlock(bm, nBlockSize);
                // 腐蚀算法测试
                //           ImageProcessor.Corrode(bm, Direction.Left | Direction.Right);



                ClearNoiseByVolidPoint(dgGrayValue,maxNearPoints,bm);
       //         ClearNoise(255, bm);
                setImage2(new Image<Gray, byte>(bm));

        //        ImageProcessor.RemoveBlock(bm, nBlockSize);

                Image<Gray, byte> raw = new Image<Gray, byte>(bm);
            //    raw = raw.Resize(scale, Emgu.CV.CvEnum.Inter.Linear);
                if (USE_AdaptiveThreshold)
                {
                    //自适应二值化，  7似乎最佳， 不加这句有时候效果更好，需要测试
                    CvInvoke.AdaptiveThreshold(raw, raw, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, AdaptiveblockSize, 2);
                }
                Image<Gray, byte> dest = new Image<Gray, byte>(raw.Width, raw.Height);


       //         CvInvoke.MedianBlur(raw, dest, medianBlurBlurKsize); //5最佳
           //     dest.Save(path + "\\" + "afterblur" + fileName);
         //       setImage2(dest);

                using (var engine = new TesseractEngine(
                    @"C:\Users\yangyiru\Documents\visual studio 2012\Projects\EmguTesseract\EmguTesseract\tessdata",
                    "eng", EngineMode.Default))
                {
                    engine.SetVariable("tessedit_char_whitelist", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                    engine.SetVariable("textord_min_xheight", 28);

                    using (var pix = PixConverter.ToPix(bm))
                    {
                        using (var page = engine.Process(pix, PageSegMode.SingleLine))
                        {
            //                textBox2.Text = String.Format("{0:P}", page.GetMeanConfidence());
                            result = page.GetText().Replace(" ","").Replace("\n","").Replace("\r","");
                            setTextBox2(result);
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                setLogT(0, "processOCR error: " + ex.Message );
                return "";
            }
            return result;
        }

        public void processResult(string Html, string code)
        {
            if (Html.Contains("Please enter the correct verification code"))
            {
                failed++;

                //System.IO.File.Move(path + "\\" + "afterblur" + fileName,
                //    path + "\\" + "wrong" + "afterblur" + fileName);
                
                rate = succeed / (succeed + failed);
                
                setLable1Red("failed");
                setLogT(0, "code: " + code + ", succeed: " + succeed + ", failed: " + failed + ", rate: " + rate.ToString("p"));
            return;
            }
            if (Html.Contains("Record with specified details does not exists"))
            {
                succeed++;
                rate = succeed / (succeed + failed);
                setLable1Green("succeed");
                setLogT(0, "code: " + code + ", succeed: " + succeed + ", failed: " + failed + ", rate: " + rate.ToString("p"));
                return;
            }
        }

        #region from http://www.cnblogs.com/yuanbao/archive/2007/11/14/958488.html

        /// <summary>
        ///  去掉杂点（适合杂点/杂线粗为1）
        /// </summary>
        /// <param name="dgGrayValue">背前景灰色界限</param>
        /// <returns></returns>
        public void ClearNoiseByVolidPoint(int dgGrayValue, int MaxNearPoints, Bitmap bmpobj)
        {
            Color piexl;
            int nearDots = 0;
            int XSpan, YSpan, tmpX, tmpY;
            //逐点判断
            for (int i = 0; i < bmpobj.Width; i++)
                for (int j = 0; j < bmpobj.Height; j++)
                {
                    piexl = bmpobj.GetPixel(i, j);
                    if (piexl.R < dgGrayValue)
                    {
                        nearDots = 0;
                        //判断周围8个点是否全为空
                        if (i == 0 || i == bmpobj.Width - 1 || j == 0 || j == bmpobj.Height - 1)  //边框全去掉
                        {
                            bmpobj.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                        }
                        else
                        {
                            if (bmpobj.GetPixel(i - 1, j - 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i, j - 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i + 1, j - 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i - 1, j).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i + 1, j).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i - 1, j + 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i, j + 1).R < dgGrayValue) nearDots++;
                            if (bmpobj.GetPixel(i + 1, j + 1).R < dgGrayValue) nearDots++;
                        }

                        if (nearDots < MaxNearPoints)
                            bmpobj.SetPixel(i, j, Color.FromArgb(255, 255, 255));   //去掉单点 && 粗细小3邻边点
                    }
                    else  //背景
                        bmpobj.SetPixel(i, j, Color.FromArgb(255, 255, 255));
                }
        }

        /// <summary>
        /// 3×3中值滤波除杂，yuanbao,2007.10
        /// </summary>
        /// <param name="dgGrayValue"></param>
        public void ClearNoise(int dgGrayValue, Bitmap bmpobj)
        {
            int x, y;
            byte[] p = new byte[9]; //最小处理窗口3*3
            byte s;
            //byte[] lpTemp=new BYTE[nByteWidth*nHeight];
            int i, j;

            //--!!!!!!!!!!!!!!下面开始窗口为3×3中值滤波!!!!!!!!!!!!!!!!
            for (y = 1; y < bmpobj.Height - 1; y++) //--第一行和最后一行无法取窗口
            {
                for (x = 1; x < bmpobj.Width - 1; x++)
                {
                    //取9个点的值
                    p[0] = bmpobj.GetPixel(x - 1, y - 1).R;
                    p[1] = bmpobj.GetPixel(x, y - 1).R;
                    p[2] = bmpobj.GetPixel(x + 1, y - 1).R;
                    p[3] = bmpobj.GetPixel(x - 1, y).R;
                    p[4] = bmpobj.GetPixel(x, y).R;
                    p[5] = bmpobj.GetPixel(x + 1, y).R;
                    p[6] = bmpobj.GetPixel(x - 1, y + 1).R;
                    p[7] = bmpobj.GetPixel(x, y + 1).R;
                    p[8] = bmpobj.GetPixel(x + 1, y + 1).R;
                    //计算中值
                    for (j = 0; j < 5; j++)
                    {
                        for (i = j + 1; i < 9; i++)
                        {
                            if (p[j] > p[i])
                            {
                                s = p[j];
                                p[j] = p[i];
                                p[i] = s;
                            }
                        }
                    }

                          if (bmpobj.GetPixel(x, y).R < dgGrayValue)
                    bmpobj.SetPixel(x, y, Color.FromArgb(p[4], p[4], p[4]));    //给有效值付中值
                }
            }
        }
        #endregion

        public void start()
        {
            

            for (int i = 0; i < testTimes; i++)
            {
                
                if (gForceToStop)
                    return;

                count++;
                setLogT(0, "test " + count);
                
                string gViewstate = "zN6xXaY%2FnQNmaaIlERdi7LQl%2BBtTJSWlQckAPk" +
                                        "%2B4oQpDovIGW80RqFi8gdy3WhVH9%2FaN7mJd%2BMEmlZBEsSF%2ByOrvGBQmXgcDAi%2BO9AZeeh%2FvK93W1m3x4J2IF47SmIiHIhH2iS" +
                                        "%2For3foC1jhAbq3mE2y7gVlT2PW0PVHQcOWIyTnacwRm1yz7MUOv0C4D6ErgIGBblYp1Eq%2FkCbk1RwOkYRsHTE9jCaRPaEdsfmgDXqVo2Jj44CXh7DJpwpTz" +
                                        "%2B9Kce5uTWQgsAeK63DU2oIDGuqRS%2BDFuwERMTl0bhGpkJQ6lURgByidtd%2FpdAi5OaiK2%2BYBbueGbIYCnxcBiQqswxO4IUTWj9dFUHiiVkSlbPdZ6Fqc4JsiEP6WTb2zKy7BtsceJJmN59AQAGFBNLYQSAD1A8k" +
                                        "%2BDekyhJ5Vp65n8SHJKcu3gTh32VGAWhiailxZioWVkiJZZsWb6tp6M1Uo%2FFdZj8Ol8Y2gRFt2hjRJzs%2FhD0gzkllIOqPWIgoD9vn9" +
                                        "%2B2qUiBdHIWE%3D";
                string gEventvalidation = "MfAzSYnx%2FBo9NlbEJZGqAfsO1qbH2Pbq2qGK8OTeqfnJLJ52qCApEepqQ%2BUvWVZdGuavmxNvymnyQeocxo4k3Q%3D%3D";
                int gTicket = 1;
                string respHtml = weLoveYue(
                    0,
                    "https://www.visaservices.in/DIAC-China-Appointment_new/AppScheduling/AppWelcome.aspx?p=sPcgcjykQzBJn3ZQhoWvHUCcn911JlTQwOXWcGhM4%2fE%3d",
                    "POST",
                    "https://www.visaservices.in/DIAC-China-Appointment_new/AppScheduling/AppWelcome.aspx?p=sPcgcjykQzBJn3ZQhoWvHUCcn911JlTQwOXWcGhM4%2fE%3d",
                    true,
                    "__EVENTTARGET=ctl00%24plhMain%24lnkPrintApp"
                    + "&__EVENTARGUMENT="
                    + "&__VIEWSTATE=" + gViewstate
                    + "&____Ticket=" + gTicket.ToString()
                    + "&__EVENTVALIDATION=" + gEventvalidation
                );
                gTicket++;

                reg = @"(?<=name=""__EVENTVALIDATION"" id=""__EVENTVALIDATION"" value="").*?(?="" />)";
                myMatch = (new Regex(reg)).Match(respHtml);
                if (myMatch.Success)
                {
                    gEventvalidation = ToUrlEncode(myMatch.Groups[0].Value);

                }
                else
                {
                    goto exception;
                }

                reg = @"(?<=id=""__VIEWSTATE"" value="").*?(?="" />)";
                myMatch = (new Regex(reg)).Match(respHtml);
                if (myMatch.Success)
                {
                    gViewstate = ToUrlEncode(myMatch.Groups[0].Value);
                }
                else
                {
                    goto exception;
                }

                string cCodeGuid = "";
                reg = @"(?<=MyCaptchaImage.aspx\?guid=).*?(?="" border=)";
                myMatch = (new Regex(reg)).Match(respHtml);
                if (myMatch.Success)
                {
                    cCodeGuid = myMatch.Groups[0].Value;
                }
                string imageUrl = @"https://www.visaservices.in/DIAC-China-Appointment_new/AppScheduling/MyCaptchaImage.aspx?guid=" + cCodeGuid;

                if (downloadImage(imageUrl))
                {
                    string code = processOCR(path + "\\" + fileName);

                    Thread.Sleep(2000);//否则服务器认为验证码是错的

                    respHtml = weLoveYue(
                    0,
                    "https://www.visaservices.in/DIAC-China-Appointment_new/AppScheduling/EmailRegistration.aspx?p=sPcgcjykQzBJn3ZQhoWvHUCcn911JlTQwOXWcGhM4%2fE%3d",
                    "POST",
                    "https://www.visaservices.in/DIAC-China-Appointment_new/AppScheduling/AppWelcome.aspx?p=sPcgcjykQzBJn3ZQhoWvHUCcn911JlTQwOXWcGhM4%2fE%3d",
                    false,
                    "__VIEWSTATE=" + gViewstate
                    + "&ctl00%24plhMain%24ImageButton1=Submit&____Ticket=" + gTicket.ToString()
                    + "&__EVENTVALIDATION=" + gEventvalidation
                    + "&ctl00%24plhMain%24mycaptchacontrol1=" + code
                    + "&ctl00%24plhMain%24txtEmailID=" + "15985830370@163.com"
                    + "&ctl00%24plhMain%24txtPassword=" + "mushroom123"
                    );
                    processResult(respHtml, code);
                }
                
                continue;

            exception:
                setLogT(0, "page response error, try again..." );
                continue;
            }

            setLogT(0, "scale: " + scale +
                ", nBlockSize=" + nBlockSize +
                ", USE_AdaptiveThreshold=" + USE_AdaptiveThreshold +
                ", BlurKsize=" + medianBlurBlurKsize +
                ", succeed:" + succeed + ", failed: " + failed + ", rate: " + rate.ToString("p"));
            return;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            gForceToStop = false;
            ThreadStart starter = delegate { start(); };
            new Thread(starter).Start();
        }



#region http interface

        string reg = "";
        Match myMatch;
        public static int timeoutTime = 60000;
        public const int retry = 5;
        public static bool gForceToStop = false;
        CookieCollection gCookieContainer = null;

        /* 
         * return response HTML
         */
        public string weLoveYue(int threadNo, string url, string method, string referer, bool allowAutoRedirect, string postData)
        {
            for (int i = 0; i < retry; i++)
            {
                if (gForceToStop)
                {
                    break;
                }
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse resp = null;
                setRequest(req, threadNo);
                req.Method = method;
                req.Referer = referer;
                if (allowAutoRedirect)
                {
                    req.AllowAutoRedirect = true;
                }

                if (method.Equals("POST"))
                {
                    if (writePostData(threadNo, req, postData) < 0)
                    {
                        continue;
                    }
                }
                string respHtml = "";
                try
                {
                    resp = (HttpWebResponse)req.GetResponse();
                }
                catch (WebException webEx)
                {
                    setLogT(0, "respStreamReader, " + webEx.Status.ToString() );
                    continue;
                }
                if (resp != null)
                {
                    respHtml = resp2html(resp);
                    if (respHtml.Equals(""))
                    {
                        continue;
                    }
                    gCookieContainer = req.CookieContainer.GetCookies(req.RequestUri);
                    resp.Close();
                    return respHtml;
                }
                else
                {
                    continue;
                }
            }
            return "";
        }
        public static string ToUrlEncode(string strCode)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = System.Text.Encoding.UTF8.GetBytes(strCode); //默认是System.Text.Encoding.Default.GetBytes(str)  
            System.Text.RegularExpressions.Regex regKey = new System.Text.RegularExpressions.Regex("^[A-Za-z0-9]+$");
            for (int i = 0; i < byStr.Length; i++)
            {
                string strBy = Convert.ToChar(byStr[i]).ToString();
                if (regKey.IsMatch(strBy))
                {
                    //是字母或者数字则不进行转换    
                    sb.Append(strBy);
                }
                else
                {
                    sb.Append(@"%" + Convert.ToString(byStr[i], 16));
                }
            }
            return (sb.ToString());
        }
        public void setRequest(HttpWebRequest req, int threadNo)
        {
            //req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            //req.Accept = "*/*";
            //req.Connection = "keep-alive";
            //req.KeepAlive = true;
            //req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; InfoPath.3; .NET4.0C; .NET4.0E";
            //req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:37.0) Gecko/20100101 Firefox/37.0";
            //req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; InfoPath.3; .NET4.0C; .NET4.0E";
            //req.Headers["Accept-Encoding"] = "gzip, deflate";
            //req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            req.Host = "www.visaservices.in";
            req.Timeout = timeoutTime;

            req.AllowAutoRedirect = false;
            req.ContentType = "application/x-www-form-urlencoded";
            req.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.10; rv:40.0) Gecko/20100101 Firefox/40.0";
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.PerDomainCapacity = 40;
            if (gCookieContainer != null)
            {
                req.CookieContainer.Add(gCookieContainer);
            }
        }
        public int writePostData(int threadNo, HttpWebRequest req, string data)
        {
            byte[] postBytes = Encoding.UTF8.GetBytes(data);
            req.ContentLength = postBytes.Length;
            Stream postDataStream = null;
            try
            {
                postDataStream = req.GetRequestStream();
                postDataStream.Write(postBytes, 0, postBytes.Length);

            }
            catch (WebException webEx)
            {
                setLogT(0, "While writing post data," + webEx.Status.ToString() );
                return -1;
            }

            postDataStream.Close();
            return 1;
        }
        public string resp2html(HttpWebResponse resp)
        {
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                StreamReader stream = new StreamReader(resp.GetResponseStream());
                return stream.ReadToEnd();
            }
            else
            {
                return resp.StatusDescription;
            }
        }

        public delegate void setLog(int threadNo, string str1);
        public void setLogT(int threadNo, string s)
        {
            if (textBox1.InvokeRequired)
            {
                setLog sl = new setLog(delegate(int number, string text)
                {
                    textBox1.AppendText("线程" + number.ToString() + " " + DateTime.Now.ToString() + " " + text + Environment.NewLine);
                });
                textBox1.Invoke(sl, threadNo, s);
            }
            else
            {
                textBox1.AppendText("线程" + threadNo.ToString() + " " + DateTime.Now.ToString() + " " + s + Environment.NewLine);
            }
        }
        public void setTextBox2(string s)
        {
            setLog sl = new setLog(delegate(int number, string text)
            {
                textBox2.Text = text;        
            });
            textBox2.Invoke(sl, 0, s);
        }
        public void setLable1Green(string s)
        {
            setLog sl = new setLog(delegate(int number, string text)
            {
                label1.Text = text;
                label1.ForeColor = Color.Green;
            });
            textBox2.Invoke(sl, 0, s);
        }
        public void setLable1Red(string s)
        {
            setLog sl = new setLog(delegate(int number, string text)
            {
                label1.Text = text;
                label1.ForeColor = Color.Red;
            });
            textBox2.Invoke(sl, 0, s);
        }

        public delegate void SetPicture(Image<Gray, byte> dest);
        public void setImage1(Image<Gray, byte> dest)
        {
            SetPicture sl = new SetPicture(delegate(Image<Gray, byte> d)
            {
                imageBox1.Image = d;

                //清空结果框
                imageBox2.Image = null;
                textBox2.Text = "";
                label1.Text = "";

            });
            textBox1.Invoke(sl, dest);
        }
        public void setImage2(Image<Gray, byte> dest)
        {
            SetPicture sl = new SetPicture(delegate(Image<Gray, byte> d)
            {
                imageBox2.Image = d;
            });
            textBox1.Invoke(sl,dest);
        }
#endregion

        #region tools
        private void button2_Click(object sender, EventArgs e)
        {
            gForceToStop = true; 
            textBox1.AppendText("user operation: stop running");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string[] appending = null;
            string destFile = null;


            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "choose dest box (will be changed)";
            openFileDialog.InitialDirectory = "C://Users//yangyiru//Dropbox//targetImage";
            openFileDialog.Filter = "box文件|*.box|所有文件|*.*";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                destFile = openFileDialog.FileName;
            }

            if (String.IsNullOrEmpty(destFile))
            {
                return;
            }
            
            openFileDialog.Title = "choose the appending box (the last 100 boxes)";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fName = openFileDialog.FileName;
                appending = File.ReadAllLines(fName);
            }

            string[] destContent =  File.ReadAllLines(destFile);
            int incremental = 0;
            for(int i = destContent.Length - 1; i >= 0; i--){
                if(!String.IsNullOrWhiteSpace(destContent[i])){
                    incremental = int.Parse(destContent[i].Split(' ').Last()) + 1;
                    break;
                }
            }
            
            string toBeAppended = "";
            foreach (string line in appending)
            {
                string[] s = line.Split(' ');
                s[s.Length - 1] = (int.Parse(s.Last()) + incremental).ToString();
                foreach(string ss in s){
                    toBeAppended += ss + " ";
                }
                toBeAppended = toBeAppended.Remove(toBeAppended.Length - 1, 1);
                toBeAppended += Environment.NewLine;
            }
            File.AppendAllText(destFile, toBeAppended, Encoding.UTF8);
            MessageBox.Show("merge box files succeed!");
        }

        #endregion







    }

    #region LicensePlateDetector

    //for calling
    /*
    List<IInputOutputArray> licensePlateImagesList = new List<IInputOutputArray>();
                    List<IInputOutputArray> filteredLicensePlateImagesList = new List<IInputOutputArray>();
                    List<RotatedRect> detectedLicensePlateRegionList = new List<RotatedRect>();
                    Mat i1 = new Mat(path + "\\" + fileName, LoadImageType.Color);
                    LicensePlateDetector l = new LicensePlateDetector(@"C:\Users\yangyiru\Documents\visual studio 2012\Projects\EmguTesseract\EmguTesseract\tessdata");
                    List<String> s = l.DetectLicensePlate(
                                   i1,
                                    licensePlateImagesList,
                                   filteredLicensePlateImagesList,
                                   detectedLicensePlateRegionList);
                    foreach (string ss in s)
                    {
                        setTextBox2(ss);
                    }
    */



    // <summary>
    /// A simple license plate detector
    /// </summary>
    public class LicensePlateDetector : DisposableObject
    {
        /// <summary>
        /// The OCR engine
        /// </summary>
        private TesseractEngine _ocr;

        /// <summary>
        /// Create a license plate detector
        /// </summary>
        /// <param name="dataPath">
        /// The datapath must be the name of the parent directory of tessdata and
        /// must end in / . Any name after the last / will be stripped.
        /// </param>
        public LicensePlateDetector(String dataPath)
        {
            //create OCR engine
            _ocr = new TesseractEngine(dataPath, "eng", EngineMode.Default);
            _ocr.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyz");
        }

        /// <summary>
        /// Detect license plate from the given image
        /// </summary>
        /// <param name="img">The image to search license plate from</param>
        /// <param name="licensePlateImagesList">A list of images where the detected license plate regions are stored</param>
        /// <param name="filteredLicensePlateImagesList">A list of images where the detected license plate regions (with noise removed) are stored</param>
        /// <param name="detectedLicensePlateRegionList">A list where the regions of license plate (defined by an MCvBox2D) are stored</param>
        /// <returns>The list of words for each license plate</returns>
        public List<String> DetectLicensePlate(
           IInputArray img,
           List<IInputOutputArray> licensePlateImagesList,
           List<IInputOutputArray> filteredLicensePlateImagesList,
           List<RotatedRect> detectedLicensePlateRegionList)
        {
            List<String> licenses = new List<String>();
            using (Mat gray = new Mat())
            using (Mat canny = new Mat())
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
                CvInvoke.Canny(gray, canny, 100, 50, 3, false);
                int[,] hierachy = CvInvoke.FindContourTree(canny, contours, ChainApproxMethod.ChainApproxSimple);

                FindLicensePlate(contours, hierachy, 0, gray, canny, licensePlateImagesList, filteredLicensePlateImagesList, detectedLicensePlateRegionList, licenses);
            }
            return licenses;
        }

        private static int GetNumberOfChildren(int[,] hierachy, int idx)
        {
            //first child
            idx = hierachy[idx, 2];
            if (idx < 0)
                return 0;

            int count = 1;
            while (hierachy[idx, 0] > 0)
            {
                count++;
                idx = hierachy[idx, 0];
            }
            return count;
        }

        private void FindLicensePlate(
           VectorOfVectorOfPoint contours, int[,] hierachy, int idx, IInputArray gray, IInputArray canny,
           List<IInputOutputArray> licensePlateImagesList, List<IInputOutputArray> filteredLicensePlateImagesList, List<RotatedRect> detectedLicensePlateRegionList,
           List<String> licenses)
        {
            for (; idx >= 0; idx = hierachy[idx, 0])
            {
                int numberOfChildren = GetNumberOfChildren(hierachy, idx);
                //if it does not contains any children (charactor), it is not a license plate region
                if (numberOfChildren == 0) continue;

                using (VectorOfPoint contour = contours[idx])
                {
                    if (CvInvoke.ContourArea(contour) > 10)
                    {
                        if (numberOfChildren < 3)
                        {
                            //If the contour has less than 3 children, it is not a license plate (assuming license plate has at least 3 charactor)
                            //However we should search the children of this contour to see if any of them is a license plate
                            FindLicensePlate(contours, hierachy, hierachy[idx, 2], gray, canny, licensePlateImagesList,
                               filteredLicensePlateImagesList, detectedLicensePlateRegionList, licenses);
                            continue;
                        }

                        RotatedRect box = CvInvoke.MinAreaRect(contour);
                        if (box.Angle < -45.0)
                        {
                            float tmp = box.Size.Width;
                            box.Size.Width = box.Size.Height;
                            box.Size.Height = tmp;
                            box.Angle += 90.0f;
                        }
                        else if (box.Angle > 45.0)
                        {
                            float tmp = box.Size.Width;
                            box.Size.Width = box.Size.Height;
                            box.Size.Height = tmp;
                            box.Angle -= 90.0f;
                        }

                        double whRatio = (double)box.Size.Width / box.Size.Height;
                        if (!(3.0 < whRatio && whRatio < 7.0))
                        //if (!(3.0 < whRatio && whRatio < 10.0))
                        {
                            //if the width height ratio is not in the specific range,it is not a license plate 
                            //However we should search the children of this contour to see if any of them is a license plate
                            //Contour<Point> child = contours.VNext;
                            if (hierachy[idx, 2] > 0)
                                FindLicensePlate(contours, hierachy, hierachy[idx, 2], gray, canny, licensePlateImagesList,
                                   filteredLicensePlateImagesList, detectedLicensePlateRegionList, licenses);
                            continue;
                        }

                        using (UMat tmp1 = new UMat())
                        using (UMat tmp2 = new UMat())
                        {
                            PointF[] srcCorners = box.GetVertices();

                            PointF[] destCorners = new PointF[] {
                        new PointF(0, box.Size.Height - 1),
                        new PointF(0, 0),
                        new PointF(box.Size.Width - 1, 0), 
                        new PointF(box.Size.Width - 1, box.Size.Height - 1)};

                            using (Mat rot = CameraCalibration.GetAffineTransform(srcCorners, destCorners))
                            {
                                CvInvoke.WarpAffine(gray, tmp1, rot, Size.Round(box.Size));
                            }

                            //resize the license plate such that the front is ~ 10-12. This size of front results in better accuracy from tesseract
                            Size approxSize = new Size(240, 180);
                            double scale = Math.Min(approxSize.Width / box.Size.Width, approxSize.Height / box.Size.Height);
                            Size newSize = new Size((int)Math.Round(box.Size.Width * scale), (int)Math.Round(box.Size.Height * scale));
                            CvInvoke.Resize(tmp1, tmp2, newSize, 0, 0, Inter.Cubic);

                            //removes some pixels from the edge
                            int edgePixelSize = 2;
                            Rectangle newRoi = new Rectangle(new Point(edgePixelSize, edgePixelSize),
                               tmp2.Size - new Size(2 * edgePixelSize, 2 * edgePixelSize));
                            UMat plate = new UMat(tmp2, newRoi);

                            UMat filteredPlate = FilterPlate(plate);

                            string words;
                            using (UMat tmp = filteredPlate.Clone())
                            {
                                var pix = PixConverter.ToPix(tmp.Bitmap);
                                var page = _ocr.Process(pix, PageSegMode.SingleLine);
                                words = page.GetText();

                                if (words.Length == 0) continue;
                            }

                            licenses.Add(words);
                            licensePlateImagesList.Add(plate);
                            filteredLicensePlateImagesList.Add(filteredPlate);
                            detectedLicensePlateRegionList.Add(box);

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Filter the license plate to remove noise
        /// </summary>
        /// <param name="plate">The license plate image</param>
        /// <returns>License plate image without the noise</returns>
        private static UMat FilterPlate(UMat plate)
        {
            UMat thresh = new UMat();
            CvInvoke.Threshold(plate, thresh, 120, 255, ThresholdType.BinaryInv);
            //Image<Gray, Byte> thresh = plate.ThresholdBinaryInv(new Gray(120), new Gray(255));

            Size plateSize = plate.Size;
            using (Mat plateMask = new Mat(plateSize.Height, plateSize.Width, DepthType.Cv8U, 1))
            using (Mat plateCanny = new Mat())
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                plateMask.SetTo(new MCvScalar(255.0));
                CvInvoke.Canny(plate, plateCanny, 100, 50);
                CvInvoke.FindContours(plateCanny, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                int count = contours.Size;
                for (int i = 1; i < count; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    {

                        Rectangle rect = CvInvoke.BoundingRectangle(contour);
                        if (rect.Height > (plateSize.Height >> 1))
                        {
                            rect.X -= 1; rect.Y -= 1; rect.Width += 2; rect.Height += 2;
                            Rectangle roi = new Rectangle(Point.Empty, plate.Size);
                            rect.Intersect(roi);
                            CvInvoke.Rectangle(plateMask, rect, new MCvScalar(), -1);
                            //plateMask.Draw(rect, new Gray(0.0), -1);
                        }
                    }

                }

                thresh.SetTo(new MCvScalar(), plateMask);
            }

            CvInvoke.Erode(thresh, thresh, null, new Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
            CvInvoke.Dilate(thresh, thresh, null, new Point(-1, -1), 1, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);

            return thresh;
        }

        protected override void DisposeObject()
        {
            _ocr.Dispose();
        }
    }
#endregion
}
