﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using MSScriptControl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShareX.ScreenCaptureLib;
using TrOCR.Helper;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace TrOCR
{

	public sealed partial class FmMain
    {

		public FmMain()
		{
			set_merge = false;
			set_split = false;
			set_split = false;
			StaticValue.截图排斥 = false;
			pinyin_flag = false;
			tranclick = false;
			are = new AutoResetEvent(false);
			imagelist = new List<Image>();
			StaticValue.NoteCount = Convert.ToInt32(IniHelper.GetValue("配置", "记录数目"));
			baidu_flags = "";
			esc = "";
			voice_count = 0;
			fmNote = new FmNote();
			pubnote = new string[StaticValue.NoteCount];
			for (var i = 0; i < StaticValue.NoteCount; i++)
			{
				pubnote[i] = "";
			}
			StaticValue.v_note = pubnote;
			StaticValue.mainHandle = Handle;
			Font = new Font(Font.Name, 9f / StaticValue.DpiFactor, Font.Style, Font.Unit, Font.GdiCharSet, Font.GdiVerticalFont);
			googleTranslate_txt = "";
			num_ok = 0;
			F_factor = Program.Factor;
			components = null;
			InitializeComponent();
			nextClipboardViewer = (IntPtr)HelpWin32.SetClipboardViewer((int)Handle);
			InitMinimize();
			InitConfig();
			WindowState = FormWindowState.Minimized;
			Visible = false;
			split_txt = "";
			MinimumSize = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
			speak_copy = false;
			OCR_foreach("");
		}

		private void Load_Click(object sender, EventArgs e)
		{
			WindowState = FormWindowState.Minimized;
			Visible = false;
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 953)
			{
				speaking = false;
			}
			if (m.Msg == 274 && (int)m.WParam == 61536)
			{
				WindowState = FormWindowState.Minimized;
				Visible = false;
				return;
			}
			if (m.Msg == 600 && (int)m.WParam == 725)
			{
				if (IniHelper.GetValue("工具栏", "顶置") == "True")
				{
					TopMost = true;
					return;
				}
				TopMost = false;
				return;
			}

            if (m.Msg == 786 && m.WParam.ToInt32() == 530 && RichBoxBody.Text != null)
            {
                p_note(RichBoxBody.Text);
                StaticValue.v_note = pubnote;
                if (fmNote.Created)
                {
                    fmNote.TextNote = "";
                }
            }
            if (m.Msg == 786 && m.WParam.ToInt32() == 520)
            {
                fmNote.Show();
                fmNote.Focus();
                fmNote.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - fmNote.Width, Screen.PrimaryScreen.WorkingArea.Height - fmNote.Height);
                fmNote.WindowState = FormWindowState.Normal;
                return;
            }
            if (m.Msg == 786 && m.WParam.ToInt32() == 580)
            {
                HelpWin32.UnregisterHotKey(Handle, 205);
                change_QQ_screenshot = false;
                FormBorderStyle = FormBorderStyle.None;
                Hide();
                if (transtalate_fla == "开启")
                {
                    form_width = Width / 2;
                }
                else
                {
                    form_width = Width;
                }
                form_height = Height;
                minico.Visible = false;
                minico.Visible = true;
                menu.Close();
                menu_copy.Close();
                auto_fla = "开启";
                split_txt = "";
                RichBoxBody.Text = "***该区域未发现文本***";
                RichBoxBody_T.Text = "";
                typeset_txt = "";
                transtalate_fla = "关闭";
                Trans_close.PerformClick();
                Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
                FormBorderStyle = FormBorderStyle.Sizable;
                StaticValue.截图排斥 = true;
                image_screen = StaticValue.image_OCR;
                if (IniHelper.GetValue("工具栏", "分栏") == "True")
                {
                    minico.Visible = true;
                    thread = new Thread(ShowLoading);
                    thread.Start();
                    ts = new TimeSpan(DateTime.Now.Ticks);
                    var image = image_screen;
                    var image2 = new Bitmap(image.Width, image.Height);
                    var graphics = Graphics.FromImage(image2);
                    graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                    graphics.Save();
                    graphics.Dispose();
                    image_ori = image2;
                    ((Bitmap)FindBundingBox_fences((Bitmap)image)).Save("Data\\分栏预览图.jpg");
                }
                else
                {
                    minico.Visible = true;
                    thread = new Thread(ShowLoading);
                    thread.Start();
                    ts = new TimeSpan(DateTime.Now.Ticks);
                    var messageload = new Messageload();
                    messageload.ShowDialog();
                    if (messageload.DialogResult == DialogResult.OK)
                    {
                        esc_thread = new Thread(Main_OCR_Thread);
                        esc_thread.Start();
                    }
                }
            }
            if (m.Msg == 786 && m.WParam.ToInt32() == 590 && speak_copyb == "朗读")
            {
                TTS();
                return;
            }
            if (m.Msg == 786 && m.WParam.ToInt32() == 511)
            {
                base.MinimumSize = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
                transtalate_fla = "关闭";
                RichBoxBody.Dock = DockStyle.Fill;
                RichBoxBody_T.Visible = false;
                PictureBox1.Visible = false;
                RichBoxBody_T.Text = "";
                if (WindowState == FormWindowState.Maximized)
                {
                    WindowState = FormWindowState.Normal;
                }
                Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
            }
            if (m.Msg == 786 && m.WParam.ToInt32() == 512)
            {
                transtalate_Click();
            }
            if (m.Msg == 786 && m.WParam.ToInt32() == 518)
            {
                if (ActiveControl.Name == "htmlTextBoxBody")
                {
                    htmltxt = RichBoxBody.Text;
                }
                if (ActiveControl.Name == "rich_trans")
                {
                    htmltxt = RichBoxBody_T.Text;
                }
                if (htmltxt == "")
                {
                    return;
                }
                TTS();
            }
            if (m.Msg == 161)
            {
                HelpWin32.SetForegroundWindow(Handle);
                base.WndProc(ref m);
                return;
            }
            if (m.Msg != 163)
            {
                if (m.Msg == 786 && m.WParam.ToInt32() == 222)
                {
                    try
                    {
                        StaticValue.截图排斥 = false;
                        esc = "退出";
                        fmloading.FmlClose = "窗体已关闭";
                        esc_thread.Abort();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    FormBorderStyle = FormBorderStyle.Sizable;
                    Visible = true;
                    base.Show();
                    WindowState = FormWindowState.Normal;
                    if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
                    {
                        var value = IniHelper.GetValue("快捷键", "翻译文本");
                        var text = "None";
                        var text2 = "F9";
                        SetHotkey(text, text2, value, 205);
                    }
                    HelpWin32.UnregisterHotKey(Handle, 222);
                }
                if (m.Msg == 786 && m.WParam.ToInt32() == 200)
                {
                    HelpWin32.UnregisterHotKey(Handle, 205);
                    menu.Hide();
                    RichBoxBody.Hide = "";
                    RichBoxBody_T.Hide = "";
                    Main_OCR_Quickscreenshots();
                }
                if (m.Msg == 786 && m.WParam.ToInt32() == 206)
                {
                    if (!fmNote.Visible || base.Focused)
                    {
                        fmNote.Show();
                        fmNote.WindowState = FormWindowState.Normal;
                        fmNote.Visible = true;
                    }
                    else
                    {
                        fmNote.Hide();
                        fmNote.WindowState = FormWindowState.Minimized;
                        fmNote.Visible = false;
                    }
                }
                if (m.Msg == 786 && m.WParam.ToInt32() == 235)
                {
                    if (!Visible)
                    {
                        TopMost = true;
                        base.Show();
                        WindowState = FormWindowState.Normal;
                        Visible = true;
                        Thread.Sleep(100);
                        if (IniHelper.GetValue("工具栏", "顶置") == "False")
                        {
                            TopMost = false;
                            return;
                        }
                    }
                    else
                    {
                        Hide();
                        Visible = false;
                    }
                }
                if (m.Msg == 786 && m.WParam.ToInt32() == 205)
                {
                    翻译文本();
                }
                base.WndProc(ref m);
                return;
            }
            if (transtalate_fla == "开启")
            {
                WindowState = FormWindowState.Normal;
                Size = new Size((int)font_base.Width * 23 * 2, (int)font_base.Height * 24);
                Location = (Point)new Size(Screen.PrimaryScreen.Bounds.Width / 2 - Screen.PrimaryScreen.Bounds.Width / 10 * 2, Screen.PrimaryScreen.Bounds.Height / 2 - Screen.PrimaryScreen.Bounds.Height / 6);
                return;
            }
            WindowState = FormWindowState.Normal;
            Location = (Point)new Size(Screen.PrimaryScreen.Bounds.Width / 2 - Screen.PrimaryScreen.Bounds.Width / 10, Screen.PrimaryScreen.Bounds.Height / 2 - Screen.PrimaryScreen.Bounds.Height / 6);
            Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
        }

		public void InitMinimize()
		{
			try
			{
				var menuItems = new[]
				{
                    new MenuItem("显示", trayShowClick),
                    new MenuItem("设置", tray_Set_Click),
                    new MenuItem("更新", tray_update_Click),
                    new MenuItem("帮助", tray_help_Click),
                    new MenuItem("退出", trayExitClick)
                };
				minico.ContextMenu = new ContextMenu(menuItems);
			}
			catch (Exception ex)
			{
				MessageBox.Show("InitMinimize()" + ex.Message);
			}
		}

		private void trayShowClick(object sender, EventArgs e)
		{
            Show();
			Activate();
			Visible = true;
			WindowState = FormWindowState.Normal;
            TopMost = IniHelper.GetValue("工具栏", "顶置") == "True";
		}

		private void trayExitClick(object sender, EventArgs e)
		{
			minico.Dispose();
			saveIniFile();
			Process.GetCurrentProcess().Kill();
		}

		private void MainCopyClick(object sender, EventArgs e)
		{
			RichBoxBody.Focus();
			RichBoxBody.richTextBox1.Copy();
		}

		private void Main_SelectAll_Click(object sender, EventArgs e)
		{
			RichBoxBody.Focus();
			RichBoxBody.richTextBox1.SelectAll();
		}

		private void Main_paste_Click(object sender, EventArgs e)
		{
			RichBoxBody.Focus();
			RichBoxBody.richTextBox1.Paste();
		}

		public void Split_Click(object sender, EventArgs e)
		{
			RichBoxBody.Text = split_txt;
		}

		public static byte[] copybyte(byte[] a, byte[] b)
		{
			var array = new byte[a.Length + b.Length];
			a.CopyTo(array, 0);
			b.CopyTo(array, a.Length);
			return array;
		}

		public void OCR_sougou()
		{
			try
			{
				split_txt = "";
				var image = image_screen;
				var i = image.Width;
				var j = image.Height;
				if (i < 300)
				{
					while (i < 300)
					{
						j *= 2;
						i *= 2;
					}
				}
				if (j < 120)
				{
					while (j < 120)
					{
						j *= 2;
						i *= 2;
					}
				}
				var bitmap = new Bitmap(i, j);
				var graphics = Graphics.FromImage(bitmap);
				graphics.DrawImage(image, 0, 0, i, j);
				graphics.Save();
				graphics.Dispose();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(OCR_sougou_SogouOCR(bitmap)))["result"].ToString());
				bitmap.Dispose();
				checked_txt(jarray, 2, "content");
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public byte[] ImageTobyte(Image imgPhoto)
		{
			var memoryStream = new MemoryStream();
			imgPhoto.Save(memoryStream, ImageFormat.Jpeg);
			var array = new byte[memoryStream.Length];
			memoryStream.Position = 0L;
			memoryStream.Read(array, 0, array.Length);
			memoryStream.Close();
			return array;
		}

		private Bitmap GetPlus(Bitmap bm, double times)
		{
			var width = (int)(bm.Width / times);
			var height = (int)(bm.Height / times);
			var bitmap = new Bitmap(width, height);
			if (times >= 1.0 && times <= 1.1)
			{
				bitmap = bm;
			}
			else
			{
				var graphics = Graphics.FromImage(bitmap);
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.DrawImage(bm, new Rectangle(0, 0, width, height), new Rectangle(0, 0, bm.Width, bm.Height), GraphicsUnit.Pixel);
				graphics.Dispose();
			}
			return bitmap;
		}

		public void OCR_Tencent()
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundaryRDEqU0w702X9cWPJ";
				var image = image_screen;
				if (image.Width > 90 && image.Height < 90)
				{
					var bitmap = new Bitmap(image.Width, 300);
					var graphics = Graphics.FromImage(bitmap);
					graphics.DrawImage(image, 5, 0, image.Width, image.Height);
					graphics.Save();
					graphics.Dispose();
					image = new Bitmap(bitmap);
				}
				else if (image.Width <= 90 && image.Height >= 90)
				{
					var bitmap2 = new Bitmap(300, image.Height);
					var graphics2 = Graphics.FromImage(bitmap2);
					graphics2.DrawImage(image, 0, 5, image.Width, image.Height);
					graphics2.Save();
					graphics2.Dispose();
					image = new Bitmap(bitmap2);
				}
				else if (image.Width < 90 && image.Height < 90)
				{
					var bitmap3 = new Bitmap(300, 300);
					var graphics3 = Graphics.FromImage(bitmap3);
					graphics3.DrawImage(image, 5, 5, image.Width, image.Height);
					graphics3.Save();
					graphics3.Dispose();
					image = new Bitmap(bitmap3);
				}
				else
				{
					image = image_screen;
				}
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"image_file\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://ai.qq.com/cgi-bin/appdemo_generalocr");
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = "http://ai.qq.com/product/ocr.shtml";
				httpWebRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				httpWebRequest.Timeout = 8000;
				httpWebRequest.ReadWriteTimeout = 2000;
				var buffer = array;
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(buffer, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["item_list"].ToString());
				checked_txt(jarray, 1, "itemstring");
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public void OCR_baidu_bak()
		{
			split_txt = "";
			try
			{
				var str = "CHN_ENG";
				split_txt = "";
				var image = image_screen;
				var memoryStream = new MemoryStream();
				image.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				if (interface_flag == "中英")
				{
					str = "CHN_ENG";
				}
				if (interface_flag == "日语")
				{
					str = "JAP";
				}
				if (interface_flag == "韩语")
				{
					str = "KOR";
				}
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var str2 = "";
				var str3 = "";
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					var array2 = jobject["words"].ToString().ToCharArray();
					if (!char.IsPunctuation(array2[array2.Length - 1]))
					{
						if (!contain_ch(jobject["words"].ToString()))
						{
							str3 = str3 + jobject["words"].ToString().Trim() + " ";
						}
						else
						{
							str3 += jobject["words"].ToString();
						}
					}
					else if (own_punctuation(array2[array2.Length - 1].ToString()))
					{
						if (!contain_ch(jobject["words"].ToString()))
						{
							str3 = str3 + jobject["words"].ToString().Trim() + " ";
						}
						else
						{
							str3 += jobject["words"].ToString();
						}
					}
					else
					{
						str3 = str3 + jobject["words"] + "\r\n";
					}
					str2 = str2 + jobject["words"] + "\r\n";
				}
				split_txt = str2;
				typeset_txt = str3;
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		private void OCR_sougou_Click(object sender, EventArgs e)
		{
			OCR_foreach("搜狗");
		}

		private void OCR_tencent_Click(object sender, EventArgs e)
		{
			OCR_foreach("腾讯");
		}

		private void OCR_baidu_Click(object sender, EventArgs e)
		{
		}

		public void OCR_youdao()
		{
			try
			{
				split_txt = "";
				var image = image_screen;
				if (image.Width > 90 && image.Height < 90)
				{
					var bitmap = new Bitmap(image.Width, 200);
					var graphics = Graphics.FromImage(bitmap);
					graphics.DrawImage(image, 5, 0, image.Width, image.Height);
					graphics.Save();
					graphics.Dispose();
					image = new Bitmap(bitmap);
				}
				else if (image.Width <= 90 && image.Height >= 90)
				{
					var bitmap2 = new Bitmap(200, image.Height);
					var graphics2 = Graphics.FromImage(bitmap2);
					graphics2.DrawImage(image, 0, 5, image.Width, image.Height);
					graphics2.Save();
					graphics2.Dispose();
					image = new Bitmap(bitmap2);
				}
				else if (image.Width < 90 && image.Height < 90)
				{
					var bitmap3 = new Bitmap(200, 200);
					var graphics3 = Graphics.FromImage(bitmap3);
					graphics3.DrawImage(image, 5, 5, image.Width, image.Height);
					graphics3.Save();
					graphics3.Dispose();
					image = new Bitmap(bitmap3);
				}
				else
				{
					image = image_screen;
				}
				var i = image.Width;
				var j = image.Height;
				if (i < 600)
				{
					while (i < 600)
					{
						j *= 2;
						i *= 2;
					}
				}
				if (j < 120)
				{
					while (j < 120)
					{
						j *= 2;
						i *= 2;
					}
				}
				var bitmap4 = new Bitmap(i, j);
				var graphics4 = Graphics.FromImage(bitmap4);
				graphics4.DrawImage(image, 0, 0, i, j);
				graphics4.Save();
				graphics4.Dispose();
				image = new Bitmap(bitmap4);
				var inArray = OCR_ImgToByte(image);
				var s = "imgBase=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(inArray)) + "&lang=auto&company=";
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://aidemo.youdao.com/ocrapi1");
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = "http://aidemo.youdao.com/ocrdemo";
				httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest.Timeout = 8000;
				httpWebRequest.ReadWriteTimeout = 2000;
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["lines"].ToString());
				checked_txt(jarray, 1, "words");
				image.Dispose();
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public void OCR_youdao_Click(object sender, EventArgs e)
		{
			OCR_foreach("有道");
		}

		public void change_Chinese_Click(object sender, EventArgs e)
		{
			language = "中文标点";
			if (typeset_txt != "")
			{
				RichBoxBody.Text = punctuation_en_ch_x(RichBoxBody.Text);
				RichBoxBody.Text = punctuation_quotation(RichBoxBody.Text);
			}
		}

		public void change_English_Click(object sender, EventArgs e)
		{
			language = "英文标点";
			if (typeset_txt != "")
			{
				RichBoxBody.Text = punctuation_ch_en(RichBoxBody.Text);
			}
		}

		public static string punctuation_ch_en(string text)
		{
			var array = text.ToCharArray();
			for (var i = 0; i < array.Length; i++)
			{
				var num = "：。；，？！“”‘’【】（）".IndexOf(array[i]);
				if (num != -1)
				{
					array[i] = ":.;,?!\"\"''[]()"[num];
				}
			}
			return new string(array);
		}

		public void saveIniFile()
		{
			IniHelper.SetValue("配置", "接口", interface_flag);
		}

		private void InitConfig()
		{
			interface_flag = IniHelper.GetValue("配置", "接口");
			if (interface_flag == "发生错误")
			{
				IniHelper.SetValue("配置", "接口", "搜狗");
				OCR_foreach("搜狗");
			}
			else
			{
				OCR_foreach(interface_flag);
			}
			var filePath = AppDomain.CurrentDomain.BaseDirectory + "Data\\config.ini";
			if (IniHelper.GetValue("快捷键", "文字识别") != "请按下快捷键")
			{
				var value = IniHelper.GetValue("快捷键", "文字识别");
				var text = "None";
				var text2 = "F4";
				SetHotkey(text, text2, value, 200);
			}
			if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
			{
				var value2 = IniHelper.GetValue("快捷键", "翻译文本");
				var text3 = "None";
				var text4 = "F7";
				SetHotkey(text3, text4, value2, 205);
			}
			if (IniHelper.GetValue("快捷键", "记录界面") != "请按下快捷键")
			{
				var value3 = IniHelper.GetValue("快捷键", "记录界面");
				var text5 = "None";
				var text6 = "F8";
				SetHotkey(text5, text6, value3, 206);
			}
			if (IniHelper.GetValue("快捷键", "识别界面") != "请按下快捷键")
			{
				var value4 = IniHelper.GetValue("快捷键", "识别界面");
				var text7 = "None";
				var text8 = "F11";
				SetHotkey(text7, text8, value4, 235);
			}
			StaticValue.BD_API_ID = HelpWin32.IniFileHelper.GetValue("密钥_百度", "secret_id", filePath);
			if (HelpWin32.IniFileHelper.GetValue("密钥_百度", "secret_id", filePath) == "发生错误")
			{
				StaticValue.BD_API_ID = "请输入secret_id";
			}
			StaticValue.BD_API_KEY = HelpWin32.IniFileHelper.GetValue("密钥_百度", "secret_key", filePath);
			if (HelpWin32.IniFileHelper.GetValue("密钥_百度", "secret_key", filePath) == "发生错误")
			{
				StaticValue.BD_API_KEY = "请输入secret_key";
			}
		}

		public static string check_ch_en(string text)
		{
			var array = text.ToCharArray();
			for (var i = 0; i < array.Length; i++)
			{
				var num = "：".IndexOf(array[i]);
				if (num != -1 && i - 1 >= 0 && i + 1 < array.Length && contain_en(array[i - 1].ToString()) && contain_en(array[i + 1].ToString()))
				{
					array[i] = ":"[num];
				}
				if (num != -1 && i - 1 >= 0 && i + 1 < array.Length && contain_en(array[i - 1].ToString()) && contain_punctuation(array[i + 1].ToString()))
				{
					array[i] = ":"[num];
				}
			}
			return new string(array);
		}

		public void tray_Set_Click(object sender, EventArgs e)
		{
			var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			HelpWin32.UnregisterHotKey(Handle, 200);
			HelpWin32.UnregisterHotKey(Handle, 205);
			HelpWin32.UnregisterHotKey(Handle, 206);
			HelpWin32.UnregisterHotKey(Handle, 235);
			WindowState = FormWindowState.Minimized;
			var fmSetting = new FmSetting();
			fmSetting.TopMost = true;
			fmSetting.ShowDialog();
			if (fmSetting.DialogResult == DialogResult.OK)
			{
				var filePath = AppDomain.CurrentDomain.BaseDirectory + "Data\\config.ini";
				StaticValue.NoteCount = Convert.ToInt32(HelpWin32.IniFileHelper.GetValue("配置", "记录数目", filePath));
				pubnote = new string[StaticValue.NoteCount];
				for (var i = 0; i < StaticValue.NoteCount; i++)
				{
					pubnote[i] = "";
				}
				StaticValue.v_note = pubnote;
				fmNote.TextNoteChange = "";
				fmNote.Location = new Point(Screen.AllScreens[0].WorkingArea.Width - fmNote.Width, Screen.AllScreens[0].WorkingArea.Height - fmNote.Height);
				if (IniHelper.GetValue("快捷键", "文字识别") != "请按下快捷键")
				{
					var value = IniHelper.GetValue("快捷键", "文字识别");
					var text = "None";
					var text2 = "F4";
					SetHotkey(text, text2, value, 200);
				}
				if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
				{
					var value2 = IniHelper.GetValue("快捷键", "翻译文本");
					var text3 = "None";
					var text4 = "F9";
					SetHotkey(text3, text4, value2, 205);
				}
				if (IniHelper.GetValue("快捷键", "记录界面") != "请按下快捷键")
				{
					var value3 = IniHelper.GetValue("快捷键", "记录界面");
					var text5 = "None";
					var text6 = "F8";
					SetHotkey(text5, text6, value3, 206);
				}
				if (IniHelper.GetValue("快捷键", "识别界面") != "请按下快捷键")
				{
					var value4 = IniHelper.GetValue("快捷键", "识别界面");
					var text7 = "None";
					var text8 = "F11";
					SetHotkey(text7, text8, value4, 235);
				}
                StaticValue.BD_API_ID = IniHelper.GetValue("密钥_百度", "secret_id");
                StaticValue.BD_API_KEY = IniHelper.GetValue("密钥_百度", "secret_key");
			}
		}

		public static bool IsNum(string str)
        {
            return Regex.IsMatch(str, "^\\d+$");
		}

		public bool own_punctuation(string text)
		{
			return ",;，、<>《》()-（）.。".IndexOf(text) != -1;
		}

		public static string punctuation_Del_space(string text)
		{
			var pattern = "(?<=.)([^\\*]+)(?=.)";
			string result;
			if (Regex.Match(text, pattern).ToString().IndexOf(" ") >= 0)
			{
				text = Regex.Replace(text, "(?<=[\\p{P}*])([a-zA-Z])(?=[a-zA-Z])", " $1");
				char[] trimChars = null;
				text = text.TrimEnd(trimChars).Replace("- ", "-").Replace("_ ", "_").Replace("( ", "(").Replace("/ ", "/").Replace("\" ", "\"");
				result = text;
			}
			else
			{
				result = text;
			}
			return result;
		}

		public static bool contain_ch(string str)
		{
			return Regex.IsMatch(str, "[\\u4e00-\\u9fa5]");
		}

		public void transtalate_Click()
		{
			typeset_txt = RichBoxBody.Text;
			RichBoxBody_T.Visible = true;
			WindowState = FormWindowState.Normal;
			transtalate_fla = "开启";
			RichBoxBody.Dock = DockStyle.None;
			RichBoxBody_T.Dock = DockStyle.None;
			RichBoxBody_T.BorderStyle = BorderStyle.Fixed3D;
			RichBoxBody_T.Text = "";
			RichBoxBody.Focus();
			if (num_ok == 0)
			{
				RichBoxBody.Size = new Size(ClientRectangle.Width, ClientRectangle.Height);
				Size = new Size(RichBoxBody.Width * 2, RichBoxBody.Height);
				RichBoxBody_T.Size = new Size(RichBoxBody.Width, RichBoxBody.Height);
				RichBoxBody_T.Location = (Point)new Size(RichBoxBody.Width, 0);
				RichBoxBody_T.Name = "rich_trans";
				RichBoxBody_T.TabIndex = 1;
				RichBoxBody_T.Text_flag = "我是翻译文本框";
				RichBoxBody_T.ImeMode = ImeMode.On;
			}
			num_ok++;
			PictureBox1.Visible = true;
			PictureBox1.BringToFront();
			MinimumSize = new Size((int)font_base.Width * 23 * 2, (int)font_base.Height * 24);
			Size = new Size((int)font_base.Width * 23 * 2, (int)font_base.Height * 24);
			CheckForIllegalCrossThreadCalls = false;
			new Thread(trans_Calculate).Start();
		}

		private void Form_Resize(object sender, EventArgs e)
		{
			if (RichBoxBody.Dock != DockStyle.Fill)
			{
				RichBoxBody.Size = new Size(ClientRectangle.Width / 2, ClientRectangle.Height);
				RichBoxBody_T.Size = new Size(RichBoxBody.Width, ClientRectangle.Height);
				RichBoxBody_T.Location = (Point)new Size(RichBoxBody.Width, 0);
			}
		}

		public void Trans_copy_Click(object sender, EventArgs e)
		{
			RichBoxBody_T.Focus();
			RichBoxBody_T.richTextBox1.Copy();
		}

		public void Trans_paste_Click(object sender, EventArgs e)
		{
			RichBoxBody_T.Focus();
			RichBoxBody_T.richTextBox1.Paste();
		}

		public void Trans_SelectAll_Click(object sender, EventArgs e)
		{
			RichBoxBody_T.Focus();
			RichBoxBody_T.richTextBox1.SelectAll();
		}

		public void trans_Calculate()
		{
			if (pinyin_flag)
			{
				googleTranslate_txt = HanToPinyin.GetFullPinyin(typeset_txt);
			}
			else if (typeset_txt == "")
			{
				googleTranslate_txt = "";
			}
			else
			{
				if (interface_flag == "韩语")
				{
					StaticValue.ZH2EN = false;
					StaticValue.ZH2JP = false;
					StaticValue.ZH2KO = true;
					RichBoxBody_T.set_language = "韩语";
				}
				if (interface_flag == "日语")
				{
					StaticValue.ZH2EN = false;
					StaticValue.ZH2JP = true;
					StaticValue.ZH2KO = false;
					RichBoxBody_T.set_language = "日语";
				}
				if (interface_flag == "中英")
				{
					StaticValue.ZH2EN = true;
					StaticValue.ZH2JP = false;
					StaticValue.ZH2KO = false;
					RichBoxBody_T.set_language = "中英";
				}
				if (IniHelper.GetValue("配置", "翻译接口") == "谷歌")
				{
					googleTranslate_txt = Translate_Google(typeset_txt);
				}
				if (IniHelper.GetValue("配置", "翻译接口") == "百度")
				{
					googleTranslate_txt = Translate_baidu(typeset_txt);
				}
				if (IniHelper.GetValue("配置", "翻译接口") == "腾讯")
				{
					googleTranslate_txt = Translate_Tencent(typeset_txt);
				}
			}
			PictureBox1.Visible = false;
			PictureBox1.SendToBack();
			Invoke(new translate(translate_child));
			pinyin_flag = false;
		}

		public void Trans_close_Click(object sender, EventArgs e)
		{
			base.MinimumSize = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
			transtalate_fla = "关闭";
			RichBoxBody.Dock = DockStyle.Fill;
			RichBoxBody_T.Visible = false;
			PictureBox1.Visible = false;
			RichBoxBody_T.Text = "";
			if (WindowState == FormWindowState.Maximized)
			{
				WindowState = FormWindowState.Normal;
			}
			Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
		}

		private void ShowLoading()
		{
			try
			{
				fmloading = new FmLoading();
				Application.Run(fmloading);
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
			finally
			{
				thread.Abort();
			}
		}

		public bool contain(string text, string subStr)
		{
			return text.Contains(subStr);
		}

		public static bool contain_en(string str)
		{
			return Regex.IsMatch(str, "[a-zA-Z]");
		}

		public static bool punctuation_has_punctuation(string str)
		{
			var pattern = contain_ch(str) ? "[\\；\\，\\。\\！\\？]" : "[\\;\\,\\.\\!\\?]";
			return Regex.IsMatch(str, pattern);
		}

		private string punctuation_quotation(string pStr)
		{
			pStr = pStr.Replace("“", "\"").Replace("”", "\"");
			var array = pStr.Split('"');
			var text = "";
			for (var i = 1; i <= array.Length; i++)
			{
				if (i % 2 == 0)
				{
					text = text + array[i - 1] + "”";
				}
				else
				{
					text = text + array[i - 1] + "“";
				}
			}
			return text.Substring(0, text.Length - 1);
		}

		public static bool HasenPunctuation(string str)
		{
			var pattern = "[\\;\\,\\.\\!\\?]";
			return Regex.IsMatch(str, pattern);
		}

		public static string Del_Space(string text)
		{
			text = Regex.Replace(text, "([\\p{P}]+)", "**&&**$1**&&**");
			char[] trimChars = null;
			text = text.TrimEnd(trimChars).Replace(" **&&**", "").Replace("**&&** ", "").Replace("**&&**", "");
			return text;
		}

		public void TTS()
		{
			new Thread(TTS_thread).Start();
		}

		private void translate_child()
		{
			RichBoxBody_T.Text = googleTranslate_txt;
			googleTranslate_txt = "";
		}

        public void TTS_thread()
        {
            try
            {
                var text = htmltxt.Replace("***", "");
                var lang = CommonHelper.LangDetect(text);
                var url = "https://fanyi.baidu.com/gettts?lan=" + lang + "&text=" + HttpUtility.UrlEncode(text) +
                          "&vol=9&per=0&spd=6&pit=4&source=web&ctp=1";
                ttsData = new HttpClient
                {
                    Timeout = new TimeSpan(0, 2, 0)
                }.GetByteArrayAsync(url).Result;
                if (speak_copyb == "朗读" || voice_count == 0)
                {
                    Invoke(new translate(Speak_child));
                    speak_copyb = "";
                }
                else
                {
                    Invoke(new translate(TTS_child));
                }
                voice_count++;
            }
            catch (Exception)
            {
                MessageBox.Show("文本过长，请使用右键菜单中的选中朗读！", "提醒");
            }
        }

        public void TTS_child()
		{
			if (RichBoxBody.Text != null || RichBoxBody_T.Text != "")
			{
				if (speaking)
				{
					HelpWin32.mciSendString("close media", null, 0, IntPtr.Zero);
					speaking = false;
					return;
				}
				var tempPath = Path.GetTempPath();
				var text = tempPath + "\\声音.mp3";
				try
				{
					File.WriteAllBytes(text, ttsData);
				}
				catch
				{
					text = tempPath + "\\声音1.mp3";
					File.WriteAllBytes(text, ttsData);
				}
				PlaySong(text);
				speaking = true;
			}
		}
        
		protected override CreateParams CreateParams
		{
			get
			{
				var createParams = base.CreateParams;
				createParams.ExStyle |= 134217728;
				return createParams;
			}
		}

		public string Translate_Google(string text)
		{
			var text2 = "";
			try
			{
				var text3 = "zh-CN";
				var text4 = "en";
				if (StaticValue.ZH2EN)
				{
					if (ch_count(text.Trim()) > en_count(text.Trim()) || (en_count(text.Trim()) == 1 && ch_count(text.Trim()) == 1))
					{
						text3 = "zh-CN";
						text4 = "en";
					}
					else
					{
						text3 = "en";
						text4 = "zh-CN";
					}
				}
				if (StaticValue.ZH2JP)
				{
					if (contain_jap(replaceStr(Del_ch(text.Trim()))))
					{
						text3 = "ja";
						text4 = "zh-CN";
					}
					else
					{
						text3 = "zh-CN";
						text4 = "ja";
					}
				}
				if (StaticValue.ZH2KO)
				{
					if (contain_kor(text.Trim()))
					{
						text3 = "ko";
						text4 = "zh-CN";
					}
					else
					{
						text3 = "zh-CN";
						text4 = "ko";
					}
				}

                //string.Concat("client=gtx&sl=", text3, "&tl=", text4, "&dt=t&q=", HttpUtility.UrlEncode(text).Replace("+", "%20"))
                var data = new Dictionary<string, string>();
                data.Add("client", "gtx");
                data.Add("sl", text3);
                data.Add("tl", text4);
                data.Add("dt", "t");
                data.Add("q", text);
                var html = CommonHelper.PostData("https://translate.google.cn/translate_a/single", data);

				var jArray = (JArray)JsonConvert.DeserializeObject(html);
				var count = ((JArray)jArray[0]).Count;
				for (var i = 0; i < count; i++)
				{
					text2 += jArray[0][i][0].ToString();
				}
			}
			catch (Exception)
			{
				text2 = "[谷歌接口报错]：\r\n1.网络错误或者文本过长。\r\n2.谷歌接口可能对于某些网络不能用，具体不清楚。可以尝试挂VPN试试。\r\n3.这个问题我没办法修复，请右键菜单更换百度、腾讯翻译接口。";
			}
			return text2;
		}

		public static string CookieCollectionToStrCookie(CookieCollection cookie)
		{
			string result;
			if (cookie == null)
			{
				result = string.Empty;
			}
			else
			{
				var text = string.Empty;
				foreach (var obj in cookie)
				{
					var cookie2 = (Cookie)obj;
					text += string.Format("{0}={1};", cookie2.Name, cookie2.Value);
				}
				result = text;
			}
			return result;
		}

		public string ScanQRCode()
		{
			var result = "";
			try
			{
				var image = new BinaryBitmap(new HybridBinarizer(new BitmapLuminanceSource((Bitmap)image_screen)));
				var result2 = new QRCodeReader().decode(image);
				if (result2 != null)
				{
					result = result2.Text;
				}
			}
			catch
			{
			}
			return result;
		}

		public void SearchSelText(object sender, EventArgs e)
		{
			Process.Start("https://www.baidu.com/s?wd=" + RichBoxBody.SelectText);
		}

		public void tray_update_Click(object sender, EventArgs e)
		{
			Program.CheckUpdate();
		}

		public static bool contain_jap(string str)
		{
			return Regex.IsMatch(str, "[\\u3040-\\u309F]") || Regex.IsMatch(str, "[\\u30A0-\\u30FF]");
		}

		public static bool contain_kor(string str)
		{
			return Regex.IsMatch(str, "[\\uac00-\\ud7ff]");
		}

		public static string Del_ch(string str)
		{
			var text = str;
			if (Regex.IsMatch(str, "[\\u4e00-\\u9fa5]"))
			{
				text = string.Empty;
				var array = str.ToCharArray();
				for (var i = 0; i < array.Length; i++)
				{
					if (array[i] < '一' || array[i] > '龥')
					{
						text += array[i].ToString();
					}
				}
			}
			return text;
		}

        private static string replaceStr(string hexData)
		{
			return Regex.Replace(hexData, "[\\p{P}+~$`^=|<>～｀＄＾＋＝｜＜＞￥×┊ ]", "").ToUpper();
		}

		public static string RemovePunctuation(string str)
		{
			str = str.Replace(",", "").Replace("，", "").Replace(".", "").Replace("。", "").Replace("!", "").Replace("！", "").Replace("?", "").Replace("？", "").Replace(":", "").Replace("：", "").Replace(";", "").Replace("；", "").Replace("～", "").Replace("-", "").Replace("_", "").Replace("——", "").Replace("—", "").Replace("--", "").Replace("【", "").Replace("】", "").Replace("\\", "").Replace("(", "").Replace(")", "").Replace("（", "").Replace("）", "").Replace("#", "").Replace("$", "").Replace("、", "").Replace("‘", "").Replace("’", "").Replace("“", "").Replace("”", "");
			return str;
		}

		public static string GetUniqueFileName(string fullName)
		{
			string result;
			if (!File.Exists(fullName))
			{
				result = fullName;
			}
			else
			{
				var directoryName = Path.GetDirectoryName(fullName);
				var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullName);
				var extension = Path.GetExtension(fullName);
				var num = 1;
				string text;
				do
				{
					text = Path.Combine(directoryName, string.Format("{0}[{1}].{2}", fileNameWithoutExtension, num++, extension));
				}
				while (File.Exists(text));
				result = text;
			}
			return result;
		}

		public static string ReFileName(string strFolderPath, string strFileName)
		{
			var text = strFolderPath + "\\" + strFileName;
			var startIndex = text.LastIndexOf('.');
			text = text.Insert(startIndex, "_{0}");
			var num = 1;
			var path = string.Format(text, num);
			while (File.Exists(path))
			{
				path = string.Format(text, num);
				num++;
			}
			return Path.GetFileName(path);
		}

		public void PlaySong(string file)
		{
			HelpWin32.mciSendString("close media", null, 0, IntPtr.Zero);
			HelpWin32.mciSendString("open \"" + file + "\" type mpegvideo alias media", null, 0, IntPtr.Zero);
			HelpWin32.mciSendString("play media notify", null, 0, Handle);
		}

		public void Main_Voice_Click(object sender, EventArgs e)
		{
			RichBoxBody.Focus();
			speak_copyb = "朗读";
			htmltxt = RichBoxBody.SelectText;
			HelpWin32.SendMessage(Handle, 786, 590);
		}

		public void Trans_Voice_Click(object sender, EventArgs e)
		{
			RichBoxBody_T.Focus();
			speak_copyb = "朗读";
			htmltxt = RichBoxBody_T.SelectText;
			HelpWin32.SendMessage(Handle, 786, 590);
		}

		public void Speak_child()
		{
			if (RichBoxBody.Text != null || RichBoxBody_T.Text != "")
			{
				var tempPath = Path.GetTempPath();
				var text = tempPath + "\\声音.mp3";
				try
				{
					File.WriteAllBytes(text, ttsData);
				}
				catch
				{
					text = tempPath + "\\声音1.mp3";
					File.WriteAllBytes(text, ttsData);
				}
				PlaySong(text);
				speaking = true;
			}
		}

		public static string ToSimplified(string source)
		{
			var text = new string(' ', source.Length);
			HelpWin32.LCMapString(2048, 33554432, source, source.Length, text, source.Length);
			return text;
		}

		public static string ToTraditional(string source)
		{
			var text = new string(' ', source.Length);
			HelpWin32.LCMapString(2048, 67108864, source, source.Length, text, source.Length);
			return text;
		}

		public void change_zh_tra_Click(object sender, EventArgs e)
		{
			if (RichBoxBody.Text != null)
			{
				RichBoxBody.Text = ToTraditional(RichBoxBody.Text);
			}
		}

		public void change_tra_zh_Click(object sender, EventArgs e)
		{
			if (RichBoxBody.Text != null)
			{
				RichBoxBody.Text = ToSimplified(RichBoxBody.Text);
			}
		}

		public void change_str_Upper_Click(object sender, EventArgs e)
		{
			if (RichBoxBody.Text != null)
			{
				RichBoxBody.Text = RichBoxBody.Text.ToUpper();
			}
		}

		public void change_Upper_str_Click(object sender, EventArgs e)
		{
			if (RichBoxBody.Text != null)
			{
				RichBoxBody.Text = RichBoxBody.Text.ToLower();
			}
		}

		public string[] hotkey(string text, string text2, string value)
		{
			var array = (value + "+").Split('+');
			if (array.Length == 3)
			{
				text = array[0];
				text2 = array[1];
			}
			if (array.Length == 2)
			{
				text = "None";
				text2 = value;
			}
			return new[]
			{
				text,
				text2
			};
		}

		public void SetHotkey(string text, string text2, string value, int flag)
		{
			var array = (value + "+").Split('+');
			if (array.Length == 3)
			{
				text = array[0];
				text2 = array[1];
			}
			if (array.Length == 2)
			{
				text = "None";
				text2 = value;
			}
			var array2 = new[]
			{
				text,
				text2
			};
			if (!HelpWin32.RegisterHotKey(Handle, flag, (HelpWin32.KeyModifiers)Enum.Parse(typeof(HelpWin32.KeyModifiers), array2[0].Trim()), (Keys)Enum.Parse(typeof(Keys), array2[1].Trim())))
			{
				CommonHelper.ShowHelpMsg("快捷键冲突，请更换！");
			}
			HelpWin32.RegisterHotKey(Handle, flag, (HelpWin32.KeyModifiers)Enum.Parse(typeof(HelpWin32.KeyModifiers), array2[0].Trim()), (Keys)Enum.Parse(typeof(Keys), array2[1].Trim()));
		}

		public void bool_error()
		{
		}

		public void p_note(string a)
		{
			for (var i = 0; i < StaticValue.NoteCount; i++)
			{
				if (i == StaticValue.NoteCount - 1)
				{
					pubnote[StaticValue.NoteCount - 1] = a;
				}
				else
				{
					pubnote[i] = pubnote[i + 1];
				}
			}
		}

		public void tray_note_Click(object sender, EventArgs e)
		{
			fmNote.Show();
			fmNote.WindowState = FormWindowState.Normal;
			fmNote.Visible = true;
		}

		public string Google_Hotkey(string text)
		{
			var text2 = "";
			try
			{
				string text3;
				string text4;
				if (contain_ch(trans_hotkey.Trim()))
				{
					text3 = "zh-CN";
					text4 = "en";
				}
				else
				{
					text3 = "en";
					text4 = "zh-CN";
				}
				var url = string.Concat("https://translate.google.cn/translate_a/single?client=gtx&sl=", text3, "&tl=", text4, "&dt=t&q=", HttpUtility.UrlEncode(text).Replace("+", "%20"));
				var jarray = (JArray)JsonConvert.DeserializeObject(Get_GoogletHtml(url));
				var count = ((JArray)jarray[0]).Count;
				for (var i = 0; i < count; i++)
				{
					text2 += jarray[0][i][0].ToString();
				}
			}
			catch (Exception ex)
			{
				text2 = "[Error]:" + ex.Message;
			}
			return text2;
		}

		private string GetTextFromClipboard()
		{
			if (Thread.CurrentThread.GetApartmentState() > ApartmentState.STA)
			{
				var thread = new Thread(delegate()
				{
					SendKeys.SendWait("^c");
					SendKeys.Flush();
				});
				thread.SetApartmentState(ApartmentState.STA);
				thread.Start();
				thread.Join();
			}
			else
			{
				SendKeys.SendWait("^c");
				SendKeys.Flush();
			}
			var text = Clipboard.GetText();
			text = (string.IsNullOrWhiteSpace(text) ? null : text);
			if (text != null)
			{
				Clipboard.Clear();
			}
			return text;
		}

		public static string Get_html(string url)
		{
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						result = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				httpWebRequest.Abort();
			}
			catch
			{
				result = "";
			}
			return result;
		}

		public CookieContainer Post_Html_Getcookie(string url, string post_str)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 5000;
			httpWebRequest.Headers.Add("Accept-Language:zh-CN,zh;q=0.8");
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			httpWebRequest.CookieContainer = new CookieContainer();
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return httpWebRequest.CookieContainer;
		}

		public string Post_Html_Reccookie(string url, string post_str, CookieContainer CookieContainer)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 6000;
			httpWebRequest.Headers.Add("Accept-Language:zh-CN,zh;q=0.8");
			httpWebRequest.Headers.Add("Accept-Encoding: gzip, deflate");
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			httpWebRequest.CookieContainer = CookieContainer;
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		public string Post_Html(string url, string post_str)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 6000;
			httpWebRequest.ContentType = "application/x-www-form-urlencoded";
			httpWebRequest.Headers.Add("Accept-Language: zh-CN,en,*");
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		public void Main_OCR_Quickscreenshots()
		{
			if (!StaticValue.截图排斥)
			{
				try
				{
					change_QQ_screenshot = false;
					FormBorderStyle = FormBorderStyle.None;
					Visible = false;
					Thread.Sleep(100);
					if (transtalate_fla == "开启")
					{
						form_width = Width / 2;
					}
					else
					{
						form_width = Width;
					}
					shupai_Right_txt = "";
					shupai_Left_txt = "";
					form_height = Height;
					minico.Visible = false;
					minico.Visible = true;
					menu.Close();
					menu_copy.Close();
					auto_fla = "开启";
					split_txt = "";
					RichBoxBody.Text = "***该区域未发现文本***";
					RichBoxBody_T.Text = "";
					typeset_txt = "";
					transtalate_fla = "关闭";
					if (IniHelper.GetValue("工具栏", "翻译") == "False")
					{
						Trans_close.PerformClick();
					}
					Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
					FormBorderStyle = FormBorderStyle.Sizable;
					StaticValue.截图排斥 = true;
					string mode_flag;
					Point point;
					Rectangle[] buildRects;
					image_screen = RegionCaptureTasks.GetRegionImage_Mo(new RegionCaptureOptions
					{
						ShowMagnifier = false,
						UseSquareMagnifier = false,
						MagnifierPixelCount = 15,
						MagnifierPixelSize = 10
					}, out mode_flag, out point, out buildRects);
					if (mode_flag == "高级截图")
					{
						var mode = RegionCaptureMode.Annotation;
						var options = new RegionCaptureOptions();
						using (var regionCaptureForm = new RegionCaptureForm(mode, options))
						{
							regionCaptureForm.Image_get = false;
							regionCaptureForm.Prepare(image_screen);
							regionCaptureForm.ShowDialog();
							image_screen = null;
							image_screen = regionCaptureForm.GetResultImage();
							mode_flag = regionCaptureForm.Mode_flag;
						}
					}
					HelpWin32.RegisterHotKey(Handle, 222, HelpWin32.KeyModifiers.None, Keys.Escape);
					if (mode_flag == "贴图")
					{
						var locationPoint = new Point(point.X, point.Y);
						new FmScreenPaste(image_screen, locationPoint).Show();
						if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
						{
							var value = IniHelper.GetValue("快捷键", "翻译文本");
							var text = "None";
							var text2 = "F9";
							SetHotkey(text, text2, value, 205);
						}
						HelpWin32.UnregisterHotKey(Handle, 222);
						StaticValue.截图排斥 = false;
					}
					else if (mode_flag == "区域多选")
					{
						if (image_screen == null)
						{
							if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
							{
								var value2 = IniHelper.GetValue("快捷键", "翻译文本");
								var text3 = "None";
								var text4 = "F9";
								SetHotkey(text3, text4, value2, 205);
							}
							HelpWin32.UnregisterHotKey(Handle, 222);
							StaticValue.截图排斥 = false;
						}
						else
						{
							minico.Visible = true;
							thread = new Thread(ShowLoading);
							thread.Start();
							ts = new TimeSpan(DateTime.Now.Ticks);
							getSubPics_ocr(image_screen, buildRects);
						}
					}
					else if (mode_flag == "取色")
					{
						if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
						{
							var value3 = IniHelper.GetValue("快捷键", "翻译文本");
							var text5 = "None";
							var text6 = "F9";
							SetHotkey(text5, text6, value3, 205);
						}
						HelpWin32.UnregisterHotKey(Handle, 222);
						StaticValue.截图排斥 = false;
						CommonHelper.ShowHelpMsg("已复制颜色");
					}
					else if (image_screen == null)
					{
						if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
						{
							var value4 = IniHelper.GetValue("快捷键", "翻译文本");
							var text7 = "None";
							var text8 = "F9";
							SetHotkey(text7, text8, value4, 205);
						}
						HelpWin32.UnregisterHotKey(Handle, 222);
						StaticValue.截图排斥 = false;
					}
					else
					{
						if (mode_flag == "百度")
						{
							baidu_flags = "百度";
						}
						if (mode_flag == "拆分")
						{
							set_merge = false;
							set_split = true;
						}
						if (mode_flag == "合并")
						{
							set_merge = true;
							set_split = false;
						}
						if (mode_flag == "截图")
						{
							Clipboard.SetImage(image_screen);
							if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
							{
								var value5 = IniHelper.GetValue("快捷键", "翻译文本");
								var text9 = "None";
								var text10 = "F9";
								SetHotkey(text9, text10, value5, 205);
							}
							HelpWin32.UnregisterHotKey(Handle, 222);
							StaticValue.截图排斥 = false;
							if (IniHelper.GetValue("截图音效", "粘贴板") == "True")
							{
								PlaySong(IniHelper.GetValue("截图音效", "音效路径"));
							}
                            CommonHelper.ShowHelpMsg("已复制截图");
                        }
						else if (mode_flag == "自动保存" && IniHelper.GetValue("配置", "自动保存") == "True")
						{
							var filename = IniHelper.GetValue("配置", "截图位置") + "\\" + ReFileName(IniHelper.GetValue("配置", "截图位置"), "图片.Png");
							image_screen.Save(filename, ImageFormat.Png);
							StaticValue.截图排斥 = false;
							if (IniHelper.GetValue("截图音效", "自动保存") == "True")
							{
								PlaySong(IniHelper.GetValue("截图音效", "音效路径"));
							}
                            CommonHelper.ShowHelpMsg("已保存图片");
                        }
						else if (mode_flag == "多区域自动保存" && IniHelper.GetValue("配置", "自动保存") == "True")
						{
							getSubPics(image_screen, buildRects);
							StaticValue.截图排斥 = false;
							if (IniHelper.GetValue("截图音效", "自动保存") == "True")
							{
								PlaySong(IniHelper.GetValue("截图音效", "音效路径"));
							}
                            CommonHelper.ShowHelpMsg("已保存图片");
						}
						else if (mode_flag == "保存")
						{
							var saveFileDialog = new SaveFileDialog();
							saveFileDialog.Filter = "png图片(*.png)|*.png|jpg图片(*.jpg)|*.jpg|bmp图片(*.bmp)|*.bmp";
							saveFileDialog.AddExtension = false;
							saveFileDialog.FileName = string.Concat("tianruo_", DateTime.Now.Year.ToString(), "-", DateTime.Now.Month.ToString(), "-", DateTime.Now.Day.ToString(), "-", DateTime.Now.Ticks.ToString());
							saveFileDialog.Title = "保存图片";
							saveFileDialog.FilterIndex = 1;
							saveFileDialog.RestoreDirectory = true;
							if (saveFileDialog.ShowDialog() == DialogResult.OK)
							{
								var extension = Path.GetExtension(saveFileDialog.FileName);
								if (extension.Equals(".jpg"))
								{
									image_screen.Save(saveFileDialog.FileName, ImageFormat.Jpeg);
								}
								if (extension.Equals(".png"))
								{
									image_screen.Save(saveFileDialog.FileName, ImageFormat.Png);
								}
								if (extension.Equals(".bmp"))
								{
									image_screen.Save(saveFileDialog.FileName, ImageFormat.Bmp);
								}
							}
							if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
							{
								var value6 = IniHelper.GetValue("快捷键", "翻译文本");
								var text11 = "None";
								var text12 = "F9";
								SetHotkey(text11, text12, value6, 205);
							}
							HelpWin32.UnregisterHotKey(Handle, 222);
							StaticValue.截图排斥 = false;
						}
						else if (image_screen != null)
						{
							if (IniHelper.GetValue("工具栏", "分栏") == "True")
							{
								minico.Visible = true;
								thread = new Thread(ShowLoading);
								thread.Start();
								ts = new TimeSpan(DateTime.Now.Ticks);
								var image = image_screen;
								var graphics = Graphics.FromImage(new Bitmap(image.Width, image.Height));
								graphics.DrawImage(image, 0, 0, image.Width, image.Height);
								graphics.Save();
								graphics.Dispose();
								((Bitmap)FindBundingBox_fences((Bitmap)image)).Save("Data\\分栏预览图.jpg");
								image.Dispose();
								image_screen.Dispose();
							}
							else
							{
								minico.Visible = true;
								thread = new Thread(ShowLoading);
								thread.Start();
								ts = new TimeSpan(DateTime.Now.Ticks);
								var messageload = new Messageload();
								messageload.ShowDialog();
								if (messageload.DialogResult == DialogResult.OK)
								{
									esc_thread = new Thread(Main_OCR_Thread);
									esc_thread.Start();
								}
							}
						}
					}
				}
				catch
				{
					StaticValue.截图排斥 = false;
				}
			}
		}

		public void Main_OCR_Thread()
		{
			if (ScanQRCode() != "")
			{
				typeset_txt = ScanQRCode();
				RichBoxBody.Text = typeset_txt;
				fmloading.FmlClose = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "搜狗")
			{
				OCR_sougou2();
				fmloading.FmlClose = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "腾讯")
			{
				OCR_Tencent();
				fmloading.FmlClose = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "有道")
			{
				OCR_youdao();
				fmloading.FmlClose = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "公式")
			{
				OCR_Math();
				fmloading.FmlClose = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
				return;
			}
			if (interface_flag == "百度表格")
			{
				BdTableOCR();
				fmloading.FmlClose = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_table));
				return;
			}
			if (interface_flag == "阿里表格")
			{
				OCR_ali_table();
				fmloading.FmlClose = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_table));
				return;
			}
			if (interface_flag == "日语" || interface_flag == "中英" || interface_flag == "韩语")
			{
				OCR_baidu();
				fmloading.FmlClose = "窗体已关闭";
				Invoke(new ocr_thread(Main_OCR_Thread_last));
			}
			if (interface_flag == "从左向右" || interface_flag == "从右向左")
			{
				shupai_Right_txt = "";
				var image = image_screen;
				var bitmap = new Bitmap(image.Width, image.Height);
				var graphics = Graphics.FromImage(bitmap);
				graphics.DrawImage(image, 0, 0, image.Width, image.Height);
				graphics.Save();
				graphics.Dispose();
				image_ori = bitmap;
				var image2 = new Image<Gray, byte>(bitmap);
				var image3 = new Image<Gray, byte>((Bitmap)FindBundingBox(image2.ToBitmap()));
				var draw = image3.Convert<Bgr, byte>();
				var image4 = image3.Clone();
				CvInvoke.Canny(image3, image4, 0.0, 0.0, 5, true);
				select_image(image4, draw);
				bitmap.Dispose();
				image2.Dispose();
				image3.Dispose();
			}
			image_screen.Dispose();
			GC.Collect();
		}

		public void Main_OCR_Thread_last()
		{
			image_screen.Dispose();
			StaticValue.截图排斥 = false;
			var text = typeset_txt;
			text = check_str(text);
			split_txt = check_str(split_txt);
			if (!punctuation_has_punctuation(text))
			{
				text = split_txt;
			}
			if (contain_ch(text.Trim()))
			{
				text = Del_Space(text);
			}
			if (text != "")
			{
				RichBoxBody.Text = text;
			}
			StaticValue.v_Split = split_txt;
			if (bool.Parse(IniHelper.GetValue("工具栏", "拆分")) || set_split)
			{
				set_split = false;
				RichBoxBody.Text = split_txt;
			}
			if (bool.Parse(IniHelper.GetValue("工具栏", "合并")) || set_merge)
			{
				set_merge = false;
				RichBoxBody.Text = text.Replace("\n", "").Replace("\r", "");
			}
			var timeSpan = new TimeSpan(DateTime.Now.Ticks);
			var timeSpan2 = timeSpan.Subtract(ts).Duration();
			var str = string.Concat(new[]
			{
				timeSpan2.Seconds.ToString(),
				".",
				Convert.ToInt32(timeSpan2.TotalMilliseconds).ToString(),
				"秒"
			});
			if (RichBoxBody.Text != null)
			{
				p_note(RichBoxBody.Text);
				StaticValue.v_note = pubnote;
				if (fmNote.Created)
				{
					fmNote.TextNote = "";
				}
			}
			if (StaticValue.v_topmost)
			{
				TopMost = true;
			}
			else
			{
				TopMost = false;
			}
			Text = "耗时：" + str;
			minico.Visible = true;
			if (interface_flag == "从右向左")
			{
				RichBoxBody.Text = shupai_Right_txt;
			}
			if (interface_flag == "从左向右")
			{
				RichBoxBody.Text = shupai_Left_txt;
			}
			Clipboard.SetDataObject(RichBoxBody.Text);
			if (baidu_flags == "百度")
			{
				FormBorderStyle = FormBorderStyle.Sizable;
				Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
				Visible = false;
				WindowState = FormWindowState.Minimized;
				base.Show();
				Process.Start("https://www.baidu.com/s?wd=" + RichBoxBody.Text);
				baidu_flags = "";
				if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
				{
					var value = IniHelper.GetValue("快捷键", "翻译文本");
					var text2 = "None";
					var text3 = "F9";
					SetHotkey(text2, text3, value, 205);
				}
				HelpWin32.UnregisterHotKey(Handle, 222);
				return;
			}
			if (IniHelper.GetValue("配置", "识别弹窗") == "False")
			{
				FormBorderStyle = FormBorderStyle.Sizable;
				Size = new Size((int)font_base.Width * 23, (int)font_base.Height * 24);
				Visible = false;
                CommonHelper.ShowHelpMsg(RichBoxBody.Text == "***该区域未发现文本***" ? "无文本" : "已识别");
                if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
				{
					var value2 = IniHelper.GetValue("快捷键", "翻译文本");
					var text4 = "None";
					var text5 = "F9";
					SetHotkey(text4, text5, value2, 205);
				}
				HelpWin32.UnregisterHotKey(Handle, 222);
				return;
			}
			FormBorderStyle = FormBorderStyle.Sizable;
			Visible = true;
			base.Show();
			WindowState = FormWindowState.Normal;
			Size = new Size(form_width, form_height);
			HelpWin32.SetForegroundWindow(Handle);
			StaticValue.v_googleTranslate_txt = RichBoxBody.Text;
			if (bool.Parse(IniHelper.GetValue("工具栏", "翻译")))
			{
				try
				{
					auto_fla = "";
					Invoke(new translate(transtalate_Click));
				}
				catch
				{
				}
			}
			if (bool.Parse(IniHelper.GetValue("工具栏", "检查")))
			{
				try
				{
					RichBoxBody.Find = "";
				}
				catch
				{
				}
			}
			if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
			{
				var value3 = IniHelper.GetValue("快捷键", "翻译文本");
				var text6 = "None";
				var text7 = "F9";
				SetHotkey(text6, text7, value3, 205);
			}
			HelpWin32.UnregisterHotKey(Handle, 222);
			RichBoxBody.Refresh();
		}

		private void OCR_baidu_Ch_and_En_Click(object sender, EventArgs e)
		{
			OCR_foreach("中英");
		}

		private void OCR_baidu_Jap_Click(object sender, EventArgs e)
		{
			OCR_foreach("日语");
		}

		private void OCR_baidu_Kor_Click(object sender, EventArgs e)
		{
			OCR_foreach("韩语");
		}

		public string Get_GoogletHtml(string url)
		{
			var text = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "GET";
			httpWebRequest.Timeout = 5000;
			httpWebRequest.Headers.Add("Accept-Language: zh-CN;q=0.8,en-US;q=0.6,en;q=0.4");
			httpWebRequest.Headers.Add("Accept-Encoding: gzip,deflate");
			httpWebRequest.Headers.Add("Accept-Charset: utf-8");
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
			httpWebRequest.Host = "translate.google.cn";
			httpWebRequest.Accept = "*/*";
			httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
			string result;
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public void OCR_baidu()
		{
			split_txt = "";
			try
			{
				baidu_vip = Get_html(string.Format("{0}?{1}", "https://aip.baidubce.com/oauth/2.0/token", "grant_type=client_credentials&client_id=" + StaticValue.BD_API_ID + "&client_secret=" + StaticValue.BD_API_KEY));
				if (baidu_vip == "")
				{
					MessageBox.Show("请检查密钥输入是否正确！", "提醒");
				}
				else
				{
					var str = "CHN_ENG";
					split_txt = "";
					var img = image_screen;
					var inArray = OCR_ImgToByte(img);
					if (interface_flag == "中英")
					{
						str = "CHN_ENG";
					}
					if (interface_flag == "日语")
					{
						str = "JAP";
					}
					if (interface_flag == "韩语")
					{
						str = "KOR";
					}
					var s = "image=" + HttpUtility.UrlEncode(Convert.ToBase64String(inArray)) + "&language_type=" + str;
					var bytes = Encoding.UTF8.GetBytes(s);
					var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic?access_token=" + ((JObject)JsonConvert.DeserializeObject(baidu_vip))["access_token"]);
					httpWebRequest.Method = "POST";
					httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
					httpWebRequest.Timeout = 8000;
					httpWebRequest.ReadWriteTimeout = 5000;
					using (var requestStream = httpWebRequest.GetRequestStream())
					{
						requestStream.Write(bytes, 0, bytes.Length);
					}
					var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
					var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
					responseStream.Close();
					var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["words_result"].ToString());
					checked_txt(jarray, 1, "words");
				}
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本或者密钥次数用尽***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public string check_str(string text)
		{
			if (contain_ch(text.Trim()))
			{
				text = CommonHelper.EnPunctuation2Ch(text.Trim());
				text = check_ch_en(text.Trim());
			}
			else
			{
				text = punctuation_ch_en(text.Trim());
				if (contain(text, ".") && (contain(text, ",") || contain(text, "!") || contain(text, "(") || contain(text, ")") || contain(text, "'")))
				{
					text = punctuation_Del_space(text);
				}
			}
			return text;
		}

		public static string punctuation_en_ch_x(string text)
		{
			var array = text.ToCharArray();
			for (var i = 0; i < array.Length; i++)
			{
				var num = ".:;,?![]()".IndexOf(array[i]);
				if (num != -1)
				{
					array[i] = "。：；，？！【】（）"[num];
				}
			}
			return new string(array);
		}

		public string OCR_sougou_SogouPost(string url, CookieContainer cookie, byte[] content)
		{
			var text = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.CookieContainer = cookie;
			httpWebRequest.Timeout = 10000;
			httpWebRequest.Referer = "http://pic.sogou.com/resource/pic/shitu_intro/index.html";
			httpWebRequest.ContentType = "multipart/form-data; boundary=----WebKitFormBoundary1ZZDB9E4sro7pf0g";
			httpWebRequest.Accept = "*/*";
			httpWebRequest.Headers.Add("Origin: http://pic.sogou.com");
			httpWebRequest.Headers.Add("Accept-Encoding: gzip,deflate");
			httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
			httpWebRequest.ServicePoint.Expect100Continue = false;
			httpWebRequest.ProtocolVersion = new Version(1, 1);
			httpWebRequest.ContentLength = content.Length;
			var requestStream = httpWebRequest.GetRequestStream();
			requestStream.Write(content, 0, content.Length);
			requestStream.Close();
			string result;
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					var stream = httpWebResponse.GetResponseStream();
					if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
					{
						stream = new GZipStream(stream, CompressionMode.Decompress);
					}
					using (var streamReader = new StreamReader(stream, Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public string OCR_sougou_SogouGet(string url, CookieContainer cookie, string refer)
		{
			var text = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "GET";
			httpWebRequest.CookieContainer = cookie;
			httpWebRequest.Referer = refer;
			httpWebRequest.Timeout = 10000;
			httpWebRequest.Accept = "application/json";
			httpWebRequest.Headers.Add("X-Requested-With: XMLHttpRequest");
			httpWebRequest.Headers.Add("Accept-Encoding: gzip,deflate");
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)";
			httpWebRequest.ServicePoint.Expect100Continue = false;
			httpWebRequest.ProtocolVersion = new Version(1, 1);
			string result;
			try
			{
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					var stream = httpWebResponse.GetResponseStream();
					if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
					{
						stream = new GZipStream(stream, CompressionMode.Decompress);
					}
					using (var streamReader = new StreamReader(stream, Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public string OCR_sougou_SogouOCR(Image img)
		{
			var cookie = new CookieContainer();
			var url = "http://pic.sogou.com/pic/upload_pic.jsp";
			var str = OCR_sougou_SogouPost(url, cookie, OCR_sougou_Content_Length(img));
			var url2 = "http://pic.sogou.com/pic/ocr/ocrOnline.jsp?query=" + str;
			var refer = "http://pic.sogou.com/resource/pic/shitu_intro/word_1.html?keyword=" + str;
			return OCR_sougou_SogouGet(url2, cookie, refer);
		}

		private byte[] OCR_ImgToByte(Image img)
		{
			byte[] result;
			try
			{
				var memoryStream = new MemoryStream();
				img.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				result = array;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public byte[] OCR_sougou_Content_Length(Image img)
		{
			var bytes = Encoding.UTF8.GetBytes("------WebKitFormBoundary1ZZDB9E4sro7pf0g\r\nContent-Disposition: form-data; name=\"pic_path\"; filename=\"test2018.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n");
			var array = OCR_ImgToByte(img);
			var bytes2 = Encoding.UTF8.GetBytes("\r\n------WebKitFormBoundary1ZZDB9E4sro7pf0g--\r\n");
			var array2 = new byte[bytes.Length + array.Length + bytes2.Length];
			bytes.CopyTo(array2, 0);
			array.CopyTo(array2, bytes.Length);
			bytes2.CopyTo(array2, bytes.Length + array.Length);
			return array2;
		}

		public void OCR_sougou2()
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundary8orYTmcj8BHvQpVU";
				Image image = ZoomImage((Bitmap)image_screen, 120, 120);
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"pic\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ocr.shouji.sogou.com/v2/ocr/json");
				httpWebRequest.Timeout = 8000;
				httpWebRequest.Method = "POST";
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(array, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["result"].ToString());
				if (IniHelper.GetValue("工具栏", "分段") == "True")
				{
					checked_location_sougou(jarray, 2, "content", "frame");
				}
				else
				{
					checked_txt(jarray, 2, "content");
				}
				image.Dispose();
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public static byte[] Mergebyte(byte[] a, byte[] b, byte[] c)
		{
			var array = new byte[a.Length + b.Length + c.Length];
			a.CopyTo(array, 0);
			b.CopyTo(array, a.Length);
			c.CopyTo(array, a.Length + b.Length);
			return array;
		}

		public static bool contain_punctuation(string str)
		{
			return Regex.IsMatch(str, "\\p{P}");
		}

		private void tray_help_Click(object sender, EventArgs e)
		{
			WindowState = FormWindowState.Minimized;
			new FmHelp().Show();
		}

		public bool Is_punctuation(string text)
		{
			return ",;:，（）、；".IndexOf(text) != -1;
		}

		public bool has_punctuation(string text)
		{
			return ",;，；、<>《》()-（）".IndexOf(text) != -1;
		}

		public void checked_txt(JArray jarray, int lastlength, string words)
		{
			var num = 0;
			for (var i = 0; i < jarray.Count; i++)
			{
				var length = JObject.Parse(jarray[i].ToString())[words].ToString().Length;
				if (length > num)
				{
					num = length;
				}
			}
			var str = "";
			var text = "";
			for (var j = 0; j < jarray.Count - 1; j++)
			{
				var jobject = JObject.Parse(jarray[j].ToString());
				var array = jobject[words].ToString().ToCharArray();
				var jobject2 = JObject.Parse(jarray[j + 1].ToString());
				var array2 = jobject2[words].ToString().ToCharArray();
				var length2 = jobject[words].ToString().Length;
				var length3 = jobject2[words].ToString().Length;
				if (Math.Abs(length2 - length3) <= 0)
				{
					if (split_paragraph(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else
					{
						text += jobject[words].ToString().Trim();
					}
				}
				else if (split_paragraph(array[array.Length - lastlength].ToString()) && Math.Abs(length2 - length3) <= 1)
				{
					if (split_paragraph(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else
					{
						text += jobject[words].ToString().Trim();
					}
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && length2 <= num / 2)
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()) && length3 - length2 < 4 && array2[1].ToString() == ".")
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_en(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text = text + jobject[words].ToString().Trim() + " ";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_en(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (Is_punctuation(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (Is_punctuation(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text = text + jobject[words].ToString().Trim() + " ";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (IsNum(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (IsNum(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				if (has_punctuation(jobject[words].ToString()))
				{
					text += "\r\n";
				}
				str = str + jobject[words].ToString().Trim() + "\r\n";
			}
			split_txt = str + JObject.Parse(jarray[jarray.Count - 1].ToString())[words];
			typeset_txt = text.Replace("\r\n\r\n", "\r\n") + JObject.Parse(jarray[jarray.Count - 1].ToString())[words];
		}

		private void OCR_foreach(string name)
		{
			var filePath = AppDomain.CurrentDomain.BaseDirectory + "Data\\config.ini";
			if (name == "韩语")
			{
				interface_flag = "韩语";
				Refresh();
				baidu.Text = "百度√";
				kor.Text = "韩语√";
			}
			if (name == "日语")
			{
				interface_flag = "日语";
				Refresh();
				baidu.Text = "百度√";
				jap.Text = "日语√";
			}
			if (name == "中英")
			{
				interface_flag = "中英";
				Refresh();
				baidu.Text = "百度√";
				ch_en.Text = "中英√";
			}
			if (name == "搜狗")
			{
				interface_flag = "搜狗";
				Refresh();
				sougou.Text = "搜狗√";
			}
			if (name == "腾讯")
			{
				interface_flag = "腾讯";
				Refresh();
				tencent.Text = "腾讯√";
			}
			if (name == "有道")
			{
				interface_flag = "有道";
				Refresh();
				youdao.Text = "有道√";
			}
			if (name == "公式")
			{
				interface_flag = "公式";
				Refresh();
				Mathfuntion.Text = "公式√";
			}
			if (name == "百度表格")
			{
				interface_flag = "百度表格";
				Refresh();
				ocr_table.Text = "表格√";
				baidu_table.Text = "百度√";
			}
			if (name == "阿里表格")
			{
				interface_flag = "阿里表格";
				Refresh();
				ocr_table.Text = "表格√";
				ali_table.Text = "阿里√";
			}
			if (name == "从左向右")
			{
				if (!File.Exists("cvextern.dll"))
				{
					MessageBox.Show("请从蓝奏网盘中下载cvextern.dll大小约25m，点击确定自动弹出网页。\r\n将下载后的文件与 天若OCR文字识别.exe 这个文件放在一起。");
					Process.Start("https://www.lanzous.com/i1ab3vg");
				}
				else
				{
					interface_flag = "从左向右";
					Refresh();
					shupai.Text = "竖排√";
					left_right.Text = "从左向右√";
				}
			}
			if (name == "从右向左")
			{
				if (!File.Exists("cvextern.dll"))
				{
					MessageBox.Show("请从蓝奏网盘中下载cvextern.dll大小约25m，点击确定自动弹出网页。\r\n将下载后的文件与 天若OCR文字识别.exe 这个文件放在一起。");
					Process.Start("https://www.lanzous.com/i1ab3vg");
					return;
				}
				interface_flag = "从右向左";
				Refresh();
				shupai.Text = "竖排√";
				righ_left.Text = "从右向左√";
			}
			HelpWin32.IniFileHelper.SetValue("配置", "接口", interface_flag, filePath);
		}

		private void OCR_shupai_Click(object sender, EventArgs e)
		{
		}

		private void OCR_write_Click(object sender, EventArgs e)
		{
			OCR_foreach("手写");
		}

		private void OCR_lefttoright_Click(object sender, EventArgs e)
		{
			OCR_foreach("从左向右");
		}

		private void OCR_righttoleft_Click(object sender, EventArgs e)
		{
			OCR_foreach("从右向左");
		}

		public void OCR_baidu_acc()
		{
			split_txt = "";
			var text = "";
			try
			{
				baidu_vip = Get_html(string.Format("{0}?{1}", "https://aip.baidubce.com/oauth/2.0/token", "grant_type=client_credentials&client_id=" + StaticValue.BD_API_ID + "&client_secret=" + StaticValue.BD_API_KEY));
				if (baidu_vip == "")
				{
					MessageBox.Show("请检查密钥输入是否正确！", "提醒");
				}
				else
				{
					split_txt = "";
					var img = image_screen;
					var inArray = OCR_ImgToByte(img);
					var s = "image=" + HttpUtility.UrlEncode(Convert.ToBase64String(inArray));
					var bytes = Encoding.UTF8.GetBytes(s);
					var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic?access_token=" + ((JObject)JsonConvert.DeserializeObject(baidu_vip))["access_token"]);
					httpWebRequest.Method = "POST";
					httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
					httpWebRequest.Timeout = 8000;
					httpWebRequest.ReadWriteTimeout = 5000;
					ServicePointManager.DefaultConnectionLimit = 512;
					using (var requestStream = httpWebRequest.GetRequestStream())
					{
						requestStream.Write(bytes, 0, bytes.Length);
					}
					var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
					var value = text = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
					responseStream.Close();
					var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["words_result"].ToString());
					var text2 = "";
					for (var i = 0; i < jarray.Count; i++)
					{
						var jobject = JObject.Parse(jarray[i].ToString());
						text2 += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					}
					shupai_Right_txt = shupai_Right_txt + text2 + "\r\n";
					Thread.Sleep(600);
				}
			}
			catch
			{
				MessageBox.Show(text, "提醒");
				StaticValue.截图排斥 = false;
				esc = "退出";
				fmloading.FmlClose = "窗体已关闭";
				esc_thread.Abort();
			}
		}

		public void OCR_Tencent_handwriting()
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundaryRDEqU0w702X9cWPJ";
				var image = image_screen;
				if (image.Width > 90 && image.Height < 90)
				{
					var bitmap = new Bitmap(image.Width, 300);
					var graphics = Graphics.FromImage(bitmap);
					graphics.DrawImage(image, 5, 0, image.Width, image.Height);
					graphics.Save();
					graphics.Dispose();
					image = new Bitmap(bitmap);
				}
				else if (image.Width <= 90 && image.Height >= 90)
				{
					var bitmap2 = new Bitmap(300, image.Height);
					var graphics2 = Graphics.FromImage(bitmap2);
					graphics2.DrawImage(image, 0, 5, image.Width, image.Height);
					graphics2.Save();
					graphics2.Dispose();
					image = new Bitmap(bitmap2);
				}
				else if (image.Width < 90 && image.Height < 90)
				{
					var bitmap3 = new Bitmap(300, 300);
					var graphics3 = Graphics.FromImage(bitmap3);
					graphics3.DrawImage(image, 5, 5, image.Width, image.Height);
					graphics3.Save();
					graphics3.Dispose();
					image = new Bitmap(bitmap3);
				}
				else
				{
					image = image_screen;
				}
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"image_file\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://ai.qq.com/cgi-bin/appdemo_handwritingocr");
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = "http://ai.qq.com/product/ocr.shtml";
				httpWebRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				httpWebRequest.Timeout = 8000;
				httpWebRequest.ReadWriteTimeout = 2000;
				var buffer = array;
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(buffer, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["item_list"].ToString());
				checked_txt(jarray, 1, "itemstring");
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public Image BoundingBox(Image<Gray, byte> src, Image<Bgr, byte> draw)
		{
			Image result;
			using (var vectorOfVectorOfPoint = new VectorOfVectorOfPoint())
			{
				CvInvoke.FindContours(src, vectorOfVectorOfPoint, null, RetrType.List, ChainApproxMethod.ChainApproxSimple, default(Point));
				Image image = draw.ToBitmap();
				var graphics = Graphics.FromImage(image);
				var size = vectorOfVectorOfPoint.Size;
				for (var i = 0; i < size; i++)
				{
					using (var vectorOfPoint = vectorOfVectorOfPoint[i])
					{
						var rectangle = CvInvoke.BoundingRectangle(vectorOfPoint);
						var x = rectangle.Location.X;
						var y = rectangle.Location.Y;
						var width = rectangle.Size.Width;
						var height = rectangle.Size.Height;
						if (width > 5 || height > 5)
						{
							graphics.FillRectangle(Brushes.White, x, 0, width, image.Size.Height);
						}
					}
				}
				graphics.Dispose();
				var bitmap = new Bitmap(image.Width + 2, image.Height + 2);
				var graphics2 = Graphics.FromImage(bitmap);
				graphics2.DrawImage(image, 1, 1, image.Width, image.Height);
				graphics2.Save();
				graphics2.Dispose();
				result = bitmap;
			}
			return result;
		}

		public void select_image(Image<Gray, byte> src, Image<Bgr, byte> draw)
		{
			try
			{
				using (var vectorOfVectorOfPoint = new VectorOfVectorOfPoint())
				{
					CvInvoke.FindContours(src, vectorOfVectorOfPoint, null, RetrType.List, ChainApproxMethod.ChainApproxSimple, default(Point));
					var num = vectorOfVectorOfPoint.Size / 2;
					imagelist_lenght = num;
					bool_image_count(num);
					if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Data\\image_temp"))
					{
						Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Data\\image_temp");
					}
					OCR_baidu_a = "";
					OCR_baidu_b = "";
					OCR_baidu_c = "";
					OCR_baidu_d = "";
					OCR_baidu_e = "";
					for (var i = 0; i < num; i++)
					{
						using (var vectorOfPoint = vectorOfVectorOfPoint[i])
						{
							var rectangle = CvInvoke.BoundingRectangle(vectorOfPoint);
							if (rectangle.Size.Width > 1 && rectangle.Size.Height > 1)
							{
								var x = rectangle.Location.X;
								var y = rectangle.Location.Y;
								var width = rectangle.Size.Width;
								var height = rectangle.Size.Height;
								new Point(x, 0);
								new Point(x, image_ori.Size.Height);
								var srcRect = new Rectangle(x, 0, width, image_ori.Size.Height);
								var bitmap = new Bitmap(width + 70, srcRect.Size.Height);
								var graphics = Graphics.FromImage(bitmap);
								graphics.FillRectangle(Brushes.White, 0, 0, bitmap.Size.Width, bitmap.Size.Height);
								graphics.DrawImage(image_ori, 30, 0, srcRect, GraphicsUnit.Pixel);
								var bitmap2 = Image.FromHbitmap(bitmap.GetHbitmap());
								bitmap2.Save("Data\\image_temp\\" + i + ".jpg", ImageFormat.Jpeg);
								bitmap2.Dispose();
								bitmap.Dispose();
								graphics.Dispose();
							}
						}
					}
					var messageload = new Messageload();
					messageload.ShowDialog();
					if (messageload.DialogResult == DialogResult.OK)
					{
						var array = new[]
						{
							new ManualResetEvent(false)
						};
						ThreadPool.QueueUserWorkItem(DoWork, array[0]);
					}
				}
			}
			catch
			{
				exit_thread();
			}
		}

		public Image FindBundingBox(Bitmap bitmap)
		{
			var image = new Image<Bgr, byte>(bitmap);
			var image2 = new Image<Gray, byte>(image.Width, image.Height);
			CvInvoke.CvtColor(image, image2, ColorConversion.Bgra2Gray, 0);
			var structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(4, 4), new Point(1, 1));
			CvInvoke.Erode(image2, image2, structuringElement, new Point(0, 2), 1, BorderType.Reflect101, default(MCvScalar));
			CvInvoke.Threshold(image2, image2, 100.0, 255.0, (ThresholdType)9);
			var image3 = new Image<Gray, byte>(image2.ToBitmap());
			var draw = image3.Convert<Bgr, byte>();
			var image4 = image3.Clone();
			CvInvoke.Canny(image3, image4, 255.0, 255.0, 5, true);
			return BoundingBox(image4, draw);
		}

		public void Captureimage(int width, Image g_image, string saveFilePath, Rectangle rect)
		{
			var bitmap = new Bitmap(width + 70, g_image.Size.Height);
			var graphics = Graphics.FromImage(bitmap);
			graphics.FillRectangle(Brushes.White, 0, 0, bitmap.Size.Width, bitmap.Size.Height);
			graphics.DrawImage(g_image, 30, 0, rect, GraphicsUnit.Pixel);
			var bitmap2 = Image.FromHbitmap(bitmap.GetHbitmap());
			bitmap2.Save(saveFilePath, ImageFormat.Jpeg);
			image_screen = bitmap2;
			OCR_baidu_use();
			bitmap2.Dispose();
			bitmap.Dispose();
			graphics.Dispose();
		}

		public void OCR_baidu_use()
		{
			split_txt = "";
			try
			{
				var str = "CHN_ENG";
				split_txt = "";
				var image = image_screen;
				var memoryStream = new MemoryStream();
				image.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var text2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					text2 += array2[j];
				}
				shupai_Right_txt = (shupai_Right_txt + text + "\r\n").Replace("\r\n\r\n", "");
				shupai_Left_txt = text2.Replace("\r\n\r\n", "");
				MessageBox.Show(shupai_Left_txt);
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_sougou_use()
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundary8orYTmcj8BHvQpVU";
				var image = image_screen;
				var i = image.Width;
				var j = image.Height;
				if (i < 300)
				{
					while (i < 300)
					{
						j *= 2;
						i *= 2;
					}
				}
				if (j < 120)
				{
					while (j < 120)
					{
						j *= 2;
						i *= 2;
					}
				}
				var bitmap = new Bitmap(i, j);
				var graphics = Graphics.FromImage(bitmap);
				graphics.DrawImage(image, 0, 0, i, j);
				graphics.Save();
				graphics.Dispose();
				image = new Bitmap(bitmap);
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"pic\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ocr.shouji.sogou.com/v2/ocr/json");
				httpWebRequest.Timeout = 8000;
				httpWebRequest.Method = "POST";
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(array, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["result"].ToString());
				var text2 = "";
				for (var k = 0; k < jarray.Count; k++)
				{
					var jobject = JObject.Parse(jarray[k].ToString());
					text2 += jobject["content"].ToString().Replace("\r", "").Replace("\n", "");
				}
				shupai_Right_txt = shupai_Right_txt + text2 + "\r\n";
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public bool split_paragraph(string text)
		{
			return "。？！?!：".IndexOf(text) != -1;
		}

		public void baidu_image_a(object objEvent)
		{
			try
			{
				for (var i = 0; i < image_num[0]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_A(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		public void baidu_image_b(object objEvent)
		{
			try
			{
				for (var i = image_num[0]; i < image_num[1]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_B(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		private void DoWork(object state)
		{
			var array = new ManualResetEvent[5];
			array[0] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_a, array[0]);
			array[1] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_b, array[1]);
			array[2] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_c, array[2]);
			array[3] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_d, array[3]);
			array[4] = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(baidu_image_e, array[4]);
			WaitHandle[] waitHandles = array;
			WaitHandle.WaitAll(waitHandles);
			shupai_Right_txt = string.Concat(OCR_baidu_a, OCR_baidu_b, OCR_baidu_c, OCR_baidu_d, OCR_baidu_e).Replace("\r\n\r\n", "");
			var text = shupai_Right_txt.TrimEnd('\n').TrimEnd('\r').TrimEnd('\n');
			if (text.Split(Environment.NewLine.ToCharArray()).Length > 1)
			{
				var array2 = text.Split(new[]
				{
					"\r\n"
				}, StringSplitOptions.None);
				var str = "";
				for (var i = 0; i < array2.Length; i++)
				{
					str = str + array2[array2.Length - i - 1].Replace("\r", "").Replace("\n", "") + "\r\n";
				}
				shupai_Left_txt = str;
			}
			fmloading.FmlClose = "窗体已关闭";
			Invoke(new ocr_thread(Main_OCR_Thread_last));
			try
			{
				DeleteFile("Data\\image_temp");
			}
			catch
			{
				exit_thread();
			}
			image_ori.Dispose();
		}

		public void OCR_baidu_use_B(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_b = (OCR_baidu_b + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_baidu_use_A(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_a = (OCR_baidu_a + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void DeleteFile(string path)
		{
			if (File.GetAttributes(path) == FileAttributes.Directory)
			{
				Directory.Delete(path, true);
				return;
			}
			File.Delete(path);
		}

		public void OCR_baidu_image(Image imagearr, string str_image)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				str_image = (str_image + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_baidu_use_E(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_e = (OCR_baidu_e + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_baidu_use_D(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_d = (OCR_baidu_d + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void OCR_baidu_use_C(Image imagearr)
		{
			try
			{
				var str = "CHN_ENG";
				var memoryStream = new MemoryStream();
				imagearr.Save(memoryStream, ImageFormat.Jpeg);
				var array = new byte[memoryStream.Length];
				memoryStream.Position = 0L;
				memoryStream.Read(array, 0, (int)memoryStream.Length);
				memoryStream.Close();
				var s = "type=general_location&image=data" + HttpUtility.UrlEncode(":image/jpeg;base64," + Convert.ToBase64String(array)) + "&language_type=" + str;
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/tech/ocr/general");
				httpWebRequest.CookieContainer = new CookieContainer();
				httpWebRequest.GetResponse().Close();
				var httpWebRequest2 = (HttpWebRequest)WebRequest.Create("http://ai.baidu.com/aidemo");
				httpWebRequest2.Method = "POST";
				httpWebRequest2.Referer = "http://ai.baidu.com/tech/ocr/general";
				httpWebRequest2.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest2.Timeout = 8000;
				httpWebRequest2.ReadWriteTimeout = 5000;
				httpWebRequest2.Headers.Add("Cookie:" + CookieCollectionToStrCookie(((HttpWebResponse)httpWebRequest.GetResponse()).Cookies));
				using (var requestStream = httpWebRequest2.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest2.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["data"]["words_result"].ToString());
				var text = "";
				var array2 = new string[jarray.Count];
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
					array2[jarray.Count - 1 - i] = jobject["words"].ToString().Replace("\r", "").Replace("\n", "");
				}
				var str2 = "";
				for (var j = 0; j < array2.Length; j++)
				{
					str2 += array2[j];
				}
				OCR_baidu_c = (OCR_baidu_c + text + "\r\n").Replace("\r\n\r\n", "");
				Thread.Sleep(10);
			}
			catch
			{
			}
		}

		public void baidu_image_c(object objEvent)
		{
			try
			{
				for (var i = image_num[1]; i < image_num[2]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_C(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		public void baidu_image_d(object objEvent)
		{
			try
			{
				for (var i = image_num[2]; i < image_num[3]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_D(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		public void baidu_image_e(object objEvent)
		{
			try
			{
				for (var i = image_num[3]; i < image_num[4]; i++)
				{
					Stream stream = File.Open("Data\\image_temp\\" + i + ".jpg", FileMode.Open);
					OCR_baidu_use_E(Image.FromStream(stream));
					stream.Close();
				}
				((ManualResetEvent)objEvent).Set();
			}
			catch
			{
				exit_thread();
			}
		}

		public void bool_image_count(int num)
		{
			if (num >= 5)
			{
				image_num = new int[num];
				if (num - num / 5 * 5 == 0)
				{
					image_num[0] = num / 5;
					image_num[1] = num / 5 * 2;
					image_num[2] = num / 5 * 3;
					image_num[3] = num / 5 * 4;
					image_num[4] = num;
				}
				if (num - num / 5 * 5 == 1)
				{
					image_num[0] = num / 5 + 1;
					image_num[1] = num / 5 * 2;
					image_num[2] = num / 5 * 3;
					image_num[3] = num / 5 * 4;
					image_num[4] = num;
				}
				if (num - num / 5 * 5 == 2)
				{
					image_num[0] = num / 5 + 1;
					image_num[1] = num / 5 * 2 + 1;
					image_num[2] = num / 5 * 3;
					image_num[3] = num / 5 * 4;
					image_num[4] = num;
				}
				if (num - num / 5 * 5 == 3)
				{
					image_num[0] = num / 5 + 1;
					image_num[1] = num / 5 * 2 + 1;
					image_num[2] = num / 5 * 3 + 1;
					image_num[3] = num / 5 * 4;
					image_num[4] = num;
				}
				if (num - num / 5 * 5 == 4)
				{
					image_num[0] = num / 5 + 1;
					image_num[1] = num / 5 * 2 + 1;
					image_num[2] = num / 5 * 3 + 1;
					image_num[3] = num / 5 * 4 + 1;
					image_num[4] = num;
				}
			}
			if (num == 4)
			{
				image_num = new int[5];
				image_num[0] = 1;
				image_num[1] = 2;
				image_num[2] = 3;
				image_num[3] = 4;
				image_num[4] = 0;
			}
			if (num == 3)
			{
				image_num = new int[5];
				image_num[0] = 1;
				image_num[1] = 2;
				image_num[2] = 3;
				image_num[3] = 0;
				image_num[4] = 0;
			}
			if (num == 2)
			{
				image_num = new int[5];
				image_num[0] = 1;
				image_num[1] = 2;
				image_num[2] = 0;
				image_num[3] = 0;
				image_num[4] = 0;
			}
			if (num == 1)
			{
				image_num = new int[5];
				image_num[0] = 1;
				image_num[1] = 0;
				image_num[2] = 0;
				image_num[3] = 0;
				image_num[4] = 0;
			}
			if (num == 0)
			{
				image_num = new int[5];
				image_num[0] = 0;
				image_num[1] = 0;
				image_num[2] = 0;
				image_num[3] = 0;
				image_num[4] = 0;
			}
		}

		public void exit_thread()
		{
			try
			{
				StaticValue.截图排斥 = false;
				esc = "退出";
				fmloading.FmlClose = "窗体已关闭";
				esc_thread.Abort();
			}
			catch
			{
			}
			FormBorderStyle = FormBorderStyle.Sizable;
			Visible = true;
			base.Show();
			WindowState = FormWindowState.Normal;
			if (IniHelper.GetValue("快捷键", "翻译文本") != "请按下快捷键")
			{
				var value = IniHelper.GetValue("快捷键", "翻译文本");
				var text = "None";
				var text2 = "F9";
				SetHotkey(text, text2, value, 205);
			}
			HelpWin32.UnregisterHotKey(Handle, 222);
		}

		private Image<Gray, byte> randon(Image<Gray, byte> imageInput)
		{
			var width = imageInput.Width;
			var height = imageInput.Height;
			var num = 0;
			var array = new int[height];
			var result = imageInput;
			for (var i = -20; i < 20; i++)
			{
				var image = imageInput.Rotate(i, new Gray(1.0));
				for (var j = 0; j < height; j++)
				{
					var num2 = 0;
					for (var k = 0; k < width; k++)
					{
						num2 += image.Data[j, k, 0];
					}
					array[j] = num2;
				}
				var num3 = 0;
				for (var l = 0; l < height - 1; l++)
				{
					num3 += Math.Abs(array[l] - array[l + 1]);
				}
				if (num3 > num)
				{
					result = image;
					num = num3;
				}
			}
			return result;
		}

		public void SetGlobalProxy()
		{
			WebRequest.DefaultWebProxy = null;
		}

		private void tray_null_Proxy_Click(object sender, EventArgs e)
		{
			null_Proxy.Text = "不使用代理√";
			customize_Proxy.Text = "自定义代理";
			system_Proxy.Text = "系统代理";
			Proxy_flag = "关闭";
			WebRequest.DefaultWebProxy = null;
		}

		private void tray_system_Proxy_Click(object sender, EventArgs e)
		{
			null_Proxy.Text = "不使用代理";
			customize_Proxy.Text = "自定义代理";
			system_Proxy.Text = "系统代理√";
			Proxy_flag = "系统";
			WebRequest.DefaultWebProxy = WebRequest.GetSystemWebProxy();
		}

		public void change_pinyin_Click(object sender, EventArgs e)
		{
			pinyin_flag = true;
			transtalate_Click();
		}

		public string Post_Html_pinyin(string url, string post_str)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = 6000;
			httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		private Bitmap ZoomImage(Bitmap bitmap1, int destHeight, int destWidth)
		{
			var num = (double)bitmap1.Width;
			var num2 = (double)bitmap1.Height;
			if (num < destHeight)
			{
				while (num < destHeight)
				{
					num2 *= 1.1;
					num *= 1.1;
				}
			}
			if (num2 < destWidth)
			{
				while (num2 < destWidth)
				{
					num2 *= 1.1;
					num *= 1.1;
				}
			}
			var width = (int)num;
			var height = (int)num2;
			var bitmap2 = new Bitmap(width, height);
			var graphics = Graphics.FromImage(bitmap2);
			graphics.DrawImage(bitmap1, 0, 0, width, height);
			graphics.Save();
			graphics.Dispose();
			return new Bitmap(bitmap2);
		}

		public void 翻译文本()
		{
			if (IniHelper.GetValue("配置", "快速翻译") == "True")
			{
				var data = "";
				try
				{
					trans_hotkey = GetTextFromClipboard();
					if (IniHelper.GetValue("配置", "翻译接口") == "谷歌")
					{
						data = Translate_Google(trans_hotkey);
					}
					if (IniHelper.GetValue("配置", "翻译接口") == "百度")
					{
						data = Translate_baidu(trans_hotkey);
					}
					if (IniHelper.GetValue("配置", "翻译接口") == "腾讯")
					{
						data = Translate_Tencent(trans_hotkey);
					}
					Clipboard.SetData(DataFormats.UnicodeText, data);
					SendKeys.SendWait("^v");
					return;
				}
				catch
				{
					Clipboard.SetData(DataFormats.UnicodeText, data);
					SendKeys.SendWait("^v");
					return;
				}
			}
			SendKeys.SendWait("^c");
			SendKeys.Flush();
			RichBoxBody.Text = Clipboard.GetText();
			transtalate_Click();
			FormBorderStyle = FormBorderStyle.Sizable;
			Visible = true;
			HelpWin32.SetForegroundWindow(StaticValue.mainHandle);
			base.Show();
			WindowState = FormWindowState.Normal;
			if (IniHelper.GetValue("工具栏", "顶置") == "True")
			{
				TopMost = true;
				return;
			}
			TopMost = false;
		}

		public Rectangle[] GetRects(Bitmap pic)
		{
			var list = new List<Rectangle>();
			var colors = getColors(pic);
			for (var i = 0; i < pic.Height; i++)
			{
				for (var j = 0; j < pic.Width; j++)
				{
					if (Exist(colors, i, j))
					{
						var rect = GetRect(colors, i, j);
						if (rect.Width > 10 && rect.Height > 10)
						{
							list.Add(rect);
						}
					}
				}
			}
			return list.ToArray();
		}

		public Bitmap GetRect(Image pic, Rectangle Rect)
		{
			var destRect = new Rectangle(0, 0, Rect.Width, Rect.Height);
			var bitmap = new Bitmap(destRect.Width, destRect.Height);
			var graphics = Graphics.FromImage(bitmap);
			graphics.Clear(Color.FromArgb(0, 0, 0, 0));
			graphics.DrawImage(pic, destRect, Rect, GraphicsUnit.Pixel);
			graphics.Dispose();
			return bitmap;
		}

		private Bitmap[] getSubPics(Image buildPic, Rectangle[] buildRects)
		{
			var array = new Bitmap[buildRects.Length];
			for (var i = 0; i < buildRects.Length; i++)
			{
				array[i] = GetRect(buildPic, buildRects[i]);
				var filename = IniHelper.GetValue("配置", "截图位置") + "\\" + ReFileName(IniHelper.GetValue("配置", "截图位置"), "图片.Png");
				array[i].Save(filename, ImageFormat.Png);
			}
			return array;
		}

		public bool[][] getColors(Bitmap pic)
		{
			var array = new bool[pic.Height][];
			for (var i = 0; i < pic.Height; i++)
			{
				array[i] = new bool[pic.Width];
				for (var j = 0; j < pic.Width; j++)
				{
					var pixel = pic.GetPixel(j, i);
					var num = 0;
					if (pixel.R < 4)
					{
						num++;
					}
					if (pixel.G < 4)
					{
						num++;
					}
					if (pixel.B < 4)
					{
						num++;
					}
					if (pixel.A < 3 || (num >= 2 && pixel.A < 30))
					{
						array[i][j] = false;
					}
					else
					{
						array[i][j] = true;
					}
				}
			}
			return array;
		}

		public bool Exist(bool[][] Colors, int x, int y)
		{
			return x >= 0 && y >= 0 && x < Colors.Length && y < Colors[0].Length && Colors[x][y];
		}

		public bool R_Exist(bool[][] Colors, Rectangle Rect)
		{
			if (Rect.Right >= Colors[0].Length || Rect.Left < 0)
			{
				return false;
			}
			for (var i = 0; i < Rect.Height; i++)
			{
				if (Exist(Colors, Rect.Top + i, Rect.Right + 1))
				{
					return true;
				}
			}
			return false;
		}

		public bool D_Exist(bool[][] Colors, Rectangle Rect)
		{
			if (Rect.Bottom >= Colors.Length || Rect.Top < 0)
			{
				return false;
			}
			for (var i = 0; i < Rect.Width; i++)
			{
				if (Exist(Colors, Rect.Bottom + 1, Rect.Left + i))
				{
					return true;
				}
			}
			return false;
		}

		public bool L_Exist(bool[][] Colors, Rectangle Rect)
		{
			if (Rect.Right >= Colors[0].Length || Rect.Left < 0)
			{
				return false;
			}
			for (var i = 0; i < Rect.Height; i++)
			{
				if (Exist(Colors, Rect.Top + i, Rect.Left - 1))
				{
					return true;
				}
			}
			return false;
		}

		public bool U_Exist(bool[][] Colors, Rectangle Rect)
		{
			if (Rect.Bottom >= Colors.Length || Rect.Top < 0)
			{
				return false;
			}
			for (var i = 0; i < Rect.Width; i++)
			{
				if (Exist(Colors, Rect.Top - 1, Rect.Left + i))
				{
					return true;
				}
			}
			return false;
		}

		public Rectangle GetRect(bool[][] Colors, int x, int y)
		{
			var rectangle = new Rectangle(new Point(y, x), new Size(1, 1));
			bool flag;
			int num;
			do
			{
				flag = false;
				while (R_Exist(Colors, rectangle))
				{
					num = rectangle.Width;
					rectangle.Width = num + 1;
					flag = true;
				}
				while (D_Exist(Colors, rectangle))
				{
					num = rectangle.Height;
					rectangle.Height = num + 1;
					flag = true;
				}
				while (L_Exist(Colors, rectangle))
				{
					num = rectangle.Width;
					rectangle.Width = num + 1;
					num = rectangle.X;
					rectangle.X = num - 1;
					flag = true;
				}
				while (U_Exist(Colors, rectangle))
				{
					num = rectangle.Height;
					rectangle.Height = num + 1;
					num = rectangle.Y;
					rectangle.Y = num - 1;
					flag = true;
				}
			}
			while (flag);
			clearRect(Colors, rectangle);
			num = rectangle.Width;
			rectangle.Width = num + 1;
			num = rectangle.Height;
			rectangle.Height = num + 1;
			return rectangle;
		}

		public void clearRect(bool[][] Colors, Rectangle Rect)
		{
			for (var i = Rect.Top; i <= Rect.Bottom; i++)
			{
				for (var j = Rect.Left; j <= Rect.Right; j++)
				{
					Colors[i][j] = false;
				}
			}
		}

		public static string ReFileNamekey(string strFilePath)
		{
			var startIndex = strFilePath.LastIndexOf('.');
			var format = strFilePath.Insert(startIndex, "_{0}");
			var num = 1;
			var text = string.Format(format, num);
			while (File.Exists(text))
			{
				text = string.Format(format, num);
				num++;
			}
			return text;
		}

		private Bitmap[] getSubPics_ocr(Image buildPic, Rectangle[] buildRects)
		{
			var text = "";
			var array = new Bitmap[buildRects.Length];
			var text2 = "";
			for (var i = 0; i < buildRects.Length; i++)
			{
				array[i] = GetRect(buildPic, buildRects[i]);
				image_screen = array[i];
				var messageload = new Messageload();
				messageload.ShowDialog();
				if (messageload.DialogResult == DialogResult.OK)
				{
					if (interface_flag == "搜狗")
					{
						OCR_sougou2();
					}
					if (interface_flag == "腾讯")
					{
						OCR_Tencent();
					}
					if (interface_flag == "有道")
					{
						OCR_youdao();
					}
					if (interface_flag == "日语" || interface_flag == "中英" || interface_flag == "韩语")
					{
						OCR_baidu();
					}
					messageload.Dispose();
				}
				if (IniHelper.GetValue("工具栏", "分栏") == "True")
				{
					if (paragraph)
					{
						text = text + "\r\n" + typeset_txt.Trim();
						text2 = text2 + "\r\n" + split_txt.Trim() + "\r\n";
					}
					else
					{
						text += typeset_txt.Trim();
						text2 = text2 + "\r\n" + split_txt.Trim() + "\r\n";
					}
				}
				else if (paragraph)
				{
					text = text + "\r\n" + typeset_txt.Trim() + "\r\n";
					text2 = text2 + "\r\n" + split_txt.Trim() + "\r\n";
				}
				else
				{
					text = text + typeset_txt.Trim() + "\r\n";
					text2 = text2 + "\r\n" + split_txt.Trim() + "\r\n";
				}
			}
			typeset_txt = text.Replace("\r\n\r\n", "\r\n");
			split_txt = text2.Replace("\r\n\r\n", "\r\n");
			fmloading.FmlClose = "窗体已关闭";
			Invoke(new ocr_thread(Main_OCR_Thread_last));
			return array;
		}

		public void OCR_sougou_bat(Bitmap image_screen)
		{
			try
			{
				split_txt = "";
				var text = "------WebKitFormBoundary8orYTmcj8BHvQpVU";
				Image image = ZoomImage(image_screen, 120, 120);
				var b = OCR_ImgToByte(image);
				var s = text + "\r\nContent-Disposition: form-data; name=\"pic\"; filename=\"pic.jpg\"\r\nContent-Type: image/jpeg\r\n\r\n";
				var s2 = "\r\n" + text + "--\r\n";
				var bytes = Encoding.ASCII.GetBytes(s);
				var bytes2 = Encoding.ASCII.GetBytes(s2);
				var array = Mergebyte(bytes, b, bytes2);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://ocr.shouji.sogou.com/v2/ocr/json");
				httpWebRequest.Timeout = 8000;
				httpWebRequest.Method = "POST";
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + text.Substring(2);
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(array, 0, array.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["result"].ToString());
				checked_txt(jarray, 2, "content");
				image.Dispose();
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public Image BoundingBox_fences(Image<Gray, byte> src, Image<Bgr, byte> draw)
		{
			Image result;
			using (var vectorOfVectorOfPoint = new VectorOfVectorOfPoint())
			{
				CvInvoke.FindContours(src, vectorOfVectorOfPoint, null, RetrType.List, ChainApproxMethod.ChainApproxSimple, default(Point));
				Image image = draw.ToBitmap();
				var graphics = Graphics.FromImage(image);
				var size = vectorOfVectorOfPoint.Size;
				for (var i = 0; i < size; i++)
				{
					using (var vectorOfPoint = vectorOfVectorOfPoint[i])
					{
						var rectangle = CvInvoke.BoundingRectangle(vectorOfPoint);
						var x = rectangle.Location.X;
						var y = rectangle.Location.Y;
						var width = rectangle.Size.Width;
						var height = rectangle.Size.Height;
						graphics.FillRectangle(Brushes.White, x, 0, width, draw.Height);
					}
				}
				graphics.Dispose();
				var bitmap = new Bitmap(image.Width + 2, image.Height + 2);
				var graphics2 = Graphics.FromImage(bitmap);
				graphics2.DrawImage(image, 1, 1, image.Width, image.Height);
				graphics2.Save();
				graphics2.Dispose();
				image.Dispose();
				src.Dispose();
				result = bitmap;
			}
			return result;
		}

		public Image FindBundingBox_fences(Bitmap bitmap)
		{
			var image = new Image<Bgr, byte>(bitmap);
			var image2 = new Image<Gray, byte>(image.Width, image.Height);
			CvInvoke.CvtColor(image, image2, ColorConversion.Bgra2Gray, 0);
			var structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(6, 20), new Point(1, 1));
			CvInvoke.Erode(image2, image2, structuringElement, new Point(0, 2), 1, BorderType.Reflect101, default(MCvScalar));
			CvInvoke.Threshold(image2, image2, 100.0, 255.0, (ThresholdType)9);
			var image3 = new Image<Gray, byte>(image2.ToBitmap());
			var draw = image3.Convert<Bgr, byte>();
			var image4 = image3.Clone();
			CvInvoke.Canny(image3, image4, 255.0, 255.0, 5, true);
			var image5 = BoundingBox_fences(image4, draw);
			var image6 = new Image<Gray, byte>((Bitmap)image5);
			BoundingBox_fences_Up(image6);
			image.Dispose();
			image2.Dispose();
			image3.Dispose();
			image6.Dispose();
			return image5;
		}

		public void BoundingBox_fences_Up(Image<Gray, byte> src)
		{
			using (var vectorOfVectorOfPoint = new VectorOfVectorOfPoint())
			{
				CvInvoke.FindContours(src, vectorOfVectorOfPoint, null, RetrType.List, ChainApproxMethod.ChainApproxSimple, default(Point));
				var size = vectorOfVectorOfPoint.Size;
				var array = new Rectangle[size];
				for (var i = 0; i < size; i++)
				{
					using (var vectorOfPoint = vectorOfVectorOfPoint[i])
					{
						array[size - 1 - i] = CvInvoke.BoundingRectangle(vectorOfPoint);
					}
				}
				getSubPics_ocr(image_screen, array);
			}
		}

		public void checked_location_txt(JArray jarray, int lastlength, string words)
		{
			var num = 0;
			for (var i = 0; i < jarray.Count; i++)
			{
				var length = JObject.Parse(jarray[i].ToString())[words].ToString().Length;
				if (length > num)
				{
					num = length;
				}
			}
			var str = "";
			var text = "";
			for (var j = 0; j < jarray.Count - 1; j++)
			{
				var jobject = JObject.Parse(jarray[j].ToString());
				var array = jobject[words].ToString().ToCharArray();
				var jobject2 = JObject.Parse(jarray[j + 1].ToString());
				var array2 = jobject2[words].ToString().ToCharArray();
				var length2 = jobject[words].ToString().Length;
				var length3 = jobject2[words].ToString().Length;
				if (Math.Abs(length2 - length3) <= 0)
				{
					if (split_paragraph(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else
					{
						text += jobject[words].ToString().Trim();
					}
				}
				else if (split_paragraph(array[array.Length - lastlength].ToString()) && Math.Abs(length2 - length3) <= 1)
				{
					if (split_paragraph(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else if (split_paragraph(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
					{
						text = text + jobject[words].ToString().Trim() + "\r\n";
					}
					else
					{
						text += jobject[words].ToString().Trim();
					}
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && length2 <= num / 2)
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()) && length3 - length2 < 4 && array2[1].ToString() == ".")
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_en(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text = text + jobject[words].ToString().Trim() + " ";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_en(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && Is_punctuation(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (Is_punctuation(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (Is_punctuation(array[array.Length - lastlength].ToString()) && contain_en(array2[0].ToString()))
				{
					text = text + jobject[words].ToString().Trim() + " ";
				}
				else if (contain_ch(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (IsNum(array[array.Length - lastlength].ToString()) && contain_ch(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else if (IsNum(array[array.Length - lastlength].ToString()) && IsNum(array2[0].ToString()))
				{
					text += jobject[words].ToString().Trim();
				}
				else
				{
					text = text + jobject[words].ToString().Trim() + "\r\n";
				}
				if (has_punctuation(jobject[words].ToString()))
				{
					text += "\r\n";
				}
				str = str + jobject[words].ToString().Trim() + "\r\n";
			}
			split_txt = str + JObject.Parse(jarray[jarray.Count - 1].ToString())[words];
			typeset_txt = text.Replace("\r\n\r\n", "\r\n") + JObject.Parse(jarray[jarray.Count - 1].ToString())[words];
		}

		public void checked_location_sougou(JArray jarray, int lastlength, string words, string location)
		{
			paragraph = false;
			var num = 20000;
			var num2 = 0;
			for (var i = 0; i < jarray.Count; i++)
			{
				var jobject = JObject.Parse(jarray[i].ToString());
				var num3 = split_char_x(jobject[location][1].ToString()) - split_char_x(jobject[location][0].ToString());
				if (num3 > num2)
				{
					num2 = num3;
				}
				var num4 = split_char_x(jobject[location][0].ToString());
				if (num4 < num)
				{
					num = num4;
				}
			}
			var jobject2 = JObject.Parse(jarray[0].ToString());
			if (Math.Abs(split_char_x(jobject2[location][0].ToString()) - num) > 10)
			{
				paragraph = true;
			}
			var text = "";
			var text2 = "";
			for (var j = 0; j < jarray.Count; j++)
			{
				var jobject3 = JObject.Parse(jarray[j].ToString());
				var array = jobject3[words].ToString().ToCharArray();
				var jobject4 = JObject.Parse(jarray[j].ToString());
				var flag = Math.Abs(split_char_x(jobject4[location][1].ToString()) - split_char_x(jobject4[location][0].ToString()) - num2) > 20;
				var flag2 = Math.Abs(split_char_x(jobject4[location][0].ToString()) - num) > 10;
				if (flag && flag2)
				{
					text = text.Trim() + "\r\n" + jobject4[words].ToString().Trim();
				}
				else if (IsNum(array[0].ToString()) && !contain_ch(array[1].ToString()) && flag)
				{
					text = text.Trim() + "\r\n" + jobject4[words].ToString().Trim() + "\r\n";
				}
				else
				{
					text += jobject4[words].ToString().Trim();
				}
				if (contain_en(array[array.Length - lastlength].ToString()))
				{
					text = text + jobject3[words].ToString().Trim() + " ";
				}
				text2 = text2 + jobject4[words].ToString().Trim() + "\r\n";
			}
			split_txt = text2.Replace("\r\n\r\n", "\r\n");
			typeset_txt = text;
		}

		public int split_char_x(string split_char)
		{
			return Convert.ToInt32(split_char.Split(',')[0]);
		}

		private void tray_double_Click(object sender, EventArgs e)
		{
			HelpWin32.UnregisterHotKey(Handle, 205);
			menu.Hide();
			RichBoxBody.Hide = "";
			RichBoxBody_T.Hide = "";
			Main_OCR_Quickscreenshots();
		}

		public int en_count(string text)
		{
			return Regex.Matches(text, "\\s+").Count + 1;
		}

		public int ch_count(string str)
		{
			var num = 0;
			var regex = new Regex("^[\\u4E00-\\u9FA5]{0,}$");
			for (var i = 0; i < str.Length; i++)
			{
				if (regex.IsMatch(str[i].ToString()))
				{
					num++;
				}
			}
			return num;
		}

		public void checked_location_youdao(JArray jarray, int lastlength, string words, string location)
		{
			paragraph = false;
			var num = 20000;
			var num2 = 0;
			for (var i = 0; i < jarray.Count; i++)
			{
				var jobject = JObject.Parse(jarray[i].ToString());
				var num3 = split_char_youdao(jobject[location].ToString(), 3) - split_char_youdao(jobject[location].ToString(), 1);
				if (num3 > num2)
				{
					num2 = num3;
				}
				var num4 = split_char_youdao(jobject[location].ToString(), 1);
				if (num4 < num)
				{
					num = num4;
				}
			}
			var jobject2 = JObject.Parse(jarray[0].ToString());
			if (Math.Abs(split_char_youdao(jobject2[location].ToString(), 1) - num) > 10)
			{
				paragraph = true;
			}
			var text = "";
			var text2 = "";
			for (var j = 0; j < jarray.Count; j++)
			{
				var jobject3 = JObject.Parse(jarray[j].ToString());
				var array = jobject3[words].ToString().ToCharArray();
				var jobject4 = JObject.Parse(jarray[j].ToString());
				var flag = Math.Abs(split_char_youdao(jobject4[location].ToString(), 3) - split_char_youdao(jobject4[location].ToString(), 1) - num2) > 20;
				var flag2 = Math.Abs(split_char_youdao(jobject4[location].ToString(), 1) - num) > 10;
				if (flag && flag2)
				{
					text = text.Trim() + "\r\n" + jobject4[words].ToString().Trim();
				}
				else if (IsNum(array[0].ToString()) && !contain_ch(array[1].ToString()) && flag)
				{
					text = text.Trim() + "\r\n" + jobject4[words].ToString().Trim() + "\r\n";
				}
				else
				{
					text += jobject4[words].ToString().Trim();
				}
				if (contain_en(array[array.Length - lastlength].ToString()))
				{
					text = text + jobject3[words].ToString().Trim() + " ";
				}
				text2 = text2 + jobject4[words].ToString().Trim() + "\r\n";
			}
			split_txt = text2.Replace("\r\n\r\n", "\r\n");
			typeset_txt = text;
		}

		public int split_char_youdao(string split_char, int i)
		{
			return Convert.ToInt32(split_char.Split(',')[i - 1]);
		}

		public void Trans_google_Click(object sender, EventArgs e)
		{
			Trans_foreach("谷歌");
		}

		public void Trans_baidu_Click(object sender, EventArgs e)
		{
			Trans_foreach("百度");
		}

		private void Trans_foreach(string name)
		{
			if (name == "百度")
			{
				trans_baidu.Text = "百度√";
				trans_google.Text = "谷歌";
				trans_tencent.Text = "腾讯";
				IniHelper.SetValue("配置", "翻译接口", "百度");
			}
			if (name == "谷歌")
			{
				trans_baidu.Text = "百度";
				trans_google.Text = "谷歌√";
				trans_tencent.Text = "腾讯";
				IniHelper.SetValue("配置", "翻译接口", "谷歌");
			}
			if (name == "腾讯")
			{
				trans_google.Text = "谷歌";
				trans_baidu.Text = "百度";
				trans_tencent.Text = "腾讯√";
				IniHelper.SetValue("配置", "翻译接口", "腾讯");
			}
		}

		public string GetBaiduHtml(string url, CookieContainer cookie, string refer, string content_length)
		{
			string result;
			try
			{
				var text = "";
				var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = refer;
				httpWebRequest.Timeout = 1500;
				httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				var bytes = Encoding.UTF8.GetBytes(content_length);
				var requestStream = httpWebRequest.GetRequestStream();
				requestStream.Write(bytes, 0, bytes.Length);
				requestStream.Close();
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
			}
			catch
			{
				result = GetBaiduHtml(url, cookie, refer, content_length);
			}
			return result;
		}

		private string Translate_baidu(string Text)
		{
			var text = "";
			try
			{
				new CookieContainer();
				var text2 = "zh";
				var text3 = "en";
				if (StaticValue.ZH2EN)
				{
					if (ch_count(Text.Trim()) > en_count(Text.Trim()) || (en_count(text.Trim()) == 1 && ch_count(text.Trim()) == 1))
					{
						text2 = "zh";
						text3 = "en";
					}
					else
					{
						text2 = "en";
						text3 = "zh";
					}
				}
				if (StaticValue.ZH2JP)
				{
					if (contain_jap(replaceStr(Del_ch(Text.Trim()))))
					{
						text2 = "jp";
						text3 = "zh";
					}
					else
					{
						text2 = "zh";
						text3 = "jp";
					}
				}
				if (StaticValue.ZH2KO)
				{
					if (contain_kor(Text.Trim()))
					{
						text2 = "kor";
						text3 = "zh";
					}
					else
					{
						text2 = "zh";
						text3 = "kor";
					}
				}
                var html = CommonHelper.PostData("https://fanyi.baidu.com/basetrans",
                    string.Concat("query=", HttpUtility.UrlEncode(Text.Trim()).Replace("+", "%20"), "&from=", text2,
                        "&to=", text3));
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(html))["trans"].ToString());
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text = text + jobject["dst"] + "\r\n";
				}
			}
			catch (Exception)
			{
				text = "[百度接口报错]：\r\n1.接口请求出现问题等待修复。";
			}
			return text;
		}

		public void Trans_tencent_Click(object sender, EventArgs e)
		{
			Trans_foreach("腾讯");
		}

		public string Content_Length(string text, string fromlang, string tolang)
		{
			return string.Concat("&source=", fromlang, "&target=", tolang, "&sourceText=", HttpUtility.UrlEncode(text).Replace("+", "%20"));
		}

		public string TencentPOST(string url, string content)
		{
			string result;
			try
			{
				var text = "";
				var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
				httpWebRequest.Method = "POST";
				httpWebRequest.Referer = "https://fanyi.qq.com/";
				httpWebRequest.Timeout = 5000;
				httpWebRequest.Accept = "application/json, text/javascript, */*; q=0.01";
				httpWebRequest.Headers.Add("X-Requested-With: XMLHttpRequest");
				httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
				httpWebRequest.Headers.Add("Accept-Language: zh-CN,zh;q=0.9");
				httpWebRequest.Headers.Add("cookie:" + GetCookies("http://fanyi.qq.com"));
				var bytes = Encoding.UTF8.GetBytes(content);
				httpWebRequest.ContentLength = bytes.Length;
				var requestStream = httpWebRequest.GetRequestStream();
				requestStream.Write(bytes, 0, bytes.Length);
				requestStream.Close();
				using (var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
				{
					using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
					{
						text = streamReader.ReadToEnd();
						streamReader.Close();
						httpWebResponse.Close();
					}
				}
				result = text;
				if (text.Contains("\"records\":[]"))
				{
					Thread.Sleep(8);
					return TencentPOST(url, content);
				}
			}
			catch
			{
				result = "[腾讯接口报错]：\r\n请切换其它接口或再次尝试。";
			}
			return result;
		}

		private string Translate_Tencent(string strtrans)
		{
			var text = "";
			try
			{
				var fromlang = "zh";
				var tolang = "en";
				if (StaticValue.ZH2EN)
				{
					if (ch_count(strtrans.Trim()) > en_count(strtrans.Trim()) || (en_count(text.Trim()) == 1 && ch_count(text.Trim()) == 1))
					{
						fromlang = "zh";
						tolang = "en";
					}
					else
					{
						fromlang = "en";
						tolang = "zh";
					}
				}
				if (StaticValue.ZH2JP)
				{
					if (contain_jap(replaceStr(Del_ch(strtrans.Trim()))))
					{
						fromlang = "jp";
						tolang = "zh";
					}
					else
					{
						fromlang = "zh";
						tolang = "jp";
					}
				}
				if (StaticValue.ZH2KO)
				{
					if (contain_kor(strtrans.Trim()))
					{
						fromlang = "kr";
						tolang = "zh";
					}
					else
					{
						fromlang = "zh";
						tolang = "kr";
					}
				}
				var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(TencentPOST("https://fanyi.qq.com/api/translate", Content_Length(strtrans, fromlang, tolang))))["translate"]["records"].ToString());
				for (var i = 0; i < jarray.Count; i++)
				{
					var jobject = JObject.Parse(jarray[i].ToString());
					text += jobject["targetText"].ToString();
				}
			}
			catch (Exception)
			{
				text = "[腾讯接口报错]：\r\n1.接口请求出现问题等待修复。";
			}
			return text;
		}

		public void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
		}

		private static string GetCookies(string url)
		{
			var num = 1024u;
			var stringBuilder = new StringBuilder((int)num);
			if (!InternetGetCookieEx(url, null, stringBuilder, ref num, 8192, IntPtr.Zero))
			{
				if (num < 0u)
				{
					return null;
				}
				stringBuilder = new StringBuilder((int)num);
				if (!InternetGetCookieEx(url, null, stringBuilder, ref num, 8192, IntPtr.Zero))
				{
					return null;
				}
			}
			return stringBuilder.ToString();
		}

		[DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref uint pcchCookieData, int dwFlags, IntPtr lpReserved);

		public void BdTableOCR()
		{
			typeset_txt = "[消息]：表格已下载！";
			split_txt = "";
			try
			{
				baidu_vip = Get_html(string.Format("{0}?{1}", "https://aip.baidubce.com/oauth/2.0/token", "grant_type=client_credentials&client_id=" + StaticValue.BD_API_ID + "&client_secret=" + StaticValue.BD_API_KEY));
				if (baidu_vip == "")
				{
					MessageBox.Show("请检查密钥输入是否正确！", "提醒");
				}
				else
				{
					split_txt = "";
					var image = image_screen;
					var memoryStream = new MemoryStream();
					image.Save(memoryStream, ImageFormat.Jpeg);
					var array = new byte[memoryStream.Length];
					memoryStream.Position = 0L;
					memoryStream.Read(array, 0, (int)memoryStream.Length);
					memoryStream.Close();
					var s = "image=" + HttpUtility.UrlEncode(Convert.ToBase64String(array));
					var bytes = Encoding.UTF8.GetBytes(s);
					var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://aip.baidubce.com/rest/2.0/solution/v1/form_ocr/request?access_token=" + ((JObject)JsonConvert.DeserializeObject(baidu_vip))["access_token"]);
					httpWebRequest.Proxy = null;
					httpWebRequest.Method = "POST";
					httpWebRequest.ContentType = "application/x-www-form-urlencoded";
					httpWebRequest.Timeout = 8000;
					httpWebRequest.ReadWriteTimeout = 5000;
					using (var requestStream = httpWebRequest.GetRequestStream())
					{
						requestStream.Write(bytes, 0, bytes.Length);
					}
					var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
					var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
					responseStream.Close();
					var post_str = "request_id=" + JObject.Parse(JArray.Parse(((JObject)JsonConvert.DeserializeObject(value))["result"].ToString())[0].ToString())["request_id"].ToString().Trim() + "&result_type=json";
					var text = "";
					while (!text.Contains("已完成"))
					{
						if (text.Contains("image recognize error"))
						{
							RichBoxBody.Text = "[消息]：未发现表格！";
							break;
						}
						Thread.Sleep(120);
						text = Post_Html("https://aip.baidubce.com/rest/2.0/solution/v1/form_ocr/get_request_result?access_token=" + ((JObject)JsonConvert.DeserializeObject(baidu_vip))["access_token"], post_str);
					}
					if (!text.Contains("image recognize error"))
					{
						get_table(text);
					}
				}
			}
			catch
			{
				RichBoxBody.Text = "[消息]：免费百度密钥50次已经耗完！请更换自己的密钥继续使用！";
			}
		}

		public void OCR_table_Click(object sender, EventArgs e)
		{
			OCR_foreach("表格");
		}

		private void get_table(string str)
		{
			var jarray = JArray.Parse(((JObject)JsonConvert.DeserializeObject(((JObject)JsonConvert.DeserializeObject(str))["result"]["result_data"].ToString().Replace("\\", "")))["forms"][0]["body"].ToString());
			var array = new int[jarray.Count];
			var array2 = new int[jarray.Count];
			for (var i = 0; i < jarray.Count; i++)
			{
				var jobject = JObject.Parse(jarray[i].ToString());
				var value = jobject["column"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				var value2 = jobject["row"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				array[i] = Convert.ToInt32(value);
				array2[i] = Convert.ToInt32(value2);
			}
			var array3 = new string[array2.Max() + 1, array.Max() + 1];
			for (var j = 0; j < jarray.Count; j++)
			{
				var jobject2 = JObject.Parse(jarray[j].ToString());
				var value3 = jobject2["column"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				var value4 = jobject2["row"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				array[j] = Convert.ToInt32(value3);
				array2[j] = Convert.ToInt32(value4);
				var text = jobject2["word"].ToString().Replace("[", "").Replace("]", "").Replace("\r", "").Replace("\n", "").Trim();
				array3[Convert.ToInt32(value4), Convert.ToInt32(value3)] = text;
			}
			var graphics = CreateGraphics();
			var array4 = new int[array.Max() + 1];
			var num = 0;
			var sizeF = new SizeF(10f, 10f);
			var num2 = Screen.PrimaryScreen.Bounds.Width / 4;
			for (var k = 0; k < array3.GetLength(1); k++)
			{
				for (var l = 0; l < array3.GetLength(0); l++)
				{
					sizeF = graphics.MeasureString(array3[l, k], new Font("宋体", 12f));
					if (num < (int)sizeF.Width)
					{
						num = (int)sizeF.Width;
					}
					if (num > num2)
					{
						num = num2;
					}
				}
				array4[k] = num;
				num = 0;
			}
			graphics.Dispose();
			setClipboard_Table(array3, array4);
		}

		public void Main_OCR_Thread_table()
		{
			ailibaba = new AliTable();
			var timeSpan = new TimeSpan(DateTime.Now.Ticks);
			var timeSpan2 = timeSpan.Subtract(ts).Duration();
			var str = string.Concat(new[]
			{
				timeSpan2.Seconds.ToString(),
				".",
				Convert.ToInt32(timeSpan2.TotalMilliseconds).ToString(),
				"秒"
			});
			if (StaticValue.v_topmost)
			{
				TopMost = true;
			}
			else
			{
				TopMost = false;
			}
			Text = "耗时：" + str;
			if (interface_flag == "百度表格")
			{
				var dataObject = new DataObject();
				dataObject.SetData(DataFormats.Rtf, RichBoxBody.Rtx1Rtf);
				dataObject.SetData(DataFormats.UnicodeText, RichBoxBody.Text);
				RichBoxBody.Text = "[消息]：表格已复制到粘贴板！";
				Clipboard.SetDataObject(dataObject);
			}
			image_screen.Dispose();
			GC.Collect();
			StaticValue.截图排斥 = false;
			FormBorderStyle = FormBorderStyle.Sizable;
			Visible = true;
			base.Show();
			WindowState = FormWindowState.Normal;
			Size = new Size(form_width, form_height);
			HelpWin32.SetForegroundWindow(Handle);
			if (interface_flag == "阿里表格")
			{
				if (split_txt == "弹出cookie")
				{
					split_txt = "";
					ailibaba.TopMost = true;
					ailibaba.getcookie = "";
					IniHelper.SetValue("特殊", "ali_cookie", ailibaba.getcookie);
					ailibaba.ShowDialog();
					HelpWin32.SetForegroundWindow(ailibaba.Handle);
					return;
				}
				Clipboard.SetDataObject(typeset_txt);
				CopyHtmlToClipBoard(typeset_txt);
			}
		}

		private void setClipboard_Table(string[,] wordo, int[] cc)
		{
			var str = "{\\rtf1\\ansi\\ansicpg936\\deff0\\deflang1033\\deflangfe2052{\\fonttbl{\\f0\\fnil\\fprq2\\fcharset134";
			str += "\\'cb\\'ce\\'cc\\'e5;}{\\f1\\fnil\\fcharset134 \\'cb\\'ce\\'cc\\'e5;}}\\viewkind4\\uc1\\trowd\\trgaph108\\trleft-108";
			str += "\\trbrdrt\\brdrs\\brdrw10 \\trbrdrl\\brdrs\\brdrw10 \\trbrdrb\\brdrs\\brdrw10 \\trbrdrb\\brdrs\\brdrw10 ";
			var num = 0;
			for (var i = 1; i <= cc.Length; i++)
			{
				num += cc[i - 1] * 17;
				str = str + "\\clbrdrt\\brdrw15\\brdrs\\clbrdrl\\brdrw15\\brdrs\\clbrdrb\\brdrw15\\brdrs\\clbrdrr\\brdrw15\\brdrs \\cellx" + num;
			}
			var text = "";
			var str2 = "\\pard\\intbl\\kerning2\\f0";
			var str3 = "\\row\\pard\\lang2052\\kerning0\\f1\\fs18\\par}";
			for (var j = 0; j < wordo.GetLength(0); j++)
			{
				for (var k = 0; k < wordo.GetLength(1); k++)
				{
					if (k == 0)
					{
						text = text + "\\fs24 " + wordo[j, k];
					}
					else
					{
						text = text + "\\cell " + wordo[j, k];
					}
				}
				if (j != wordo.GetLength(0) - 1)
				{
					text += "\\row\\intbl";
				}
			}
			RichBoxBody.Rtx1Rtf = str + str2 + text + str3;
		}

		public string Translate_Googlekey(string text)
		{
			var text2 = "";
			try
			{
				var text3 = "zh-CN";
				var text4 = "en";
				if (StaticValue.ZH2EN)
				{
					if (ch_count(typeset_txt.Trim()) > en_count(typeset_txt.Trim()))
					{
						text3 = "zh-CN";
						text4 = "en";
					}
					else
					{
						text3 = "en";
						text4 = "zh-CN";
					}
				}
				else if (StaticValue.ZH2JP)
				{
					if (contain_jap(replaceStr(Del_ch(typeset_txt.Trim()))))
					{
						text3 = "ja";
						text4 = "zh-CN";
					}
					else
					{
						text3 = "zh-CN";
						text4 = "ja";
					}
				}
                else if(StaticValue.ZH2KO)
				{
					if (contain_kor(typeset_txt.Trim()))
					{
						text3 = "ko";
						text4 = "zh-CN";
					}
					else
					{
						text3 = "zh-CN";
						text4 = "ko";
					}
				}
				var postData = string.Concat("client=gtx&sl=", text3, "&tl=", text4, "&dt=t&q=", HttpUtility.UrlEncode(text).Replace("+", "%20"));
				var jArray = (JArray)JsonConvert.DeserializeObject(CommonHelper.PostData("https://translate.google.cn/translate_a/single", postData));
				var count = ((JArray)jArray[0]).Count;
				for (var i = 0; i < count; i++)
				{
					text2 += jArray[0][i][0].ToString();
				}
			}
			catch (Exception)
			{
				text2 = "[谷歌接口报错]：\r\n出现这个提示文字，表示您当前的网络不适合使用谷歌接口，使用方法开启设置中的系统代理，看是否可行，仍不可行的话，请自行挂VPN，多的不再说，这个问题不要再和我反馈了，个人能力有限解决不了。\r\n请放弃使用谷歌接口，腾讯，百度接口都可以正常使用。";
			}
			return text2;
		}

		public void OCR_baidutable_Click(object sender, EventArgs e)
		{
			OCR_foreach("百度表格");
		}

		public void OCR_ailitable_Click(object sender, EventArgs e)
		{
			OCR_foreach("阿里表格");
		}

		private new void Refresh()
		{
			sougou.Text = "搜狗";
			tencent.Text = "腾讯";
			baidu.Text = "百度";
			youdao.Text = "有道";
			shupai.Text = "竖排";
			ocr_table.Text = "表格";
			ch_en.Text = "中英";
			jap.Text = "日语";
			kor.Text = "韩语";
			left_right.Text = "从左向右";
			righ_left.Text = "从右向左";
			baidu_table.Text = "百度";
			ali_table.Text = "阿里";
			Mathfuntion.Text = "公式";
		}

		public static byte[] ImageToByteArray(Image img)
		{
			return (byte[])new ImageConverter().ConvertTo(img, typeof(byte[]));
		}

		public static Stream BytesToStream(byte[] bytes)
		{
			return new MemoryStream(bytes);
		}

		public void OCR_ali_table()
		{
			var text = "";
			split_txt = "";
			try
			{
				var value = IniHelper.GetValue("特殊", "ali_cookie");
				var stream = BytesToStream(ImageToByteArray(BWPic((Bitmap)image_screen)));
				var str = Convert.ToBase64String(new BinaryReader(stream).ReadBytes(Convert.ToInt32(stream.Length)));
				stream.Close();
				var post_str = "{\n\t\"image\": \"" + str + "\",\n\t\"configure\": \"{\\\"format\\\":\\\"html\\\", \\\"finance\\\":false}\"\n}";
				var url = "https://predict-pai.data.aliyun.com/dp_experience_mall/ocr/ocr_table_parse";
				text = Post_Html_final(url, post_str, value);
				typeset_txt = ((JObject)JsonConvert.DeserializeObject(Post_Html_final(url, post_str, value)))["tables"].ToString().Replace("table tr td { border: 1px solid blue }", "table tr td {border: 0.5px black solid }").Replace("table { border: 1px solid blue }", "table { border: 0.5px black solid; border-collapse : collapse}\r\n");
				RichBoxBody.Text = "[消息]：表格已复制到粘贴板！";
			}
			catch
			{
				RichBoxBody.Text = "[消息]：阿里表格识别出错！";
				if (text.Contains("NEED_LOGIN"))
				{
					split_txt = "弹出cookie";
				}
			}
		}

		public Bitmap BWPic(Bitmap mybm)
		{
			var bitmap = new Bitmap(mybm.Width, mybm.Height);
			for (var i = 0; i < mybm.Width; i++)
			{
				for (var j = 0; j < mybm.Height; j++)
				{
					var pixel = mybm.GetPixel(i, j);
					var num = (pixel.R + pixel.G + pixel.B) / 3;
					bitmap.SetPixel(i, j, Color.FromArgb(num, num, num));
				}
			}
			return bitmap;
		}

		public string Post_Html_final(string url, string post_str, string CookieContainer)
		{
			var bytes = Encoding.UTF8.GetBytes(post_str);
			var result = "";
			var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
			httpWebRequest.Method = "POST";
			httpWebRequest.Accept = "*/*";
			httpWebRequest.Timeout = 5000;
			httpWebRequest.Headers.Add("Accept-Language:zh-CN,zh;q=0.9");
			httpWebRequest.ContentType = "text/plain";
			httpWebRequest.Headers.Add("Cookie:" + CookieContainer);
			try
			{
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var streamReader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
				result = streamReader.ReadToEnd();
				responseStream.Close();
				streamReader.Close();
				httpWebRequest.Abort();
			}
			catch
			{
			}
			return result;
		}

		public void CopyHtmlToClipBoard(string html)
		{
			var utf = Encoding.UTF8;
			var format = "Version:0.9\r\nStartHTML:{0:000000}\r\nEndHTML:{1:000000}\r\nStartFragment:{2:000000}\r\nEndFragment:{3:000000}\r\n";
			var text = "<html>\r\n<head>\r\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=" + utf.WebName + "\">\r\n<title>HTML clipboard</title>\r\n</head>\r\n<body>\r\n<!--StartFragment-->";
			var text2 = "<!--EndFragment-->\r\n</body>\r\n</html>\r\n";
			var s = string.Format(format, 0, 0, 0, 0);
			var byteCount = utf.GetByteCount(s);
			var byteCount2 = utf.GetByteCount(text);
			var byteCount3 = utf.GetByteCount(html);
			var byteCount4 = utf.GetByteCount(text2);
			var s2 = string.Format(format, byteCount, byteCount + byteCount2 + byteCount3 + byteCount4, byteCount + byteCount2, byteCount + byteCount2 + byteCount3) + text + html + text2;
			var dataObject = new DataObject();
			dataObject.SetData(DataFormats.Html, new MemoryStream(utf.GetBytes(s2)));
			var data = new HtmlToText().Convert(html);
			dataObject.SetData(DataFormats.Text, data);
			Clipboard.SetDataObject(dataObject);
		}

		public static string Encript(string functionName, object[] pams)
		{
			var code = File.ReadAllText("sign.js");
            ScriptControlClass scriptControlClass = new ScriptControlClass();
			((IScriptControl)scriptControlClass).Language = "javascript";
			((IScriptControl)scriptControlClass).AddCode(code);
			return ((IScriptControl)scriptControlClass).Run(functionName, ref pams).ToString();
		}

		private object ExecuteScript(string sExpression, string sCode)
		{
			ScriptControl scriptControl = new ScriptControlClass();
			scriptControl.UseSafeSubset = true;
			scriptControl.Language = "JScript";
			scriptControl.AddCode(sCode);
			try
			{
				return scriptControl.Eval(sExpression);
			}
			catch (Exception)
			{
			}
			return null;
		}

		private void OCR_Mathfuntion_Click(object sender, EventArgs e)
		{
			OCR_foreach("公式");
		}

		public void OCR_Math()
		{
			split_txt = "";
			try
			{
				var img = image_screen;
				var inArray = OCR_ImgToByte(img);
				var s = "{\t\"formats\": [\"latex_styled\", \"text\"],\t\"metadata\": {\t\t\"count\": 1,\t\t\"platform\": \"windows 10\",\t\t\"skip_recrop\": true,\t\t\"user_id\": \"123ab2a82ea246a0b011a37183c87bab\",\t\t\"version\": \"snip.windows@00.00.0083\"\t},\t\"ocr\": [\"text\", \"math\"],\t\"src\": \"data:image/jpeg;base64," + Convert.ToBase64String(inArray) + "\"}";
				var bytes = Encoding.UTF8.GetBytes(s);
				var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.mathpix.com/v3/latex");
				httpWebRequest.Method = "POST";
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Timeout = 8000;
				httpWebRequest.ReadWriteTimeout = 5000;
				httpWebRequest.Headers.Add("app_id: mathpix_chrome");
				httpWebRequest.Headers.Add("app_key: 85948264c5d443573286752fbe8df361");
				using (var requestStream = httpWebRequest.GetRequestStream())
				{
					requestStream.Write(bytes, 0, bytes.Length);
				}
				var responseStream = ((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream();
				var value = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
				responseStream.Close();
				var text = "$" + ((JObject)JsonConvert.DeserializeObject(value))["latex_styled"] + "$";
				split_txt = text;
				typeset_txt = text;
			}
			catch
			{
				if (esc != "退出")
				{
					RichBoxBody.Text = "***该区域未发现文本或者密钥次数用尽***";
				}
				else
				{
					RichBoxBody.Text = "***该区域未发现文本***";
					esc = "";
				}
			}
		}

		public string interface_flag;

		public string language;

		public string split_txt;

		public string note;

		public string spacechar;

		public string richTextBox1_note;

		public string transtalate_fla;

		public FmLoading fmloading;

		public Thread thread;

		public MenuItem Set;

		public string googleTranslate_txt;

		public int num_ok;

		public bool bolActive;

		public bool tencent_vip_f;

		public string auto_fla;

		public string baidu_vip;

		public string htmltxt;

		public static string TipText;

		public bool speaking;

		public static bool speak_copy;

		public string speak_copyb;

		public string speak_stop;

		public byte[] ttsData;

		public string[] pubnote;

		public FmNote fmNote;

		public Image image_screen;

		public int voice_count;

		public int form_width;

		public int form_height;

		public bool change_QQ_screenshot;

		private FmFlags fmflags;

		public string trans_hotkey;

		public TimeSpan ts;

		public System.Windows.Forms.Timer esc_timer;

		public Thread esc_thread;

		public string esc;

		private string languagle_flag;

		public static string GetTkkJS;

		public string typeset_txt;

		public string baidu_flags;

		public bool 截图排斥;

		private Image image_ori;

		public string shupai_Right_txt;

		private AutoResetEvent are;

		public string baiducookies;

		public string shupai_Left_txt;

		public Image[] image_arr;

		public string OCR_baidu_a;

		public string OCR_baidu_b;

		public List<Image> imgArr;

		public List<Image> imagelist;

		public int imagelist_lenght;

		public string OCR_baidu_d;

		public string OCR_baidu_c;

		public string OCR_baidu_e;

		public int[] image_num;

		public string Proxy_flag;

		public string Proxy_url;

		public string Proxy_port;

		public string Proxy_name;

		public string Proxy_password;

		public bool pinyin_flag;

		public bool set_split;

		public bool set_merge;

		public bool tranclick;

		public string myjsTextBox;

		private string flags_ocrorder;

		public int first_line;

		public bool paragraph;

        private WebBrowser webBrowser;

		public string tencent_cookie;

		private AliTable ailibaba;

		public delegate void translate();

		public delegate void ocr_thread();

		public delegate int Dllinput(string command);

		public class AutoClosedMsgBox
		{

			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

			[DllImport("user32.dll")]
			private static extern bool EndDialog(IntPtr hDlg, int nResult);

			[DllImport("user32.dll")]
			private static extern int MessageBoxTimeout(IntPtr hwnd, string txt, string caption, int wtype, int wlange, int dwtimeout);

			public static int Show(string text, string caption, int milliseconds, MsgBoxStyle style)
			{
				return MessageBoxTimeout(IntPtr.Zero, text, caption, (int)style, 0, milliseconds);
			}

			public static int Show(string text, string caption, int milliseconds, int style)
			{
				return MessageBoxTimeout(IntPtr.Zero, text, caption, style, 0, milliseconds);
			}

			private const int WM_CLOSE = 16;
		}

		public enum MsgBoxStyle
		{

			OK,

			OKCancel,

			AbortRetryIgnore,

			YesNoCancel,

			YesNo,

			RetryCancel,

			CancelRetryContinue,

			RedCritical_OK = 16,

			RedCritical_OKCancel,

			RedCritical_AbortRetryIgnore,

			RedCritical_YesNoCancel,

			RedCritical_YesNo,

			RedCritical_RetryCancel,

			RedCritical_CancelRetryContinue,

			BlueQuestion_OK = 32,

			BlueQuestion_OKCancel,

			BlueQuestion_AbortRetryIgnore,

			BlueQuestion_YesNoCancel,

			BlueQuestion_YesNo,

			BlueQuestion_RetryCancel,

			BlueQuestion_CancelRetryContinue,

			YellowAlert_OK = 48,

			YellowAlert_OKCancel,

			YellowAlert_AbortRetryIgnore,

			YellowAlert_YesNoCancel,

			YellowAlert_YesNo,

			YellowAlert_RetryCancel,

			YellowAlert_CancelRetryContinue,

			BlueInfo_OK = 64,

			BlueInfo_OKCancel,

			BlueInfo_AbortRetryIgnore,

			BlueInfo_YesNoCancel,

			BlueInfo_YesNo,

			BlueInfo_RetryCancel,

			BlueInfo_CancelRetryContinue
		}

		[Serializable]
		public class TransObj
		{

			public string From
			{
				get
				{
					return from;
				}
				set
				{
					from = value;
				}
			}

			public string To
			{
				get
				{
					return to;
				}
				set
				{
					to = value;
				}
			}

			public List<TransResult> Data
			{
				get
				{
					return data;
				}
				set
				{
					data = value;
				}
			}

			public List<TransResult> data;

			public string from;

			public string to;
		}

		[Serializable]
		public class TransResult
		{

			public string Src
			{
				get
				{
					return src;
				}
				set
				{
					src = value;
				}
			}

			public string Dst
			{
				get
				{
					return dst;
				}
				set
				{
					dst = value;
				}
			}

			public string dst;

			public string src;
		}

		private class HtmlToText
		{

			static HtmlToText()
			{
				_tags.Add("address", "\n");
				_tags.Add("blockquote", "\n");
				_tags.Add("div", "\n");
				_tags.Add("dl", "\n");
				_tags.Add("fieldset", "\n");
				_tags.Add("form", "\n");
				_tags.Add("h1", "\n");
				_tags.Add("/h1", "\n");
				_tags.Add("h2", "\n");
				_tags.Add("/h2", "\n");
				_tags.Add("h3", "\n");
				_tags.Add("/h3", "\n");
				_tags.Add("h4", "\n");
				_tags.Add("/h4", "\n");
				_tags.Add("h5", "\n");
				_tags.Add("/h5", "\n");
				_tags.Add("h6", "\n");
				_tags.Add("/h6", "\n");
				_tags.Add("p", "\n");
				_tags.Add("/p", "\n");
				_tags.Add("table", "\n");
				_tags.Add("/table", "\n");
				_tags.Add("ul", "\n");
				_tags.Add("/ul", "\n");
				_tags.Add("ol", "\n");
				_tags.Add("/ol", "\n");
				_tags.Add("/li", "\n");
				_tags.Add("br", "\n");
				_tags.Add("/td", "\t");
				_tags.Add("/tr", "\n");
				_tags.Add("/pre", "\n");
				_ignoreTags = new HashSet<string>();
				_ignoreTags.Add("script");
				_ignoreTags.Add("noscript");
				_ignoreTags.Add("style");
				_ignoreTags.Add("object");
			}

			public string Convert(string html)
			{
				_text = new TextBuilder();
				_html = html;
				_pos = 0;
				while (!EndOfText)
				{
					if (Peek() == '<')
					{
						bool flag;
						var text = ParseTag(out flag);
						if (text == "body")
						{
							_text.Clear();
						}
						else if (text == "/body")
						{
							_pos = _html.Length;
						}
						else if (text == "pre")
						{
							_text.Preformatted = true;
							EatWhitespaceToNextLine();
						}
						else if (text == "/pre")
						{
							_text.Preformatted = false;
						}
						string s;
						if (_tags.TryGetValue(text, out s))
						{
							_text.Write(s);
						}
						if (_ignoreTags.Contains(text))
						{
							EatInnerContent(text);
						}
					}
					else if (char.IsWhiteSpace(Peek()))
					{
						_text.Write(_text.Preformatted ? Peek() : ' ');
						MoveAhead();
					}
					else
					{
						_text.Write(Peek());
						MoveAhead();
					}
				}
				return HttpUtility.HtmlDecode(_text.ToString());
			}

			protected string ParseTag(out bool selfClosing)
			{
				var result = string.Empty;
				selfClosing = false;
				if (Peek() == '<')
				{
					MoveAhead();
					EatWhitespace();
					var pos = _pos;
					if (Peek() == '/')
					{
						MoveAhead();
					}
					while (!EndOfText && !char.IsWhiteSpace(Peek()) && Peek() != '/' && Peek() != '>')
					{
						MoveAhead();
					}
					result = _html.Substring(pos, _pos - pos).ToLower();
					while (!EndOfText && Peek() != '>')
					{
						if (Peek() == '"' || Peek() == '\'')
						{
							EatQuotedValue();
						}
						else
						{
							if (Peek() == '/')
							{
								selfClosing = true;
							}
							MoveAhead();
						}
					}
					MoveAhead();
				}
				return result;
			}

			protected void EatInnerContent(string tag)
			{
				var b = "/" + tag;
				while (!EndOfText)
				{
					if (Peek() == '<')
					{
						bool flag;
						if (ParseTag(out flag) == b)
						{
							return;
						}
						if (!flag && !tag.StartsWith("/"))
						{
							EatInnerContent(tag);
						}
					}
					else
					{
						MoveAhead();
					}
				}
			}
            
			protected bool EndOfText
			{
				get
				{
					return _pos >= _html.Length;
				}
			}

			protected char Peek()
			{
				if (_pos >= _html.Length)
				{
					return '\0';
				}
				return _html[_pos];
			}

			protected void MoveAhead()
			{
				_pos = Math.Min(_pos + 1, _html.Length);
			}

			protected void EatWhitespace()
			{
				while (char.IsWhiteSpace(Peek()))
				{
					MoveAhead();
				}
			}

			protected void EatWhitespaceToNextLine()
			{
				while (char.IsWhiteSpace(Peek()))
				{
					var num = (int)Peek();
					MoveAhead();
					if (num == 10)
					{
						break;
					}
				}
			}

			protected void EatQuotedValue()
			{
				var c = Peek();
				if (c == '"' || c == '\'')
				{
					MoveAhead();
					var pos = _pos;
					_pos = _html.IndexOfAny(new[]
					{
						c,
						'\r',
						'\n'
					}, _pos);
					if (_pos < 0)
					{
						_pos = _html.Length;
						return;
					}
					MoveAhead();
				}
			}

			protected static Dictionary<string, string> _tags = new Dictionary<string, string>();

			protected static HashSet<string> _ignoreTags;

			protected TextBuilder _text;

			protected string _html;

			protected int _pos;

			protected class TextBuilder
			{

				public TextBuilder()
				{
					_text = new StringBuilder();
					_currLine = new StringBuilder();
					_emptyLines = 0;
					_preformatted = false;
				}
                
				public bool Preformatted
				{
					get
					{
						return _preformatted;
					}
					set
					{
						if (value)
						{
							if (_currLine.Length > 0)
							{
								FlushCurrLine();
							}
							_emptyLines = 0;
						}
						_preformatted = value;
					}
				}

				public void Clear()
				{
					_text.Length = 0;
					_currLine.Length = 0;
					_emptyLines = 0;
				}

				public void Write(string s)
				{
					foreach (var c in s)
					{
						Write(c);
					}
				}

				public void Write(char c)
				{
					if (_preformatted)
					{
						_text.Append(c);
						return;
					}
					if (c != '\r')
					{
						if (c == '\n')
						{
							FlushCurrLine();
							return;
						}
						if (char.IsWhiteSpace(c))
						{
							var length = _currLine.Length;
							if (length == 0 || !char.IsWhiteSpace(_currLine[length - 1]))
							{
								_currLine.Append(' ');
								return;
							}
						}
						else
						{
							_currLine.Append(c);
						}
					}
				}

				protected void FlushCurrLine()
				{
					var text = _currLine.ToString().Trim();
					if (text.Replace("\u00a0", string.Empty).Length == 0)
					{
						_emptyLines++;
						if (_emptyLines < 2 && _text.Length > 0)
						{
							_text.AppendLine(text);
						}
					}
					else
					{
						_emptyLines = 0;
						_text.AppendLine(text);
					}
					_currLine.Length = 0;
				}

				public override string ToString()
				{
					if (_currLine.Length > 0)
					{
						FlushCurrLine();
					}
					return _text.ToString();
				}

				private StringBuilder _text;

				private StringBuilder _currLine;

				private int _emptyLines;

				private bool _preformatted;
			}
		}
	}
}
