﻿// <file>
//     <copyright see="prj:///doc/copyright.txt">2002-2005 AlphaSierraPapa</copyright>
//     <license see="prj:///doc/license.txt">GNU General Public License</license>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Text;
using DebuggerInterop.Core;

namespace DebuggerLibrary
{
	public partial class NDebugger
	{
		List<Process> processCollection = new List<Process>();

		Process currentProcess;

		public event EventHandler<ProcessEventArgs> ProcessStarted;
		public event EventHandler<ProcessEventArgs> ProcessExited;
		
		public IList<Process> Processes {
			get {
				return processCollection.AsReadOnly();
			}
		}

		public Process CurrentProcess {
			get {
				if (currentProcess == null && Processes.Count > 0) {
					currentProcess = Processes[0];
				}
				return currentProcess;
			}
			set {
				currentProcess = value;
			}
		}
		
		public bool IsCurrentProcessSafeForInspection {
			get {
				if (CurrentProcess == null) {
					return false;
				} else {
					return CurrentProcess.IsProcessSafeForInspection;
				}
			}
		}

		internal void CheckThatCurrentProcessIsSafeForInspection()
		{
			if (CurrentProcess == null) {
				throw new DebuggerException("There is no process being debugged.");
			} else {
				CurrentProcess.CheckThatProcessIsSafeForInspection();
			}
		}

		internal Process GetProcess(ICorDebugProcess corProcess)
		{
			foreach (Process process in Processes) {
				if (process.CorProcess == corProcess) {
					return process;
				}
			}
			throw new DebuggerException("Process is not in collection");
		}

		internal void AddProcess(Process process)
		{
			processCollection.Add(process);
			OnProcessStarted(process);
		}

		internal void RemoveProcess(Process process)
		{
			processCollection.Remove(process);
			OnProcessExited(process);
		}

		protected virtual void OnProcessStarted(Process process)
		{
			if (ProcessStarted != null) {
				ProcessStarted(this, new ProcessEventArgs(this, process));
			}
		}

		protected virtual void OnProcessExited(Process process)
		{
			if (ProcessExited != null) {
				ProcessExited(this, new ProcessEventArgs(this, process));
			}
		}
	}
}
