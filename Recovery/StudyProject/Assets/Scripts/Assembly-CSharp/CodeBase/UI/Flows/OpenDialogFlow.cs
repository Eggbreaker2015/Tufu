using Codebase.Core.Flows;

namespace Codebase.UI.Flows
{
	public struct OpenDialogFlow : IFlow<bool>, IFlow
	{
		public string TitleKey;

		public string MessageKey;

		public string ConfirmKey;

		public string CancelKey;

		public bool HasOptions;

		public bool RawText;

		public static OpenDialogFlow WithMessage(string titleKey, string messageKey)
		{
			// Native ELF VA 0x1658320.
			return new OpenDialogFlow {TitleKey=titleKey,MessageKey=messageKey,ConfirmKey="BTN_OK",HasOptions=false};
		}

		public static OpenDialogFlow WithRawMessage(string title, string message)
		{
			// Native ELF VA 0x16583cc.
			return new OpenDialogFlow {TitleKey=title,MessageKey=message,ConfirmKey="BTN_OK",HasOptions=false,RawText=true};
		}

		public static OpenDialogFlow WithOptions(string titleKey, string messageKey, string confirmKey, string cancelKey)
		{
			// Native ELF VA 0x165847c.
			return new OpenDialogFlow {TitleKey=titleKey,MessageKey=messageKey,ConfirmKey=confirmKey,CancelKey=cancelKey,HasOptions=true};
		}
	}
}
