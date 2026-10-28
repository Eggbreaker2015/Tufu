using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Codebase.Simulation.Harness
{
	// Sandbox state of a batch simulation run (design 2.3). The editor driver writes
	// Temp/TufuSimulation/active_request.json (with its process id) before entering Play; at
	// SubsystemRegistration, before any scene or service exists, this class reads it and turns the
	// sandbox on. Game-code hooks read Active and behave exactly as before while it is false:
	//   TimeService (clock, TimeSkipSeconds), ABTestModel (variant draw, new-user check),
	//   UserModel (direct DateTimeOffset.UtcNow reads), MainSceneInitializer (analytics/integrations).
	// Activation is compiled only into the editor; player builds never turn it on.
	public static class SimulationSandbox
	{
		public const string ActiveRequestFile = "active_request.json";

		private static bool active;
		private static DateTime clockUtc;

		public static bool Active => active;

		// Fixed sandbox time (request sandbox.clock_epoch_utc); only meaningful while Active.
		public static DateTime ClockUtc => clockUtc;

		public static DateTimeOffset ClockUtcOffset => new DateTimeOffset(clockUtc, TimeSpan.Zero);

		public static SimulationRunRequest Request { get; private set; }

		public static int JobIndex { get; private set; } = -1;

		public static string Entry { get; private set; }

		// Set when an active request belongs to this process but could not be used (harness exits 5).
		public static string ActivationError { get; private set; }

		public static string PetsState { get; private set; } = "unknown";

		public static SimulationRunRequest.Job Job =>
			Request != null && JobIndex >= 0 && JobIndex < Request.Jobs.Count ? Request.Jobs[JobIndex] : null;

		public static string ProjectDir => Path.GetDirectoryName(Application.dataPath);

		public static string TempDir => Path.Combine(ProjectDir, "Temp", "TufuSimulation");

		[JsonObject(MemberSerialization.OptIn)]
		public sealed class ActiveRequest
		{
			[JsonProperty("pid", Required = Required.Always)] public int Pid;
			[JsonProperty("request_path", Required = Required.Always)] public string RequestPath;
			[JsonProperty("job_index", Required = Required.Always)] public int JobIndex;
			[JsonProperty("entry")] public string Entry = "run";
		}

		// Variant the sandbox draws for a test with these variants: the requested one when offered,
		// else the first (same as the original for one-variant and empty lists).
		public static string SelectAbVariant(string variants)
		{
			if (string.IsNullOrEmpty(variants)) return "A";
			string wanted = Request != null ? Request.Sandbox.AbVariant : "A";
			return variants.Contains(wanted) && wanted.Length == 1 ? wanted : variants[0].ToString();
		}

		public static void Deactivate()
		{
			active = false;
			Request = null;
			JobIndex = -1;
			Entry = null;
			ActivationError = null;
		}

#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Activate()
		{
			Deactivate();
			PetsState = "unknown";
			string path = Path.Combine(TempDir, ActiveRequestFile);
			if (!File.Exists(path)) return;
			ActiveRequest marker;
			try
			{
				marker = SimulationJson.ReadFile<ActiveRequest>(path);
			}
			catch (Exception e)
			{
				Debug.LogWarning("[TufuSim] ignoring unreadable " + path + ": " + e.Message);
				return;
			}
			// A file left behind by a crashed run belongs to another process: normal Play ignores it.
			if (marker.Pid != System.Diagnostics.Process.GetCurrentProcess().Id) return;
			Entry = marker.Entry;
			try
			{
				SimulationRunRequest request = SimulationRunRequest.Load(marker.RequestPath);
				string error = request.Validate();
				if (error == null && (marker.JobIndex < 0 || marker.JobIndex >= request.Jobs.Count)) error = "job_index out of range";
				if (error == null && !SimulationRunRequest.TryParseClock(request.Sandbox.ClockEpochUtc, out clockUtc)) error = "bad clock";
				if (error != null)
				{
					ActivationError = error;
					return;
				}
				Request = request;
				JobIndex = marker.JobIndex;
				active = true;
				PetsState = DisablePets();
				Debug.Log("[TufuSim] sandbox on: job " + Job.JobId + ", clock " + request.Sandbox.ClockEpochUtc + ", AB " + request.Sandbox.AbVariant +
				          ", pets " + PetsState + ", persistentDataPath " + Application.persistentDataPath);
			}
			catch (Exception e)
			{
				ActivationError = e.GetType().Name + ": " + e.Message;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void CreateHarness()
		{
			if (active || ActivationError != null) SimulationHarness.Create();
		}
#endif

		// This branch has no pet code. After the pet merge: set PetConfig.Enabled = false in memory by
		// reflection (no compile-time dependency). Returns absent, disabled, or failed:<reason>.
		private static string DisablePets()
		{
			Type config = typeof(SimulationSandbox).Assembly.GetTypes().FirstOrDefault(t => t.Name == "PetConfig");
			if (config == null) return "absent";
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			MemberInfo enabled = (MemberInfo)config.GetField("Enabled", flags) ?? config.GetProperty("Enabled", flags);
			if (enabled == null) return "failed:no PetConfig.Enabled";
			UnityEngine.Object[] instances = Resources.FindObjectsOfTypeAll(config);
			foreach (UnityEngine.Object instance in instances)
			{
				if (enabled is FieldInfo field) field.SetValue(instance, false);
				else ((PropertyInfo)enabled).SetValue(instance, false);
			}
			return "disabled";
		}
	}
}
