﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AScore_DLL
{
	public class MessageEventBase
	{
		#region "Event Delegates and Classes"

		public event MessageEventHandler ErrorEvent;
		public event MessageEventHandler WarningEvent;
		public event MessageEventHandler MessageEvent;

		public delegate void MessageEventHandler(object sender, MessageEventArgs e);
		#endregion

		protected void ReportError(string message)
		{
			OnErrorMessage(new MessageEventArgs(message));
		}

		protected void ReportMessage(string message)
		{
			OnMessage(new MessageEventArgs(message));
		}
		protected void ReportWarning(string message)
		{
			OnWarningMessage(new MessageEventArgs(message));
		}

		#region "Event Functions"

		protected void OnErrorMessage(MessageEventArgs e)
		{
			if (ErrorEvent != null)
				ErrorEvent(this, e);
		}

		protected void OnMessage(MessageEventArgs e)
		{
			if (MessageEvent != null)
				MessageEvent(this, e);
		}

		protected void OnWarningMessage(MessageEventArgs e)
		{
			if (WarningEvent != null)
				WarningEvent(this, e);
		}
		#endregion
	}

	public class MessageEventArgs : System.EventArgs
	{
		public readonly string Message;

		public MessageEventArgs(string strMessage)
		{
			Message = strMessage;
		}
	}

}