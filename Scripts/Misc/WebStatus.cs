// **********
// RpiUO - WebStatus.cs
// Last Edit: 2015/12/23
// Look for Rpi comment
// **********

#region References
using System;
using System.IO;
//using System.Linq;
using System.Net;
using System.Text;

using Server.Guilds;
using Server.Network;
#endregion

namespace Server.Misc
{
	public class StatusPage : Timer
	{
		public static readonly bool Enabled = false;

		private static HttpListener _Listener;

		private static string _StatusPage = String.Empty;
		private static byte[] _StatusBuffer = new byte[0];

		private static readonly object _StatusLock = new object();

		public static void Initialize()
		{
			if (!Enabled)
			{
				return;
			}

			new StatusPage().Start();

			Listen();
		}

		private static void Listen()
		{
			if (!HttpListener.IsSupported)
			{
				return;
			}

			if (_Listener == null)
			{
				_Listener = new HttpListener();
				_Listener.Prefixes.Add("http://*:80/status/");
				_Listener.Start();
			}
			else if (!_Listener.IsListening)
			{
				_Listener.Start();
			}

			if (_Listener.IsListening)
			{
				_Listener.BeginGetContext(ListenerCallback, null);
			}
		}

		private static void ListenerCallback(IAsyncResult result)
		{
			try
			{
				var context = _Listener.EndGetContext(result);

				byte[] buffer;

				lock (_StatusLock)
				{
					buffer = _StatusBuffer;
				}

				context.Response.ContentLength64 = buffer.Length;
				context.Response.OutputStream.Write(buffer, 0, buffer.Length);
				context.Response.OutputStream.Close();
			}
			catch
			{ }

			Listen();
		}

		private static string Encode(string input)
		{
			StringBuilder stringBuilder = new StringBuilder(input);

			stringBuilder.Replace("&", "&amp;");
			stringBuilder.Replace("<", "&lt;");
			stringBuilder.Replace(">", "&gt;");
			stringBuilder.Replace("\"", "&quot;");
			stringBuilder.Replace("'", "&apos;");

			return stringBuilder.ToString();
		}

		public StatusPage()
			: base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(60.0))
		{
			Priority = TimerPriority.FiveSeconds;
		}

		protected override void OnTick()
		{
			if (!Directory.Exists("web"))
			{
				Directory.CreateDirectory("web");
			}

			using (StreamWriter streamWriter = new StreamWriter("web/status.html"))
			{
				streamWriter.WriteLine("<!DOCTYPE html>");
				streamWriter.WriteLine("<html>");
				streamWriter.WriteLine("   <head>");
				streamWriter.WriteLine("      <title>" + ServerList.ServerName + " Server Status</title>");
				streamWriter.WriteLine("   </head>");
				streamWriter.WriteLine("   <style type=\"text/css\">");
				streamWriter.WriteLine("   body { background: #999; }");
				streamWriter.WriteLine("   table { width: 100%; }");
				streamWriter.WriteLine("   tr.ruo-header td { background: #000; color: #FFF; }");
				streamWriter.WriteLine("   tr.odd td { background: #222; color: #DDD; }");
				streamWriter.WriteLine("   tr.even td { background: #DDD; color: #222; }");
				streamWriter.WriteLine("   </style>");
				streamWriter.WriteLine("   <body>");
				streamWriter.WriteLine("      <h1>RunUO Server Status</h1>");
				streamWriter.WriteLine("      <h3>Online clients</h3>");
				streamWriter.WriteLine("      <table cellpadding=\"0\" cellspacing=\"0\">");
				streamWriter.WriteLine("         <tr class=\"ruo-header\"><td>Name</td><td>Location</td><td>Kills</td><td>Karma/Fame</td></tr>");

				int index = 0;

                //Rpi - Removed linq code for better performance
                //foreach (Mobile mobile in NetState.Instances.Where(state => state.Mobile != null).Select(state => state.Mobile))

                //Rpi - Replaces the linq code above
                Mobile mobile;
                for(int counter=0; counter < NetState.Instances.Count; counter++)
				{
                    mobile = NetState.Instances[counter].Mobile;

                    if(mobile != null)
                    {
                        ++index;

                        var g = mobile.Guild as Guild;

                        streamWriter.Write("         <tr class=\"ruo-result " + (index % 2 == 0 ? "even" : "odd") + "\"><td>");

                        if (g != null)
                        {
                            streamWriter.Write(Encode(mobile.Name));
                            streamWriter.Write(" [");

                            var title = mobile.GuildTitle;

                            title = title != null ? title.Trim() : String.Empty;

                            if (title.Length > 0)
                            {
                                streamWriter.Write(Encode(title));
                                streamWriter.Write(", ");
                            }

                            streamWriter.Write(Encode(g.Abbreviation));

                            streamWriter.Write(']');
                        }
                        else
                        {
                            streamWriter.Write(Encode(mobile.Name));
                        }

                        streamWriter.Write("</td><td>");
                        streamWriter.Write(mobile.X);
                        streamWriter.Write(", ");
                        streamWriter.Write(mobile.Y);
                        streamWriter.Write(", ");
                        streamWriter.Write(mobile.Z);
                        streamWriter.Write(" (");
                        streamWriter.Write(mobile.Map);
                        streamWriter.Write(")</td><td>");
                        streamWriter.Write(mobile.Kills);
                        streamWriter.Write("</td><td>");
                        streamWriter.Write(mobile.Karma);
                        streamWriter.Write(" / ");
                        streamWriter.Write(mobile.Fame);
                        streamWriter.WriteLine("</td></tr>");
                    }
				}

				streamWriter.WriteLine("         <tr>");
				streamWriter.WriteLine("      </table>");
				streamWriter.WriteLine("   </body>");
				streamWriter.WriteLine("</html>");
			}

			lock (_StatusLock)
			{
				_StatusPage = File.ReadAllText("web/status.html");
				_StatusBuffer = Encoding.UTF8.GetBytes(_StatusPage);
			}
		}
	}
}