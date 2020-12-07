using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.InterfacesLibrary.ProjectModel.Collections;
using ZennoLab.InterfacesLibrary.ProjectModel.Enums;
using ZennoLab.Macros;
using Global.ZennoExtensions;
using ZennoLab.Emulation;
using HtmlAgilityPack;

namespace ZennoLab.OwnCode
{
	/// <summary>
	/// A simple class of the common code
	/// </summary>
	public class CommonCode
	{
		/// <summary>
		/// Lock this object to mark part of code for single thread execution
		/// </summary>
		public static object SyncObject = new object();

		// Insert your code here
	}
}


namespace yelpPars
{
	public static class Extension
	{
		public static string HtmlDecode (this string LineIn)
		{
			return System.Web.HttpUtility.HtmlDecode(LineIn);	
		}
		
		public static string FormLine (string numberCompany, string nameCompany, string linkCompany)
		{
			return String.Format("{0};{1};{2}", numberCompany, nameCompany, linkCompany);	
		}
	}
	
	
}