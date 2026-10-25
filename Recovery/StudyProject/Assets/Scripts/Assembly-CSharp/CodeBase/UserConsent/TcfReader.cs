using UnityEngine;

namespace Codebase.UserConsent
{
	public static class TcfReader
	{
		public const string KEY_TC_STRING = "IABTCF_TCString";

		public const string KEY_GDPR_APPLIES = "IABTCF_gdprApplies";

		public const string KEY_CMP_SDK_ID = "IABTCF_CmpSdkID";

		public const string KEY_PURPOSE_CONSENTS = "IABTCF_PurposeConsents";

		public const string KEY_VENDOR_CONSENTS = "IABTCF_VendorConsents";

		public const string KEY_GPP_STRING = "IABGPP_HDR_GppString";

		public const string KEY_US_PRIVACY = "IABUSPrivacy_String";

		public static string TcString => null;

		public static int GdprApplies => 0;

		public static int CmpSdkId => 0;

		public static string PurposeConsents => null;

		public static string VendorConsents => null;

		public static string GppString => null;

		public static string UsPrivacy => null;

		public static bool HasPurposeConsent(int purposeId)
		{
			return false;
		}

		public static bool HasVendorConsent(int vendorId)
		{
			return false;
		}

		public static string DumpState()
		{
			return null;
		}

		private static string GetString(string key, string defaultValue = "")
		{
			return null;
		}

		private static int GetInt(string key, int defaultValue = 0)
		{
			return 0;
		}

		private static AndroidJavaObject GetAndroidDefaultPrefs()
		{
			return null;
		}
	}
}
